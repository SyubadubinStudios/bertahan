"""The family of Kampung Damai: Bapak, Ibu, Kakak, Ade, Kake and Nene.

Chibi proportions (big heads), flat cartoon colours and a few procedural
fabric textures (batik, kebaya flowers, sarung plaid). Every part is rigidly
bound to one bone, which suits the toy-like style and keeps skinning cheap.
"""

import math

import numpy as np
from mathutils import Matrix, Vector

import anims
import btk as T
import weapons as W

SKIN = {"light": 0xF1C29B, "tan": 0xD9A273, "brown": 0xB97A4F, "old": 0xD7A983}


# ------------------------------------------------------------------ textures

def tex_batik(name, base, ink, accent):
    b, i, a = np.array(T.hexf(base)), np.array(T.hexf(ink)), np.array(T.hexf(accent))

    def fn(u, v):
        d = ((u + v) * 5.0) % 1.0
        band = np.abs(d - 0.5) < 0.16
        edge = (np.abs(d - 0.5) > 0.16) & (np.abs(d - 0.5) < 0.2)
        wave = np.sin((u - v) * 60.0) * 0.5 + 0.5
        dots = (((u * 20) % 1 - 0.5) ** 2 + ((v * 20) % 1 - 0.5) ** 2) < 0.04
        col = np.where(band[..., None], a * (0.8 + 0.2 * wave[..., None]), b)
        col = np.where(edge[..., None], i, col)
        col = np.where((dots & ~band)[..., None], a * 0.7, col)
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def tex_flowers(name, base, colors):
    base = np.array(T.hexf(base))
    palette = [np.array(T.hexf(c)) for c in colors]

    def fn(u, v):
        cu, cv = (u * 6) % 1 - 0.5, (v * 6) % 1 - 0.5
        cell = (np.floor(u * 6) + np.floor(v * 6) * 3).astype(int) % len(palette)
        ang = np.arctan2(cv, cu)
        r = np.sqrt(cu ** 2 + cv ** 2)
        petal = r < (0.22 + 0.1 * np.cos(ang * 5))
        centre = r < 0.08
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        for k, p in enumerate(palette):
            m = petal & (cell == k)
            col[m] = p
        col[centre] = (1.0, 0.85, 0.3)
        leaf = (np.abs(((u + 0.5) * 6) % 1 - 0.5) < 0.08) & (np.abs(((v + 0.5) * 6) % 1 - 0.5) < 0.2)
        col[leaf & ~petal] = (0.35, 0.65, 0.35)
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def tex_plaid(name, base, dark, line):
    b, d, l = np.array(T.hexf(base)), np.array(T.hexf(dark)), np.array(T.hexf(line))

    def fn(u, v):
        su = ((u * 6) % 1) < 0.35
        sv = ((v * 6) % 1) < 0.35
        thin = (np.abs((u * 12) % 1 - 0.5) < 0.04) | (np.abs((v * 12) % 1 - 0.5) < 0.04)
        col = np.broadcast_to(b, u.shape + (3,)).copy()
        col[su | sv] = b * 0.75 + d * 0.25
        col[su & sv] = d
        col[thin] = l
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


# -------------------------------------------------------------------- builder

class Spec:
    def __init__(self, **kw):
        self.__dict__.update(dict(
            height=1.7, head=0.5, shoulder=0.24, hip_w=0.1, leg=0.6, arm=0.52, girth=1.0, depth=1.0,
            skin=SKIN["tan"], hair=0x2B1D14, hair_style="short", top=None, top_sleeve="short",
            bottom=None, bottom_style="pants", shoes=0x2A2A2A, socks=None, extras=(), brows=0x2B1D14,
            eye=0x20180F, blush=0.5, posture=None, bounce=1.0, weapon="pentungan", face="cute", eye_glow=0xFFE14A,
            ears=1.0, weapons=True, hooks=()))
        self.__dict__.update(kw)


def build_human(name, s):
    """Builds mesh + armature for spec `s`. Returns (armature, mesh)."""
    j = T.humanoid_joints(s.height, s.head, s.shoulder, s.hip_w, s.leg, s.arm)
    arm = T.build_armature(name + "_rig", j)
    skin = T.mat(name + "_skin", s.skin, 0.65)
    top = s.top if not isinstance(s.top, int) else T.mat(name + "_top", s.top, 0.8)
    bottom = s.bottom if not isinstance(s.bottom, int) else T.mat(name + "_bottom", s.bottom, 0.8)
    shoes = T.mat(name + "_shoes", s.shoes, 0.6)
    hair = T.mat(name + "_hair", s.hair, 0.7)
    eye = T.mat("eye_dark", s.eye, 0.3)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    mouth = T.mat("mouth", 0x7A2530, 0.6)
    blush = T.mat("blush", 0xF08A8A, 0.8)
    brow = T.mat(name + "_brow", s.brows, 0.8)
    P = []

    hr = s.head * 0.5
    hc = Vector((0, 0, s.height - hr))
    neck, chest, hip = j["neck"], j["chest"], j["hip"]
    g, dp = s.girth, s.depth

    # ---- head & face (facing -Y)
    P.append(T.sphere("head", hc, (hr * 1.02, hr * 0.96, hr), skin, "head", seg=20, rings=12))
    zombie = s.face == "zombie"
    iris = T.mat("zombie_eye", s.eye_glow, 0.3, emit=s.eye_glow, strength=4.0) if zombie else eye
    for sx in (-1, 1):
        if s.ears:
            P.append(T.sphere("ear", hc + Vector((sx * hr * 0.98, 0.02, -0.02)), (0.05 * s.ears, 0.035, 0.07 * s.ears), skin, "head", seg=8, rings=6))
        ex = sx * hr * 0.36
        if zombie:
            # big white eyeballs with small glowing pupils, one eye a bit larger: goofy, not scary
            big = 1.18 if sx > 0 else 0.92
            P.append(T.sphere("eyeball", hc + Vector((ex, -hr * 0.8, -hr * 0.05)), (hr * 0.22 * big, 0.07, hr * 0.24 * big), white, "head", seg=12, rings=8))
            P.append(T.sphere("pupil", hc + Vector((ex + sx * 0.01, -hr * 0.8 - 0.06, -hr * 0.08)), (hr * 0.11 * big, 0.025, hr * 0.11 * big), eye, "head", seg=10, rings=6))
            P.append(T.sphere("glint", hc + Vector((ex + sx * 0.01, -hr * 0.8 - 0.083, -hr * 0.08)), (hr * 0.045, 0.01, hr * 0.045), iris, "head", seg=6, rings=4))
            P.append(T.box("brow", hc + Vector((ex, -hr * 0.9, hr * 0.26)), (hr * 0.32, 0.03, hr * 0.07), brow, "head",
                           rot=(0, sx * math.radians(14), 0), bevel=0.008))
        else:
            P.append(T.sphere("eye", hc + Vector((ex, -hr * 0.86, -hr * 0.08)), (hr * 0.15, 0.05, hr * 0.2), eye, "head", seg=10, rings=8))
            P.append(T.sphere("eyehi", hc + Vector((ex + sx * 0.012, -hr * 0.97, -hr * 0.0)), (hr * 0.05, 0.02, hr * 0.06), white, "head", seg=6, rings=4))
            P.append(T.box("brow", hc + Vector((ex, -hr * 0.9, hr * 0.2)), (hr * 0.3, 0.03, hr * 0.06), brow, "head",
                           rot=(0, sx * math.radians(-8), 0), bevel=0.008))
        if s.blush > 0 and not zombie:
            P.append(T.sphere("blush", hc + Vector((sx * hr * 0.58, -hr * 0.74, -hr * 0.34)), (hr * 0.16, 0.03, hr * 0.09), blush, "head", seg=8, rings=4))
    P.append(T.sphere("nose", hc + Vector((0, -hr * 0.98, -hr * 0.22)), (hr * 0.09, 0.05, hr * 0.08), skin, "head", seg=8, rings=6))
    if zombie:
        gum = T.mat("zombie_mouth", 0x3A1420, 0.6)
        tooth = T.mat("zombie_tooth", 0xF2EBC8, 0.5)
        P.append(T.sphere("mouth", hc + Vector((0, -hr * 0.84, -hr * 0.5)), (hr * 0.34, 0.06, hr * 0.14), gum, "head", seg=12, rings=6))
        for i, tx in enumerate((-0.12, 0.05, 0.16)):
            P.append(T.box("tooth", hc + Vector((tx * hr * 1.6, -hr * 0.9, -hr * (0.42 if i != 1 else 0.58))), (hr * 0.1, 0.02, hr * 0.1), tooth, "head"))
        stitch = T.mat("stitch", 0x2B2B22, 0.8)
        for i in range(4):
            P.append(T.box("stitch", hc + Vector((-hr * 0.55 + i * 0.035, -hr * 0.6, hr * 0.62)), (0.012, 0.012, 0.06), stitch, "head", rot=(0.6, 0, 0.3)))
        P.append(T.box("stitch_line", hc + Vector((-hr * 0.5, -hr * 0.6, hr * 0.62)), (0.14, 0.01, 0.012), stitch, "head", rot=(0.6, 0, 0.3)))
    else:
        P.append(T.sphere("mouth", hc + Vector((0, -hr * 0.88, -hr * 0.45)), (hr * 0.2, 0.04, hr * 0.07), mouth, "head", seg=10, rings=6))

    # ---- hair styles
    hs = s.hair_style
    if hs in ("short", "kid", "bun", "old"):
        P.append(T.sphere("hair", hc + Vector((0, hr * 0.12, hr * 0.14)), (hr * 1.07, hr * 1.0, hr * 0.95), hair, "head", seg=20, rings=12))
        P.append(T.sphere("fringe", hc + Vector((0, -hr * 0.55, hr * 0.62)), (hr * 0.8, hr * 0.38, hr * 0.3), hair, "head", seg=14, rings=8,
                          rot=(math.radians(-20), 0, 0)))
    if hs == "kid":
        for i, (x, z) in enumerate(((-0.08, 0.95), (0.03, 1.0), (0.12, 0.9), (-0.16, 0.82))):
            P.append(T.cyl("spike", hc + Vector((x, -hr * 0.2, hr * z * 0.9)), hc + Vector((x * 1.4, -hr * 0.55, hr * (z + 0.3))), 0.06, hair, r1=0.0, bone="head", verts=6))
    if hs == "bun":
        P.append(T.sphere("bun", hc + Vector((0, hr * 0.95, hr * 0.25)), hr * 0.42, hair, "head", seg=12, rings=8))
    if hs == "old":  # white sides under the peci
        P.append(T.sphere("mustache", hc + Vector((0, -hr * 0.9, -hr * 0.3)), (hr * 0.42, 0.06, hr * 0.1), hair, "head", seg=12, rings=6))
    if hs == "hijab":
        hij = T.mat(name + "_hijab", s.hair, 0.85)
        P.append(T.sphere("hijab", hc + Vector((0, hr * 0.1, hr * 0.02)), (hr * 1.12, hr * 1.08, hr * 1.1), hij, "head", seg=20, rings=12))
        P.append(T.cyl("hijab_drape", hc + Vector((0, hr * 0.15, -hr * 0.6)), neck + Vector((0, 0.02, -0.22)), hr * 0.85, hij, r1=s.shoulder * 1.15, bone="chest", verts=16))
        P.append(T.torus("hijab_face", hc + Vector((0, -hr * 0.78, -hr * 0.08)), hr * 0.72, 0.05, hij, "head", rot=(math.pi / 2, 0, 0), seg=18, ring_seg=6))

    # ---- extras on the head
    if "peci" in s.extras:
        peci = T.mat("peci", 0x1B1B1F, 0.7)
        P.append(T.cyl("peci", hc + Vector((0, 0, hr * 0.62)), hc + Vector((0, 0, hr * 1.15)), hr * 0.86, peci, r1=hr * 0.8, bone="head", verts=18))
    if "glasses" in s.extras:
        frame = T.mat("glasses", 0x3A2A1A, 0.4, 0.5)
        for sx in (-1, 1):
            P.append(T.torus("lens", hc + Vector((sx * hr * 0.36, -hr * 0.98, -hr * 0.08)), hr * 0.2, 0.012, frame, "head", rot=(math.pi / 2, 0, 0), seg=12, ring_seg=4))
        P.append(T.box("bridge", hc + Vector((0, -hr * 1.0, -hr * 0.05)), (hr * 0.3, 0.015, 0.015), frame, "head"))

    # ---- neck & torso
    P.append(T.cyl("neck", neck + Vector((0, 0, -0.06)), neck + Vector((0, 0, 0.08)), 0.055 * g, skin, bone="chest", verts=10))
    torso_h = neck.z - hip.z
    P.append(T.sphere("chest", Vector((0, 0, chest.z + (neck.z - chest.z) * 0.35)), (min(s.shoulder * 0.95 * g, s.shoulder * 0.9), 0.15 * dp * g, (neck.z - chest.z) * 0.75), top, "chest", seg=16, rings=10))
    P.append(T.sphere("belly", Vector((0, 0, hip.z + torso_h * 0.38)), (s.shoulder * 0.85 * g, 0.15 * dp * g * 1.05, torso_h * 0.36), top, "spine", seg=16, rings=10))
    for sx in (-1, 1):
        sh = j["shoulder_" + ("L" if sx > 0 else "R")]
        P.append(T.sphere("shoulder", sh + Vector((-sx * 0.02, 0, -0.01)), 0.075 * g, top, "chest", seg=10, rings=8))
    if "collar" in s.extras:
        col = T.mat(name + "_collar", 0xF5F5F0, 0.8)
        for sx in (-1, 1):
            P.append(T.box("collar", neck + Vector((sx * 0.06, -0.07, -0.04)), (0.09, 0.02, 0.07), col, "chest", rot=(0.3, 0, sx * 0.5)))
    if "tie" in s.extras:
        tie = T.mat("tie_red", 0xC62828, 0.7)
        P.append(T.box("tie", Vector((0, -0.15 * dp * g - 0.005, chest.z + 0.02)), (0.05, 0.02, 0.2), tie, "chest", bevel=0.01))
    if "scarf" in s.extras:
        sc = T.mat("scarf_red", 0xC62828, 0.8)
        P.append(T.torus("scarf", neck + Vector((0, -0.01, -0.04)), 0.08, 0.025, sc, "chest", seg=14, ring_seg=6))
        P.append(T.cyl("scarf_tail", neck + Vector((0, -0.1, -0.05)), Vector((0, -0.16 * g, chest.z - 0.02)), 0.04, sc, r1=0.015, bone="chest", verts=6))
    if "apron" in s.extras:
        ap = T.mat("apron", 0xFAF6EE, 0.85)
        # flat panel following the front slope of the skirt, plus a bib
        r_top, r_bot = s.hip_w * 2.0 * g, s.hip_w * 2.7 * g
        z_top, z_bot = hip.z + 0.06, hip.z - 0.34
        t_top = (hip.z + 0.08 - z_top) / (hip.z + 0.08 - 0.12)
        t_bot = (hip.z + 0.08 - z_bot) / (hip.z + 0.08 - 0.12)
        y_top, y_bot = -(r_top + (r_bot - r_top) * t_top) - 0.012, -(r_top + (r_bot - r_top) * t_bot) - 0.012
        tilt = math.atan2(y_top - y_bot, z_top - z_bot)
        P.append(T.box("apron", Vector((0, (y_top + y_bot) / 2, (z_top + z_bot) / 2)), (s.shoulder * 1.15, 0.012, z_top - z_bot), ap, "hips",
                       rot=(tilt, 0, 0)))
    if "badge" in s.extras:
        bd = T.mat("badge", 0xE8C04A, 0.4, 0.6)
        P.append(T.box("badge", Vector((0.08, -0.155 * g, chest.z + 0.06)), (0.05, 0.015, 0.05), bd, "chest", bevel=0.005))

    # ---- arms
    sleeve = top
    for side in ("L", "R"):
        sh, el, wr, fi = (j[k + "_" + side] for k in ("shoulder", "elbow", "wrist", "fingers"))
        upper_mat = skin if s.top_sleeve == "none" else sleeve
        P.append(T.cyl("upperarm", sh, el, 0.055 * g, upper_mat, r1=0.048 * g, bone="upperarm_" + side, verts=10))
        fore_mat = sleeve if s.top_sleeve == "long" else skin
        P.append(T.cyl("forearm", el, wr, 0.046 * g, fore_mat, r1=0.04 * g, bone="forearm_" + side, verts=10))
        P.append(T.sphere("elbow", el, 0.048 * g, fore_mat if s.top_sleeve == "long" else upper_mat, "upperarm_" + side, seg=8, rings=6))
        P.append(T.sphere("hand", (wr + fi) * 0.5, (0.055, 0.045, 0.065), skin, "hand_" + side, seg=10, rings=8))

    # ---- legs
    for side in ("L", "R"):
        hp, kn, an, to = (j[k + "_" + side] for k in ("hipj", "knee", "ankle", "toe"))
        thigh_mat = bottom if s.bottom_style in ("pants", "shorts", "skirt", "sarung") else skin
        shin_mat = bottom if s.bottom_style in ("pants", "skirt", "sarung") else skin
        if s.bottom_style == "shorts":
            P.append(T.cyl("thigh", hp + Vector((0, 0, 0.04)), kn + (hp - kn) * 0.3, 0.085 * g, bottom, r1=0.075 * g, bone="thigh_" + side, verts=10))
            P.append(T.cyl("thigh_skin", kn + (hp - kn) * 0.35, kn, 0.055 * g, skin, bone="thigh_" + side, verts=10))
        else:
            P.append(T.cyl("thigh", hp + Vector((0, 0, 0.04)), kn, 0.07 * g, thigh_mat, r1=0.06 * g, bone="thigh_" + side, verts=10))
        P.append(T.sphere("knee", kn, 0.058 * g, shin_mat, "shin_" + side, seg=8, rings=6))
        P.append(T.cyl("shin", kn, an + Vector((0, 0, 0.03)), 0.056 * g, shin_mat, r1=0.045 * g, bone="shin_" + side, verts=10))
        if s.socks:
            P.append(T.cyl("sock", an + Vector((0, 0, 0.0)), an + Vector((0, 0, 0.12)), 0.047 * g, T.mat("socks", s.socks, 0.9), bone="shin_" + side, verts=10))
        P.append(T.box("shoe", (an + to) * 0.5 + Vector((0, 0, -0.01)), (0.11 * g, 0.24, 0.09), shoes, "foot_" + side, bevel=0.035))

    # ---- hips / skirts
    P.append(T.sphere("hips", hip + Vector((0, 0, 0.03)), (s.hip_w * 1.9 * g, 0.15 * dp * g, 0.12), bottom if isinstance(bottom, type(skin)) else skin, "hips", seg=14, rings=8))
    if s.bottom_style in ("skirt", "sarung"):
        P.append(T.cyl("skirt", hip + Vector((0, 0, 0.08)), Vector((0, 0.0, 0.12)), s.hip_w * 2.0 * g, bottom, r1=s.hip_w * 2.7 * g,
                       bone="hips", verts=18))
    if "belt" in s.extras:
        P.append(T.torus("belt", hip + Vector((0, 0, 0.1)), s.hip_w * 1.85 * g, 0.022, T.mat("belt", 0x2A1E14, 0.5), "hips", seg=16, ring_seg=4))

    for hook in s.hooks:
        P.extend(p for p in hook(name, s, j, hc, hr) if p is not None)

    mesh = T.join(P, name)
    T.skin(mesh, arm)
    if not s.weapons:
        return arm, mesh

    # ---- weapons in the right hand, all exported, the game shows one at a time
    grip_pos = (j["wrist_R"] + j["fingers_R"]) * 0.5
    for key, fn in W.BUILDERS.items():
        wobj = fn("W_" + key)
        rot = W.GRIP.get(key, W.DEFAULT_GRIP)
        wobj.matrix_world = Matrix.Translation(grip_pos) @ rot
        T.attach_to_bone(wobj, arm, "hand_R")
    return arm, mesh


FAMILY = {
    "bapak": Spec(height=1.72, head=0.5, shoulder=0.25, leg=0.62, arm=0.56, girth=1.08, skin=SKIN["tan"], hair=0x1E1611,
                  hair_style="short", top="batik", top_sleeve="short", bottom=0x24242A, shoes=0x1C1C1C, extras=("belt",),
                  weapon="bambu"),
    "ibu": Spec(height=1.62, head=0.49, shoulder=0.22, hip_w=0.1, leg=0.56, arm=0.52, girth=0.98, skin=SKIN["light"], hair=0x2A1A12,
                hair_style="bun", top="kebaya", top_sleeve="long", bottom="batik_skirt", bottom_style="skirt", shoes=0xE0578E,
                extras=("apron",), blush=0.8, weapon="wajan"),
    "kakak": Spec(height=1.42, head=0.47, shoulder=0.2, hip_w=0.085, leg=0.48, arm=0.46, girth=0.9, skin=SKIN["tan"], hair=0x2B1D14,
                  hair_style="kid", top=0xF7F7F2, top_sleeve="short", bottom=0xB3202A, bottom_style="shorts", shoes=0x2A2A2A,
                  socks=0xFFFFFF, extras=("tie", "collar"), bounce=1.3, weapon="pentungan"),
    "ade": Spec(height=1.22, head=0.46, shoulder=0.18, hip_w=0.08, leg=0.4, arm=0.4, girth=0.85, skin=SKIN["tan"], hair=0x3A2616,
                hair_style="kid", top=0xA0784A, top_sleeve="short", bottom=0x5B3D22, bottom_style="shorts", shoes=0x4A2E1B,
                socks=0x6B4E2E, extras=("scarf", "badge", "belt"), bounce=1.5, weapon="pentungan"),
    "kake": Spec(height=1.56, head=0.47, shoulder=0.21, leg=0.54, arm=0.5, girth=0.92, skin=SKIN["old"], hair=0xEDEDED,
                 hair_style="old", top=0xF4F4EE, top_sleeve="short", bottom="sarung", bottom_style="sarung", shoes=0x6B4A2E,
                 extras=("peci", "glasses"), brows=0xE8E8E8, bounce=0.6, weapon="pentungan",
                 posture=anims.Posture(spine=(10, 0, 0), chest=(8, 0, 0), head=(-14, 0, 0))),
    "nene": Spec(height=1.5, head=0.47, shoulder=0.2, hip_w=0.095, leg=0.5, arm=0.48, girth=0.95, skin=SKIN["old"], hair=0xA566C9,
                 hair_style="hijab", top=0xE58FA8, top_sleeve="long", bottom=0x7A4B32, bottom_style="skirt", shoes=0x9A6E4E,
                 extras=("glasses",), brows=0x8A8A8A, blush=0.7, bounce=0.6, weapon="sapu",
                 posture=anims.Posture(spine=(9, 0, 0), chest=(6, 0, 0), head=(-12, 0, 0))),
}


def resolve_fabrics(name, s):
    if s.top == "batik":
        s.top = T.mat(name + "_batik", 0xFFFFFF, 0.8, image=tex_batik(name + "_batik_tex", 0x6B3A1E, 0x2A160B, 0xD9A55B))
    elif s.top == "kebaya":
        s.top = T.mat(name + "_kebaya", 0xFFFFFF, 0.8, image=tex_flowers(name + "_kebaya_tex", 0x6FC7C0, [0xE84C8B, 0xF2B632, 0x8E5BD6, 0x3E8FE0]))
    if s.bottom == "batik_skirt":
        s.bottom = T.mat(name + "_skirt", 0xFFFFFF, 0.8, image=tex_batik(name + "_skirt_tex", 0x8C2F39, 0x3A1016, 0xF0C35A))
    elif s.bottom == "sarung":
        s.bottom = T.mat(name + "_sarung", 0xFFFFFF, 0.8, image=tex_plaid(name + "_sarung_tex", 0x3E8E4E, 0x1E4D2A, 0xB9E08A))


def build(name, export=True, preview=None):
    T.clear_scene()
    T.reset_materials()
    s = FAMILY[name]
    resolve_fabrics(name, s)
    arm, mesh = build_human(name, s)
    clips = anims.build_family(arm, s.posture, s.bounce)
    if preview:
        T.preview(preview, target=(0, 0, s.height * 0.5), distance=4.2, height=0.6)
    path = T.export_glb("char_" + name, [arm]) if export else None
    return {"clips": clips, "path": path}


def build_all():
    return {name: build(name) for name in FAMILY}
