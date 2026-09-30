"""Bos level 5-10 (art/boss-level-5..10.png):

5  Jeng Roro Kembang Malam  - penguasa sumur tua: four-armed hair spirit rising from a well
6  Kuntilanak Penguasa      - five crowned heads, a floating ragged robe
7  Genderuwo Raja           - a giant ape king with a crown and a spiked club
8  Kraken Raja              - an octopus king with eight tentacles
9  Leviathan Kuno           - a three-headed ancient sea serpent
10 Demon King Abyss         - a winged, horned demon with dragon heads on its shoulders

Every boss exports the zombie clip names the game blends: idle, walk, run,
spawn, die (base) and attack, cast, hit (actions)."""

import math

from mathutils import Vector

import anims
import btk as T
import characters as C
import zanims
import zombies as Z
from animals import eyes, spots_texture


def _mats(prefix, skin, cloth, glow):
    return (T.mat(prefix + "_skin", skin, 0.7), T.mat(prefix + "_cloth", cloth, 0.9),
            T.mat(prefix + "_glow", glow, 0.3, emit=glow, strength=6.0),
            T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3), T.mat("eye_dark", 0x1A120C, 0.3))


def _finish(name, arm, clips, export):
    names = []
    for clip, loop in clips:
        clip.build(loop=loop)
        names.append(clip.name)
    path = T.export_glb("zombie_" + name, [arm]) if export else None
    return {"clips": names, "path": path}


# ======================================================================= 5. Jeng Roro

def build_jeng_roro(name="jeng_roro"):
    skin, cloth, glow, white, dark = _mats(name, 0xB8A8C0, 0x5A4636, 0xFFD24A)
    hair = T.mat(name + "_hair", 0x16101C, 0.6)
    stone = T.mat("sumur_batu", 0xFFFFFF, 0.95, image=Z.tex_torn("sumur_batu_tex", 0x6E7268, 0x4A5A3E))
    water = T.mat("air_sumur", 0x1E4A4A, 0.1, emit=0x2A6A5A, strength=0.6)
    bone = T.mat("skull", 0xEDE6CF, 0.6)
    wood = T.mat("ember_kayu", 0x6B4A2E, 0.8)
    lamp = T.mat("lentera", 0xFFE08A, 0.3, emit=0xFFC04A, strength=8.0)
    bz = 1.0
    bones = [
        ("root", (0, 0, 0), (0, 0, 0.3), None),
        ("hips", (0, 0, bz - 0.4), (0, 0, bz + 0.4), "root"),
        ("spine", (0, 0, bz + 0.4), (0, 0, bz + 1.1), "hips"),
        ("chest", (0, 0, bz + 1.1), (0, 0, bz + 1.7), "spine"),
        ("head", (0, 0, bz + 1.7), (0, 0, bz + 2.3), "chest"),
        ("hair_B", (0, 0.2, bz + 2.2), (0, 0.8, bz + 0.6), "head"),
        ("hair_L", (0.25, 0.1, bz + 2.2), (1.1, 0.3, bz + 0.9), "head"),
        ("hair_R", (-0.25, 0.1, bz + 2.2), (-1.1, 0.3, bz + 0.9), "head"),
    ]
    arms = []
    for idx, (z, spread) in enumerate(((bz + 1.55, 0.35), (bz + 1.2, 0.3))):
        for side, sx in (("L", 1), ("R", -1)):
            sh = Vector((sx * spread, 0, z))
            el = sh + Vector((sx * 0.55, -0.15, -0.25))
            wr = el + Vector((sx * 0.35, -0.45, 0.05))
            tag = "%s%d" % (side, idx)
            bones.append(("upperarm_" + tag, tuple(sh), tuple(el), "chest"))
            bones.append(("forearm_" + tag, tuple(el), tuple(wr), "upperarm_" + tag))
            arms.append((tag, sx, sh, el, wr))
    arm = T.build_custom_armature(name + "_rig", bones)
    P = [
        # the well itself: stays put on the root while she rises and sinks
        T.cyl("well", (0, 0, 0), (0, 0, 1.0), 1.25, stone, verts=20),
        T.cyl("well_in", (0, 0, 0.8), (0, 0, 1.01), 1.05, water, verts=20),
        T.torus("well_lip", (0, 0, 1.0), 1.2, 0.1, stone, "root", seg=24, ring_seg=6),
        T.cyl("rope", (0.9, -0.3, 1.0), (0.9, -0.3, 0.2), 0.03, T.mat("tali", 0xB9A57A, 0.9), bone="root", verts=5),
        # body in a torn kebaya
        T.cyl("torso_low", (0, 0, bz - 0.3), (0, 0, bz + 0.5), 0.42, cloth, r1=0.36, bone="hips", verts=14),
        T.sphere("torso", (0, 0, bz + 0.85), (0.38, 0.3, 0.45), cloth, "spine", seg=14, rings=10),
        T.sphere("chest", (0, 0, bz + 1.4), (0.42, 0.3, 0.32), cloth, "chest", seg=14, rings=10),
        T.cyl("neck", (0, 0, bz + 1.6), (0, 0, bz + 1.8), 0.1, skin, bone="head", verts=10),
        T.sphere("head", (0, -0.03, bz + 2.0), (0.26, 0.26, 0.3), skin, "head", seg=16, rings=12),
        T.sphere("mouth", (0, -0.27, bz + 1.9), (0.1, 0.03, 0.05), T.mat("zombie_mouth", 0x3A1420, 0.6), "head", seg=8, rings=5),
        # a huge mane of hair spilling over the well
        T.sphere("hair_cap", (0, 0.06, bz + 2.08), (0.33, 0.33, 0.33), hair, "head", seg=16, rings=10),
        T.cyl("hair_back", (0, 0.2, bz + 2.1), (0, 0.9, bz + 0.5), 0.35, hair, r1=0.6, bone="hair_B", verts=12),
        T.cyl("hair_left", (0.2, 0.05, bz + 2.1), (1.2, 0.3, bz + 0.8), 0.22, hair, r1=0.45, bone="hair_L", verts=10),
        T.cyl("hair_right", (-0.2, 0.05, bz + 2.1), (-1.2, 0.3, bz + 0.8), 0.22, hair, r1=0.45, bone="hair_R", verts=10),
    ]
    for k in range(4):
        a = (k - 1.5) * 0.5
        P.append(T.sphere("hair_skull", (math.sin(a) * 0.3, -0.05, bz + 2.33 + (0.04 if k % 2 else 0)), 0.07, bone, "head", seg=8, rings=6))
    eyes(P, Vector((0, -0.03, bz + 2.02)), 0.26, 0.85, 0.35, glow, dark, "head", size=0.2, up=0.1)
    for tag, sx, sh, el, wr in arms:
        P.append(T.cyl("upperarm", sh, el, 0.07, skin, r1=0.06, bone="upperarm_" + tag, verts=8))
        P.append(T.cyl("forearm", el, wr, 0.06, skin, r1=0.05, bone="forearm_" + tag, verts=8))
        for k in range(3):
            base = wr + Vector(((k - 1) * 0.04, -0.02, 0))
            P.append(T.cyl("claw", base, base + Vector((sx * 0.05, -0.18, -0.05 - k * 0.02)), 0.018, T.mat("kuku", 0x3A2A30, 0.5), r1=0.0,
                           bone="forearm_" + tag, verts=5))
    # bucket in a lower hand, a lantern in an upper hand (as in the art)
    wr_bucket = [w for t, s, a, b, w in arms if t == "R1"][0]
    P.append(T.cyl("bucket", wr_bucket + Vector((0, -0.05, -0.35)), wr_bucket + Vector((0, -0.05, -0.05)), 0.15, wood, r1=0.18, bone="forearm_R1", verts=12))
    wr_lamp = [w for t, s, a, b, w in arms if t == "L0"][0]
    P.append(T.cyl("lamp_frame", wr_lamp + Vector((0, -0.05, -0.38)), wr_lamp + Vector((0, -0.05, -0.08)), 0.1, T.mat("besi", 0x3A3C40, 0.4, 0.8),
                   bone="forearm_L0", verts=6))
    P.append(T.sphere("lamp", wr_lamp + Vector((0, -0.05, -0.23)), 0.085, lamp, "forearm_L0", seg=10, rings=8))
    mesh = T.join(P, name)
    T.skin(mesh, arm)

    arms_rest = {}
    arms_up = {}
    arms_slam = {}
    for tag, sx, *_ in arms:
        arms_rest["upperarm_" + tag] = (0, 0, 0)
        arms_rest["forearm_" + tag] = (0, 0, 0)
        arms_up["upperarm_" + tag] = (-40, -sx * 50, 0)
        arms_up["forearm_" + tag] = (-30, 0, 0)
        arms_slam["upperarm_" + tag] = (35, sx * 20, 0)
        arms_slam["forearm_" + tag] = (30, 0, 0)
    clips = []
    c = T.Clip(arm, "idle", 72)
    c.pose(0, spine=(0, 0, 6), chest=(0, 0, -4), head=(0, 0, 8), hair_L=(0, 10, 0), hair_R=(0, -10, 0), **arms_rest)
    c.pose(36, spine=(0, 0, -6), chest=(4, 0, 4), head=(-6, 0, -10), hair_L=(0, -8, 0), hair_R=(0, 8, 0),
           **{k: (v[0] - 12, v[1], v[2] + 10) for k, v in arms_rest.items()})
    c.move(0, "hips", (0, 0, 0)).move(36, "hips", (0, 0, 0.12))
    clips.append((c, True))
    for nm, frames in (("walk", 40), ("run", 24)):
        c = T.Clip(arm, nm, frames)
        c.pose(0, spine=(8, 0, 10), head=(0, 0, 10), **arms_rest)
        c.pose(frames // 2, spine=(8, 0, -10), head=(0, 0, -10), **{k: (v[0] - 25, v[1], v[2]) for k, v in arms_rest.items()})
        c.move(0, "hips", (0, 0, 0)).move(frames // 2, "hips", (0, 0, 0.2))
        clips.append((c, True))
    c = T.Clip(arm, "spawn", 48)
    c.pose(0, head=(-30, 0, 0), **arms_up).pose(30, head=(10, 0, 0)).pose(48, head=(0, 0, 0), **arms_rest)
    c.move(0, "hips", (0, 0, -2.4)).move(34, "hips", (0, 0, 0.2)).move(48, "hips", (0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "die", 48)
    c.pose(0, **arms_rest).pose(16, head=(-40, 0, 0), chest=(-20, 0, 0), **arms_up).pose(48, head=(-40, 0, 0), **arms_up)
    c.move(0, "hips", (0, 0, 0)).move(16, "hips", (0, 0, 0.3)).move(48, "hips", (0, 0, -2.6))
    clips.append((c, False))
    action_bones = ["spine", "chest", "head", "hair_L", "hair_R", "hair_B"] + list(arms_rest)
    c = T.Clip(arm, "attack", 22, action_bones)
    c.pose(0, chest=(0, 0, 0), **arms_rest).pose(8, chest=(-20, 0, 0), **arms_up).pose(13, chest=(30, 0, 0), **arms_slam)
    c.pose(22, chest=(0, 0, 0), **arms_rest)
    clips.append((c, False))
    c = T.Clip(arm, "cast", 30, action_bones)
    c.pose(0, head=(0, 0, 0), hair_L=(0, 0, 0), hair_R=(0, 0, 0), hair_B=(0, 0, 0), **arms_rest)
    c.pose(10, head=(-35, 0, 0), chest=(-15, 0, 0), hair_L=(0, -60, 0), hair_R=(0, 60, 0), hair_B=(-50, 0, 0), **arms_up)
    c.pose(30, head=(0, 0, 0), chest=(0, 0, 0), hair_L=(0, 0, 0), hair_R=(0, 0, 0), hair_B=(0, 0, 0), **arms_rest)
    clips.append((c, False))
    c = T.Clip(arm, "hit", 12, action_bones)
    c.pose(0, chest=(0, 0, 0), head=(0, 0, 0)).pose(4, chest=(-20, 0, 12), head=(-25, 0, -15)).pose(12, chest=(0, 0, 0), head=(0, 0, 0))
    clips.append((c, False))
    return arm, clips


# ======================================================================= 6. Kuntilanak Penguasa

def build_kunti_penguasa(name="kunti_penguasa"):
    skin, cloth, glow, white, dark = _mats(name, 0xC8D8C8, 0x7A8A78, 0xFF2A2A)
    hair = T.mat(name + "_hair", 0x0E140E, 0.6)
    wisp = T.mat(name + "_wisp", 0x5AFF9A, 0.4, emit=0x3AFF7A, strength=4.0)
    gold = T.mat("gold", 0xE0B040, 0.35, 0.8)
    gem = T.mat("permata_merah", 0xFF2A3A, 0.2, emit=0xFF1A2A, strength=5.0)
    rag = T.mat(name + "_rag", 0xFFFFFF, 0.9, image=spots_texture(name + "_rag_tex", 0x8E9A86, 0x5E6A56, scale=6.0, seed=2))
    bz = 1.4
    bones = [
        ("root", (0, 0, 0), (0, 0, 0.3), None),
        ("hips", (0, 0, bz - 0.6), (0, 0, bz + 0.4), "root"),
        ("chest", (0, 0, bz + 0.4), (0, 0, bz + 1.4), "hips"),
        ("head", (0, 0, bz + 1.4), (0, 0, bz + 1.95), "chest"),
        ("upperarm_L", (0.55, 0, bz + 1.25), (1.05, -0.2, bz + 0.8), "chest"),
        ("forearm_L", (1.05, -0.2, bz + 0.8), (1.25, -0.75, bz + 0.7), "upperarm_L"),
        ("upperarm_R", (-0.55, 0, bz + 1.25), (-1.05, -0.2, bz + 0.8), "chest"),
        ("forearm_R", (-1.05, -0.2, bz + 0.8), (-1.25, -0.75, bz + 0.7), "upperarm_R"),
    ]
    side_heads = []
    for k, (x, z, y) in enumerate(((0.45, 1.3, 0.05), (0.85, 1.05, 0.15), (-0.45, 1.3, 0.05), (-0.85, 1.05, 0.15))):
        tag = "head_%d" % k
        base = Vector((x * 0.6, y, bz + z - 0.2))
        top = Vector((x, y, bz + z + 0.25))
        bones.append((tag, tuple(base), tuple(top), "chest"))
        side_heads.append((tag, base, top))
    arm = T.build_custom_armature(name + "_rig", bones)
    P = [
        T.cyl("robe", (0, 0, 0.15), (0, 0, bz + 0.5), 0.95, rag, r1=0.45, bone="hips", verts=18),
        T.sphere("robe_hem", (0, 0, 0.2), (0.95, 0.95, 0.2), rag, "hips", seg=18, rings=8),
        T.sphere("torso", (0, 0, bz + 0.9), (0.55, 0.38, 0.6), cloth, "chest", seg=16, rings=10),
    ]

    def head_parts(c, r, bone):
        parts = [T.sphere("head", c, (r, r, r * 1.1), skin, bone, seg=14, rings=10),
                 T.sphere("hair", c + Vector((0, r * 0.25, r * 0.1)), (r * 1.1, r * 1.05, r * 1.1), hair, bone, seg=14, rings=10),
                 T.cyl("hair_fall", c + Vector((0, r * 0.4, 0)), c + Vector((0, r * 0.9, -r * 3.0)), r * 0.9, hair, r1=r * 1.3, bone=bone, verts=10),
                 T.sphere("mouth", c + Vector((0, -r * 0.9, -r * 0.45)), (r * 0.35, r * 0.1, r * 0.3), T.mat("zombie_mouth", 0x3A1420, 0.6), bone,
                          seg=8, rings=5),
                 T.cyl("crown", c + Vector((0, 0, r * 0.7)), c + Vector((0, 0, r * 1.3)), r * 0.75, gold, r1=r * 0.9, bone=bone, verts=10),
                 T.sphere("crown_gem", c + Vector((0, -r * 0.78, r * 1.0)), r * 0.15, gem, bone, seg=8, rings=6)]
        eyes(parts, c, r, 0.9, 0.38, glow, dark, bone, size=0.22, up=0.05)
        for k in range(3):
            a = (k - 1) * 0.6
            base = c + Vector((math.sin(a) * r, r * 0.5, r * 0.9))
            parts.append(T.cyl("wisp", base, base + Vector((math.sin(a) * r * 0.8, r * 0.8, r * 1.4)), r * 0.15, wisp, r1=0.0, bone=bone, verts=5))
        return parts

    P += head_parts(Vector((0, -0.05, bz + 1.65)), 0.3, "head")
    for tag, base, top in side_heads:
        P.append(T.cyl("neck_stalk", base, top, 0.08, skin, r1=0.07, bone=tag, verts=8))
        P += head_parts(top + Vector((0, -0.02, 0.1)), 0.22, tag)
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr = Vector((sx * 0.55, 0, bz + 1.25)), Vector((sx * 1.05, -0.2, bz + 0.8)), Vector((sx * 1.25, -0.75, bz + 0.7))
        P.append(T.cyl("sleeve", sh, el, 0.16, rag, r1=0.2, bone="upperarm_" + side, verts=10))
        P.append(T.cyl("forearm", el, wr, 0.06, skin, r1=0.05, bone="forearm_" + side, verts=8))
        for k in range(4):
            base = wr + Vector(((k - 1.5) * 0.04, -0.02, 0))
            P.append(T.cyl("claw", base, base + Vector((sx * 0.03, -0.22, -0.08)), 0.02, T.mat("kuku", 0x3A2A30, 0.5), r1=0.0,
                           bone="forearm_" + side, verts=5))
    # a staff with a red gem in the left hand
    wr_l = Vector((1.25, -0.75, bz + 0.7))
    P.append(T.cyl("staff", wr_l + Vector((0, 0.05, -1.6)), wr_l + Vector((0, 0, 0.9)), 0.035, T.mat("tongkat_hitam", 0x2A1E1A, 0.8), bone="forearm_L", verts=8))
    P.append(T.sphere("staff_gem", wr_l + Vector((0, 0, 1.0)), 0.13, gem, "forearm_L", seg=10, rings=8))
    mesh = T.join(P, name)
    T.skin(mesh, arm)

    heads = [t for t, *_ in side_heads]
    clips = []
    c = T.Clip(arm, "idle", 60)
    c.pose(0, chest=(0, 0, 5), head=(0, 0, 10), **{h: (0, 0, 10 if i % 2 else -10) for i, h in enumerate(heads)})
    c.pose(30, chest=(0, 0, -5), head=(8, 0, -10), **{h: (8, 0, -10 if i % 2 else 10) for i, h in enumerate(heads)})
    c.move(0, "root", (0, 0, 0.35)).move(30, "root", (0, 0, 0.6))
    clips.append((c, True))
    for nm, frames, lean in (("walk", 36, 10), ("run", 20, 22)):
        c = T.Clip(arm, nm, frames)
        c.pose(0, hips=(lean, 0, 6), chest=(lean * 0.5, 0, -6), upperarm_L=(-lean * 2, 0, 0), upperarm_R=(-lean * 2, 0, 0))
        c.pose(frames // 2, hips=(lean, 0, -6), chest=(lean * 0.5, 0, 6))
        c.move(0, "root", (0, 0, 0.4)).move(frames // 2, "root", (0, 0, 0.6))
        clips.append((c, True))
    c = T.Clip(arm, "spawn", 40)
    c.pose(0, chest=(-40, 0, 0)).pose(28, chest=(10, 0, 0)).pose(40, chest=(0, 0, 0))
    c.move(0, "root", (0, 0, -3.2)).move(30, "root", (0, 0, 0.8)).move(40, "root", (0, 0, 0.4))
    clips.append((c, False))
    c = T.Clip(arm, "die", 44)
    c.pose(0).pose(14, chest=(-30, 0, 0), head=(-30, 0, 0), **{h: (-30, 0, 0) for h in heads}).pose(44, root=(80, 0, 0), chest=(20, 0, 0))
    c.move(0, "root", (0, 0, 0.4)).move(14, "root", (0, 0, 1.0)).move(44, "root", (0, 0.4, 0.4))
    clips.append((c, False))
    act = ["chest", "head", "upperarm_L", "forearm_L", "upperarm_R", "forearm_R"] + heads
    c = T.Clip(arm, "attack", 20, act)
    c.pose(0, chest=(0, 0, 0), upperarm_L=(0, 0, 0), upperarm_R=(0, 0, 0))
    c.pose(7, chest=(-15, 0, 0), upperarm_L=(-60, -30, 0), upperarm_R=(-60, 30, 0), forearm_L=(-40, 0, 0), forearm_R=(-40, 0, 0))
    c.pose(12, chest=(25, 0, 0), upperarm_L=(40, 0, 0), upperarm_R=(40, 0, 0), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0))
    c.pose(20, chest=(0, 0, 0), upperarm_L=(0, 0, 0), upperarm_R=(0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "cast", 30, act)  # all five heads scream at once
    c.pose(0, head=(0, 0, 0), **{h: (0, 0, 0) for h in heads})
    c.pose(10, head=(-35, 0, 0), chest=(-20, 0, 0), upperarm_L=(-120, -40, 0), upperarm_R=(-120, 40, 0),
           **{h: (-35, 0, 25 if i % 2 else -25) for i, h in enumerate(heads)})
    c.pose(30, head=(0, 0, 0), chest=(0, 0, 0), upperarm_L=(0, 0, 0), upperarm_R=(0, 0, 0), **{h: (0, 0, 0) for h in heads})
    clips.append((c, False))
    c = T.Clip(arm, "hit", 12, act)
    c.pose(0, chest=(0, 0, 0)).pose(4, chest=(-20, 0, 10), head=(-20, 0, 0)).pose(12, chest=(0, 0, 0), head=(0, 0, 0))
    clips.append((c, False))
    return arm, clips


# ======================================================================= 7. Genderuwo Raja

def crown_hook(name, s, j, hc, hr):
    gold = T.mat("gold", 0xE0B040, 0.35, 0.8)
    gem = T.mat("permata_merah", 0xFF2A3A, 0.2, emit=0xFF1A2A, strength=5.0)
    parts = [T.cyl("crown", hc + Vector((0, 0, hr * 0.6)), hc + Vector((0, 0, hr * 1.05)), hr * 0.85, gold, r1=hr * 0.95, bone="head", verts=16)]
    for k in range(7):
        a = k / 7 * math.tau
        base = hc + Vector((math.cos(a) * hr * 0.9, math.sin(a) * hr * 0.9, hr * 1.02))
        parts.append(T.cyl("crown_spike", base, base + Vector((0, 0, hr * 0.4)), hr * 0.12, gold, r1=0.0, bone="head", verts=5))
    parts.append(T.sphere("crown_gem", hc + Vector((0, -hr * 0.92, hr * 0.82)), hr * 0.14, gem, "head", seg=8, rings=6))
    return parts


def club_hook(name, s, j, hc, hr):
    wood = T.mat("gada", 0x5A3A22, 0.85)
    metal = T.mat("paku_gada", 0xB0B4B8, 0.3, 0.9)
    grip = (j["wrist_R"] + j["fingers_R"]) * 0.5
    head = grip + Vector((0, -0.4, 1.6))
    parts = [T.cyl("club", grip + Vector((0, 0.1, -0.3)), head, 0.08, wood, r1=0.26, bone="hand_R", verts=10)]
    for k in range(10):
        a = k / 10 * math.tau
        z = 0.2 + (k % 3) * 0.3
        p = grip + (head - grip) * (0.65 + z * 0.2) + Vector((math.cos(a) * 0.22, math.sin(a) * 0.22, 0))
        parts.append(T.cyl("spike", p, p + Vector((math.cos(a) * 0.14, math.sin(a) * 0.14, 0.04)), 0.035, metal, r1=0.0, bone="hand_R", verts=5))
    return parts


def skull_necklace_hook(name, s, j, hc, hr):
    bone = T.mat("skull", 0xEDE6CF, 0.6)
    parts = []
    for k in range(7):
        a = (k - 3) * 0.35
        p = j["neck"] + Vector((math.sin(a) * s.shoulder * 1.1, -math.cos(a) * 0.38, -0.25 - abs(math.sin(a)) * 0.1))
        parts.append(T.sphere("neck_skull", p, 0.09, bone, "chest", seg=8, rings=6))
    return parts


def genderuwo_raja_spec():
    return C.Spec(height=3.7, head=0.74, shoulder=0.72, hip_w=0.26, leg=1.15, arm=1.6, girth=1.5, depth=1.15, limb=4.0,
                  skin=0x5A3A26, hair_style="none", top="raja_fur", top_sleeve="none", bottom="raja_fur", bottom_style="pants",
                  shoes=0x3A2616, face="zombie", eye_glow=0xFF3A1A, brows=0x1E120A, ears=1.3, weapons=False,
                  hooks=(crown_hook, club_hook, skull_necklace_hook))


# ======================================================================= 8. Kraken Raja

def build_kraken_raja(name="kraken_raja"):
    skin_tex = T.mat(name + "_skin", 0xFFFFFF, 0.6, image=spots_texture(name + "_tex", 0x2E6E6A, 0x6A2A7A, scale=4.0, seed=5))
    sucker = T.mat(name + "_sucker", 0xD8A8C8, 0.5)
    glow = T.mat(name + "_glow", 0xFF2A2A, 0.3, emit=0xFF2A2A, strength=7.0)
    dark = T.mat("eye_dark", 0x1A120C, 0.3)
    iron = T.mat("jangkar", 0x3A3C40, 0.4, 0.85)
    bz = 1.6
    bones = [("root", (0, 0, 0), (0, 0, 0.3), None),
             ("hips", (0, 0, 0.6), (0, 0, bz), "root"),
             ("head", (0, 0.1, bz), (0, 0.4, bz + 2.0), "hips")]
    tents = []
    for i in range(8):
        a = (i + 0.5) / 8 * math.tau - math.pi / 2
        d = Vector((math.cos(a), math.sin(a), 0))
        p0 = Vector((0, 0, 0.8)) + d * 0.9
        p1 = p0 + d * 1.2 + Vector((0, 0, -0.5))
        p2 = p1 + d * 1.1 + Vector((0, 0, -0.25))
        p3 = p2 + d * 1.0 + Vector((0, 0, 0.1))
        for k, (a0, b0) in enumerate(((p0, p1), (p1, p2), (p2, p3))):
            bones.append(("t%d_%d" % (i, k), tuple(a0), tuple(b0), "hips" if k == 0 else "t%d_%d" % (i, k - 1)))
        tents.append((i, [p0, p1, p2, p3]))
    arm = T.build_custom_armature(name + "_rig", bones)
    P = [T.sphere("body", (0, 0, 1.0), (1.3, 1.3, 0.9), skin_tex, "hips", seg=20, rings=12),
         T.sphere("mantle", (0, 0.35, bz + 1.1), (1.1, 1.2, 1.5), skin_tex, "head", seg=20, rings=14),
         T.sphere("brow", (0, -0.75, bz + 0.2), (1.0, 0.4, 0.35), skin_tex, "hips", seg=14, rings=8)]
    eyes(P, Vector((0, -0.6, bz - 0.1)), 0.9, 0.55, 0.45, glow, dark, "hips", size=0.24, up=0.1)
    for i, pts in tents:
        radii = (0.34, 0.26, 0.17, 0.06)
        for k in range(3):
            P.append(T.cyl("tentacle", pts[k], pts[k + 1], radii[k], skin_tex, r1=radii[k + 1], bone="t%d_%d" % (i, k), verts=10))
            mid = (pts[k] + pts[k + 1]) * 0.5 - Vector((0, 0, radii[k] * 0.8))
            P.append(T.sphere("sucker", mid, radii[k] * 0.45, sucker, "t%d_%d" % (i, k), seg=8, rings=5))
    # an old ship's anchor wrapped in the front-left tentacle
    tip = tents[3][1][2]
    P.append(T.cyl("anchor_shank", tip + Vector((0, 0, -0.6)), tip + Vector((0, 0, 1.2)), 0.08, iron, bone="t3_2", verts=8))
    P.append(T.torus("anchor_ring", tip + Vector((0, 0, 1.3)), 0.15, 0.04, iron, "t3_2", seg=10, ring_seg=4, rot=(math.pi / 2, 0, 0)))
    P.append(T.cyl("anchor_arm", tip + Vector((-0.5, 0, -0.4)), tip + Vector((0.5, 0, -0.4)), 0.07, iron, bone="t3_2", verts=8))
    mesh = T.join(P, name)
    T.skin(mesh, arm)

    def wave(c, frames, amp, steps=6, lift=0.0, speed=1.0):
        for s in range(steps + 1):
            f = int(frames * s / steps)
            ph = s / steps * math.tau * speed
            rots = {}
            for i in range(8):
                for k in range(3):
                    rots["t%d_%d" % (i, k)] = (amp * math.sin(ph + i * 0.8 + k * 0.9), 0, amp * 0.5 * math.cos(ph + i * 0.7))
            rots["head"] = (5 * math.sin(ph), 0, 4 * math.cos(ph))
            c.pose(f, **rots)
            c.move(f, "hips", (0, 0, lift * (0.5 + 0.5 * math.sin(ph))))

    clips = []
    c = T.Clip(arm, "idle", 60); wave(c, 60, 12, lift=0.08); clips.append((c, True))
    c = T.Clip(arm, "walk", 36); wave(c, 36, 22, lift=0.15); clips.append((c, True))
    c = T.Clip(arm, "run", 20); wave(c, 20, 28, lift=0.2); clips.append((c, True))
    c = T.Clip(arm, "spawn", 40)
    c.pose(0, head=(-20, 0, 0)).pose(40, head=(0, 0, 0))
    c.move(0, "root", (0, 0, -3.5)).move(32, "root", (0, 0, 0.3)).move(40, "root", (0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "die", 44)
    c.pose(0).pose(44, head=(40, 0, 0), **{"t%d_%d" % (i, k): (30, 0, 0) for i in range(8) for k in range(3)})
    c.move(0, "root", (0, 0, 0)).move(44, "root", (0, 0, -1.6))
    clips.append((c, False))
    front = ["t3_0", "t3_1", "t3_2", "t4_0", "t4_1", "t4_2"]
    c = T.Clip(arm, "attack", 24, front + ["head"])
    c.pose(0, **{b: (0, 0, 0) for b in front}, head=(0, 0, 0))
    c.pose(9, **{b: (-45, 0, 0) for b in front}, head=(-15, 0, 0))
    c.pose(14, **{b: (35, 0, 0) for b in front}, head=(10, 0, 0))
    c.pose(24, **{b: (0, 0, 0) for b in front}, head=(0, 0, 0))
    clips.append((c, False))
    allt = ["t%d_%d" % (i, k) for i in range(8) for k in range(3)]
    c = T.Clip(arm, "cast", 30, allt + ["head"])
    c.pose(0, **{b: (0, 0, 0) for b in allt}).pose(12, head=(-20, 0, 0), **{b: (-35, 0, 0) for b in allt}).pose(30, head=(0, 0, 0), **{b: (0, 0, 0) for b in allt})
    clips.append((c, False))
    c = T.Clip(arm, "hit", 12, ["head"])
    c.pose(0, head=(0, 0, 0)).pose(4, head=(-15, 0, 12)).pose(12, head=(0, 0, 0))
    clips.append((c, False))
    return arm, clips


# ======================================================================= 9. Leviathan Kuno

def build_leviathan(name="leviathan"):
    scale_tex = T.mat(name + "_scale", 0xFFFFFF, 0.5, image=spots_texture(name + "_tex", 0x1E5A5E, 0x2A7A5A, scale=7.0, seed=9))
    belly = T.mat(name + "_belly", 0xC8D8A0, 0.6)
    fin = T.mat(name + "_fin", 0x3AAA8A, 0.5)
    horn = T.mat("horn", 0xE8DCC0, 0.5)
    glow = T.mat(name + "_glow", 0x6AFFE0, 0.3, emit=0x3AFFD0, strength=6.0)
    dark = T.mat("eye_dark", 0x1A120C, 0.3)
    tooth = T.mat("zombie_tooth", 0xF2EBC8, 0.5)
    bz = 1.3
    bones = [("root", (0, 0, 0), (0, 0, 0.3), None), ("hips", (0, 0.6, bz), (0, -0.6, bz), "root")]
    necks = []
    for h, (x, spread) in enumerate(((0.0, 0.0), (0.9, 0.35), (-0.9, -0.35))):
        # necks rise and then arch forward like the hydra in the art
        pts = [Vector((x * 0.6, -0.8, bz + 0.4))]
        for off in ((0, -0.25, 0.85), (0, -0.35, 0.75), (0, -0.55, 0.45), (0, -0.7, 0.15)):
            pts.append(pts[-1] + Vector((spread * 0.45 + off[0], off[1], off[2])))
        for k in range(4):
            bones.append(("n%d_%d" % (h, k), tuple(pts[k]), tuple(pts[k + 1]), "hips" if k == 0 else "n%d_%d" % (h, k - 1)))
        hd = pts[-1] + Vector((0, -0.7, 0.1))
        bones.append(("h%d" % h, tuple(pts[-1]), tuple(hd), "n%d_3" % h))
        necks.append((h, pts, hd))
    tail = [Vector((0, 0.9, bz - 0.2))]
    for k in range(4):
        tail.append(tail[-1] + Vector((0.35 * math.sin(k), 0.9, -0.2)))
    for k in range(4):
        bones.append(("tail%d" % k, tuple(tail[k]), tuple(tail[k + 1]), "hips" if k == 0 else "tail%d" % (k - 1)))
    arm = T.build_custom_armature(name + "_rig", bones)
    P = [T.sphere("body", (0, 0, bz), (1.3, 1.7, 1.0), scale_tex, "hips", seg=20, rings=12),
         T.sphere("belly", (0, -0.3, bz - 0.45), (1.0, 1.3, 0.5), belly, "hips", seg=16, rings=8)]
    for k in range(5):
        P.append(T.cyl("back_fin", (0, -0.8 + k * 0.4, bz + 0.85), (0, -0.6 + k * 0.4, bz + 1.5 - k * 0.08), 0.12, fin, r1=0.0, bone="hips", verts=4))
    for sx in (-1, 1):
        P.append(T.sphere("flipper", (sx * 1.3, -0.4, bz - 0.5), (0.7, 0.35, 0.12), fin, "hips", seg=12, rings=6, rot=(0, sx * 0.4, 0)))
    for h, pts, hd in necks:
        for k in range(4):
            r0, r1 = 0.36 - k * 0.04, 0.32 - k * 0.04
            P.append(T.cyl("neck", pts[k], pts[k + 1], r0, scale_tex, r1=r1, bone="n%d_%d" % (h, k), verts=12))
            P.append(T.cyl("neck_fin", pts[k] + Vector((0, 0.2, 0.2)), pts[k] + Vector((0, 0.4, 0.55)), 0.08, fin, r1=0.0, bone="n%d_%d" % (h, k), verts=4))
        base = pts[-1]
        hc = base + Vector((0, -0.35, 0.1))
        P.append(T.sphere("head", hc, (0.38, 0.62, 0.34), scale_tex, "h%d" % h, seg=14, rings=10))
        P.append(T.sphere("jaw", hc + Vector((0, -0.3, -0.22)), (0.3, 0.5, 0.14), belly, "h%d" % h, seg=12, rings=8))
        for sx in (-1, 1):
            P.append(T.cyl("horn", hc + Vector((sx * 0.18, 0.2, 0.2)), hc + Vector((sx * 0.35, 0.65, 0.55)), 0.06, horn, r1=0.0, bone="h%d" % h, verts=6))
            P.append(T.cyl("fang", hc + Vector((sx * 0.12, -0.55, -0.1)), hc + Vector((sx * 0.1, -0.55, -0.25)), 0.03, tooth, r1=0.0, bone="h%d" % h, verts=5))
        eyes(P, hc + Vector((0, 0.0, 0.1)), 0.38, 1.2, 0.55, glow, dark, "h%d" % h, size=0.2, up=0.25)
    for k in range(4):
        P.append(T.cyl("tail", tail[k], tail[k + 1], 0.5 - k * 0.1, scale_tex, r1=0.42 - k * 0.1, bone="tail%d" % k, verts=12))
    P.append(T.sphere("tail_fin", tail[-1] + Vector((0, 0.2, 0)), (0.6, 0.3, 0.08), fin, "tail3", seg=10, rings=6))
    mesh = T.join(P, name)
    T.skin(mesh, arm)

    neck_bones = ["n%d_%d" % (h, k) for h in range(3) for k in range(4)]
    tail_bones = ["tail%d" % k for k in range(4)]

    def sway(c, frames, amp, steps=6):
        for s in range(steps + 1):
            f = int(frames * s / steps)
            ph = s / steps * math.tau
            rots = {("n%d_%d" % (h, k)): (amp * 0.4 * math.sin(ph + h), 0, amp * math.sin(ph + h * 2.1 + k * 0.5)) for h in range(3) for k in range(4)}
            rots.update({("tail%d" % k): (0, 0, amp * 1.2 * math.sin(ph - k * 0.8)) for k in range(4)})
            c.pose(f, **rots)

    clips = []
    c = T.Clip(arm, "idle", 60); sway(c, 60, 8); c.move(0, "hips", (0, 0, 0)).move(30, "hips", (0, 0, 0.12)); clips.append((c, True))
    c = T.Clip(arm, "walk", 40); sway(c, 40, 14); c.move(0, "hips", (0, 0, 0)).move(20, "hips", (0, 0, 0.2)); clips.append((c, True))
    c = T.Clip(arm, "run", 24); sway(c, 24, 18); c.move(0, "hips", (0, 0, 0)).move(12, "hips", (0, 0, 0.25)); clips.append((c, True))
    c = T.Clip(arm, "spawn", 44)
    c.pose(0, **{b: (30, 0, 0) for b in neck_bones}).pose(44, **{b: (0, 0, 0) for b in neck_bones})
    c.move(0, "root", (0, 0, -4.0)).move(34, "root", (0, 0, 0.3)).move(44, "root", (0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "die", 48)
    c.pose(0).pose(20, **{b: (-20, 0, 15) for b in neck_bones}).pose(48, **{b: (35, 0, 20) for b in neck_bones})
    c.move(0, "root", (0, 0, 0)).move(48, "root", (0, 0, -2.2))
    clips.append((c, False))
    act = neck_bones + ["h0", "h1", "h2"]
    c = T.Clip(arm, "attack", 22, act)  # the middle head lunges and bites
    c.pose(0, **{b: (0, 0, 0) for b in act})
    c.pose(8, **{"n0_%d" % k: (-15, 0, 0) for k in range(4)}, h0=(-30, 0, 0))
    c.pose(13, **{"n0_%d" % k: (22, 0, 0) for k in range(4)}, h0=(25, 0, 0))
    c.pose(22, **{b: (0, 0, 0) for b in act})
    clips.append((c, False))
    c = T.Clip(arm, "cast", 30, act)  # all three heads rear up to breathe
    c.pose(0, **{b: (0, 0, 0) for b in act})
    c.pose(12, **{b: (-12, 0, 0) for b in neck_bones}, h0=(-35, 0, 0), h1=(-35, 0, 0), h2=(-35, 0, 0))
    c.pose(20, **{b: (10, 0, 0) for b in neck_bones}, h0=(10, 0, 0), h1=(10, 0, 0), h2=(10, 0, 0))
    c.pose(30, **{b: (0, 0, 0) for b in act})
    clips.append((c, False))
    c = T.Clip(arm, "hit", 12, ["h0", "h1", "h2"])
    c.pose(0, h0=(0, 0, 0), h1=(0, 0, 0), h2=(0, 0, 0)).pose(4, h0=(-25, 0, 0), h1=(-20, 0, 15), h2=(-20, 0, -15)).pose(12, h0=(0, 0, 0), h1=(0, 0, 0), h2=(0, 0, 0))
    clips.append((c, False))
    return arm, clips


# ======================================================================= 10. Demon King Abyss

def demon_bones(j):
    ch = j["chest"]
    return [
        ("wing_L", tuple(ch + Vector((0.3, 0.25, 0.35))), tuple(ch + Vector((1.9, 0.9, 1.1))), "chest"),
        ("wing_R", tuple(ch + Vector((-0.3, 0.25, 0.35))), tuple(ch + Vector((-1.9, 0.9, 1.1))), "chest"),
        ("tail", tuple(j["hip"] + Vector((0, 0.3, -0.1))), tuple(j["hip"] + Vector((0.4, 1.8, -0.8))), "hips"),
    ]


def demon_hook(name, s, j, hc, hr):
    horn = T.mat("tanduk_iblis", 0x2A1A14, 0.5)
    membrane = T.mat("sayap_iblis", 0x5A1A1A, 0.7)
    rib = T.mat("tulang_sayap", 0x2A1414, 0.6)
    lava = T.mat("lava", 0xFF6A1A, 0.4, emit=0xFF4A0A, strength=6.0)
    scale = T.mat(name + "_scale", 0x3A1A1A, 0.6)
    glow = T.mat("mata_iblis", 0xFFB02A, 0.3, emit=0xFF8A0A, strength=8.0)
    parts = []
    for sx in (-1, 1):
        base = hc + Vector((sx * hr * 0.55, 0.05, hr * 0.55))
        mid = base + Vector((sx * hr * 0.7, 0.1, hr * 0.7))
        parts.append(T.cyl("horn", base, mid, hr * 0.22, horn, r1=hr * 0.14, bone="head", verts=8))
        parts.append(T.cyl("horn_tip", mid, mid + Vector((sx * hr * 0.1, 0.25, hr * 0.9)), hr * 0.14, horn, r1=0.0, bone="head", verts=8))
    parts.append(T.sphere("crown_fire", hc + Vector((0, 0, hr * 1.05)), (hr * 0.5, hr * 0.5, hr * 0.3), lava, "head", seg=10, rings=6))
    # wings: bony fingers with a membrane stretched between them
    ch = j["chest"]
    for side, sx in (("L", 1), ("R", -1)):
        root = ch + Vector((sx * 0.3, 0.25, 0.35))
        tip = ch + Vector((sx * 1.9, 0.9, 1.1))
        parts.append(T.cyl("wing_arm", root, tip, 0.08, rib, r1=0.04, bone="wing_" + side, verts=8))
        for k in range(4):
            f_end = tip + Vector((sx * (0.2 - k * 0.35), 0.3, -0.6 - k * 0.55))
            parts.append(T.cyl("wing_finger", tip, f_end, 0.04, rib, r1=0.01, bone="wing_" + side, verts=6))
            mid = (root + tip + f_end) / 3
            parts.append(T.sphere("wing_membrane", mid, (0.55, 0.05, 0.75), membrane, "wing_" + side, seg=10, rings=6,
                                  rot=(0.2, sx * (0.5 + k * 0.2), 0)))
        # a dragon head growling from each shoulder
        sh = j["shoulder_" + side]
        dc = sh + Vector((sx * 0.35, 0.05, 0.45))
        parts.append(T.sphere("dragon_head", dc, (0.2, 0.32, 0.2), scale, "chest", seg=12, rings=8))
        parts.append(T.cyl("dragon_horn", dc + Vector((sx * 0.1, 0.15, 0.12)), dc + Vector((sx * 0.2, 0.45, 0.4)), 0.05, horn, r1=0.0, bone="chest", verts=6))
        parts.append(T.sphere("dragon_eye", dc + Vector((sx * 0.1, -0.2, 0.08)), 0.04, glow, "chest", seg=6, rings=4))
    # tail with an arrowhead tip
    t0 = j["hip"] + Vector((0, 0.3, -0.1))
    t1 = j["hip"] + Vector((0.4, 1.8, -0.8))
    parts.append(T.cyl("tail", t0, t1, 0.14, scale, r1=0.05, bone="tail", verts=8))
    parts.append(T.cyl("tail_tip", t1, t1 + Vector((0.1, 0.35, -0.05)), 0.14, lava, r1=0.0, bone="tail", verts=4))
    # glowing lava cracks across the chest
    for k in range(4):
        parts.append(T.box("crack", j["chest"] + Vector((0.1 * (k - 1.5), -0.36, 0.1 - k * 0.12)), (0.05, 0.02, 0.28), lava, "chest",
                           rot=(0, (k - 1.5) * 0.4, 0)))
    return parts


def demon_spec():
    return C.Spec(height=4.2, head=0.66, shoulder=0.78, hip_w=0.28, leg=1.4, arm=1.7, girth=1.45, depth=1.1, limb=3.4,
                  skin=0x3A1A1A, hair_style="none", top="demon_lava", top_sleeve="none", bottom="demon_lava", bottom_style="pants",
                  shoes=0x1A0E0E, face="zombie", eye_glow=0xFFB02A, brows=0x0E0606, ears=1.0, weapons=False,
                  hooks=(demon_hook, claws_like), extra_bones=demon_bones)


def claws_like(name, s, j, hc, hr):
    claw = T.mat("cakar_iblis", 0x1A1010, 0.4)
    parts = []
    for side in ("L", "R"):
        hand = (j["wrist_" + side] + j["fingers_" + side]) * 0.5
        for k in range(4):
            base = hand + Vector(((k - 1.5) * 0.06, -0.08, -0.08))
            parts.append(T.cyl("claw", base, base + Vector((0, -0.1, -0.18)), 0.03, claw, r1=0.0, bone="hand_" + side, verts=5))
    return parts


def tex_lava_skin(name):
    import numpy as np
    base, lava = np.array(T.hexf(0x2E1414)), np.array(T.hexf(0xFF5A10))

    def fn(u, v):
        col = np.broadcast_to(base, u.shape + (3,)).copy()
        n = np.abs(np.sin(u * 21 + np.sin(v * 13) * 2) * np.sin(v * 17 + u * 4))
        col[n < 0.06] = lava
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


# ======================================================================= build

HUMANOID = {
    "genderuwo_raja": (genderuwo_raja_spec, anims.Posture(spine=(20, 0, 0), chest=(14, 0, 0), head=(-22, 0, 0))),
    "demon_king": (demon_spec, anims.Posture(spine=(6, 0, 0), head=(-6, 0, 0))),
}

CUSTOM = {
    "jeng_roro": build_jeng_roro,
    "kunti_penguasa": build_kunti_penguasa,
    "kraken_raja": build_kraken_raja,
    "leviathan": build_leviathan,
}

NAMES = ["jeng_roro", "kunti_penguasa", "genderuwo_raja", "kraken_raja", "leviathan", "demon_king"]


def _resolve(name, s):
    for attr in ("top", "bottom"):
        v = getattr(s, attr)
        if v == "raja_fur":
            setattr(s, attr, T.mat(name + "_fur", 0xFFFFFF, 0.95, image=Z.tex_fur(name + "_fur_tex", 0x6A3A22)))
        elif v == "demon_lava":
            setattr(s, attr, T.mat(name + "_lava", 0xFFFFFF, 0.6, image=tex_lava_skin(name + "_lava_tex")))


def build(name, export=True):
    T.clear_scene()
    T.reset_materials()
    if name in CUSTOM:
        arm, clips = CUSTOM[name]()
        return _finish(name, arm, clips, export)
    spec_fn, posture = HUMANOID[name]
    s = spec_fn()
    _resolve(name, s)
    arm, mesh = C.build_human(name, s)
    clips = zanims.zombie_clips(arm, posture, big=True)
    if name == "demon_king":
        # the wings beat and the tail swishes on top of the zombie set
        for clip, loop in clips:
            if clip.bones is None:
                n = clip.frames
                for f in range(0, n + 1, max(4, n // 4)):
                    up = (f // max(4, n // 4)) % 2 == 0
                    clip.pose(f, wing_L=(0, -35 if up else 20, 0), wing_R=(0, 35 if up else -20, 0), tail=(0, 0, 20 if up else -20))
    return _finish(name, arm, clips, export)


def build_all():
    return {n: build(n) for n in NAMES}
