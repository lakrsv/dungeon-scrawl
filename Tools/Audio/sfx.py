"""The game's new sound effects: typing, footsteps, deaths, and the menu clicks.

Each one is short and chiptune, like the original Ludum Dare sounds, and sits a little under them in level:
these play far more often.
"""

import math

from synth import add, echo, env_exp, freq, highpass, lowpass, mul, noise, normalize, osc, scale

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


def all_sounds():
    sounds = [("Key", key()), ("Word", word()), ("Typo", typo()), ("Death", death()), ("Hover", hover()),
              ("Select", select())]
    for i, s in enumerate(steps()):
        sounds.append(("Step%d" % (i + 1), s))
    return sounds
