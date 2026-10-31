"""The in-game interface: the prompt frame, its icons, and the status panel's hearts, gems and icons.

Everything is drawn in the menus' language (ink outline, cream frame, gold rivets, dark plum face), small enough
to sit at one pixel per art pixel: the prompts in the world at the world's zoom, the status panel at the menus' 4x.
"""

from PIL import Image

from pixelart import grid

INK = "#140c1c"
CREAM = "#deeed6"
GOLD = "#dad45e"
STEEL = "#8595a1"
FACE = "#1e1424"
SHADE = "#30202e"
PLUM = "#442434"
RED = "#d04648"
PINK = "#d2aa99"
BROWN = "#854c30"
ORANGE = "#d27d2c"

LEGEND = {"k": INK, "C": CREAM, "Y": GOLD, "S": STEEL, "F": FACE, "P": SHADE, "p": PLUM, "R": RED, "h": PINK,
          "B": BROWN, "O": ORANGE}

# A prompt: the menu button's frame at a third of the size. 9-sliced: 3 on the left, top and right, 4 at the
# bottom (the steel shade under the cream). Its face is 7 high, for 5-high capitals with a pixel either side.
PROMPT = [
    ".kkkkkkkk.",
    "kYCCCCCCYk",
    "kCkkkkkkCk",
    "kCkPPPPkCk",
    "kCkFFFFkCk",
    "kCkFFFFkCk",
    "kCkFFFFkCk",
    "kCkFFFFkCk",
    "kCkFFFFkCk",
    "kCkFFFFkCk",
    "kCkkkkkkCk",
    "kYCCCCCCYk",
    "kSSSSSSSSk",
    ".kkkkkkkk.",
]
PROMPT_BORDER = (3, 4, 3, 3)  # left, bottom, right, top

# The prompt icons, 7 high to fit the face: a scroll to cast at a creature, a chest to open.
SCROLL = [
    "kkkkkkk",
    "kRRRRRk",
    ".kCCCk.",
    ".kCkCk.",
    ".kCCCk.",
    "kRRRRRk",
    "kkkkkkk",
]

CHEST = [
    ".kkkkk.",
    "kBBBBBk",
    "kBBBBBk",
    "kOOYOOk",
    "kBBkBBk",
    "kBBBBBk",
    "kkkkkkk",
]

# The status panel: hearts for health, and a row of gems each for power (red) and light (gold).
HEART = [
    ".kk.kk.",
    "khRkRRk",
    "kRRRRRk",
    ".kRRRk.",
    "..kRk..",
    "...k...",
]

# The status panel's own icons are a size up (9), so they read at a glance.
POWER = [
    ".kkkkkkk.",
    "kRRRRRRRk",
    "kpRRRRRpk",
    ".kCCCCCk.",
    ".kCkkkCk.",
    ".kCCCCCk.",
    "kRRRRRRRk",
    "kpRRRRRpk",
    ".kkkkkkk.",
]

LANTERN = [
    "...kkk...",
    "..kSSSk..",
    ".kkkkkkk.",
    ".kYCCCYk.",
    ".kYCOCYk.",
    ".kYOOOYk.",
    ".kYYYYYk.",
    ".kkkkkkk.",
    "..kSSSk..",
]

GEM = [
    ".kkkkk.",
    "khhXXXk",
    "khXXXXk",
    "kXXXXXk",
    "kXXXXdk",
    "kXXXddk",
    ".kkkkk.",
]


# The move prompts' arrows, at the prompts' own size (the original arrows were drawn at half size).
ARROW = [
    "...k...",
    "..kCk..",
    ".kCCCk.",
    "kCCCCCk",
    "kkCCSkk",
    ".kCCSk.",
    ".kkkkk.",
]


def arrows():
    """Up, down, left and right."""
    up = grid(ARROW, LEGEND)
    return {
        "ArrowUp": up,
        "ArrowDown": up.transpose(Image.FLIP_TOP_BOTTOM),
        "ArrowLeft": up.transpose(Image.ROTATE_90),
        "ArrowRight": up.transpose(Image.ROTATE_270),
    }


def prompt():
    return grid(PROMPT, LEGEND)


def scroll():
    return grid(SCROLL, LEGEND)


def chest():
    return grid(CHEST, LEGEND)


def power():
    return grid(POWER, LEGEND)


def lantern():
    return grid(LANTERN, LEGEND)


def heart(full):
    legend = dict(LEGEND)
    if not full:
        legend.update({"R": PLUM, "h": PLUM})
    return grid(HEART, legend)


def gem(fill, highlight, shade):
    legend = dict(LEGEND)
    legend.update({"X": fill, "h": highlight, "d": shade})
    return grid(GEM, legend)


def all_sprites():
    """(name, image, border) for the Interface sheet."""
    return [
        ("PromptFrame", prompt(), PROMPT_BORDER),
        ("ScrollIcon", scroll(), None),
        ("ChestIcon", chest(), None),
        ("PowerIcon", power(), None),
        ("LanternIcon", lantern(), None),
        ("HeartFull", heart(True), None),
        ("HeartEmpty", heart(False), None),
        ("GemPower", gem(RED, PINK, BROWN), None),
        ("GemLight", gem(GOLD, CREAM, ORANGE), None),
        ("GemEmpty", gem(SHADE, PLUM, SHADE), None),
    ] + [(name, img, None) for name, img in arrows().items()]
