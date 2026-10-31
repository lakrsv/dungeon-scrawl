"""The music and the ambience: a title theme for the splash screen and main menu, a dungeon theme for the levels,
and the sound of the dungeon itself. All three loop without a seam.

The songs are written out below as chords and melodies; the instruments are the same pulse and triangle waves as
the sound effects, so everything sounds like one small machine.
"""

import math
import random

from synth import (TAU, add, bandpass, crossfade_loop, echo, env_adsr, env_exp, fold, freq, highpass, hz,
                   loudness, lowpass, midi, mul, noise, osc, pitch_class, reverb, scale)

RATE = 32000

CHORDS = {
    "Am": ("A", "C", "E"),
    "A": ("A", "C#", "E"),
    "Bb": ("Bb", "D", "F"),
    "C": ("C", "E", "G"),
    "Dm": ("D", "F", "A"),
    "E": ("E", "G#", "B"),
    "Em": ("E", "G", "B"),
    "F": ("F", "A", "C"),
    "G": ("G", "B", "D"),
    "Gm": ("G", "Bb", "D"),
}


def _stack(chord, floor, count):
    """`count` notes of the chord, rising from the first chord tone at or above `floor` (a midi number)."""
    tones = {pitch_class(name) for name in CHORDS[chord]}
    notes = []
    m = floor
    while len(notes) < count:
        if m % 12 in tones:
            notes.append(m)
        m += 1
    return notes


def _root(chord, floor):
    """The chord's root in the octave starting at `floor`."""
    return floor + (pitch_class(CHORDS[chord][0]) - floor) % 12


# Instruments --------------------------------------------------------------------------------------------------


def lead(f, seconds, duty=0.25):
    """The melody: a pulse wave that blooms into a slow vibrato when a note is held."""
    def wobble(t):
        depth = 0.0 if t < 0.18 else 0.006 * min(1.0, (t - 0.18) / 0.25)
        return f * (1.0 + depth * math.sin(TAU * 5.2 * t))

    release = 0.12
    tone = mul(osc("pulse", wobble, seconds + release, RATE, duty=duty),
               env_adsr(seconds, RATE, attack=0.01, decay=0.3, sustain=0.6, release=release))
    return lowpass(tone, 4200, RATE)


def pluck(f, tau=0.09, duty=0.125):
    """Arpeggios: a thin pulse that dies away on its own."""
    seconds = min(0.6, tau * 6)
    return lowpass(mul(osc("pulse", f, seconds, RATE, duty=duty), env_exp(seconds, RATE, 0.002, tau)), 3200, RATE)


def bass(f, seconds):
    release = 0.05
    return mul(osc("triangle", f, seconds + release, RATE), env_adsr(seconds, RATE, 0.004, 0.1, 0.85, release))


def pad(f, seconds):
    """A soft swell under the title theme: two detuned squares, filtered dark."""
    release = 1.0
    a = osc("pulse", f * 1.004, seconds + release, RATE)
    b = osc("pulse", f / 1.004, seconds + release, RATE, phase=0.37)
    tone = mul(add(a, b), env_adsr(seconds, RATE, 0.6, 0.5, 0.8, release))
    return lowpass(lowpass(tone, 900, RATE), 1400, RATE)


def kick():
    seconds = 0.25
    body = osc("sine", lambda t: 45 + 95 * math.exp(-t / 0.03), seconds, RATE)
    return mul(body, env_exp(seconds, RATE, 0.001, 0.09))


def snare(seed):
    seconds = 0.2
    rattle = mul(bandpass(noise(seconds, RATE, seed), 1200, 5000, RATE), env_exp(seconds, RATE, 0.001, 0.06))
    body = osc("triangle", lambda t: 185 * (1.0 + 0.3 * math.exp(-t / 0.01)), seconds, RATE)
    return add(scale(rattle, 1.6), mul(body, env_exp(seconds, RATE, 0.001, 0.04)), gain=0.6)


def hat(seed):
    seconds = 0.06
    tss = highpass(highpass(noise(seconds, RATE, seed), 6000, RATE), 6000, RATE)
    return mul(tss, env_exp(seconds, RATE, 0.0005, 0.018))


# Arrangement --------------------------------------------------------------------------------------------------


class Song:
    """A loop of bars at a tempo. Notes are placed by bar and eighth; each can also be sent to the echo and the
    reverb."""

    def __init__(self, bpm, bars):
        self.eighth = 30.0 / bpm
        self.bar = 8 * self.eighth
        self.length = int(round(bars * self.bar * RATE))
        self.dry = []
        self.echo = []
        self.room = []

    def play(self, samples, bar, eighth, gain, room=0.0, echo=0.0):
        start = int(round((bar * 8 + eighth) * self.eighth * RATE))
        add(self.dry, samples, start, gain)
        if room:
            add(self.room, samples, start, gain * room)
        if echo:
            add(self.echo, samples, start, gain * echo)

    def melody(self, bar, notes, gain, room, echo, duty=0.25):
        """notes is a list of (note, eighths); a note of None is a rest."""
        pos = 0
        for note, length in notes:
            if note:
                self.play(lead(freq(note), length * self.eighth * 0.92, duty), bar, pos, gain, room, echo)
            pos += length

    def render(self, room_size, room_gain, rms_db):
        # The echo repeats a dotted eighth later, in time with the music, and rings into the reverb too.
        repeats = echo(self.echo, RATE, delay=1.5 * self.eighth, feedback=0.35, wet=1.0, tail=3.0, dry=0.0)
        add(self.room, repeats, gain=0.5)
        mix = add(self.dry, repeats)
        add(mix, reverb(self.room, RATE, room=room_size, damp=0.3, tail=3.0), gain=room_gain)

        # Everything past the end belongs to the start of the next time round.
        return loudness(fold(highpass(mix, 20, RATE), self.length), rms_db, -3.0)


# Title theme: A minor, slow and wistful. Plays under the splash screen and the main menu, and after a win.

TITLE_CHORDS = ["Am", "F", "C", "G", "Am", "F", "Dm", "E",
                "F", "G", "Em", "Am", "F", "G", "Dm", "E"]

TITLE_MELODY = [
    [("A4", 2), ("C5", 2), ("E5", 3), ("D5", 1)],
    [("C5", 6), (None, 2)],
    [("G4", 2), ("C5", 2), ("E5", 3), ("F5", 1)],
    [("D5", 6), (None, 2)],
    [("A4", 2), ("C5", 2), ("E5", 3), ("G5", 1)],
    [("F5", 3), ("E5", 1), ("C5", 4)],
    [("D5", 2), ("E5", 2), ("F5", 3), ("E5", 1)],
    [("B4", 3), ("G#4", 1), ("E4", 4)],
    [("A5", 3), ("G5", 1), ("F5", 2), ("E5", 2)],
    [("D5", 3), ("E5", 1), ("D5", 2), ("B4", 2)],
    [("E5", 3), ("D5", 1), ("B4", 2), ("G4", 2)],
    [("A4", 6), (None, 2)],
    [("C5", 2), ("F5", 2), ("A5", 3), ("G5", 1)],
    [("G5", 3), ("F5", 1), ("D5", 4)],
    [("F5", 2), ("E5", 2), ("D5", 3), ("C5", 1)],
    [("B4", 4), ("G#4", 2), ("B4", 2)],
]


def title_theme():
    song = Song(bpm=76, bars=len(TITLE_CHORDS))
    arpeggio = [0, 1, 2, 3, 1, 2, 3, 2]
    for bar, chord in enumerate(TITLE_CHORDS):
        for note in _stack(chord, midi("G3"), 3):
            song.play(pad(hz(note), song.bar - 0.05), bar, 0, gain=0.10, room=0.5)

        root = _root(chord, midi("E2"))
        song.play(bass(hz(root), 6 * song.eighth * 0.95), bar, 0, gain=0.42)
        song.play(bass(hz(root + 12), 2 * song.eighth * 0.9), bar, 6, gain=0.30)

        notes = _stack(chord, midi("A3"), 4)
        for i, index in enumerate(arpeggio):
            song.play(pluck(hz(notes[index])), bar, i, gain=0.13, room=0.4, echo=0.5)

        song.melody(bar, TITLE_MELODY[bar], gain=0.30, room=0.5, echo=0.35)
    return song.render(room_size=0.86, room_gain=0.9, rms_db=-19.0)


# Dungeon theme: D minor, a steady walk with a little drive. It sits under constant typing, so the melody is
# simple, and the first eight bars have none at all.

DUNGEON_A = ["Dm", "Dm", "Bb", "C", "Dm", "Dm", "Gm", "A"]
DUNGEON_B = ["Bb", "C", "Am", "Dm", "Bb", "C", "Gm", "A"]
DUNGEON_CHORDS = DUNGEON_A + DUNGEON_A + DUNGEON_B + DUNGEON_A

MELODY_A = [
    [("D5", 2), ("F5", 2), ("A5", 4)],
    [("A5", 3), ("G5", 1), ("F5", 2), ("E5", 2)],
    [("F5", 4), ("D5", 2), ("F5", 2)],
    [("E5", 3), ("G5", 1), ("E5", 2), ("C5", 2)],
    [("D5", 2), ("F5", 2), ("A5", 4)],
    [("C6", 3), ("Bb5", 1), ("A5", 4)],
    [("Bb5", 3), ("A5", 1), ("G5", 4)],
    [("E5", 3), ("C#5", 1), ("A4", 4)],
]

MELODY_B = [
    [("D6", 4), ("C6", 2), ("Bb5", 2)],
    [("C6", 4), ("G5", 4)],
    [("A5", 4), ("E5", 2), ("C5", 2)],
    [("D5", 6), (None, 2)],
    [("F5", 2), ("Bb5", 2), ("D6", 3), ("C6", 1)],
    [("C6", 3), ("Bb5", 1), ("G5", 4)],
    [("Bb5", 2), ("A5", 2), ("G5", 3), ("F5", 1)],
    [("E5", 4), ("C#5", 4)],
]

DUNGEON_MELODY = [None] * 8 + MELODY_A + MELODY_B + MELODY_A


def dungeon_theme():
    song = Song(bpm=104, bars=len(DUNGEON_CHORDS))
    hats = [hat(20), hat(21)]
    snares = [snare(22), snare(23)]
    boom = kick()
    for bar, chord in enumerate(DUNGEON_CHORDS):
        root = _root(chord, midi("D2"))
        for i, octave in enumerate([0, 0, 12, 0, 0, 0, 12, 0]):
            song.play(bass(hz(root + octave), song.eighth * 0.7), bar, i, gain=0.34)

        notes = _stack(chord, midi("A3"), 4)
        for i, index in enumerate([0, 1, 2, 1] * 4):
            song.play(pluck(hz(notes[index]), tau=0.06), bar, i * 0.5, gain=0.085, room=0.4, echo=0.3)

        for i in range(8):
            song.play(hats[i % 2], bar, i, gain=0.06 if i % 2 == 0 else 0.09)

        # The drums come in after four bars of each time round.
        if bar >= 4:
            song.play(boom, bar, 0, gain=0.55)
            song.play(boom, bar, 4, gain=0.5)
            song.play(snares[0], bar, 2, gain=0.16, room=0.3)
            song.play(snares[1], bar, 6, gain=0.18, room=0.3)

        if DUNGEON_MELODY[bar]:
            song.melody(bar, DUNGEON_MELODY[bar], gain=0.24, room=0.35, echo=0.3)
    return song.render(room_size=0.84, room_gain=0.8, rms_db=-19.0)


# Ambience: cave wind, a low hum in the rock, and water dripping somewhere off in the dark.

def dungeon_ambience():
    seconds = 32.0
    length = int(seconds * RATE)
    overlap = int(2.0 * RATE)
    total = (length + overlap) / RATE

    def gust(t):
        return 0.6 + 0.25 * math.sin(TAU * 2 * t / seconds) + 0.15 * math.sin(TAU * 5 * t / seconds + 1.3)

    wind = lowpass(noise(total, RATE, seed=11), lambda t: 160 + 300 * gust(t), RATE)
    wind = highpass(lowpass(wind, 700, RATE), 60, RATE)
    wind = [v * gust(i / RATE) for i, v in enumerate(wind)]

    hum = add(osc("sine", 55.0, total, RATE), osc("sine", 82.41, total, RATE), gain=0.6)
    hum = [v * (0.7 + 0.3 * math.sin(TAU * 3 * i / RATE / seconds)) for i, v in enumerate(hum)]

    bed = crossfade_loop(add(scale(wind, 2.0), hum, gain=0.05), length, overlap)

    rng = random.Random(12)
    drips = []
    t = 1.0
    while t < seconds - 0.5:
        f0 = rng.uniform(1100, 1900)
        seconds_drip = 0.15
        drop = osc("sine", lambda s, f0=f0: f0 * (1.0 + 0.6 * (1.0 - math.exp(-s / 0.01))), seconds_drip, RATE)
        add(drips, mul(drop, env_exp(seconds_drip, RATE, 0.0005, 0.03)), int(t * RATE), gain=rng.uniform(0.3, 0.7))
        t += rng.uniform(2.0, 5.5)
    cave = add(scale(drips, 0.5), reverb(drips, RATE, room=0.9, damp=0.35, tail=4.0), gain=1.4)

    out = add(bed, fold(cave, length))
    return loudness(out, -24.0, -6.0)


def all_tracks():
    return [("TitleTheme", title_theme), ("DungeonTheme", dungeon_theme), ("DungeonAmbience", dungeon_ambience)]
