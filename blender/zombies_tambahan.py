"""Musuh tambahan (art/enemies-additional.png): Genderuwo Raksasa, Siluman
Harimau, Tuyul Serdadu, Kuntilanak Geni, Pocong Penjaga and Dukun Santet.
Built with the same humanoid builder and zombie clips as zombies.py."""

import math

import numpy as np
from mathutils import Vector

import anims
import btk as T
import characters as C
import zanims
import zombies as Z


# ------------------------------------------------------------------ textures

def tex_tiger(name):
    base, stripe = np.array(T.hexf(0xE8892A)), np.array(T.hexf(0x2A1A10))

    def fn(u, v):
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        wave = np.abs(np.sin(v * 30.0 + np.sin(u * 12.0) * 1.5))
        col[wave > 0.86] = stripe
        blood = (np.sin(u * 17 + 2) * np.sin(v * 11)) > 0.82
        col[blood] = (0.45, 0.06, 0.05)
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def tex_camo(name):
    cols = [np.array(T.hexf(c)) for c in (0x4B5A2A, 0x6B6A3A, 0x2F3A1E, 0x7A6040)]

    def fn(u, v):
        n = (np.sin(u * 23 + np.sin(v * 13) * 2) + np.sin(v * 29 + u * 5) + np.sin((u - v) * 17)) / 3
        idx = np.clip(((n + 1) * 2).astype(int), 0, 3)
        col = np.stack([cols[i] for i in range(4)])[idx]
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def tex_santet_robe(name):
    base, trim = np.array(T.hexf(0x1A1420)), np.array(T.hexf(0x6A2A9A))

    def fn(u, v):
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        col[np.abs((v * 5) % 1 - 0.5) < 0.04] = trim
        col[(np.abs((u * 12) % 1 - 0.5) < 0.05) & (v < 0.25)] = trim
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


# ------------------------------------------------------------------ hooks

def moss_hook(name, s, j, hc, hr):
    moss = T.mat(name + "_moss", 0x4E6B2A, 0.9)
    parts = []
    for sx in (-1, 1):
        sh = j["shoulder_" + ("L" if sx > 0 else "R")]
        parts.append(T.sphere("moss", sh + Vector((sx * 0.08, 0.05, 0.12)), (0.22, 0.2, 0.12), moss, "chest", seg=10, rings=6))
    parts.append(T.sphere("moss_back", j["chest"] + Vector((0, 0.35, 0.2)), (0.4, 0.15, 0.3), moss, "chest", seg=10, rings=6))
    return parts


def tiger_head_hook(name, s, j, hc, hr):
    fur = T.mat(name + "_face", 0xFFFFFF, 0.9, image=tex_tiger(name + "_face_tex"))
    white = T.mat("tiger_white", 0xF4EEE0, 0.8)
    black = T.mat("tiger_nose", 0x1A1210, 0.5)
    fang = T.mat("tiger_fang", 0xF6F0DA, 0.4)
    parts = [T.sphere("muzzle", hc + Vector((0, -hr * 0.85, -hr * 0.35)), (hr * 0.55, hr * 0.45, hr * 0.38), white, "head", seg=14, rings=10),
             T.sphere("nose", hc + Vector((0, -hr * 1.25, -hr * 0.18)), (hr * 0.18, hr * 0.1, hr * 0.12), black, "head", seg=8, rings=6),
             T.sphere("brow_fur", hc + Vector((0, -hr * 0.5, hr * 0.45)), (hr * 0.95, hr * 0.5, hr * 0.35), fur, "head", seg=12, rings=8)]
    for sx in (-1, 1):
        base = hc + Vector((sx * hr * 0.62, hr * 0.05, hr * 0.7))
        parts.append(T.cyl("ear", base, base + Vector((sx * hr * 0.15, 0, hr * 0.5)), hr * 0.3, fur, r1=0.01, bone="head", verts=5))
        parts.append(T.cyl("fang", hc + Vector((sx * hr * 0.2, -hr * 1.08, -hr * 0.45)), hc + Vector((sx * hr * 0.18, -hr * 1.12, -hr * 0.78)),
                           hr * 0.07, fang, r1=0.0, bone="head", verts=6))
        for k in range(3):
            w0 = hc + Vector((sx * hr * 0.35, -hr * 1.1, -hr * (0.25 + k * 0.08)))
            parts.append(T.cyl("whisker", w0, w0 + Vector((sx * hr * 0.8, hr * 0.1, hr * (0.1 - k * 0.1))), 0.004, white, bone="head", verts=3))
    return parts


def tiger_tail_hook(name, s, j, hc, hr):
    fur = T.mat(name + "_tail", 0xFFFFFF, 0.9, image=tex_tiger(name + "_tail_tex"))
    hip = j["hip"]
    pts = [hip + Vector((0, 0.18, -0.02))]
    for k in range(6):
        a = k * 0.35
        pts.append(pts[-1] + Vector((0.03 * math.sin(k), 0.13 * math.cos(a * 0.6), -0.08 + k * 0.05)))
    parts = [T.cyl("tail", a, b, 0.07 - i * 0.008, fur, r1=0.065 - i * 0.008, bone="hips", verts=8) for i, (a, b) in enumerate(zip(pts, pts[1:]))]
    parts.append(T.sphere("tail_tip", pts[-1], 0.05, T.mat("tiger_nose", 0x1A1210, 0.5), "hips", seg=8, rings=6))
    return parts


def claws_hook(name, s, j, hc, hr):
    claw = T.mat("cakar", 0xF2EBD2, 0.4)
    parts = []
    for side, sx in (("L", 1), ("R", -1)):
        hand = (j["wrist_" + side] + j["fingers_" + side]) * 0.5
        for k in range(3):
            base = hand + Vector(((k - 1) * 0.03, -0.04, -0.05))
            parts.append(T.cyl("claw", base, base + Vector((0, -0.05, -0.08)), 0.012, claw, r1=0.0, bone="hand_" + side, verts=5))
    return parts


def army_helmet_hook(name, s, j, hc, hr):
    m = T.mat("helm_tentara", 0xFFFFFF, 0.6, image=tex_camo("helm_camo_tex"))
    strap = T.mat("tali_helm", 0x2A2418, 0.8)
    return [T.sphere("helmet", hc + Vector((0, hr * 0.05, hr * 0.28)), (hr * 1.15, hr * 1.18, hr * 0.85), m, "head", seg=18, rings=10),
            T.torus("rim", hc + Vector((0, hr * 0.05, hr * 0.12)), hr * 1.12, 0.02, m, "head", seg=18, ring_seg=4),
            T.cyl("strap", hc + Vector((hr * 0.95, -hr * 0.1, 0)), hc + Vector((hr * 0.4, -hr * 0.6, -hr * 0.8)), 0.012, strap, bone="head", verts=4)]


def rifle_hook(name, s, j, hc, hr):
    wood = T.mat("popor", 0x6B4A2E, 0.7)
    metal = T.mat("laras", 0x2E3236, 0.4, 0.8)
    blade = T.mat("sangkur", 0xC8CCD0, 0.25, 0.9)
    grip = (j["wrist_R"] + j["fingers_R"]) * 0.5
    fwd = Vector((0.15, -0.9, 0.3)).normalized()
    stock = grip - fwd * 0.3
    muzzle = grip + fwd * 0.55
    return [T.cyl("stock", stock, grip + fwd * 0.05, 0.035, wood, r1=0.03, bone="hand_R", verts=8),
            T.cyl("barrel", grip, muzzle, 0.016, metal, bone="hand_R", verts=6),
            T.cyl("bayonet", muzzle, muzzle + fwd * 0.22, 0.012, blade, r1=0.0, bone="hand_R", verts=4)]


def fire_hair_hook(name, s, j, hc, hr):
    """Kuntilanak Geni: hair that burns upward in purple and orange flames."""
    hot = T.mat("api_rambut", 0xFF8A2A, 0.5, emit=0xFF6A10, strength=7.0)
    purple = T.mat("api_ungu", 0xB04AFF, 0.5, emit=0x9A3AFF, strength=5.0)
    dark = T.mat(name + "_hair", 0x120A18, 0.6)
    parts = [T.sphere("hair_cap", hc + Vector((0, hr * 0.15, hr * 0.1)), (hr * 1.1, hr * 1.05, hr * 1.0), dark, "head", seg=16, rings=10)]
    for k in range(9):
        a = (k - 4) * 0.33
        base = hc + Vector((math.sin(a) * hr * 0.8, hr * 0.25 + abs(math.sin(a)) * hr * 0.2, hr * 0.6))
        tip = base + Vector((math.sin(a) * hr * 0.9, hr * 0.4, hr * (1.6 + (k % 3) * 0.5)))
        parts.append(T.cyl("flame", base, tip, hr * 0.28, hot if k % 2 else purple, r1=0.0, bone="head", verts=6))
    for sx in (-1, 1):
        sh = j["shoulder_" + ("L" if sx > 0 else "R")]
        parts.append(T.cyl("flame_sh", sh, sh + Vector((sx * 0.1, 0.1, 0.45)), 0.08, hot, r1=0.0, bone="chest", verts=6))
    return parts


def santet_hook(name, s, j, hc, hr):
    """Dukun Santet: bone crown, skull necklace and a staff with a purple crystal."""
    bone = T.mat("tulang", 0xE8E0C8, 0.6)
    dark = T.mat("rongga", 0x201814, 0.8)
    crystal = T.mat("kristal_santet", 0xC06AFF, 0.2, emit=0x9A3AFF, strength=6.0)
    wood = T.mat("tongkat_hitam", 0x2A1E1A, 0.8)
    parts = []
    for k in range(7):
        a = (k - 3) * 0.4
        base = hc + Vector((math.sin(a) * hr * 0.9, -math.cos(a) * hr * 0.4, hr * 0.7))
        parts.append(T.cyl("crown_spike", base, base + Vector((math.sin(a) * 0.05, 0.02, 0.22 + (0.1 if k == 3 else 0))), 0.035, bone, r1=0.0,
                           bone="head", verts=5))
    for k in range(5):
        a = (k - 2) * 0.45
        p = j["neck"] + Vector((math.sin(a) * 0.17, -math.cos(a) * 0.15, -0.12 - abs(math.sin(a)) * 0.05))
        parts.append(T.sphere("neck_skull", p, 0.045, bone, "chest", seg=8, rings=6))
        parts.append(T.sphere("neck_socket", p + Vector((0, -0.04, 0.005)), 0.012, dark, "chest", seg=6, rings=4))
    grip = (j["wrist_R"] + j["fingers_R"]) * 0.5
    top = grip + Vector((0, -0.3, 1.1))
    parts.append(T.cyl("staff", grip + Vector((0, 0.08, -0.4)), top, 0.03, wood, bone="hand_R", verts=8))
    parts.append(T.cyl("crystal", top, top + Vector((0, 0, 0.35)), 0.09, crystal, r1=0.0, bone="hand_R", verts=6))
    parts.append(T.cyl("crystal_b", top, top - Vector((0, 0, 0.12)), 0.09, crystal, r1=0.0, bone="hand_R", verts=6))
    # rib-like bone strips on the chest
    for k in range(3):
        parts.append(T.box("rib", j["chest"] + Vector((0, -0.16, 0.08 - k * 0.07)), (0.24 - k * 0.03, 0.02, 0.025), bone, "chest"))
    return parts


# ------------------------------------------------------------------ specs

def specs():
    return {
        "genderuwo_raksasa": C.Spec(height=3.1, head=0.64, shoulder=0.6, hip_w=0.22, leg=1.0, arm=1.4, girth=1.35, depth=1.15, limb=3.3,
                                    skin=0x4F4A32, hair_style="none", top="moss_fur", top_sleeve="none", bottom="moss_fur", bottom_style="pants",
                                    shoes=0x3A3222, face="zombie", eye_glow=0xFF2020, brows=0x1E1A10, ears=1.2, weapons=False,
                                    hooks=(Z.horns_hook, moss_hook)),
        "siluman_harimau": C.Spec(height=1.95, head=0.46, shoulder=0.26, leg=0.86, arm=0.8, girth=1.15, skin=0xE8892A, hair_style="none",
                                  top="tiger", top_sleeve="none", bottom="torn_khaki", bottom_style="shorts", shoes=0xE8892A, face="zombie",
                                  eye_glow=0xFFD21A, brows=0x2A1A10, ears=0.0, weapons=False,
                                  hooks=(tiger_head_hook, tiger_tail_hook, claws_hook)),
        "tuyul_serdadu": C.Spec(height=1.0, head=0.56, shoulder=0.15, hip_w=0.07, leg=0.3, arm=0.34, girth=0.85, skin=0x7DBB5E,
                                hair_style="none", top="camo", top_sleeve="short", bottom="camo", bottom_style="shorts", shoes=0x2A2418,
                                face="zombie", eye_glow=0xFF3B3B, brows=0x2E4A20, ears=1.6, weapons=False,
                                extras=("belt",), hooks=(army_helmet_hook, rifle_hook)),
        "kuntilanak_geni": C.Spec(height=1.78, head=0.41, shoulder=0.2, leg=0.76, arm=0.72, girth=0.9, skin=0xC8B8D8, hair_style="none",
                                  top=0x5A2A7A, top_sleeve="long", bottom=0x4A1E66, bottom_style="skirt", shoes=0xC8B8D8,
                                  face="zombie", eye_glow=0xFF8A1A, brows=0x101010, ears=0.0, weapons=False,
                                  hooks=(fire_hair_hook,)),
        "dukun_santet": C.Spec(height=1.82, head=0.42, shoulder=0.22, leg=0.76, arm=0.7, girth=0.95, skin=0xD8D0B8, hair_style="none",
                               top="santet_robe", top_sleeve="long", bottom="santet_robe", bottom_style="skirt", shoes=0x1A1420, face="zombie",
                               eye_glow=0xB04AFF, brows=0x0E0A10, weapons=False, hooks=(santet_hook,)),
    }


POSTURES = {
    "genderuwo_raksasa": anims.Posture(spine=(20, 0, 0), chest=(14, 0, 0), head=(-22, 0, 0)),
    "siluman_harimau": anims.Posture(spine=(18, 0, 0), chest=(8, 0, 0), head=(-18, 0, 0)),
    "tuyul_serdadu": anims.Posture(spine=(4, 0, 0)),
    "kuntilanak_geni": anims.Posture(head=(8, 0, 14)),
    "dukun_santet": anims.Posture(spine=(12, 0, 0), head=(-10, 0, 0)),
}


def resolve(name, s):
    for attr in ("top", "bottom"):
        v = getattr(s, attr)
        if v == "moss_fur":
            setattr(s, attr, T.mat(name + "_fur", 0xFFFFFF, 0.95, image=Z.tex_fur(name + "_fur_tex", 0x4A4030)))
        elif v == "tiger":
            setattr(s, attr, T.mat(name + "_tiger", 0xFFFFFF, 0.9, image=tex_tiger(name + "_tiger_tex")))
        elif v == "camo":
            setattr(s, attr, T.mat(name + "_camo", 0xFFFFFF, 0.85, image=tex_camo(name + "_camo_tex")))
        elif v == "santet_robe":
            setattr(s, attr, T.mat(name + "_robe", 0xFFFFFF, 0.9, image=tex_santet_robe(name + "_robe_tex")))
        elif v == "torn_khaki":
            setattr(s, attr, T.mat(name + "_pants", 0xFFFFFF, 0.9, image=Z.tex_torn(name + "_pants_tex", 0x6E6048, 0x4A3A28)))


# ------------------------------------------------------------------ pocong penjaga

def build_pocong_penjaga(export=True):
    """A taller pocong in a grimy shroud, bound with iron chains."""
    T.clear_scene()
    T.reset_materials()
    arm = T.build_custom_armature("pocong_penjaga_rig", [
        ("root", (0, 0, 0), (0, 0, 0.2), None),
        ("body", (0, 0, 0.1), (0, 0, 1.35), "root"),
        ("head", (0, 0, 1.35), (0, 0, 1.95), "body"),
    ])
    cloth = T.mat("penjaga_cloth", 0xFFFFFF, 0.9, image=Z.tex_torn("penjaga_cloth_tex", 0xC9C0AA, 0x6E5E44))
    face = T.mat("penjaga_face", 0x5E6A56, 0.7)
    chain = T.mat("rantai", 0x3A3C40, 0.35, 0.85)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    glow = T.mat("zombie_eye", 0x6AFFE0, 0.3, emit=0x3AFFD0, strength=5.0)
    P = [
        T.cyl("shroud", (0, 0, 0.12), (0, 0, 1.35), 0.28, cloth, r1=0.3, bone="body", verts=16),
        T.sphere("shroud_bottom", (0, 0, 0.14), (0.28, 0.28, 0.14), cloth, "body", seg=14, rings=8),
        T.sphere("shroud_shoulder", (0, 0, 1.3), (0.32, 0.3, 0.15), cloth, "body", seg=14, rings=8),
        T.sphere("hood", (0, 0.03, 1.62), (0.3, 0.29, 0.33), cloth, "head", seg=18, rings=12),
        T.sphere("face", (0, -0.19, 1.6), (0.18, 0.12, 0.21), face, "head", seg=14, rings=10),
        T.sphere("knot", (0, 0.0, 2.0), 0.08, cloth, "head", seg=10, rings=6),
        T.cyl("knot_tuft", (0, 0, 2.02), (0.03, 0.02, 2.2), 0.08, cloth, r1=0.01, bone="head", verts=8),
    ]
    for z in (0.35, 0.75, 1.1):
        P.append(T.torus("chain_ring", (0, 0, z), 0.31, 0.03, chain, "body", seg=20, ring_seg=6, rot=(0.12, 0, 0.3)))
    # a hanging end of chain with links
    for k in range(6):
        c = Vector((0.26, -0.18, 1.05 - k * 0.12))
        P.append(T.torus("link", c, 0.04, 0.012, chain, "body", seg=8, ring_seg=4, rot=(0, math.pi / 2 if k % 2 else 0, 0)))
    for sx in (-1, 1):
        big = 1.1 if sx > 0 else 0.95
        P.append(T.sphere("eyeball", (sx * 0.07, -0.29, 1.64), (0.05 * big, 0.03, 0.05 * big), white, "head", seg=10, rings=6))
        P.append(T.sphere("pupil", (sx * 0.075, -0.32, 1.63), 0.022, glow, "head", seg=6, rings=4))
    mesh = T.join(P, "pocong_penjaga")
    T.skin(mesh, arm)
    clips = zanims.build_pocong(arm)
    path = T.export_glb("zombie_pocong_penjaga", [arm]) if export else None
    return {"clips": clips, "path": path}


def build(name, export=True):
    if name == "pocong_penjaga":
        return build_pocong_penjaga(export)
    T.clear_scene()
    T.reset_materials()
    s = specs()[name]
    resolve(name, s)
    arm, mesh = C.build_human(name, s)
    clips = zanims.build_zombie(arm, POSTURES[name], floating=name == "kuntilanak_geni", big=name == "genderuwo_raksasa")
    path = T.export_glb("zombie_" + name, [arm]) if export else None
    return {"clips": clips, "path": path}


NAMES = ["genderuwo_raksasa", "siluman_harimau", "tuyul_serdadu", "kuntilanak_geni", "pocong_penjaga", "dukun_santet"]


def build_all():
    return {n: build(n) for n in NAMES}
