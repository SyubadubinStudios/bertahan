"""Weapons of Kampung Damai. Each builder returns one joined mesh in the
canonical weapon frame: grip at the origin, the business end along +Z and the
striking edge facing -Y."""

import math

from mathutils import Matrix

import btk as T

WOOD = 0x9A6A3C
WOOD_DARK = 0x6B4423
STEEL = 0x8C959E
STEEL_DARK = 0x4A5058


def _m():
    return {
        "wood": T.mat("wood", WOOD, 0.8),
        "wood_dark": T.mat("wood_dark", WOOD_DARK, 0.85),
        "steel": T.mat("steel", STEEL, 0.35, 0.9),
        "steel_dark": T.mat("steel_dark", STEEL_DARK, 0.4, 0.85),
        "rust": T.mat("rust_red", 0xB2382B, 0.6, 0.3),
        "bamboo": T.mat("bamboo", 0x7FA83A, 0.6),
        "bamboo_node": T.mat("bamboo_node", 0x5E7E25, 0.65),
        "bamboo_tip": T.mat("bamboo_tip", 0xD8C58A, 0.7),
        "black": T.mat("pan_black", 0x222226, 0.45, 0.6),
        "straw": T.mat("straw", 0xC9A45C, 0.9),
        "glass": T.mat("bottle_glass", 0x3F8F4F, 0.15, 0.0),
        "cloth": T.mat("wick_cloth", 0xE9E1CF, 0.9),
        "flame": T.mat("wick_flame", 0xFF8A1E, 0.5, 0.0, emit=0xFF7A10, strength=6.0),
        "tape": T.mat("grip_tape", 0x2E2A26, 0.9),
    }


def linggis(name="W_linggis"):
    m = _m()
    parts = [
        T.cyl(name + "_rod", (0, 0, -0.18), (0, 0, 0.8), 0.018, m["steel_dark"], verts=6, smooth=False),
        T.cyl(name + "_bend", (0, 0, 0.8), (0, -0.09, 0.9), 0.018, m["steel_dark"], verts=6, smooth=False),
        T.cyl(name + "_claw", (0, -0.09, 0.9), (0, -0.16, 0.87), 0.016, m["steel_dark"], r1=0.006, verts=6, smooth=False),
        T.cyl(name + "_paint", (0, 0, 0.35), (0, 0, 0.55), 0.021, m["rust"], verts=6, smooth=False),
        T.cyl(name + "_grip", (0, 0, -0.1), (0, 0, 0.12), 0.024, m["tape"], verts=8),
    ]
    return T.join(parts, name)


def kampak(name="W_kampak"):
    m = _m()
    parts = [
        T.cyl(name + "_handle", (0, 0, -0.15), (0, 0, 0.62), 0.024, m["wood"], r1=0.028, verts=8),
        T.box(name + "_head", (0, -0.06, 0.55), (0.045, 0.15, 0.1), m["steel_dark"], bevel=0.008),
        T.box(name + "_blade", (0, -0.16, 0.55), (0.016, 0.09, 0.22), m["steel"], bevel=0.005),
        T.box(name + "_back", (0, 0.03, 0.55), (0.05, 0.05, 0.07), m["steel_dark"], bevel=0.006),
    ]
    return T.join(parts, name)


def pacul(name="W_pacul"):
    m = _m()
    parts = [
        T.cyl(name + "_handle", (0, 0, -0.2), (0, 0, 0.95), 0.024, m["wood"], verts=8),
        T.box(name + "_socket", (0, -0.03, 0.93), (0.07, 0.08, 0.07), m["steel_dark"], bevel=0.01),
        # the blade hangs forward and down like a real hoe
        T.box(name + "_blade", (0, -0.09, 0.82), (0.22, 0.015, 0.27), m["steel"], rot=(math.radians(-18), 0, 0), bevel=0.004),
    ]
    return T.join(parts, name)


def sapu(name="W_sapu"):
    m = _m()
    parts = [T.cyl(name + "_handle", (0, 0, -0.25), (0, 0, 0.85), 0.018, m["wood"], verts=8),
             T.cyl(name + "_bind", (0, 0, 0.82), (0, 0, 0.9), 0.04, m["wood_dark"], verts=10)]
    # sapu lidi: a fan of thin sticks
    for i in range(14):
        a = (i / 13.0 - 0.5) * 0.9
        tip = (math.sin(a) * 0.22, -0.02 * math.cos(i), 0.9 + 0.42 * math.cos(a))
        parts.append(T.cyl(f"{name}_lidi{i}", (0, 0, 0.88), tip, 0.008, m["straw"], r1=0.004, verts=4, smooth=False))
    return T.join(parts, name)


def wajan(name="W_wajan"):
    m = _m()
    parts = [
        T.cyl(name + "_handle", (0, 0, -0.12), (0, 0, 0.2), 0.022, m["wood_dark"], verts=8),
        T.cyl(name + "_neck", (0, 0, 0.18), (0, 0, 0.3), 0.012, m["black"], verts=6),
        # the pan faces forward (-Y)
        T.sphere(name + "_pan", (0, 0.0, 0.46), (0.19, 0.06, 0.19), m["black"], seg=18, rings=8),
        T.torus(name + "_rim", (0, -0.015, 0.46), 0.19, 0.014, m["black"], rot=(math.pi / 2, 0, 0), seg=20, ring_seg=6),
        T.sphere(name + "_shine", (0.06, -0.055, 0.52), (0.05, 0.01, 0.03), m["steel"], seg=8, rings=4),
    ]
    return T.join(parts, name)


def bambu_runcing(name="W_bambu"):
    m = _m()
    parts = [T.cyl(name + "_pole", (0, 0, -0.5), (0, 0, 1.1), 0.028, m["bamboo"], verts=10),
             T.cyl(name + "_tip", (0, 0, 1.1), (0, 0, 1.38), 0.028, m["bamboo_tip"], r1=0.0, verts=10)]
    for i, z in enumerate((-0.35, 0.0, 0.35, 0.72, 1.05)):
        parts.append(T.torus(f"{name}_node{i}", (0, 0, z), 0.029, 0.007, m["bamboo_node"], seg=10, ring_seg=4))
    parts.append(T.box(name + "_flag", (0.0, 0.07, 0.98), (0.004, 0.12, 0.08), T.mat("flag_red", 0xD6282B, 0.8)))
    parts.append(T.box(name + "_flag2", (0.0, 0.07, 0.9), (0.004, 0.12, 0.08), T.mat("flag_white", 0xF3F0E8, 0.8)))
    return T.join(parts, name)


def pentungan(name="W_pentungan"):
    m = _m()
    parts = [
        T.cyl(name + "_club", (0, 0, -0.12), (0, 0, 0.62), 0.026, m["wood"], r1=0.05, verts=10),
        T.sphere(name + "_end", (0, 0, 0.62), 0.05, m["wood"], seg=10, rings=6),
        T.cyl(name + "_grip", (0, 0, -0.12), (0, 0, 0.08), 0.03, m["tape"], verts=8),
    ]
    return T.join(parts, name)


def senapan(name="W_senapan"):
    """Old hunting rifle. Barrel along +Z, grip (trigger hand) at the origin."""
    m = _m()
    parts = [
        T.box(name + "_stock", (0, 0.035, -0.22), (0.045, 0.1, 0.34), m["wood_dark"], rot=(math.radians(8), 0, 0), bevel=0.012),
        T.box(name + "_body", (0, 0.0, 0.08), (0.05, 0.07, 0.3), m["steel_dark"], bevel=0.01),
        T.box(name + "_fore", (0, 0.01, 0.34), (0.045, 0.055, 0.28), m["wood"], bevel=0.01),
        T.cyl(name + "_barrel", (0, -0.015, 0.2), (0, -0.015, 0.78), 0.014, m["steel"], verts=8),
        T.box(name + "_trigger", (0, 0.05, 0.0), (0.012, 0.03, 0.05), m["steel"]),
        T.box(name + "_sight", (0, -0.035, 0.74), (0.01, 0.02, 0.02), m["steel"]),
    ]
    return T.join(parts, name)


def molotov(name="W_molotov"):
    m = _m()
    parts = [
        T.cyl(name + "_bottle", (0, 0, -0.05), (0, 0, 0.12), 0.045, m["glass"], verts=12),
        T.cyl(name + "_shoulder", (0, 0, 0.12), (0, 0, 0.17), 0.045, m["glass"], r1=0.018, verts=12),
        T.cyl(name + "_neck", (0, 0, 0.17), (0, 0, 0.24), 0.017, m["glass"], verts=10),
        T.cyl(name + "_wick", (0, 0, 0.24), (0.01, 0, 0.31), 0.014, m["cloth"], r1=0.01, verts=6),
        T.sphere(name + "_flame", (0.01, 0, 0.33), (0.02, 0.02, 0.035), m["flame"], seg=8, rings=5),
    ]
    return T.join(parts, name)


BUILDERS = {
    "linggis": linggis,
    "kampak": kampak,
    "pacul": pacul,
    "sapu": sapu,
    "wajan": wajan,
    "bambu": bambu_runcing,
    "pentungan": pentungan,
    "senapan": senapan,
    "molotov": molotov,
}

# How each weapon sits in the right hand in the rest pose (arms hanging down):
# melee shafts point forward, the rifle barrel runs along the arm so the aim
# pose (arm raised forward) points it at the target.
GRIP = {
    "senapan": Matrix.Rotation(math.pi, 4, 'X'),
    "molotov": Matrix.Rotation(math.radians(160), 4, 'X'),
}
DEFAULT_GRIP = Matrix.Rotation(math.radians(90), 4, 'X')


def build_all(prefix="W_"):
    return {key: fn(prefix + key) for key, fn in BUILDERS.items()}


def export_pickups():
    """One GLB per weapon for the pickups lying around the levels."""
    for key, fn in BUILDERS.items():
        T.clear_scene()
        T.reset_materials()
        obj = fn("pickup_" + key)
        T.export_glb("weapon_" + key, [obj], animations=False)
