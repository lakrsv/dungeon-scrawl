"""Builds the generated sounds and music into Assets/Audio.

    python Tools/Audio/build.py                  # everything
    python Tools/Audio/build.py Key Typo         # only the named sounds
    python Tools/Audio/build.py --sfx            # only the sound effects (the music takes a minute or two)
"""

import math
import os
import sys
import time

import music
import sfx
from synth import levels, write_wav

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Audio"))


def _seam(x):
    """How big the jump from the last sample to the first is, next to the typical step between samples.
    Near 1 means the loop point can't be told apart from anywhere else."""
    steps = [abs(x[i + 1] - x[i]) for i in range(0, len(x) - 1, 7)]
    typical = math.sqrt(sum(s * s for s in steps) / len(steps)) or 1.0
    return abs(x[0] - x[-1]) / typical


def _write(name, samples, rate, started, loop=False):
    write_wav(os.path.join(OUT_DIR, name + ".wav"), samples, rate)
    peak, rms = levels(samples)
    seam = "  seam %.2f" % _seam(samples) if loop else ""
    print("%-16s %6.2f s  peak %6.1f dB  rms %6.1f dB%s  (%.1fs)" % (
        name, len(samples) / rate, peak, rms, seam, time.time() - started))


def main():
    names = [a for a in sys.argv[1:] if not a.startswith("--")]
    only_sfx = "--sfx" in sys.argv
    os.makedirs(OUT_DIR, exist_ok=True)

    started = time.time()
    for name, samples in sfx.all_sounds():
        if not names or name in names:
            _write(name, samples, sfx.RATE, started)

    if only_sfx:
        return
    for name, make in music.all_tracks():
        if not names or name in names:
            started = time.time()
            _write(name, make(), music.RATE, started, loop=True)


if __name__ == "__main__":
    main()
