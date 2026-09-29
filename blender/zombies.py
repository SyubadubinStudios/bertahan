"""Musuh Kampung Damai: warga zombie, tuyul, pocong, satpam, kuntilanak,
genderuwo and the dukun boss. Goofy rather than gory: googly eyes, green
skin, stitches and glowing pupils."""

import math

import numpy as np
from mathutils import Vector

import anims
import btk as T
import characters as C
import zanims


def tex_torn(name, base, stain):
    b, st = np.array(T.hexf(base)), np.array(T.hexf(stain))

    def fn(u, v):
        n = (np.sin(u * 23.0 + np.sin(v * 17.0) * 2.0) + np.sin(v * 31.0 + u * 7.0) + np.sin((u + v) * 13.0)) / 3.0
        col = np.broadcast_to(b, u.shape + (3,)).copy()
        col[n > 0.55] = st
        col[(n > 0.45) & (n <= 0.55)] = b * 0.6
        patch = (np.abs(((u * 3) % 1) - 0.3) < 0.12) & (np.abs(((v * 2) % 1) - 0.6) < 0.1)
        col[patch] = (0.55, 0.45, 0.3)
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def tex_fur(name, base):
    b = np.array(T.hexf(base))

    rng = np.random.default_rng(7)
    noise = rng.random((128, 128))
    # blur a little so the speckle reads as fur clumps instead of pixel noise
    noise = (noise + np.roll(noise, 1, 0) + np.roll(noise, 1, 1) + np.roll(noise, -1, 0)) / 4.0

    def fn(u, v):
        k = 0.7 + 0.5 * noise[: u.shape[0], : u.shape[1]]
        return b[0] * k, b[1] * k, b[2] * k
    return T.make_image(name, 128, fn)


def tex_robe(name):
    base, gold = np.array(T.hexf(0x5E1A24)), np.array(T.hexf(0xE0B040))

    def fn(u, v):
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        band = (np.abs((v * 4) % 1 - 0.5) < 0.06)
        zig = np.abs(((u * 16) % 1) - 0.5) * 0.12
        col[band | (np.abs((v * 4) % 1 - 0.5 - zig) < 0.02)] = gold
        eye = (((u * 8) % 1 - 0.5) ** 2 + ((v * 4) % 1 - 0.25) ** 2) < 0.01
        col[eye] = gold * 0.9
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


# ---------------------------------------------------------------- hooks

def cap_hook(color):
    def hook(name, s, j, hc, hr):
        m = T.mat(name + "_cap", color, 0.7)
        brim = T.mat(name + "_brim", 0x1B1B20, 0.5)
        return [T.cyl("cap", hc + Vector((0, 0, hr * 0.45)), hc + Vector((0, 0, hr * 1.1)), hr * 1.02, m, r1=hr * 1.08, bone="head", verts=16),
                T.box("brim", hc + Vector((0, -hr * 0.95, hr * 0.5)), (hr * 1.4, hr * 0.7, 0.03), brim, "head", rot=(0.15, 0, 0), bevel=0.01),
                T.box("cap_badge", hc + Vector((0, -hr * 1.04, hr * 0.8)), (0.07, 0.02, 0.07), T.mat("badge", 0xE8C04A, 0.4, 0.6), "head")]
    return hook


def baton_hook(name, s, j, hc, hr):
    grip = (j["wrist_R"] + j["fingers_R"]) * 0.5
    m = T.mat("baton", 0x1E1E22, 0.4)
    return [T.cyl("baton", grip + Vector((0, 0.1, 0)), grip + Vector((0, -0.55, -0.05)), 0.03, m, bone="hand_R", verts=8)]


def long_hair_hook(name, s, j, hc, hr):
    m = T.mat(name + "_hair", 0x0E0C10, 0.6)
    parts = [T.sphere("hair_cap", hc + Vector((0, hr * 0.15, hr * 0.12)), (hr * 1.1, hr * 1.05, hr * 1.0), m, "head", seg=18, rings=10),
             T.cyl("hair_back", hc + Vector((0, hr * 0.55, 0)), j["hip"] + Vector((0, 0.2, -0.05)), hr * 0.95, m, r1=hr * 1.2, bone="chest", verts=14)]
    # bangs hanging over one eye
    for i, x in enumerate((-0.14, -0.06, 0.03)):
        parts.append(T.cyl("bang", hc + Vector((x, -hr * 0.75, hr * 0.7)), hc + Vector((x - 0.04, -hr * 1.02, -hr * (0.1 + 0.25 * i))), 0.045, m, r1=0.02, bone="head", verts=6))
    return parts


def horns_hook(name, s, j, hc, hr):
    m = T.mat("horn", 0xE8DCC0, 0.5)
    fur = T.mat(name + "_tuft", 0x3E2A1E, 0.9)
    parts = []
    for sx in (-1, 1):
        base = hc + Vector((sx * hr * 0.6, 0, hr * 0.6))
        parts.append(T.cyl("horn", base, base + Vector((sx * 0.22, -0.05, 0.28)), 0.07, m, r1=0.0, bone="head", verts=8))
    for k in range(10):
        a = k / 10 * math.tau
        p = j["chest"] + Vector((math.cos(a) * s.shoulder * 0.9, 0.18 + math.sin(a) * 0.05, 0.15 + (k % 3) * 0.08))
        parts.append(T.cyl("tuft", p, p + Vector((math.cos(a) * 0.08, 0.14, 0.12)), 0.07, fur, r1=0.0, bone="chest", verts=5))
    for sx in (-1, 1):
        sh = j["shoulder_" + ("L" if sx > 0 else "R")]
        for k in range(3):
            p = sh + Vector((sx * 0.05, 0.02 * k, 0.05 + 0.04 * k))
            parts.append(T.cyl("shoulder_tuft", p, p + Vector((sx * 0.14, 0.02, 0.1)), 0.06, fur, r1=0.0, bone="chest", verts=5))
    cloth = T.mat("loincloth", 0x5B5A36, 0.9)
    parts.append(T.box("loincloth", j["hip"] + Vector((0, -0.24, -0.12)), (0.36, 0.03, 0.34), cloth, "hips", rot=(-0.1, 0, 0)))
    return parts


def dukun_hook(name, s, j, hc, hr):
    gold = T.mat("gold", 0xE0B040, 0.35, 0.8)
    feather = T.mat("feather", 0xD23C2A, 0.7)
    beard = T.mat("beard", 0xCFCFC4, 0.9)
    wood = T.mat("staff_wood", 0x4A3220, 0.8)
    bone = T.mat("skull", 0xEDE6CF, 0.6, emit=0x9CFF6A, strength=0.6)
    glow = T.mat("skull_glow", 0x9CFF6A, 0.3, emit=0x9CFF6A, strength=8.0)
    parts = [T.cyl("crown", hc + Vector((0, 0, hr * 0.55)), hc + Vector((0, 0, hr * 1.1)), hr * 0.95, gold, r1=hr * 1.05, bone="head", verts=14),
             T.sphere("beard", hc + Vector((0, -hr * 0.75, -hr * 0.8)), (hr * 0.45, hr * 0.25, hr * 0.5), beard, "head", seg=10, rings=8)]
    for k in range(5):
        a = (k - 2) * 0.35
        base = hc + Vector((math.sin(a) * hr * 0.8, -math.cos(a) * hr * 0.6, hr * 1.05))
        parts.append(T.cyl("feather", base, base + Vector((math.sin(a) * 0.12, -0.02, 0.3)), 0.04, feather, r1=0.01, bone="head", verts=6))
    grip = (j["wrist_R"] + j["fingers_R"]) * 0.5
    top = grip + Vector((0, -0.35, 1.1))
    parts.append(T.cyl("staff", grip + Vector((0, 0.08, -0.35)), top, 0.03, wood, bone="hand_R", verts=8))
    parts.append(T.sphere("skull", top + Vector((0, 0, 0.1)), (0.1, 0.09, 0.11), bone, "hand_R", seg=12, rings=8))
    for sx in (-1, 1):
        parts.append(T.sphere("skull_eye", top + Vector((sx * 0.035, -0.08, 0.11)), 0.025, glow, "hand_R", seg=6, rings=4))
    parts.append(T.torus("necklace", j["neck"] + Vector((0, -0.02, -0.08)), 0.14, 0.02, gold, "chest", rot=(0.3, 0, 0), seg=16, ring_seg=4))
    return parts


# ---------------------------------------------------------------- specs

def specs():
    return {
        "warga": C.Spec(height=1.62, head=0.52, shoulder=0.24, leg=0.58, arm=0.56, skin=0x8FC46E, hair=0x3A2A1A, hair_style="short",
                        top="torn_shirt", top_sleeve="short", bottom=0x4A5A7A, bottom_style="shorts", shoes=0x8FC46E, face="zombie",
                        eye_glow=0xFFE14A, brows=0x2A3A1A, weapons=False),
        "tuyul": C.Spec(height=0.95, head=0.56, shoulder=0.15, hip_w=0.07, leg=0.28, arm=0.32, girth=0.8, skin=0x7DBB5E,
                        hair_style="none", top=0x7DBB5E, top_sleeve="none", bottom=0xEDE8D8, bottom_style="shorts", shoes=0x7DBB5E,
                        face="zombie", eye_glow=0xFF3B3B, brows=0x2E4A20, ears=1.8, weapons=False),
        "satpam": C.Spec(height=1.86, head=0.5, shoulder=0.3, hip_w=0.12, leg=0.66, arm=0.62, girth=1.3, skin=0x9DB77E,
                         hair_style="none", top="torn_khaki", top_sleeve="short", bottom=0x23232A, shoes=0x151515,
                         extras=("badge", "belt"), face="zombie", eye_glow=0xFF6A2A, brows=0x1E2A14, weapons=False,
                         hooks=(cap_hook(0x2A3550), baton_hook)),
        "kuntilanak": C.Spec(height=1.72, head=0.5, shoulder=0.21, leg=0.62, arm=0.62, girth=0.9, skin=0xD6E2D0, hair_style="none",
                             top=0xF3F1EA, top_sleeve="long", bottom=0xF3F1EA, bottom_style="skirt", shoes=0xD6E2D0,
                             face="zombie", eye_glow=0xFF2A2A, brows=0x101010, ears=0.0, weapons=False,
                             hooks=(long_hair_hook,)),
        "genderuwo": C.Spec(height=2.35, head=0.64, shoulder=0.52, hip_w=0.16, leg=0.72, arm=0.95, girth=1.2, depth=1.1,
                            skin=0x6B4A33, hair_style="none", top="fur", top_sleeve="none", bottom="fur", bottom_style="pants",
                            shoes=0x4A3222, face="zombie", eye_glow=0xFF2020, brows=0x2A1A10, ears=1.2, weapons=False,
                            hooks=(horns_hook,)),
        "dukun": C.Spec(height=1.74, head=0.5, shoulder=0.23, leg=0.6, arm=0.56, girth=1.05, skin=0x94A07E, hair_style="none",
                        top="robe", top_sleeve="long", bottom="robe", bottom_style="skirt", shoes=0x3A2A1E, face="zombie",
                        eye_glow=0x7CFF4A, brows=0xD0D0C8, weapons=False, hooks=(dukun_hook,)),
    }


POSTURES = {
    "warga": anims.Posture(spine=(8, 0, 0), head=(6, 0, 12)),
    "tuyul": anims.Posture(spine=(6, 0, 0)),
    "satpam": anims.Posture(spine=(6, 0, 0), head=(4, 0, -10)),
    "kuntilanak": anims.Posture(head=(10, 0, 18)),
    "genderuwo": anims.Posture(spine=(18, 0, 0), chest=(12, 0, 0), head=(-20, 0, 0)),
    "dukun": anims.Posture(spine=(10, 0, 0), head=(-8, 0, 0)),
}


def resolve(name, s):
    if s.top == "torn_shirt":
        s.top = T.mat(name + "_shirt", 0xFFFFFF, 0.85, image=tex_torn(name + "_shirt_tex", 0xD9A62E, 0x6E8A3C))
    elif s.top == "torn_khaki":
        s.top = T.mat(name + "_shirt", 0xFFFFFF, 0.85, image=tex_torn(name + "_shirt_tex", 0xC8B98C, 0x7E6A4A))
    elif s.top == "fur":
        s.top = T.mat(name + "_fur", 0xFFFFFF, 0.95, image=tex_fur(name + "_fur_tex", 0x6B4A33))
    elif s.top == "robe":
        s.top = T.mat(name + "_robe", 0xFFFFFF, 0.85, image=tex_robe(name + "_robe_tex"))
    if s.bottom == "fur":
        s.bottom = s.top
    elif s.bottom == "robe":
        s.bottom = s.top


def build_pocong(export=True):
    T.clear_scene()
    T.reset_materials()
    arm = T.build_custom_armature("pocong_rig", [
        ("root", (0, 0, 0), (0, 0, 0.2), None),
        ("body", (0, 0, 0.1), (0, 0, 1.2), "root"),
        ("head", (0, 0, 1.2), (0, 0, 1.75), "body"),
    ])
    cloth_img = tex_torn("pocong_cloth_tex", 0xEDE8DA, 0xB8AE92)
    cloth = T.mat("pocong_cloth", 0xFFFFFF, 0.9, image=cloth_img)
    face = T.mat("pocong_face", 0x9AB88A, 0.7)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    glow = T.mat("zombie_eye", 0xFF4040, 0.3, emit=0xFF4040, strength=4.0)
    gum = T.mat("zombie_mouth", 0x3A1420, 0.6)
    rope = T.mat("pocong_tie", 0xD8CFB5, 0.9)
    P = [
        T.cyl("shroud", (0, 0, 0.12), (0, 0, 1.2), 0.24, cloth, r1=0.27, bone="body", verts=16),
        T.sphere("shroud_bottom", (0, 0, 0.14), (0.24, 0.24, 0.14), cloth, "body", seg=14, rings=8),
        T.sphere("shroud_shoulder", (0, 0, 1.15), (0.28, 0.26, 0.14), cloth, "body", seg=14, rings=8),
        T.sphere("hood", (0, 0.02, 1.45), (0.27, 0.26, 0.3), cloth, "head", seg=18, rings=12),
        T.sphere("face", (0, -0.17, 1.43), (0.17, 0.12, 0.2), face, "head", seg=14, rings=10),
        T.sphere("knot", (0, 0.0, 1.8), 0.07, cloth, "head", seg=10, rings=6),
        T.cyl("knot_tuft", (0, 0, 1.82), (0.02, 0.02, 1.97), 0.07, cloth, r1=0.01, bone="head", verts=8),
        T.torus("neck_tie", (0, 0, 1.2), 0.2, 0.025, rope, "body", seg=16, ring_seg=5),
        T.torus("foot_tie", (0, 0, 0.26), 0.245, 0.025, rope, "body", seg=16, ring_seg=5),
        T.sphere("mouth", (0, -0.27, 1.36), (0.07, 0.03, 0.04), gum, "head", seg=8, rings=5),
    ]
    for sx in (-1, 1):
        big = 1.2 if sx > 0 else 0.9
        P.append(T.sphere("eyeball", (sx * 0.065, -0.26, 1.47), (0.05 * big, 0.03, 0.055 * big), white, "head", seg=10, rings=6))
        P.append(T.sphere("pupil", (sx * 0.07, -0.29, 1.46), 0.02, glow, "head", seg=6, rings=4))
    mesh = T.join(P, "pocong")
    T.skin(mesh, arm)
    clips = zanims.build_pocong(arm)
    path = T.export_glb("zombie_pocong", [arm]) if export else None
    return {"clips": clips, "path": path}


def build(name, export=True):
    if name == "pocong":
        return build_pocong(export)
    T.clear_scene()
    T.reset_materials()
    s = specs()[name]
    resolve(name, s)
    arm, mesh = C.build_human(name, s)
    clips = zanims.build_zombie(arm, POSTURES[name], floating=name == "kuntilanak", big=name == "genderuwo")
    path = T.export_glb("zombie_" + name, [arm]) if export else None
    return {"clips": clips, "path": path}


NAMES = ["warga", "tuyul", "pocong", "satpam", "kuntilanak", "genderuwo", "dukun"]


def build_all():
    return {n: build(n) for n in NAMES}
