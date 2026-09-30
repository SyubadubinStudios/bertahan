"""Props for the later levels (Latar Lanjutan, art/level 5-10.png):
Jembatan Bambu, Sekolah Terbengkalai, Kuburan Kuno, Hutan Larangan,
Masjid Rusak and Candi Terlarang. Same conventions as props.py: origin at the
ground centre, fronts face -Y. Exported as prop_<name>.glb."""

import math
import random

from mathutils import Vector

import btk as T
from props import M, tex_planks, tex_plaster, tex_stone


def _rng(seed):
    return random.Random(seed)


# ------------------------------------------------------------ level 5: jembatan bambu

def jembatan_bambu(name="jembatan_bambu", length=12.0, width=2.4):
    """A long bamboo footbridge with sagging rope rails. The deck stays low so the
    player walks over it at ground height; it spans the river along Y."""
    m = M()
    rope = T.mat("tali", 0xB9A57A, 0.9)
    r = _rng(5)
    P = []
    n = int(length / 0.18)
    for i in range(n):
        y = -length / 2 + (i + 0.5) * length / n
        tilt = r.uniform(-0.04, 0.04)
        P.append(T.cyl("slat", (-width / 2, y, 0.12), (width / 2, y, 0.12 + tilt), 0.075, m["bamboo_dry"] if i % 3 else m["bamboo"], verts=6))
    for sx in (-1, 1):
        P.append(T.cyl("beam", (sx * width * 0.42, -length / 2, 0.04), (sx * width * 0.42, length / 2, 0.04), 0.09, m["wood_dark"], verts=8))
        for k in range(5):
            y = -length / 2 + k * length / 4
            P.append(T.cyl("post", (sx * width / 2, y, -0.3), (sx * width / 2, y, 1.25), 0.07, m["bamboo"], verts=8))
        # sagging rope between posts: a chain of short segments
        for k in range(4):
            y0, y1 = -length / 2 + k * length / 4, -length / 2 + (k + 1) * length / 4
            pts = [Vector((sx * width / 2, y0 + (y1 - y0) * t / 6, 1.15 - 0.25 * math.sin(math.pi * t / 6))) for t in range(7)]
            for a, b in zip(pts, pts[1:]):
                P.append(T.cyl("rope", a, b, 0.025, rope, verts=5))
                P.append(T.cyl("rope_low", a - Vector((0, 0, 0.5)), b - Vector((0, 0, 0.5)), 0.02, rope, verts=5))
    return T.join(P, name)


def perahu(name="perahu"):
    """A wrecked wooden canoe on the river bank."""
    m = M()
    hull = T.mat("perahu_hull", 0xFFFFFF, 0.85, image=tex_planks("perahu_tex", 0x6B4A2E))
    P = [T.sphere("hull", (0, 0, 0.2), (0.6, 2.4, 0.35), hull, seg=18, rings=10),
         T.box("seat", (0, -0.6, 0.35), (1.0, 0.18, 0.06), m["wood_light"]),
         T.box("seat2", (0, 0.7, 0.35), (1.0, 0.18, 0.06), m["wood_light"]),
         T.cyl("oar", (-0.3, -1.2, 0.4), (0.8, 1.4, 0.55), 0.03, m["wood"], verts=6)]
    return T.join(P, name)


def tiang_perahu(name="tiang_perahu"):
    """Mooring poles sticking out of the water."""
    m = M()
    r = _rng(9)
    P = []
    for i in range(3):
        x, y = r.uniform(-0.5, 0.5), r.uniform(-0.5, 0.5)
        P.append(T.cyl("pole", (x, y, -0.3), (x + r.uniform(-0.1, 0.1), y, r.uniform(1.2, 1.8)), 0.08, m["bamboo"], verts=8))
    return T.join(P, name)


# ------------------------------------------------------------ level 6: sekolah terbengkalai

def sekolah(name="sekolah", w=9.0, d=6.0, h=2.9):
    """A roofless, crumbling classroom block: walls with windows and a doorway, a
    blackboard inside and broken roof beams, so the top-down camera sees in."""
    m = M()
    wall = T.mat("sekolah_wall", 0xFFFFFF, 0.9, image=tex_plaster("sekolah_wall_tex", 0xE8DDB0))
    trim = T.mat("sekolah_trim", 0x3F7F5F, 0.7)
    board = T.mat("papan_tulis_hitam", 0x1F3B2E, 0.6)
    chalk = T.mat("kapur", 0xEDEDE0, 0.8)
    P = [T.box("floor", (0, 0, 0.05), (w, d, 0.1), T.mat("lantai", 0x9C9588, 0.9))]
    t = 0.22
    r = _rng(3)
    # back wall with the blackboard, side walls, a front wall with windows and a door gap
    P.append(T.box("back", (0, d / 2, h / 2), (w, t, h), wall))
    P.append(T.box("board", (0, d / 2 - 0.13, 1.5), (3.2, 0.04, 1.2), board))
    P.append(T.box("board_frame", (0, d / 2 - 0.12, 0.85), (3.4, 0.08, 0.08), m["wood"]))
    for i in range(5):
        P.append(T.box("chalk", (-1.2 + i * 0.5, d / 2 - 0.16, 1.4 + r.uniform(-0.3, 0.4)), (r.uniform(0.2, 0.5), 0.01, 0.02), chalk,
                       rot=(0, r.uniform(-0.4, 0.4), 0)))
    for sx in (-1, 1):
        # side walls broken at the top: two stacked boxes with a jagged cap
        P.append(T.box("side", (sx * w / 2, 0, h * 0.4), (t, d, h * 0.8), wall))
        for k in range(4):
            P.append(T.box("side_top", (sx * w / 2, -d / 2 + (k + 0.5) * d / 4, h * 0.8 + 0.15), (t, d / 4 * 0.9, r.uniform(0.1, 0.9)), wall))
    # front wall: pieces between windows, the door on the right
    xs = [-w / 2, -w / 2 + 1.4, -w / 2 + 3.4, -w / 2 + 5.4, w / 2 - 1.6, w / 2]
    for a, b in zip(xs[:-1], xs[1:]):
        P.append(T.box("front", ((a + b) / 2, -d / 2, 0.45), (b - a, t, 0.9), wall))
    for i, (a, b) in enumerate(zip(xs[:-1], xs[1:])):
        if i == 4:  # door gap
            continue
        P.append(T.box("front_top", ((a + b) / 2, -d / 2, h - 0.35 - (0.3 if i == 2 else 0)), (b - a, t, 0.7), wall))
    for x in (-w / 2 + 0.7, -w / 2 + 2.4, -w / 2 + 4.4):
        P.append(T.box("pier", (x + 0.7, -d / 2, h / 2), (0.25, t, h), wall))
    P.append(T.box("trim", (0, -d / 2 - 0.12, 0.92), (w, 0.04, 0.08), trim))
    # the broken roof: a few beams and hanging zinc sheets
    for k in range(4):
        x = -w / 2 + 1 + k * 2.3
        P.append(T.box("beam", (x, 0, h + 0.1), (0.15, d + 0.4, 0.15), m["wood_dark"], rot=(r.uniform(-0.15, 0.15), 0, 0)))
    P.append(T.box("zinc", (w / 2 - 1.5, d / 4, h + 0.15), (2.5, 2.4, 0.03), m["zinc"], rot=(0.25, 0.1, 0)))
    P.append(T.box("sign", (-w / 2 + 1.2, -d / 2 - 0.2, h + 0.35), (2.2, 0.06, 0.45), trim))
    return T.join(P, name)


def meja_sekolah(name="meja_sekolah", tipped=False):
    m = M()
    top = T.mat("meja_top", 0xFFFFFF, 0.8, image=tex_planks("meja_tex", 0x9C6A3E))
    P = [T.box("top", (0, 0, 0.72), (1.1, 0.5, 0.05), top)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.box("leg", (sx * 0.5, sy * 0.2, 0.36), (0.05, 0.05, 0.72), m["wood_dark"]))
    P.append(T.box("bench", (0, 0.6, 0.42), (1.1, 0.3, 0.05), top))
    for sx in (-1, 1):
        P.append(T.box("bench_leg", (sx * 0.5, 0.6, 0.21), (0.05, 0.25, 0.42), m["wood_dark"]))
    P.append(T.box("book", (0.2, -0.05, 0.77), (0.25, 0.18, 0.03), T.mat("buku_merah", 0xC0392B, 0.7), rot=(0, 0, 0.3)))
    obj = T.join(P, name)
    if tipped:
        obj.rotation_euler = (0, math.radians(80), 0.3)
        obj.location.z = 0.5
    return obj


def tiang_bendera(name="tiang_bendera"):
    m = M()
    red = T.mat("bendera_merah", 0xD32F2F, 0.8)
    white = T.mat("bendera_putih", 0xF4F4F0, 0.8)
    P = [T.cyl("pole", (0, 0, 0), (0, 0, 6.0), 0.06, m["metal"], verts=8),
         T.box("base", (0, 0, 0.2), (1.2, 1.2, 0.4), m["concrete"]),
         T.box("red", (0.55, 0, 5.55), (1.0, 0.03, 0.35), red, rot=(0, 0.1, 0)),
         T.box("white", (0.5, 0, 5.2), (0.9, 0.03, 0.35), white, rot=(0, 0.25, 0.1))]
    return T.join(P, name)


def papan_tulis(name="papan_tulis"):
    """A fallen blackboard on an easel in the schoolyard."""
    m = M()
    board = T.mat("papan_tulis_hitam", 0x1F3B2E, 0.6)
    P = [T.box("board", (0, 0, 1.1), (1.6, 0.05, 1.0), board, rot=(0.15, 0, 0))]
    for sx in (-1, 1):
        P.append(T.cyl("leg", (sx * 0.6, -0.2, 0), (sx * 0.65, 0.05, 1.7), 0.03, m["wood"], verts=6))
    P.append(T.cyl("leg_b", (0, 0.5, 0), (0, 0.05, 1.7), 0.03, m["wood"], verts=6))
    return T.join(P, name)


# ------------------------------------------------------------ level 7: kuburan kuno

def nisan_kuno(name="nisan_kuno", seed=0):
    """Old Javanese grave (kijing) of mossy stone with a carved headstone."""
    stone = T.mat("batu_kuno", 0xFFFFFF, 0.95, image=tex_stone("batu_kuno_tex", 0x7C8274))
    r = _rng(seed)
    lean = r.uniform(-0.2, 0.2)
    P = [T.box("kijing", (0, 0, 0.18), (0.9, 1.9, 0.36), stone, bevel=0.04),
         T.box("kijing_top", (0, 0, 0.42), (0.7, 1.6, 0.14), stone, bevel=0.03)]
    for y, h in ((-0.75, 0.9), (0.75, 0.7)):
        P.append(T.box("head", (0, y, 0.5 + h / 2), (0.34, 0.14, h), stone, rot=(lean, 0, 0), bevel=0.04))
        P.append(T.cyl("crown", (0, y - 0.07, 0.5 + h), (0, y + 0.07, 0.5 + h), 0.17, stone, verts=10))
    return T.join(P, name)


def cungkup(name="cungkup"):
    """A little shrine roof over a sacred grave (makam keramat)."""
    m = M()
    P = [T.gable_roof("roof", 3.0, 3.6, 1.4, 0.3, m["tile_dark"], loc=(0, 0, 2.3))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.cyl("post", (sx * 1.3, sy * 1.6, 0), (sx * 1.3, sy * 1.6, 2.35), 0.1, m["wood_dark"], verts=8))
    obj_parts = P + [nisan_kuno("inner_grave", 7)]
    cloth = T.mat("kain_kafan_hijau", 0x2E6B3A, 0.9)
    obj_parts.append(T.box("cloth", (0, 0, 0.55), (0.8, 1.5, 0.04), cloth))
    return T.join(obj_parts, name)


def arca(name="arca", seed=0):
    """A weathered stone statue (arca) sitting on a plinth."""
    stone = T.mat("batu_arca", 0xFFFFFF, 0.95, image=tex_stone("batu_arca_tex", 0x8A8778))
    P = [T.box("plinth", (0, 0, 0.3), (1.0, 1.0, 0.6), stone, bevel=0.05),
         T.sphere("body", (0, 0, 1.05), (0.4, 0.32, 0.5), stone, seg=14, rings=10),
         T.sphere("head", (0, 0, 1.75), (0.24, 0.24, 0.28), stone, seg=12, rings=10),
         T.cyl("crown", (0, 0, 1.95), (0, 0, 2.25), 0.2, stone, r1=0.05, verts=10)]
    for sx in (-1, 1):
        P.append(T.sphere("knee", (sx * 0.28, -0.25, 0.72), (0.2, 0.25, 0.14), stone, seg=10, rings=6))
        P.append(T.cyl("arm", (sx * 0.38, 0, 1.35), (sx * 0.2, -0.25, 0.85), 0.09, stone, verts=8))
    obj = T.join(P, name)
    obj.rotation_euler.y = _rng(seed).uniform(-0.1, 0.1)
    return obj


def reruntuhan(name="reruntuhan", seed=0):
    """Broken stone wall with fallen blocks."""
    stone = T.mat("batu_bata_kuno", 0xFFFFFF, 0.95, image=tex_stone("batu_bata_tex", 0x8E6A52))
    r = _rng(seed)
    P = []
    for i in range(5):
        h = r.uniform(0.6, 2.2)
        P.append(T.box("wall", (-2 + i, 0, h / 2), (0.95, 0.5, h), stone, bevel=0.03))
    for i in range(5):
        P.append(T.box("block", (r.uniform(-2.5, 2.5), r.uniform(-1.5, -0.4), 0.15), (0.5, 0.35, 0.3), stone, rot=(0, 0, r.uniform(0, 3)), bevel=0.03))
    return T.join(P, name)


# ------------------------------------------------------------ level 8: hutan larangan

def pohon_larangan(name="pohon_larangan", seed=0):
    """A huge twisted forbidden-forest tree: gnarled trunk, flaring roots,
    hanging vines and a dark crown."""
    r = _rng(seed)
    bark = T.mat("kulit_kayu_tua", 0x3E3024, 0.95)
    moss = T.mat("lumut", 0x3E5A2A, 0.9)
    crown = T.mat("daun_gelap", 0x1F3A22, 0.85)
    vine = T.mat("akar_gantung", 0x2E3A20, 0.9)
    P = []
    # trunk as a stack of slightly offset, bending segments
    p = Vector((0, 0, 0))
    radius = 0.75
    for k in range(6):
        q = p + Vector((r.uniform(-0.35, 0.35), r.uniform(-0.35, 0.35), 1.1))
        P.append(T.cyl("trunk", p, q, radius, bark, r1=radius * 0.82, verts=12))
        P.append(T.sphere("knot", q, radius * 0.85, bark if k % 2 else moss, seg=10, rings=8))
        p, radius = q, radius * 0.82
    top = p
    for a in range(5):
        ang = a / 5 * math.tau + r.uniform(-0.3, 0.3)
        end = top + Vector((math.cos(ang) * 2.6, math.sin(ang) * 2.6, r.uniform(0.4, 1.4)))
        P.append(T.cyl("branch", top, end, 0.28, bark, r1=0.1, verts=8))
        P.append(T.sphere("crown", end + Vector((0, 0, 0.5)), (r.uniform(1.6, 2.2), r.uniform(1.6, 2.2), r.uniform(1.0, 1.4)), crown, seg=12, rings=8))
        for v in range(2):
            vs = end + Vector((r.uniform(-0.6, 0.6), r.uniform(-0.6, 0.6), -0.2))
            P.append(T.cyl("vine", vs, vs - Vector((0, 0, r.uniform(2.5, 4.5))), 0.04, vine, verts=5))
    P.append(T.sphere("crown_mid", top + Vector((0, 0, 1.2)), (2.4, 2.4, 1.6), crown, seg=14, rings=8))
    for a in range(6):
        ang = a / 6 * math.tau + r.uniform(-0.2, 0.2)
        base = Vector((math.cos(ang) * 0.5, math.sin(ang) * 0.5, 0.6))
        end = Vector((math.cos(ang) * r.uniform(1.8, 2.6), math.sin(ang) * r.uniform(1.8, 2.6), -0.1))
        P.append(T.cyl("root", base, end, 0.3, bark, r1=0.08, verts=8))
    return T.join(P, name)


def batang_tumbang(name="batang_tumbang"):
    bark = T.mat("kulit_kayu_tua", 0x3E3024, 0.95)
    moss = T.mat("lumut", 0x3E5A2A, 0.9)
    wood = T.mat("kayu_lapuk", 0x8A6A48, 0.9)
    P = [T.cyl("log", (-3, 0, 0.45), (3, 0.3, 0.4), 0.45, bark, verts=12),
         T.cyl("end", (3, 0.3, 0.4), (3.02, 0.3, 0.4), 0.44, wood, verts=12),
         T.sphere("moss", (0, 0.1, 0.8), (2.2, 0.35, 0.12), moss, seg=12, rings=6),
         T.cyl("branch", (-1, 0, 0.6), (-1.6, -0.9, 1.3), 0.1, bark, r1=0.03, verts=6)]
    return T.join(P, name)


def jamur_nyala(name="jamur_nyala"):
    """A cluster of glowing mushrooms: the only light in the forest."""
    stem = T.mat("jamur_batang", 0xE8E0C8, 0.7)
    cap = T.mat("jamur_nyala", 0x6AF0FF, 0.4, emit=0x4AD8FF, strength=4.0)
    r = _rng(4)
    P = []
    for i in range(6):
        x, y, h = r.uniform(-0.4, 0.4), r.uniform(-0.4, 0.4), r.uniform(0.12, 0.35)
        P.append(T.cyl("stem", (x, y, 0), (x, y, h), 0.025, stem, verts=6))
        P.append(T.sphere("cap", (x, y, h), (0.09 * h / 0.25, 0.09 * h / 0.25, 0.05), cap, seg=10, rings=6))
    return T.join(P, name)


def semak_duri(name="semak_duri"):
    thorn = T.mat("duri", 0x3A2A1E, 0.8)
    leaf = T.mat("daun_duri", 0x24402A, 0.85)
    r = _rng(8)
    P = [T.sphere("bush", (0, 0, 0.45), (0.9, 0.8, 0.55), leaf, seg=12, rings=8)]
    for i in range(14):
        a, e = r.uniform(0, math.tau), r.uniform(0.1, 1.2)
        base = Vector((math.cos(a) * 0.6, math.sin(a) * 0.55, 0.45 + math.sin(e) * 0.3))
        P.append(T.cyl("thorn", base, base * 1.5 + Vector((0, 0, 0.1)), 0.04, thorn, r1=0.0, verts=5))
    return T.join(P, name)


# ------------------------------------------------------------ level 9: masjid rusak

def masjid_rusak(name="masjid_rusak"):
    """The village mosque after the attack: cracked dome, a toppled minaret top,
    broken walls and rubble. Still a landmark in the burning village."""
    m = M()
    wall = T.mat("masjid_rusak_wall", 0xFFFFFF, 0.9, image=tex_plaster("masjid_rusak_tex", 0xB8C2B0))
    dome = T.mat("kubah_retak", 0x2A6E5A, 0.55, 0.15)
    char = T.mat("gosong", 0x2A2420, 0.95)
    r = _rng(21)
    P = [T.box("base", (0, 0, 0.2), (8.4, 8.4, 0.4), m["concrete"])]
    # walls with gaps (left wall half fallen)
    P.append(T.box("back", (0, 3.6, 2.2), (7.5, 0.35, 3.6), wall))
    P.append(T.box("right", (3.6, 0, 2.2), (0.35, 7.5, 3.6), wall))
    P.append(T.box("left_low", (-3.6, 0.8, 1.0), (0.35, 5.8, 1.6), wall))
    P.append(T.box("front_l", (-2.4, -3.6, 1.9), (2.6, 0.35, 3.0), wall))
    P.append(T.box("front_r", (2.5, -3.6, 2.2), (2.4, 0.35, 3.6), wall))
    P.append(T.cyl("arch", (0, -3.5, 3.3), (0, -3.75, 3.3), 0.9, wall, verts=16))
    # half the roof and a cracked dome
    P.append(T.box("roof", (1.3, 1.2, 4.1), (5.2, 5.4, 0.3), dome, rot=(0.05, -0.04, 0)))
    P.append(T.sphere("dome", (0.8, 0.8, 4.9), (2.2, 2.2, 2.0), dome, seg=22, rings=12))
    for i in range(6):
        P.append(T.box("crack", (0.8 + r.uniform(-1.5, 1.5), -1.2, 5.2 + r.uniform(-0.6, 0.8)), (0.08, 0.05, r.uniform(0.4, 1.1)), char,
                       rot=(0, r.uniform(-0.8, 0.8), 0)))
    # the minaret, its top snapped off and lying on the ground
    mx, my = 4.8, 3.2
    P.append(T.cyl("minaret", (mx, my, 0.4), (mx, my, 6.5), 0.7, wall, r1=0.62, verts=12))
    P.append(T.cyl("minaret_top", (mx - 1.5, my - 3.0, 0.6), (mx + 1.2, my - 5.2, 0.9), 0.6, wall, verts=12))
    P.append(T.sphere("minaret_dome", (mx + 1.5, my - 5.5, 0.9), 0.7, dome, seg=12, rings=8))
    # scorch marks and rubble
    for i in range(18):
        P.append(T.box("rubble", (r.uniform(-5, 5), r.uniform(-6, -3.8), 0.15), (r.uniform(0.2, 0.7), r.uniform(0.2, 0.6), r.uniform(0.15, 0.4)),
                       wall if i % 3 else char, rot=(r.uniform(0, 1), r.uniform(0, 1), r.uniform(0, 3)), bevel=0.02))
    P.append(T.box("scorch", (-2.4, -3.8, 2.4), (2.4, 0.02, 1.8), char))
    return T.join(P, name)


def puing(name="puing", seed=0):
    """A heap of rubble and broken planks from a collapsed house."""
    m = M()
    r = _rng(seed)
    char = T.mat("gosong", 0x2A2420, 0.95)
    P = []
    for i in range(16):
        P.append(T.box("brick", (r.uniform(-1.2, 1.2), r.uniform(-1.0, 1.0), r.uniform(0.1, 0.6)), (r.uniform(0.3, 0.7), r.uniform(0.2, 0.4), 0.2),
                       m["concrete"] if i % 2 else char, rot=(r.uniform(0, 1), r.uniform(0, 1), r.uniform(0, 3)), bevel=0.02))
    for i in range(5):
        P.append(T.box("plank", (r.uniform(-1, 1), r.uniform(-0.8, 0.8), 0.5), (0.15, r.uniform(1.2, 2.2), 0.06), m["wood_dark"] if i % 2 else char,
                       rot=(r.uniform(-0.6, 0.6), r.uniform(-0.5, 0.5), r.uniform(0, 3))))
    return T.join(P, name)


def rumah_terbakar(name="rumah_terbakar"):
    """The charred frame of a village house."""
    m = M()
    char = T.mat("gosong", 0x2A2420, 0.95)
    ember = T.mat("bara", 0xFF5A1A, 0.6, emit=0xFF4A10, strength=6.0)
    r = _rng(12)
    P = [T.box("floor", (0, 0, 0.1), (4.4, 3.8, 0.2), m["concrete"])]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.box("post", (sx * 2.0, sy * 1.7, 1.3), (0.2, 0.2, 2.6), char))
    P.append(T.box("wall", (0, 1.8, 0.9), (4.2, 0.15, 1.8), char))
    P.append(T.box("beam", (0, 0, 2.6), (4.4, 0.2, 0.2), char, rot=(0, 0.15, 0)))
    P.append(T.box("rafter", (-0.8, 0.6, 2.2), (0.15, 3.4, 0.15), char, rot=(0.5, 0, 0)))
    for i in range(6):
        P.append(T.sphere("ember", (r.uniform(-1.8, 1.8), r.uniform(-1.4, 1.4), 0.25), 0.12, ember, seg=6, rings=4))
    return T.join(P, name)


# ------------------------------------------------------------ level 10: candi terlarang

def _candi_stone():
    return T.mat("batu_candi", 0xFFFFFF, 0.95, image=tex_stone("batu_candi_tex", 0x6E5E54))


def _rune():
    return T.mat("rune_merah", 0xFF3A2A, 0.4, emit=0xFF2A1A, strength=5.0)


def candi_dinding(name="candi_dinding"):
    """A stretch of temple wall with a carved frieze and glowing red runes."""
    stone = _candi_stone()
    rune = _rune()
    P = [T.box("wall", (0, 0, 1.6), (6.0, 0.8, 3.2), stone, bevel=0.03),
         T.box("cornice", (0, 0, 3.3), (6.3, 1.0, 0.25), stone),
         T.box("plinth", (0, 0, 0.2), (6.4, 1.1, 0.4), stone)]
    for i in range(5):
        x = -2.4 + i * 1.2
        P.append(T.box("panel", (x, -0.42, 1.7), (0.9, 0.06, 1.6), stone, bevel=0.02))
        # an old Javanese-looking glyph: a diamond eye over a wavy stroke
        P.append(T.box("rune", (x, -0.46, 2.05), (0.32, 0.02, 0.32), rune, rot=(0, math.radians(45), 0)))
        P.append(T.box("rune_eye", (x, -0.47, 2.05), (0.12, 0.02, 0.12), stone, rot=(0, math.radians(45), 0)))
        for k in range(3):
            P.append(T.box("rune_wave", (x + (k - 1) * 0.16, -0.46, 1.45 + (0.06 if k == 1 else 0)), (0.16, 0.02, 0.06), rune,
                           rot=(0, (0.5 if k % 2 else -0.5), 0)))
    for i in range(7):
        P.append(T.cyl("merlon", (-3 + i, 0, 3.4), (-3 + i, 0, 3.9), 0.22, stone, r1=0.08, verts=6))
    return T.join(P, name)


def candi_pilar(name="candi_pilar"):
    stone = _candi_stone()
    rune = _rune()
    P = [T.box("base", (0, 0, 0.3), (1.2, 1.2, 0.6), stone, bevel=0.03),
         T.cyl("shaft", (0, 0, 0.6), (0, 0, 4.2), 0.42, stone, r1=0.38, verts=12),
         T.box("capital", (0, 0, 4.35), (1.1, 1.1, 0.35), stone, bevel=0.03),
         T.torus("band", (0, 0, 2.4), 0.43, 0.04, rune, seg=16, ring_seg=4)]
    return T.join(P, name)


def candi_gapura(name="candi_gapura"):
    """Candi bentar: the split temple gate, two stepped towers facing each other."""
    stone = _candi_stone()
    rune = _rune()
    P = []
    for sx in (-1, 1):
        x = sx * 2.2
        for k in range(5):
            s = 1.8 - k * 0.3
            P.append(T.box("tier", (x, 0, 0.6 + k * 1.1), (s, 1.8 - k * 0.25, 1.1), stone, bevel=0.04))
        P.append(T.cyl("tip", (x, 0, 6.1), (x, 0, 7.0), 0.3, stone, r1=0.02, verts=8))
        P.append(T.box("eye", (x - sx * 0.95, -0.2, 2.3), (0.05, 0.4, 0.4), rune))
    P.append(T.box("step", (0, -1.3, 0.1), (2.6, 1.0, 0.2), stone))
    return T.join(P, name)


def altar_ritual(name="altar_ritual"):
    """The heart of the ritual: a stone slab on a raised dais ringed by candles,
    a glowing sigil on the floor."""
    m = M()
    stone = _candi_stone()
    rune = _rune()
    wax = T.mat("lilin_hitam", 0x2A2226, 0.6)
    P = [T.cyl("dais", (0, 0, 0), (0, 0, 0.35), 3.2, stone, verts=24),
         T.box("slab", (0, 0, 0.8), (2.6, 1.2, 0.5), stone, bevel=0.05),
         T.box("cloth", (0, 0, 1.06), (2.7, 0.9, 0.03), T.mat("kain_ritual", 0x5E1A24, 0.9)),
         T.torus("sigil", (0, 0, 0.37), 2.7, 0.05, rune, seg=40, ring_seg=4)]
    for i in range(5):
        a1, a2 = i / 5 * math.tau, ((i + 2) % 5) / 5 * math.tau
        p1 = Vector((math.cos(a1) * 2.6, math.sin(a1) * 2.6, 0.37))
        p2 = Vector((math.cos(a2) * 2.6, math.sin(a2) * 2.6, 0.37))
        P.append(T.cyl("star", p1, p2, 0.035, rune, verts=4))
    for i in range(12):
        a = i / 12 * math.tau
        p = Vector((math.cos(a) * 3.0, math.sin(a) * 3.0, 0.35))
        h = 0.25 + (i % 3) * 0.12
        P.append(T.cyl("candle", p, p + Vector((0, 0, h)), 0.05, wax, verts=8))
        P.append(T.sphere("flame", p + Vector((0, 0, h + 0.06)), (0.03, 0.03, 0.06), m["flame"], seg=6, rings=4))
    P.append(T.sphere("skull", (0.8, 0, 1.2), (0.16, 0.15, 0.16), T.mat("skull", 0xEDE6CF, 0.6), seg=10, rings=8))
    return T.join(P, name)


def obor(name="obor"):
    """A standing torch."""
    m = M()
    P = [T.cyl("pole", (0, 0, 0), (0, 0, 1.8), 0.05, m["wood_dark"], verts=6),
         T.cyl("bowl", (0, 0, 1.75), (0, 0, 1.95), 0.12, m["dark"], r1=0.18, verts=10),
         T.sphere("fire", (0, 0, 2.1), (0.14, 0.14, 0.25), m["flame"], seg=10, rings=8)]
    return T.join(P, name)


def tengkorak(name="tengkorak"):
    """A pile of skulls for the ritual chamber, each one looking a different way."""
    from mathutils import Matrix
    bone = T.mat("skull", 0xEDE6CF, 0.6)
    dark = T.mat("rongga", 0x201814, 0.8)
    r = _rng(6)
    P = []
    for i in range(6):
        c = Vector((r.uniform(-0.45, 0.45), r.uniform(-0.45, 0.45), 0.13 + (0.22 if i > 3 else 0)))
        rot = Matrix.Rotation(r.uniform(-1.2, 1.2), 3, 'Z')

        def at(offset):
            return c + rot @ Vector(offset)
        yaw = rot.to_euler()
        P.append(T.sphere("cranium", at((0, 0.02, 0.03)), (0.13, 0.15, 0.13), bone, seg=12, rings=10, rot=tuple(yaw)))
        P.append(T.box("jaw", at((0, -0.08, -0.09)), (0.14, 0.1, 0.06), bone, rot=tuple(yaw), bevel=0.02))
        for sx in (-1, 1):
            P.append(T.sphere("socket", at((sx * 0.05, -0.125, 0.02)), (0.04, 0.02, 0.035), dark, seg=8, rings=5, rot=tuple(yaw)))
        P.append(T.sphere("nose", at((0, -0.14, -0.03)), (0.018, 0.012, 0.025), dark, seg=6, rings=4, rot=tuple(yaw)))
    return T.join(P, name)


PROPS = {
    "jembatan_bambu": jembatan_bambu,
    "perahu": perahu,
    "tiang_perahu": tiang_perahu,
    "sekolah": sekolah,
    "meja_sekolah": meja_sekolah,
    "meja_rebah": lambda: meja_sekolah("meja_rebah", tipped=True),
    "tiang_bendera": tiang_bendera,
    "papan_tulis": papan_tulis,
    "nisan_kuno_a": lambda: nisan_kuno("nisan_kuno_a", 1),
    "nisan_kuno_b": lambda: nisan_kuno("nisan_kuno_b", 2),
    "cungkup": cungkup,
    "arca": arca,
    "reruntuhan": reruntuhan,
    "pohon_larangan_a": lambda: pohon_larangan("pohon_larangan_a", 1),
    "pohon_larangan_b": lambda: pohon_larangan("pohon_larangan_b", 2),
    "batang_tumbang": batang_tumbang,
    "jamur_nyala": jamur_nyala,
    "semak_duri": semak_duri,
    "masjid_rusak": masjid_rusak,
    "puing_a": lambda: puing("puing_a", 1),
    "puing_b": lambda: puing("puing_b", 2),
    "rumah_terbakar": rumah_terbakar,
    "candi_dinding": candi_dinding,
    "candi_pilar": candi_pilar,
    "candi_gapura": candi_gapura,
    "altar_ritual": altar_ritual,
    "obor": obor,
    "tengkorak": tengkorak,
}


def build(name, export=True):
    T.clear_scene()
    T.reset_materials()
    obj = PROPS[name]()
    return T.export_glb("prop_" + name, [obj], animations=False) if export else obj


def build_all(names=None):
    return {name: build(name) for name in (names or PROPS)}
