"""Warga Kampung Damai: the villagers who fill the title screen and the opening
story. Same chibi builder and clips as the family, so they can carry tools
(sapu, pacul) and run away from zombies."""

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
    """Woven basket carried on the back."""
    m = T.mat("bakul", 0xB08040, 0.9)
    c = j["chest"] + Vector((0, 0.24, 0.02))
    return [T.cyl("bakul", c + Vector((0, 0, -0.18)), c + Vector((0, 0, 0.16)), 0.13, m, r1=0.17, bone="chest", verts=14)]


VILLAGERS = {
    "petani": C.Spec(height=1.66, head=0.49, shoulder=0.24, leg=0.6, arm=0.54, girth=1.0, skin=C.SKIN["brown"], hair=0x1E1611,
                     top=0x5E7F3A, top_sleeve="long", bottom=0x3B2F24, shoes=0x4A3322, extras=("belt",), weapon="pacul",
                     hooks=(caping_hook,)),
    "pedagang": C.Spec(height=1.58, head=0.49, shoulder=0.22, hip_w=0.1, leg=0.55, arm=0.5, girth=1.12, skin=C.SKIN["light"], hair=0x3FA7A0,
                       hair_style="hijab", top=0xF29A38, top_sleeve="long", bottom=0x6A3A7A, bottom_style="skirt", shoes=0x5A3A2A,
                       extras=("apron",), blush=0.8, weapon="sapu", hooks=(bakul_hook,)),
    "ustad": C.Spec(height=1.7, head=0.49, shoulder=0.24, leg=0.6, arm=0.54, girth=0.96, skin=C.SKIN["tan"], hair=0x1E1611,
                    top=0xF6F6F0, top_sleeve="long", bottom="sarung", bottom_style="sarung", shoes=0x3A2A1E,
                    extras=("peci",), weapon="sapu"),
    "bocah": C.Spec(height=1.18, head=0.46, shoulder=0.18, hip_w=0.08, leg=0.38, arm=0.4, girth=0.86, skin=C.SKIN["tan"], hair=0x1A120C,
                    hair_style="kid", top=0xE23B3B, top_sleeve="short", bottom=0x2E4A8A, bottom_style="shorts", shoes=0x2A2A2A,
                    bounce=1.6, weapon="bambu"),
}


def build(name, export=True, preview=None):
    T.clear_scene()
    T.reset_materials()
    s = VILLAGERS[name]
    C.resolve_fabrics(name, s)
    arm, mesh = C.build_human(name, s)
    clips = anims.build_family(arm, s.posture, s.bounce)
    if preview:
        T.preview(preview, target=(0, 0, s.height * 0.5), distance=4.2, height=0.6)
    path = T.export_glb("npc_" + name, [arm]) if export else None
    return {"clips": clips, "path": path}


def build_all():
    return {name: build(name) for name in VILLAGERS}
