"""Warga Kampung Damai: the villagers who fill the title screen, the opening
story and the levels. Same builder, proportions and clips as the family, so
they can carry tools (sapu, pacul, pentungan) and run away from zombies.

Professions: petani, pedagang sayur, pak ustad, bocah, hansip (ronda),
tukang bakso, mbok jamu gendong, tukang ojek and bu guru.
"""

from mathutils import Vector

import anims
import btk as T
import characters as C


def caping_hook(name, s, j, hc, hr):
    """Conical bamboo farmer hat."""
    m = T.mat("caping", 0xD9B26A, 0.85)
    band = T.mat("caping_band", 0x8A5A2B, 0.8)
    return [T.cyl("caping", hc + Vector((0, 0, hr * 0.55)), hc + Vector((0, 0, hr * 1.45)), hr * 2.1, m, r1=0.0, bone="head", verts=20),
            T.cyl("caping_band", hc + Vector((0, 0, hr * 0.55)), hc + Vector((0, 0, hr * 0.62)), hr * 2.12, band, bone="head", verts=20)]


def bakul_hook(name, s, j, hc, hr):
    """Woven basket carried on the back, full of vegetables."""
    m = T.mat("bakul", 0xB08040, 0.9)
    c = j["chest"] + Vector((0, 0.2, 0.02))
    parts = [T.cyl("bakul", c + Vector((0, 0, -0.18)), c + Vector((0, 0, 0.14)), 0.12, m, r1=0.16, bone="chest", verts=14)]
    for i, (col, dx) in enumerate(((0x4CAF50, -0.05), (0xE53935, 0.04), (0xFF9800, 0.0))):
        parts.append(T.sphere("sayur", c + Vector((dx, 0.0, 0.15 + i * 0.01)), 0.055, T.mat("sayur%d" % i, col, 0.7), "chest", seg=8, rings=6))
    return parts


def jamu_hook(name, s, j, hc, hr):
    """Mbok jamu: a slung selendang holding a basket of bottles on the back."""
    kain = T.mat("selendang", 0xD84315, 0.8)
    bakul = T.mat("bakul_jamu", 0xA87838, 0.9)
    glass = T.mat("botol_jamu", 0xF2C14E, 0.2, emit=0xB07A10, strength=0.3)
    c = j["chest"] + Vector((0, 0.19, -0.04))
    parts = [
        T.torus("selendang", j["chest"] + Vector((0, 0.02, 0.06)), s.shoulder * 1.05, 0.03, kain, "chest", rot=(0.5, 0.6, 0), seg=16, ring_seg=6),
        T.cyl("bakul", c + Vector((0, 0, -0.14)), c + Vector((0, 0, 0.08)), 0.15, bakul, r1=0.17, bone="chest", verts=14),
    ]
    for i in range(5):
        dx = (i - 2) * 0.05
        parts.append(T.cyl("botol", c + Vector((dx, 0.0, 0.05)), c + Vector((dx, 0.0, 0.24)), 0.022, glass, bone="chest", verts=8))
    return parts


def helmet_hook(color, visor=True):
    def hook(name, s, j, hc, hr):
        m = T.mat(name + "_helmet", color, 0.35, 0.1)
        parts = [T.sphere("helmet", hc + Vector((0, hr * 0.08, hr * 0.22)), (hr * 1.12, hr * 1.14, hr * 0.98), m, "head", seg=18, rings=10)]
        if visor:
            parts.append(T.box("visor", hc + Vector((0, -hr * 0.95, hr * 0.55)), (hr * 1.2, hr * 0.5, 0.02), T.mat("visor", 0x2A2A30, 0.3), "head",
                               rot=(0.25, 0, 0)))
        return parts
    return hook


def cap_hook(color, badge=0xE8C04A):
    def hook(name, s, j, hc, hr):
        m = T.mat(name + "_cap", color, 0.7)
        return [T.cyl("cap", hc + Vector((0, 0, hr * 0.45)), hc + Vector((0, 0, hr * 1.05)), hr * 1.02, m, r1=hr * 1.05, bone="head", verts=16),
                T.box("brim", hc + Vector((0, -hr * 0.95, hr * 0.5)), (hr * 1.3, hr * 0.6, 0.03), m, "head", rot=(0.15, 0, 0), bevel=0.01),
                T.box("cap_badge", hc + Vector((0, -hr * 1.03, hr * 0.78)), (0.06, 0.02, 0.06), T.mat("badge", badge, 0.4, 0.6), "head")]
    return hook


def chef_hat_hook(name, s, j, hc, hr):
    m = T.mat("topi_koki", 0xFFFFFF, 0.8)
    return [T.cyl("chef_hat", hc + Vector((0, 0, hr * 0.6)), hc + Vector((0, 0, hr * 1.25)), hr * 0.85, m, r1=hr * 0.95, bone="head", verts=16)]


def armband_hook(name, s, j, hc, hr):
    """Hansip: a yellow armband with the Linmas triangle."""
    m = T.mat("armband", 0xF2C94C, 0.6)
    el = j["elbow_L"]
    sh = j["shoulder_L"]
    c = sh + (el - sh) * 0.45
    return [T.cyl("armband", c + Vector((0, 0, 0.03)), c - Vector((0, 0, 0.03)), 0.062, m, bone="upperarm_L", verts=10)]


def whistle_hook(name, s, j, hc, hr):
    m = T.mat("peluit", 0xC0C0C8, 0.3, 0.8)
    return [T.cyl("peluit", j["neck"] + Vector((0.03, -0.1, -0.12)), j["neck"] + Vector((0.03, -0.14, -0.14)), 0.012, m, bone="chest", verts=6)]


def book_hook(name, s, j, hc, hr):
    """Bu guru carries a book in the left hand."""
    m = T.mat("buku", 0x2E6FD8, 0.6)
    hand = (j["wrist_L"] + j["fingers_L"]) * 0.5
    return [T.box("buku", hand + Vector((0.02, -0.05, 0.02)), (0.04, 0.16, 0.22), m, "hand_L", bevel=0.005)]


VILLAGERS = {
    "petani": C.Spec(height=1.68, head=0.39, shoulder=0.22, leg=0.76, arm=0.65, girth=1.0, skin=C.SKIN["brown"], hair=0x1E1611,
                     top=0x5E7F3A, top_sleeve="long", bottom=0x3B2F24, shoes=0x4A3322, extras=("belt", "shirt_collar"), weapon="pacul",
                     hooks=(caping_hook,)),
    "pedagang": C.Spec(height=1.58, head=0.38, shoulder=0.21, hip_w=0.1, leg=0.7, arm=0.6, girth=1.1, skin=C.SKIN["light"], hair=0x3FA7A0,
                       hair_style="hijab", top=0xF29A38, top_sleeve="long", bottom=0x6A3A7A, bottom_style="skirt", shoes=0x5A3A2A,
                       extras=("apron",), blush=0.8, weapon="sapu", hooks=(bakul_hook,)),
    "ustad": C.Spec(height=1.72, head=0.39, shoulder=0.22, leg=0.77, arm=0.66, girth=0.96, skin=C.SKIN["tan"], hair=0x1E1611,
                    top=0xF6F6F0, top_sleeve="long", bottom="sarung", bottom_style="sarung", shoes=0x3A2A1E,
                    extras=("peci", "shirt_collar"), weapon="sapu"),
    "bocah": C.Spec(height=1.2, head=0.36, shoulder=0.16, hip_w=0.075, leg=0.5, arm=0.46, girth=0.86, skin=C.SKIN["tan"], hair=0x1A120C,
                    hair_style="kid", top=0xE23B3B, top_sleeve="short", bottom=0x2E4A8A, bottom_style="shorts", shoes=0x2A2A2A,
                    bounce=1.6, weapon="bambu"),
    "hansip": C.Spec(height=1.74, head=0.39, shoulder=0.23, leg=0.79, arm=0.67, girth=1.08, skin=C.SKIN["tan"], hair=0x1E1611,
                     top=0x556B2F, top_sleeve="long", bottom=0x4A5A28, shoes=0x1C1C1C, extras=("belt", "shirt_collar"), weapon="pentungan",
                     hooks=(cap_hook(0x3E4F22), armband_hook, whistle_hook)),
    "bakso": C.Spec(height=1.66, head=0.39, shoulder=0.22, leg=0.75, arm=0.64, girth=1.12, skin=C.SKIN["tan"], hair=0x1E1611,
                    top=0xFAFAF5, top_sleeve="short", bottom=0x2F3B5C, shoes=0x3A2A1E, extras=("apron", "shirt_collar"), weapon="pentungan",
                    hooks=(chef_hat_hook,)),
    "jamu": C.Spec(height=1.56, head=0.38, shoulder=0.2, hip_w=0.1, leg=0.69, arm=0.6, girth=1.0, skin=C.SKIN["brown"], hair=0x1A120C,
                   hair_style="bun", top="kebaya_jamu", top_sleeve="long", bottom="batik_jamu", bottom_style="skirt", shoes=0x6A4A2A,
                   blush=0.6, weapon="sapu", hooks=(jamu_hook,)),
    "ojek": C.Spec(height=1.7, head=0.39, shoulder=0.23, leg=0.77, arm=0.66, girth=1.0, skin=C.SKIN["tan"], hair=0x1E1611,
                   top=0x2E8B3A, top_sleeve="long", bottom=0x2A2F3A, shoes=0x1C1C1C, extras=("shirt_collar",), weapon="pentungan",
                   hooks=(helmet_hook(0x2E8B3A),)),
    "guru": C.Spec(height=1.62, head=0.38, shoulder=0.2, hip_w=0.1, leg=0.73, arm=0.61, girth=0.95, skin=C.SKIN["light"], hair=0x2A1A12,
                   hair_style="bun", top="batik_guru", top_sleeve="long", bottom=0x3A3A5A, bottom_style="skirt", shoes=0x2A2A2A,
                   extras=("glasses",), blush=0.7, weapon="sapu", hooks=(book_hook,)),
}


def resolve_fabrics(name, s):
    """Villager-specific cloths on top of the family ones."""
    if s.top == "kebaya_jamu":
        s.top = T.mat(name + "_kebaya", 0xFFFFFF, 0.8, image=C.tex_flowers(name + "_kebaya_tex", 0xF4E3B0, [0xC62828, 0x2E7D32, 0xF9A825]))
    elif s.top == "batik_guru":
        s.top = T.mat(name + "_batik", 0xFFFFFF, 0.8, image=C.tex_batik(name + "_batik_tex", 0x2F5D8A, 0x0F2A44, 0xE0C070))
    if s.bottom == "batik_jamu":
        s.bottom = T.mat(name + "_jarik", 0xFFFFFF, 0.8, image=C.tex_batik(name + "_jarik_tex", 0x6B3A1E, 0x2A160B, 0xD9A55B))
    C.resolve_fabrics(name, s)


def build(name, export=True, preview=None):
    T.clear_scene()
    T.reset_materials()
    s = VILLAGERS[name]
    resolve_fabrics(name, s)
    arm, mesh = C.build_human(name, s)
    clips = anims.build_family(arm, s.posture, s.bounce)
    if preview:
        T.preview(preview, target=(0, 0, s.height * 0.5), distance=4.2, height=0.6)
    path = T.export_glb("npc_" + name, [arm]) if export else None
    return {"clips": clips, "path": path}


def build_all():
    return {name: build(name) for name in VILLAGERS}
