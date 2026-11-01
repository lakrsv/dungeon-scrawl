"""The game's new sound effects: typing, footsteps, deaths, the menu clicks, and the Scrawl.

Each one is short and chiptune, like the original Ludum Dare sounds, and sits a little under them in level:
these play far more often.
"""

import math

from synth import add, bandpass, echo, env_exp, freq, highpass, lowpass, mul, noise, normalize, osc, reverb, scale

RATE = 44100


def _pluck(f, seconds, duty, tau, attack=0.002):
    return mul(osc("pulse", f, seconds, RATE, duty=duty), env_exp(seconds, RATE, attack, tau))


def key():
    """A typed letter: a short blip with a tick on its front. The game raises its pitch letter by letter, up the
    dungeon theme's scale, so it is tuned to A4, the theme's fifth."""
    seconds = 0.07
    base = freq("A4")
    tone = _pluck(lambda t: base * (1.0 + 0.05 * math.exp(-t / 0.004)), seconds, 0.25, 0.022, attack=0.0015)
    tick = mul(highpass(noise(seconds, RATE, seed=1), 3000, RATE), env_exp(seconds, RATE, 0.0003, 0.0015))
    return normalize(lowpass(add(tone, tick, gain=0.35), 5500, RATE), -9)


def word():
    """A finished word: a quick rising fourth that lands on the dungeon theme's home note."""
    out = []
    add(out, _pluck(freq("A4"), 0.16, 0.25, 0.04), 0, gain=0.7)
    add(out, _pluck(freq("D5"), 0.22, 0.25, 0.06), int(0.05 * RATE))
    out = echo(out, RATE, delay=0.09, feedback=0.25, wet=0.3, tail=0.3)
    return normalize(lowpass(out, 6000, RATE), -8)


def typo():
    """A letter that fits no word: a dull, sagging buzz of two clashing squares over a thud."""
    seconds = 0.18
    sag = lambda f: (lambda t: f * (1.0 - 0.12 * t / seconds))
    buzz = add(osc("pulse", sag(110.0), seconds, RATE), osc("pulse", sag(116.5), seconds, RATE))
    buzz = mul(buzz, env_exp(seconds, RATE, 0.002, 0.06))
    thud = mul(lowpass(noise(seconds, RATE, seed=2), 500, RATE), env_exp(seconds, RATE, 0.001, 0.025))
    return normalize(add(lowpass(buzz, 1600, RATE), thud, gain=1.6), -7)


def step(seed, cutoff, thump):
    """A footstep on stone: a low knock, the grit of the sole, and a faint scuff."""
    seconds = 0.12
    knock = mul(osc("sine", lambda t: thump * (1.0 + 0.8 * math.exp(-t / 0.008)), seconds, RATE),
                env_exp(seconds, RATE, 0.001, 0.025))
    grit = mul(lowpass(noise(seconds, RATE, seed), cutoff, RATE), env_exp(seconds, RATE, 0.0005, 0.018))
    scuff = mul(highpass(noise(seconds, RATE, seed + 10), 3500, RATE), env_exp(seconds, RATE, 0.0003, 0.006))
    out = add(scale(knock, 0.9), grit, gain=0.8)
    return normalize(add(out, scuff, gain=0.25), -12)


def steps():
    return [step(4, 1100, 80), step(5, 1400, 90), step(6, 1700, 72)]


def death():
    """A creature going up in smoke: a puff of noise closing down, under a falling chirp."""
    seconds = 0.55
    puff = lowpass(noise(seconds, RATE, seed=3), lambda t: 250 + 4500 * math.exp(-t / 0.09), RATE)
    puff = mul(puff, env_exp(seconds, RATE, 0.004, 0.14))
    chirp = _pluck(lambda t: 110 + 420 * math.exp(-t / 0.08), seconds, 0.5, 0.09)
    return normalize(lowpass(add(puff, chirp, gain=0.45), 7000, RATE), -5)


def hover():
    """The pointer coming onto a menu button: the faintest tick."""
    return normalize(_pluck(freq("E6"), 0.05, 0.125, 0.010, attack=0.001), -15)


def select():
    """A menu choice, or the key that leaves the splash screen: a bright rising fifth with an echo."""
    out = []
    add(out, _pluck(freq("A5"), 0.2, 0.25, 0.07))
    add(out, _pluck(freq("E6"), 0.2, 0.25, 0.07), int(0.06 * RATE))
    out = echo(out, RATE, delay=0.11, feedback=0.3, wet=0.35, tail=0.35)
    return normalize(lowpass(out, 7000, RATE), -7)



# The Scrawl --------------------------------------------------------------------------------------------------------


def _swell(seconds, attack, release):
    """Rises over `attack` seconds, holds, and falls away over the last `release`."""
    def at(t):
        if t < attack:
            return t / attack
        if t > seconds - release:
            return max(0.0, (seconds - t) / release)
        return 1.0
    return [at(i / RATE) for i in range(int(seconds * RATE))]


def _wave(seconds, f):
    """f(t) for every sample: for tremolos and other slow shapes."""
    return [f(i / RATE) for i in range(int(seconds * RATE))]


def boss_rumble():
    """The Scrawl rising out of the void: the rock groaning, a deep falling drone, and ink seething high above."""
    seconds = 2.6
    groan = lowpass(lowpass(noise(seconds, RATE, seed=40), 140, RATE), 140, RATE)
    drone = add(osc("sine", lambda t: 62 - 18 * t / seconds, seconds, RATE),
                osc("pulse", lambda t: 31 - 9 * t / seconds, seconds, RATE, duty=0.3), gain=0.25)
    seethe = mul(bandpass(noise(seconds, RATE, seed=41), 2500, 6000, RATE),
                 _wave(seconds, lambda t: 0.5 + 0.5 * math.sin(2 * math.pi * 7 * t)))
    out = add(scale(groan, 3.0), lowpass(drone, 400, RATE), gain=0.9)
    out = add(out, seethe, gain=0.05)
    return normalize(mul(out, _swell(seconds, 0.8, 1.2)), -4)


def boss_charge():
    """A hand or the head charging: a whine climbing and trembling faster, over crackling sparks."""
    seconds = 1.2

    def climb(f0, f1):
        return lambda t: f0 * (f1 / f0) ** (t / seconds)

    whine = add(osc("pulse", climb(180, 720), seconds, RATE, duty=0.25),
                osc("pulse", climb(184, 736), seconds, RATE, duty=0.125), gain=0.7)
    tremble = _wave(seconds, lambda t: 0.6 + 0.4 * math.sin(2 * math.pi * (6 + 18 * t / seconds) * t))
    sparks = mul(highpass(noise(seconds, RATE, seed=42), 4000, RATE),
                 _wave(seconds, lambda t: 1.0 if int(t * 63) % 5 == 0 else 0.0))
    out = add(lowpass(mul(whine, tremble), 3500, RATE), sparks, gain=0.15)
    return normalize(mul(out, _swell(seconds, 0.9, 0.08)), -5)


def boss_slam():
    """A claw coming down on the bridge: a booming thud, splintering planks, and the boards rattling after."""
    seconds = 0.9
    boom = mul(osc("sine", lambda t: 34 + 110 * math.exp(-t / 0.04), seconds, RATE),
               env_exp(seconds, RATE, 0.001, 0.22))
    crack = mul(bandpass(noise(seconds, RATE, seed=43), 700, 3500, RATE), env_exp(seconds, RATE, 0.0005, 0.03))
    rattle = mul(bandpass(noise(seconds, RATE, seed=44), 300, 1200, RATE),
                 _wave(seconds, lambda t: math.exp(-t / 0.18) * (0.5 + 0.5 * math.sin(2 * math.pi * 23 * t))))
    out = add(scale(boom, 1.4), crack, gain=0.9)
    return normalize(add(out, rattle, gain=0.5), -1)


def boss_beam():
    """The head speaking its beam: a hoarse buzz of clashing squares, letters chattering through it, falling."""
    seconds = 0.8

    def fall(f):
        return lambda t: f * (1.0 - 0.15 * t / seconds)

    voice = add(osc("pulse", fall(146.8), seconds, RATE, duty=0.5),
                osc("pulse", fall(207.7), seconds, RATE, duty=0.25), gain=0.8)
    chatter = mul(osc("pulse", lambda t: 880 + 110 * ((int(t * 28) * 7) % 5), seconds, RATE, duty=0.125),
                  _wave(seconds, lambda t: 0.5 + 0.5 * math.sin(2 * math.pi * 28 * t)))
    hiss = highpass(noise(seconds, RATE, seed=45), 3000, RATE)
    out = add(lowpass(voice, 2400, RATE), chatter, gain=0.25)
    out = add(out, hiss, gain=0.12)
    out = mul(out, env_exp(seconds, RATE, 0.004, 0.35))
    return normalize(echo(out, RATE, delay=0.08, feedback=0.3, wet=0.3, tail=0.3), -3)


def boss_hit():
    """A strike landing on the Scrawl: a wet, inky splat over a dull knock."""
    seconds = 0.3
    splat = mul(lowpass(noise(seconds, RATE, seed=46), lambda t: 300 + 3000 * math.exp(-t / 0.03), RATE),
                env_exp(seconds, RATE, 0.001, 0.06))
    knock = mul(osc("triangle", lambda t: 70 + 90 * math.exp(-t / 0.02), seconds, RATE),
                env_exp(seconds, RATE, 0.001, 0.07))
    return normalize(add(splat, knock, gain=0.9), -4)


def boss_sink():
    """A broken hand going limp and sinking into the void: a long falling groan, and the ink bubbling."""
    seconds = 1.6
    groan = osc("pulse", lambda t: 260 * (60 / 260.0) ** (t / seconds), seconds, RATE, duty=0.3)
    bubbles = []
    for k in range(9):
        f0 = 300 + 120 * ((k * 5) % 7)
        blip = mul(osc("sine", lambda t, f0=f0: f0 * (1.0 + 1.5 * t / 0.06), 0.06, RATE),
                   env_exp(0.06, RATE, 0.002, 0.02))
        add(bubbles, blip, int((0.1 + k * 0.15) * RATE), gain=0.5 - 0.04 * k)
    out = add(lowpass(groan, 900, RATE), bubbles, gain=0.6)
    return normalize(mul(out, _swell(seconds, 0.05, 0.9)), -6)


def boss_death():
    """The head shattering: four cracks as it shakes apart, then a long falling howl into the dark, and the void
    settling."""
    seconds = 3.6
    out = []
    for k in range(4):
        crack = mul(bandpass(noise(0.25, RATE, seed=47 + k), 900, 6000, RATE), env_exp(0.25, RATE, 0.0005, 0.04))
        thud = mul(osc("sine", lambda t: 50 + 80 * math.exp(-t / 0.03), 0.25, RATE),
                   env_exp(0.25, RATE, 0.001, 0.08))
        add(out, add(crack, thud, gain=0.8), int(k * 0.35 * RATE), gain=0.6 + 0.13 * k)

    start = 1.35
    length = seconds - start
    howl = add(osc("pulse", lambda t: 880 * (70 / 880.0) ** (t / length) * (1 + 0.02 * math.sin(2 * math.pi * 6 * t)),
                   length, RATE, duty=0.25),
               osc("pulse", lambda t: 587 * (47 / 587.0) ** (t / length), length, RATE, duty=0.5), gain=0.6)
    howl = mul(lowpass(howl, 2500, RATE), env_exp(length, RATE, 0.02, 0.9))
    add(out, howl, int(start * RATE), gain=0.7)

    rumble = mul(lowpass(lowpass(noise(seconds, RATE, seed=52), 120, RATE), 120, RATE), _swell(seconds, 1.2, 2.0))
    add(out, rumble, gain=2.0)
    out = add(out, reverb(out, RATE, room=0.85, damp=0.3, tail=1.5), gain=0.35)
    return normalize(out, -1)



def all_sounds():
    sounds = [("Key", key()), ("Word", word()), ("Typo", typo()), ("Death", death()), ("Hover", hover()),
              ("Select", select())]
    for i, s in enumerate(steps()):
        sounds.append(("Step%d" % (i + 1), s))
    sounds += [("BossRumble", boss_rumble()), ("BossCharge", boss_charge()), ("BossSlam", boss_slam()),
               ("BossBeam", boss_beam()), ("BossHit", boss_hit()), ("BossSink", boss_sink()),
               ("BossDeath", boss_death())]
    return sounds
