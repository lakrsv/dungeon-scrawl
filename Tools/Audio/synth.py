"""A small chiptune synthesizer in plain Python: oscillators, envelopes, filters, echo, reverb, and WAV output.

A sound is a list of floats in -1..1. Noise comes from seeded generators, so every build gives the same files.
"""

import array
import math
import random
import wave

TAU = 2.0 * math.pi

_NOTES = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8,
          "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def midi(note):
    """'A4' -> 69. Sharps and flats work: 'C#5', 'Bb3'."""
    return 12 * (int(note[-1]) + 1) + _NOTES[note[:-1]]


def pitch_class(name):
    return _NOTES[name]


def hz(midi_note):
    return 440.0 * 2.0 ** ((midi_note - 69) / 12.0)


def freq(note):
    return hz(midi(note))


def db(value):
    return 20.0 * math.log10(value) if value > 0 else -999.0


# Buffers ------------------------------------------------------------------------------------------------------


def blank(seconds, rate):
    return [0.0] * int(round(seconds * rate))


def add(dest, src, start=0, gain=1.0):
    """Mixes src into dest from sample `start` on, growing dest when src runs past its end."""
    end = start + len(src)
    if end > len(dest):
        dest.extend([0.0] * (end - len(dest)))
    dest[start:end] = [a + b * gain for a, b in zip(dest[start:end], src)]
    return dest


def mul(a, b):
    return [x * y for x, y in zip(a, b)]


def scale(x, gain):
    return [v * gain for v in x]


def normalize(x, peak_db):
    peak = max(abs(v) for v in x) or 1.0
    return scale(x, 10.0 ** (peak_db / 20.0) / peak)


def loudness(x, rms_db, max_peak_db):
    """Scales x to an average (RMS) level, unless that would push its peaks past max_peak_db."""
    rms = math.sqrt(sum(v * v for v in x) / len(x)) or 1.0
    peak = max(abs(v) for v in x) or 1.0
    return scale(x, min(10.0 ** (rms_db / 20.0) / rms, 10.0 ** (max_peak_db / 20.0) / peak))


def fade_out(x, seconds, rate):
    """Ramps the last `seconds` down to silence, so a sound never ends on a click."""
    n = min(len(x), int(seconds * rate))
    for i in range(n):
        x[len(x) - n + i] *= 1.0 - (i + 1) / n
    return x


def fold(x, length):
    """Wraps everything past `length` back onto the start: echo and reverb tails then carry across the loop point,
    as they would if the music really went round again."""
    out = x[:length] + [0.0] * max(0, length - len(x))
    for i in range(length, len(x)):
        out[i % length] += x[i]
    return out


def crossfade_loop(x, length, fade):
    """For noise beds: blends the `fade` samples rendered past `length` into the start (equal power), so the end
    runs straight on into the beginning."""
    out = x[:length]
    for i in range(fade):
        t = (i + 0.5) / fade
        out[i] = x[i] * math.sin(t * math.pi / 2) + x[length + i] * math.cos(t * math.pi / 2)
    return out


# Oscillators --------------------------------------------------------------------------------------------------


def _blep(t, dt):
    """PolyBLEP: rounds off a hard edge over one sample so square waves don't alias into a hiss."""
    if t < dt:
        t /= dt
        return t + t - t * t - 1.0
    if t > 1.0 - dt:
        t = (t - 1.0) / dt
        return t * t + t + t + 1.0
    return 0.0


def osc(shape, f, seconds, rate, duty=0.5, phase=0.0):
    """shape is 'pulse', 'triangle', or 'sine'. f is a frequency in Hz, or a function of time that returns one."""
    n = int(round(seconds * rate))
    out = [0.0] * n
    sweep = callable(f)
    dt = 0.0 if sweep else f / rate
    dc = 2.0 * duty - 1.0
    for i in range(n):
        if sweep:
            dt = f(i / rate) / rate
        if shape == "pulse":
            v = 1.0 if phase < duty else -1.0
            v += _blep(phase, dt)
            v -= _blep((phase - duty) % 1.0, dt)
            out[i] = v - dc
        elif shape == "triangle":
            out[i] = 4.0 * abs(phase - 0.5) - 1.0
        else:
            out[i] = math.sin(TAU * phase)
        phase += dt
        if phase >= 1.0:
            phase -= 1.0
    return out


def noise(seconds, rate, seed):
    rng = random.Random(seed)
    return [rng.uniform(-1.0, 1.0) for _ in range(int(round(seconds * rate)))]


# Envelopes ----------------------------------------------------------------------------------------------------


def env_exp(seconds, rate, attack, tau):
    """A linear attack, then an exponential decay with time constant tau."""
    n = int(round(seconds * rate))
    a = max(1, int(attack * rate))
    out = [0.0] * n
    for i in range(n):
        out[i] = i / a if i < a else math.exp(-(i - a) / (tau * rate))
    return fade_out(out, min(0.004, seconds / 4), rate)


def env_adsr(hold, rate, attack, decay, sustain, release):
    """Attack, decay to the sustain level, hold for `hold` seconds after the note starts, then release.
    The result is (hold + release) long."""
    n_hold = int(round(hold * rate))
    n = n_hold + int(round(release * rate))
    a = max(1, int(attack * rate))
    d = max(1, int(decay * rate))
    out = [0.0] * n
    level = 0.0
    for i in range(n_hold):
        if i < a:
            level = i / a
        elif i < a + d:
            level = 1.0 - (1.0 - sustain) * (i - a) / d
        else:
            level = sustain
        out[i] = level
    r = n - n_hold
    for i in range(r):
        out[n_hold + i] = level * (1.0 - (i + 1) / r) ** 2
    return out


# Filters ------------------------------------------------------------------------------------------------------


def lowpass(x, cutoff, rate):
    """One-pole lowpass. cutoff is in Hz, or a function of time that returns it."""
    out = [0.0] * len(x)
    sweep = callable(cutoff)
    k = 1.0 - math.exp(-TAU * (cutoff if not sweep else 1000.0) / rate)
    y = 0.0
    for i, v in enumerate(x):
        if sweep:
            k = 1.0 - math.exp(-TAU * cutoff(i / rate) / rate)
        y += k * (v - y)
        out[i] = y
    return out


def highpass(x, cutoff, rate):
    low = lowpass(x, cutoff, rate)
    return [a - b for a, b in zip(x, low)]


def bandpass(x, low, high, rate):
    return lowpass(highpass(x, low, rate), high, rate)


# Space --------------------------------------------------------------------------------------------------------


def echo(x, rate, delay, feedback, wet, tail, cutoff=3000.0, dry=1.0):
    """A feedback delay whose repeats grow darker, as they would bouncing down a stone corridor."""
    d = int(round(delay * rate))
    n = len(x) + int(round(tail * rate))
    src = x + [0.0] * (n - len(x))
    line = [0.0] * n
    k = 1.0 - math.exp(-TAU * cutoff / rate)
    y = 0.0
    for i in range(d, n):
        y += k * (src[i - d] + line[i - d] * feedback - y)
        line[i] = y
    return [a * dry + b * wet for a, b in zip(src, line)]


def _comb(x, length, feedback, damp):
    buf = [0.0] * length
    out = [0.0] * len(x)
    idx = 0
    store = 0.0
    keep = 1.0 - damp
    for i, v in enumerate(x):
        y = buf[idx]
        store = y * keep + store * damp
        buf[idx] = v + store * feedback
        out[i] = y
        idx += 1
        if idx == length:
            idx = 0
    return out


def _allpass(x, length, feedback=0.5):
    buf = [0.0] * length
    out = [0.0] * len(x)
    idx = 0
    for i, v in enumerate(x):
        b = buf[idx]
        out[i] = b - v
        buf[idx] = v + b * feedback
        idx += 1
        if idx == length:
            idx = 0
    return out


def reverb(x, rate, room=0.84, damp=0.25, tail=2.5):
    """A small Freeverb: four damped combs into two allpasses. Returns only the reverberated (wet) signal."""
    x = x + [0.0] * int(round(tail * rate))
    s = rate / 44100.0
    wet = [0.0] * len(x)
    for length in (1116, 1188, 1277, 1356):
        add(wet, _comb(x, int(length * s), room, damp), gain=0.25)
    for length in (556, 441):
        wet = _allpass(wet, int(length * s))
    return wet


# Output -------------------------------------------------------------------------------------------------------


def write_wav(path, x, rate):
    data = array.array("h", (int(round(max(-1.0, min(1.0, v)) * 32767)) for v in x))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(data.tobytes())


def levels(x):
    peak = max(abs(v) for v in x) if x else 0.0
    rms = math.sqrt(sum(v * v for v in x) / max(1, len(x)))
    return db(peak), db(rms)
