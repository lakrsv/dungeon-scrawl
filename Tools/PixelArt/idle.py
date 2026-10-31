"""Idle frames for the original creature sprites.

The creatures keep their original art. Each gets one extra frame in which everything above its waist sinks by a
pixel, so they breathe while they wait.
"""

import os

from PIL import Image

from pixelart import bob

ASSETS = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets"))

# (sprite name, path under Assets, first row of the legs)
CREATURES = [("Player", "Resources/Sprites/Player.png", 10)]
CREATURES += [("Zombie%d" % i, "Graphics/Enemies/Zombie%d.png" % i, 10) for i in range(1, 5)]
CREATURES += [("Demon%d" % i, "Graphics/Enemies/Demon%d.png" % i, 11) for i in range(1, 5)]
CREATURES += [("Slime%d" % i, "Graphics/Enemies/Slime%d.png" % i, 9) for i in range(1, 5)]


def original(path):
    return Image.open(os.path.join(ASSETS, path)).convert("RGBA")


def idle_frames():
    """(name, image) for the second frame of every creature's idle loop."""
    return [(name + "_Idle", bob(original(path), waist)) for name, path, waist in CREATURES]
