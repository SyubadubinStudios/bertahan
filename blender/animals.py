"""Hewan Kampung Damai: ternak (ayam, sapi, kambing) and wild animals (kucing,
anjing, ular, burung). Same toy style as the family: rounded primitives rigidly
bound to bones, big friendly eyes, flat cartoon colours.

Conventions follow btk: Z up, animals face -Y, their left is +X. Clips use the
game's names so AnimatedModel can blend them:
  base:   idle, walk, run (burung: terbang, ular: melata cepat), die
  action: attack (ayam/burung: mematuk, sapi/kambing: merumput,
          kucing/anjing: mengeong/menggonggong, ular: mematuk)
"""

import math

from mathutils import Vector

import btk as T


# --------------------------------------------------------------------- helpers

def eyes(P, head_c, hr, fwd, side_x, mat_white, mat_dark, bone, size=0.22, up=0.15):
    """Cute eyes on a head of radius hr centred at head_c, looking along -Y."""
    for sx in (-1, 1):
        c = head_c + Vector((sx * hr * side_x, -hr * fwd, hr * up))
        P.append(T.sphere("eye", c, (hr * size, hr * size * 0.7, hr * size * 1.1), mat_white, bone, seg=10, rings=8))
        P.append(T.sphere("pupil", c + Vector((sx * hr * 0.02, -hr * size * 0.55, -hr * 0.02)), (hr * size * 0.55, hr * size * 0.35, hr * size * 0.62),
                          mat_dark, bone, seg=8, rings=6))
        P.append(T.sphere("glint", c + Vector((sx * hr * 0.05, -hr * size * 0.85, hr * size * 0.3)), hr * size * 0.18, mat_white, bone, seg=6, rings=4))


def spots_texture(name, base, spot, scale=5.0, seed=3):
    import numpy as np
    b, s = np.array(T.hexf(base)), np.array(T.hexf(spot))

    def fn(u, v):
        n = (np.sin(u * scale * 6.3 + seed) * np.sin(v * scale * 4.1 + seed * 2) + np.sin((u + v) * scale * 3.3 + seed)) * 0.5
        col = np.broadcast_to(b, u.shape + (3,)).copy()
        col[n > 0.35] = s
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


def stripes_texture(name, base, stripe):
    import numpy as np
    b, s = np.array(T.hexf(base)), np.array(T.hexf(stripe))

    def fn(u, v):
        col = np.broadcast_to(b, u.shape + (3,)).copy()
        col[np.abs(np.sin(u * 40.0 + np.sin(v * 9.0))) > 0.82] = s
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 128, fn)


# ------------------------------------------------------------------ quadruped

class Quad:
    def __init__(self, **kw):
        self.__dict__.update(dict(
            length=0.8, leg=0.45, body_r=0.2, body_w=0.18, neck=0.25, neck_up=55, head=0.16, snout=0.1,
            coat=0xB08050, belly=None, dark=0x2A1E14, nose=0x3A2A2A, hoof=None, ears="pointy", ear_size=1.0,
            tail="thin", tail_len=0.35, horns=None, beard=False, udder=False, texture=None, leg_r=0.045,
            collar=None, eye_size=0.22, gait=1.0))
        self.__dict__.update(kw)


QUADS = {
    "sapi": Quad(length=1.45, leg=0.72, body_r=0.36, body_w=0.34, neck=0.34, neck_up=38, head=0.25, snout=0.2,
                 coat=0xF4F1EA, dark=0x2B2622, nose=0xE8A0A0, hoof=0x3A302A, ears="side", ear_size=1.1, tail="tuft",
                 tail_len=0.7, horns="short", udder=True, texture="spots", leg_r=0.075, gait=0.8),
    "kambing": Quad(length=0.78, leg=0.5, body_r=0.2, body_w=0.18, neck=0.26, neck_up=60, head=0.15, snout=0.12,
                    coat=0xEDE6D8, dark=0x6A5440, nose=0x4A3A34, hoof=0x3A302A, ears="side", ear_size=0.9, tail="stub",
                    tail_len=0.12, horns="curl", beard=True, leg_r=0.038),
    "kucing": Quad(length=0.5, leg=0.17, body_r=0.11, body_w=0.1, neck=0.1, neck_up=70, head=0.13, snout=0.03,
                   coat=0xE39A48, belly=0xFFF2E0, dark=0x8A4A1A, nose=0xE87A8A, ears="pointy", ear_size=1.2, tail="thin",
                   tail_len=0.45, texture="stripes", leg_r=0.028, eye_size=0.3, gait=1.3),
    "anjing": Quad(length=0.72, leg=0.38, body_r=0.16, body_w=0.14, neck=0.16, neck_up=60, head=0.15, snout=0.11,
                   coat=0xC9A06A, belly=0xF2E2C6, dark=0x6B4A2A, nose=0x2A2020, ears="floppy", ear_size=1.1, tail="curl",
                   tail_len=0.3, leg_r=0.04, collar=0xD83A3A, gait=1.1),
}


def quad_joints(q):
    bz = q.leg + q.body_r * 0.35
    rear, front = q.length * 0.42, -q.length * 0.42
    a = math.radians(q.neck_up)
    neck_base = Vector((0, front, bz + q.body_r * 0.35))
    neck_top = neck_base + Vector((0, -math.cos(a) * q.neck, math.sin(a) * q.neck))
    head_c = neck_top + Vector((0, -q.head * 0.55, q.head * 0.1))
    return dict(bz=bz, rear=rear, front=front, neck_base=neck_base, neck_top=neck_top, head_c=head_c)


def build_quad(name, q):
    j = quad_joints(q)
    bz, rear, front = j["bz"], j["rear"], j["front"]
    lx = q.body_w * 0.62
    bones = [
        ("root", (0, 0, 0), (0, 0, 0.15), None),
        ("hips", (0, rear, bz), (0, 0, bz), "root"),
        ("spine", (0, 0, bz), (0, front, bz), "hips"),
        ("neck", tuple(j["neck_base"]), tuple(j["neck_top"]), "spine"),
        ("head", tuple(j["neck_top"]), tuple(j["neck_top"] + Vector((0, -q.head * 1.2, 0))), "neck"),
        ("tail", (0, rear + q.body_r * 0.2, bz + q.body_r * 0.3), (0, rear + q.body_r * 0.2 + q.tail_len * 0.7, bz + q.body_r * 0.3 - q.tail_len * 0.5), "hips"),
    ]
    legs = []
    for tag, y, parent in (("F", front * 0.82, "spine"), ("B", rear * 0.82, "hips")):
        for side, sx in (("L", 1), ("R", -1)):
            top = Vector((sx * lx, y, bz - q.body_r * 0.35))
            knee = Vector((sx * lx, y + (0.02 if tag == "F" else -0.03), q.leg * 0.48))
            foot = Vector((sx * lx, y, 0.0))
            bones.append(("leg_%s%s" % (tag, side), tuple(top), tuple(knee), parent))
            bones.append(("shin_%s%s" % (tag, side), tuple(knee), tuple(foot), "leg_%s%s" % (tag, side)))
            legs.append((tag, side, sx, top, knee, foot))
    arm = T.build_custom_armature(name + "_rig", bones)

    coat = T.mat(name + "_coat", q.coat, 0.85)
    if q.texture == "spots":
        coat = T.mat(name + "_coat_tex", 0xFFFFFF, 0.85, image=spots_texture(name + "_spots", q.coat, q.dark))
    elif q.texture == "stripes":
        coat = T.mat(name + "_coat_tex", 0xFFFFFF, 0.85, image=stripes_texture(name + "_stripes", q.coat, q.dark))
    plain = T.mat(name + "_plain", q.coat, 0.85)
    belly = T.mat(name + "_belly", q.belly, 0.85) if q.belly else plain
    dark = T.mat(name + "_dark", q.dark, 0.8)
    nose = T.mat(name + "_nose", q.nose, 0.5)
    hoof = T.mat(name + "_hoof", q.hoof, 0.6) if q.hoof else dark
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    pupil = T.mat("eye_dark", 0x1A120C, 0.3)
    P = []

    # body: rear half on the hips, front half on the spine so it bends
    P.append(T.sphere("rump", Vector((0, rear * 0.45, bz)), (q.body_w, q.length * 0.34, q.body_r), coat, "hips", seg=18, rings=12))
    P.append(T.sphere("chest", Vector((0, front * 0.45, bz + q.body_r * 0.05)), (q.body_w * 1.05, q.length * 0.34, q.body_r * 1.05), coat, "spine", seg=18, rings=12))
    if q.belly:
        P.append(T.sphere("belly", Vector((0, 0, bz - q.body_r * 0.35)), (q.body_w * 0.8, q.length * 0.4, q.body_r * 0.55), belly, "spine", seg=14, rings=8))
    if q.udder:
        P.append(T.sphere("udder", Vector((0, rear * 0.5, bz - q.body_r * 0.9)), (q.body_w * 0.4, q.body_r * 0.4, q.body_r * 0.25), nose, "hips", seg=10, rings=6))

    # neck and head
    P.append(T.cyl("neck", j["neck_base"] + Vector((0, 0.03, -0.05)), j["neck_top"], q.body_r * 0.55, coat, r1=q.head * 0.55, bone="neck", verts=12))
    hc = j["head_c"]
    hr = q.head
    P.append(T.sphere("head", hc, (hr * 0.85, hr * 0.95, hr * 0.85), plain, "head", seg=16, rings=12))
    if q.snout > 0.05:
        sn = hc + Vector((0, -hr * 0.75, -hr * 0.2))
        P.append(T.cyl("snout", hc + Vector((0, -hr * 0.3, -hr * 0.12)), sn + Vector((0, -q.snout * 0.6, 0)), hr * 0.5, belly if q.belly else plain,
                       r1=hr * 0.42, bone="head", verts=12))
        P.append(T.sphere("nose", sn + Vector((0, -q.snout * 0.65, 0.01)), (hr * 0.38, hr * 0.12, hr * 0.28), nose, "head", seg=10, rings=6))
    else:
        P.append(T.sphere("muzzle", hc + Vector((0, -hr * 0.78, -hr * 0.25)), (hr * 0.4, hr * 0.2, hr * 0.25), belly, "head", seg=10, rings=6))
        P.append(T.sphere("nose", hc + Vector((0, -hr * 0.95, -hr * 0.12)), (hr * 0.1, hr * 0.06, hr * 0.07), nose, "head", seg=8, rings=4))
    eyes(P, hc, hr, 0.62, 0.42, white, pupil, "head", size=q.eye_size, up=0.18)

    # ears
    for sx in (-1, 1):
        base = hc + Vector((sx * hr * 0.55, hr * 0.05, hr * 0.55))
        s = q.ear_size
        if q.ears == "pointy":
            P.append(T.cyl("ear", base, base + Vector((sx * hr * 0.15, 0, hr * 0.55 * s)), hr * 0.3 * s, plain, r1=0.0, bone="head", verts=4))
            P.append(T.cyl("ear_in", base + Vector((0, -0.01, 0.01)), base + Vector((sx * hr * 0.13, -0.012, hr * 0.42 * s)), hr * 0.18 * s, nose, r1=0.0,
                           bone="head", verts=4))
        elif q.ears == "floppy":
            P.append(T.sphere("ear", base + Vector((sx * hr * 0.15, 0, -hr * 0.35)), (hr * 0.14 * s, hr * 0.3 * s, hr * 0.45 * s), dark, "head", seg=10, rings=8,
                              rot=(0, sx * 0.4, 0)))
        else:  # side ears (cow, goat)
            P.append(T.sphere("ear", base + Vector((sx * hr * 0.35, 0, -hr * 0.2)), (hr * 0.4 * s, hr * 0.12, hr * 0.18 * s), plain, "head", seg=10, rings=6,
                              rot=(0, sx * -0.3, 0)))

    # horns and beard
    if q.horns == "short":
        horn = T.mat("horn", 0xE8DCC0, 0.5)
        for sx in (-1, 1):
            b = hc + Vector((sx * hr * 0.45, hr * 0.1, hr * 0.7))
            P.append(T.cyl("horn", b, b + Vector((sx * hr * 0.45, -hr * 0.05, hr * 0.3)), hr * 0.12, horn, r1=0.01, bone="head", verts=8))
    elif q.horns == "curl":
        horn = T.mat("horn_goat", 0x8A7A60, 0.6)
        for sx in (-1, 1):
            b = hc + Vector((sx * hr * 0.3, hr * 0.2, hr * 0.75))
            P.append(T.cyl("horn", b, b + Vector((sx * hr * 0.2, hr * 0.55, hr * 0.35)), hr * 0.13, horn, r1=hr * 0.05, bone="head", verts=8))
            P.append(T.cyl("horn_tip", b + Vector((sx * hr * 0.2, hr * 0.55, hr * 0.35)), b + Vector((sx * hr * 0.3, hr * 0.7, -hr * 0.05)), hr * 0.06, horn,
                           r1=0.01, bone="head", verts=6))
    if q.beard:
        P.append(T.cyl("beard", hc + Vector((0, -hr * 0.7, -hr * 0.55)), hc + Vector((0, -hr * 0.75, -hr * 1.05)), hr * 0.15, dark, r1=0.01, bone="head", verts=6))
    if q.collar:
        col = T.mat(name + "_collar", q.collar, 0.5)
        P.append(T.torus("collar", j["neck_base"] + (j["neck_top"] - j["neck_base"]) * 0.35, q.body_r * 0.6, q.body_r * 0.1, col, "neck",
                         rot=(math.radians(90 - q.neck_up), 0, 0), seg=14, ring_seg=6))
        P.append(T.sphere("tag", j["neck_base"] + (j["neck_top"] - j["neck_base"]) * 0.35 + Vector((0, -q.body_r * 0.55, -q.body_r * 0.2)), q.body_r * 0.12,
                          T.mat("tag_gold", 0xF2C94C, 0.3, 0.8), "neck", seg=8, rings=6))

    # legs
    for tag, side, sx, top, knee, foot in legs:
        mat = coat if not q.belly else plain
        P.append(T.cyl("leg", top + Vector((0, 0, 0.04)), knee, q.leg_r * 1.35, mat, r1=q.leg_r * 1.05, bone="leg_%s%s" % (tag, side), verts=10))
        P.append(T.cyl("shin", knee, foot + Vector((0, 0, 0.04)), q.leg_r, mat, r1=q.leg_r * 0.9, bone="shin_%s%s" % (tag, side), verts=10))
        P.append(T.sphere("hoof", foot + Vector((0, -q.leg_r * 0.2, 0.03)), (q.leg_r * 1.1, q.leg_r * 1.3, 0.035), hoof, "shin_%s%s" % (tag, side), seg=10, rings=6))

    # tail
    t0 = Vector((0, rear + q.body_r * 0.2, bz + q.body_r * 0.3))
    t1 = t0 + Vector((0, q.tail_len * 0.7, -q.tail_len * 0.5))
    if q.tail == "thin":
        t1 = t0 + Vector((0, q.tail_len * 0.6, q.tail_len * 0.6))
        P.append(T.cyl("tail", t0, t1, q.leg_r * 0.8, coat, r1=q.leg_r * 0.6, bone="tail", verts=8))
    elif q.tail == "curl":
        t1 = t0 + Vector((0, q.tail_len * 0.4, q.tail_len * 0.8))
        P.append(T.cyl("tail", t0, t1, q.leg_r * 0.9, plain, r1=q.leg_r * 0.5, bone="tail", verts=8))
    elif q.tail == "tuft":
        P.append(T.cyl("tail", t0, t1, 0.02, plain, r1=0.015, bone="tail", verts=6))
        P.append(T.sphere("tuft", t1, (0.05, 0.05, 0.09), dark, "tail", seg=8, rings=6))
    else:
        P.append(T.sphere("tail", t0 + Vector((0, 0.03, 0.03)), (0.04, 0.05, 0.06), plain, "tail", seg=8, rings=6))

    mesh = T.join(P, name)
    T.skin(mesh, arm)
    return arm


def quad_clips(arm, q):
    g = q.gait
    clips = []
    # idle: breathe, look around, flick the tail
    c = T.Clip(arm, "idle", 72)
    c.pose(0, neck=(0, 0, 0), head=(0, 0, 0), tail=(0, 0, 0))
    c.pose(24, neck=(-4, 0, 10), head=(6, 0, 12), tail=(0, 0, 20))
    c.pose(48, neck=(2, 0, -8), head=(-4, 0, -10), tail=(0, 0, -20))
    c.move(0, "hips", (0, 0, 0)).move(36, "hips", (0, 0, -0.008))
    clips.append((c, True))

    # walk: diagonal pairs
    c = T.Clip(arm, "walk", 28)
    for f, s in ((0, 1), (14, -1)):
        c.pose(f, leg_FL=(-22 * s, 0, 0), shin_FL=(12 if s > 0 else -6, 0, 0), leg_BR=(-22 * s, 0, 0), shin_BR=(-12 if s > 0 else 10, 0, 0),
               leg_FR=(22 * s, 0, 0), shin_FR=(-6 if s > 0 else 12, 0, 0), leg_BL=(22 * s, 0, 0), shin_BL=(10 if s > 0 else -12, 0, 0),
               neck=(4, 0, 4 * s), head=(-2, 0, 0), tail=(0, 0, 12 * s), spine=(0, 0, 3 * s))
    for f in (7, 21):
        c.move(f, "hips", (0, 0, 0.015 * g))
    c.move(0, "hips", (0, 0, 0)).move(14, "hips", (0, 0, 0))
    clips.append((c, True))

    # run: bounding gallop, fronts and backs together
    c = T.Clip(arm, "run", 16)
    c.pose(0, leg_FL=(-45, 0, 0), leg_FR=(-38, 0, 0), shin_FL=(20, 0, 0), shin_FR=(15, 0, 0),
           leg_BL=(40, 0, 0), leg_BR=(34, 0, 0), shin_BL=(-10, 0, 0), shin_BR=(-10, 0, 0), spine=(-6, 0, 0), neck=(-8, 0, 0), tail=(-30, 0, 0))
    c.pose(8, leg_FL=(40, 0, 0), leg_FR=(34, 0, 0), shin_FL=(-10, 0, 0), shin_FR=(-8, 0, 0),
           leg_BL=(-45, 0, 0), leg_BR=(-38, 0, 0), shin_BL=(35, 0, 0), shin_BR=(30, 0, 0), spine=(8, 0, 0), neck=(6, 0, 0), tail=(10, 0, 0))
    c.move(0, "hips", (0, 0, 0.02)).move(4, "hips", (0, 0, 0.08 * g)).move(8, "hips", (0, 0, 0.0)).move(12, "hips", (0, 0, 0.05 * g))
    clips.append((c, True))

    # die: flop onto the side
    c = T.Clip(arm, "die", 30)
    c.pose(0)
    c.pose(12, root=(0, -88, 0), leg_FL=(-30, 0, 0), leg_BL=(30, 0, 0), neck=(10, 0, 0), head=(20, 0, 0))
    c.pose(30, root=(0, -90, 0), leg_FL=(-35, 0, 0), leg_BL=(35, 0, 0), leg_FR=(-20, 0, 0), leg_BR=(20, 0, 0), neck=(15, 0, 0), head=(25, 0, 0))
    c.move(0, "root", (0, 0, 0)).move(12, "root", (0, 0, q.body_w * 0.9)).move(30, "root", (0, 0, q.body_w * 0.9))
    clips.append((c, False))

    # attack: graze (cow, goat) or bark / meow (dog, cat)
    grazer = q.horns is not None
    c = T.Clip(arm, "attack", 40 if grazer else 16, ["neck", "head", "tail"])
    if grazer:
        c.pose(0, neck=(0, 0, 0), head=(0, 0, 0))
        c.pose(10, neck=(55, 0, 0), head=(25, 0, 0))
        c.pose(18, neck=(58, 0, 5), head=(30, 0, 8))
        c.pose(26, neck=(56, 0, -5), head=(28, 0, -8))
        c.pose(40, neck=(0, 0, 0), head=(0, 0, 0))
    else:
        c.pose(0, neck=(0, 0, 0), head=(0, 0, 0), tail=(0, 0, 0))
        c.pose(4, neck=(-15, 0, 0), head=(-20, 0, 0), tail=(-20, 0, 20))
        c.pose(8, neck=(5, 0, 0), head=(10, 0, 0), tail=(-20, 0, -20))
        c.pose(16, neck=(0, 0, 0), head=(0, 0, 0), tail=(0, 0, 0))
    clips.append((c, False))
    return clips


# ------------------------------------------------------------------- chicken

def build_ayam(name="ayam"):
    bz = 0.2          # body centre
    hz = bz + 0.13    # head centre
    bones = [
        ("root", (0, 0, 0), (0, 0, 0.1), None),
        ("hips", (0, 0.06, bz), (0, -0.06, bz), "root"),
        ("neck", (0, -0.07, bz + 0.04), (0, -0.09, hz - 0.02), "hips"),
        ("head", (0, -0.09, hz - 0.02), (0, -0.18, hz), "neck"),
        ("tail", (0, 0.1, bz + 0.03), (0, 0.18, bz + 0.14), "hips"),
        ("wing_L", (0.09, -0.02, bz + 0.03), (0.12, 0.08, bz + 0.0), "hips"),
        ("wing_R", (-0.09, -0.02, bz + 0.03), (-0.12, 0.08, bz + 0.0), "hips"),
        ("leg_L", (0.045, 0.01, bz - 0.07), (0.045, 0.0, 0.07), "hips"),
        ("shin_L", (0.045, 0.0, 0.07), (0.045, 0.0, 0.0), "leg_L"),
        ("leg_R", (-0.045, 0.01, bz - 0.07), (-0.045, 0.0, 0.07), "hips"),
        ("shin_R", (-0.045, 0.0, 0.07), (-0.045, 0.0, 0.0), "leg_R"),
    ]
    arm = T.build_custom_armature(name + "_rig", bones)
    feather = T.mat("ayam_feather", 0xF7F3EA, 0.85)
    brown = T.mat("ayam_brown", 0xC0612B, 0.8)
    red = T.mat("ayam_comb", 0xE0302A, 0.6)
    yellow = T.mat("ayam_beak", 0xF2B632, 0.5)
    tailm = T.mat("ayam_tail", 0x2D5A3A, 0.4, 0.2)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    pupil = T.mat("eye_dark", 0x1A120C, 0.3)
    head_c = Vector((0, -0.105, hz))
    P = [
        T.sphere("body", Vector((0, 0.01, bz)), (0.11, 0.14, 0.11), brown, "hips", seg=16, rings=12),
        T.sphere("breast", Vector((0, -0.06, bz - 0.005)), (0.09, 0.07, 0.09), feather, "hips", seg=12, rings=8),
        T.cyl("neckm", Vector((0, -0.07, bz + 0.03)), Vector((0, -0.095, hz - 0.03)), 0.06, feather, r1=0.05, bone="neck", verts=10),
        T.sphere("head", head_c, (0.06, 0.065, 0.062), feather, "head", seg=12, rings=10),
        T.cyl("beak", head_c + Vector((0, -0.05, -0.005)), head_c + Vector((0, -0.1, -0.015)), 0.022, yellow, r1=0.0, bone="head", verts=8),
        T.sphere("wattle", head_c + Vector((0, -0.05, -0.045)), (0.015, 0.012, 0.028), red, "head", seg=8, rings=6),
    ]
    for y, z, r in ((-0.03, 0.06, 0.02), (-0.005, 0.07, 0.025), (0.02, 0.06, 0.02)):
        P.append(T.sphere("comb", head_c + Vector((0, y, z)), (0.012, r, r), red, "head", seg=8, rings=6))
    eyes(P, head_c, 0.06, 0.7, 0.55, white, pupil, "head", size=0.26, up=0.2)
    for sx, side in ((1, "L"), (-1, "R")):
        P.append(T.sphere("wing", Vector((sx * 0.105, 0.03, bz + 0.01)), (0.032, 0.095, 0.065), brown, "wing_" + side, seg=10, rings=8))
        P.append(T.cyl("thigh", Vector((sx * 0.045, 0.01, bz - 0.06)), Vector((sx * 0.045, 0.0, 0.07)), 0.014, yellow, bone="leg_" + side, verts=6))
        P.append(T.cyl("shank", Vector((sx * 0.045, 0.0, 0.07)), Vector((sx * 0.045, 0.0, 0.01)), 0.011, yellow, bone="shin_" + side, verts=6))
        for a in (-0.5, 0, 0.5):
            P.append(T.cyl("toe", Vector((sx * 0.045, 0.0, 0.008)), Vector((sx * 0.045 + math.sin(a) * 0.045, -math.cos(a) * 0.045, 0.005)), 0.007, yellow,
                           bone="shin_" + side, verts=4))
    for a in (-0.35, 0.0, 0.35):
        P.append(T.sphere("tailf", Vector((a * 0.04, 0.14, bz + 0.12)), (0.018, 0.04, 0.09), tailm, "tail", seg=8, rings=6, rot=(-0.6, a, 0)))
    mesh = T.join(P, name)
    T.skin(mesh, arm)
    return arm


def ayam_clips(arm):
    clips = []
    c = T.Clip(arm, "idle", 48)
    c.pose(0, neck=(0, 0, 0), head=(0, 0, 0))
    c.pose(8, neck=(-8, 0, 20), head=(0, 0, 25))
    c.pose(20, neck=(-8, 0, 20), head=(0, 0, 25))
    c.pose(28, neck=(0, 0, -15), head=(0, 0, -30))
    c.pose(40, neck=(0, 0, -15), head=(0, 0, -30))
    clips.append((c, True))

    c = T.Clip(arm, "walk", 16)
    for f, s in ((0, 1), (8, -1)):
        c.pose(f, leg_L=(-30 * s, 0, 0), shin_L=(20 if s > 0 else 5, 0, 0), leg_R=(30 * s, 0, 0), shin_R=(5 if s > 0 else 20, 0, 0),
               hips=(0, 0, 6 * s), tail=(0, 0, -8 * s))
    # the famous chicken head bob
    c.pose(0, neck=(-15, 0, 0)).pose(4, neck=(15, 0, 0)).pose(8, neck=(-15, 0, 0)).pose(12, neck=(15, 0, 0))
    c.move(0, "hips", (0, 0, 0)).move(4, "hips", (0, 0, 0.012)).move(8, "hips", (0, 0, 0)).move(12, "hips", (0, 0, 0.012))
    clips.append((c, True))

    c = T.Clip(arm, "run", 8)
    c.pose(0, leg_L=(-50, 0, 0), shin_L=(40, 0, 0), leg_R=(45, 0, 0), shin_R=(5, 0, 0), wing_L=(0, 60, 0), wing_R=(0, -60, 0), hips=(15, 0, 0), neck=(-25, 0, 0))
    c.pose(4, leg_L=(45, 0, 0), shin_L=(5, 0, 0), leg_R=(-50, 0, 0), shin_R=(40, 0, 0), wing_L=(0, -40, 0), wing_R=(0, 40, 0), hips=(15, 0, 0), neck=(-25, 0, 0))
    c.move(0, "hips", (0, 0, 0.0)).move(2, "hips", (0, 0, 0.05)).move(4, "hips", (0, 0, 0.0)).move(6, "hips", (0, 0, 0.05))
    clips.append((c, True))

    c = T.Clip(arm, "die", 24)
    c.pose(0)
    c.pose(10, root=(0, 85, 0), wing_L=(0, 60, 0), wing_R=(0, -60, 0), neck=(30, 0, 0))
    c.pose(24, root=(0, 90, 0), wing_L=(0, 70, 0), wing_R=(0, -30, 0), neck=(50, 0, 0), leg_L=(-40, 0, 0), leg_R=(-30, 0, 0))
    c.move(0, "root", (0, 0, 0)).move(10, "root", (0, 0, 0.08)).move(24, "root", (0, 0, 0.08))
    clips.append((c, False))

    c = T.Clip(arm, "attack", 14, ["neck", "head", "hips"])  # peck the ground
    c.pose(0, neck=(0, 0, 0), head=(0, 0, 0), hips=(0, 0, 0))
    c.pose(4, neck=(70, 0, 0), head=(30, 0, 0), hips=(20, 0, 0))
    c.pose(6, neck=(80, 0, 0), head=(35, 0, 0), hips=(22, 0, 0))
    c.pose(9, neck=(60, 0, 0), head=(20, 0, 0), hips=(18, 0, 0))
    c.pose(14, neck=(0, 0, 0), head=(0, 0, 0), hips=(0, 0, 0))
    clips.append((c, False))
    return clips


# ---------------------------------------------------------------------- bird

def build_burung(name="burung"):
    bz = 0.07
    bones = [
        ("root", (0, 0, 0), (0, 0, 0.05), None),
        ("hips", (0, 0.03, bz), (0, -0.03, bz), "root"),
        ("head", (0, -0.03, bz + 0.03), (0, -0.07, bz + 0.05), "hips"),
        ("tail", (0, 0.04, bz), (0, 0.1, bz - 0.01), "hips"),
        ("wing_L", (0.03, 0.0, bz + 0.02), (0.14, 0.01, bz + 0.02), "hips"),
        ("wing_R", (-0.03, 0.0, bz + 0.02), (-0.14, 0.01, bz + 0.02), "hips"),
    ]
    arm = T.build_custom_armature(name + "_rig", bones)
    body = T.mat("burung_body", 0x6B4A2E, 0.8)
    breast = T.mat("burung_breast", 0xE8D2A8, 0.8)
    wing = T.mat("burung_wing", 0x4A3220, 0.8)
    beak = T.mat("burung_beak", 0xF2B632, 0.5)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    pupil = T.mat("eye_dark", 0x1A120C, 0.3)
    P = [
        T.sphere("body", Vector((0, 0.0, bz)), (0.035, 0.055, 0.035), body, "hips", seg=12, rings=10),
        T.sphere("breast", Vector((0, -0.02, bz - 0.008)), (0.03, 0.035, 0.03), breast, "hips", seg=10, rings=8),
        T.sphere("head", Vector((0, -0.05, bz + 0.035)), 0.03, body, "head", seg=12, rings=10),
        T.cyl("beak", Vector((0, -0.075, bz + 0.033)), Vector((0, -0.1, bz + 0.03)), 0.009, beak, r1=0.0, bone="head", verts=6),
        T.sphere("tail", Vector((0, 0.07, bz)), (0.022, 0.04, 0.008), wing, "tail", seg=8, rings=6),
    ]
    eyes(P, Vector((0, -0.05, bz + 0.035)), 0.03, 0.6, 0.55, white, pupil, "head", size=0.3, up=0.15)
    for sx, side in ((1, "L"), (-1, "R")):
        P.append(T.sphere("wing", Vector((sx * 0.07, 0.01, bz + 0.02)), (0.06, 0.03, 0.008), wing, "wing_" + side, seg=10, rings=6))
        P.append(T.cyl("leg", Vector((sx * 0.012, 0.0, bz - 0.03)), Vector((sx * 0.012, 0.0, 0.0)), 0.004, beak, bone="hips", verts=4))
    mesh = T.join(P, name)
    T.skin(mesh, arm)
    return arm


def burung_clips(arm):
    clips = []
    c = T.Clip(arm, "idle", 40)  # perched: wings folded down, head twitches
    c.pose(0, wing_L=(0, 70, 0), wing_R=(0, -70, 0), head=(0, 0, 0), tail=(0, 0, 0))
    c.pose(10, head=(0, 0, 30))
    c.pose(22, head=(-10, 0, -25), tail=(-15, 0, 0))
    c.pose(30, head=(0, 0, 0), tail=(0, 0, 0))
    clips.append((c, True))

    c = T.Clip(arm, "walk", 12)  # little hops
    c.pose(0, wing_L=(0, 70, 0), wing_R=(0, -70, 0), hips=(0, 0, 0), tail=(10, 0, 0))
    c.pose(4, wing_L=(0, 55, 0), wing_R=(0, -55, 0), hips=(-10, 0, 0), tail=(-15, 0, 0))
    c.move(0, "hips", (0, 0, 0)).move(3, "hips", (0, 0, 0.03)).move(6, "hips", (0, 0, 0))
    clips.append((c, True))

    c = T.Clip(arm, "run", 8)  # flying: flap
    c.pose(0, wing_L=(0, -55, 0), wing_R=(0, 55, 0), tail=(-10, 0, 0), hips=(-5, 0, 0))
    c.pose(4, wing_L=(0, 50, 0), wing_R=(0, -50, 0), tail=(5, 0, 0), hips=(-5, 0, 0))
    c.move(0, "hips", (0, 0, 0.0)).move(4, "hips", (0, 0, 0.02))
    clips.append((c, True))

    c = T.Clip(arm, "die", 18)
    c.pose(0)
    c.pose(10, root=(0, 90, 0), wing_L=(0, 30, 0), wing_R=(0, -80, 0))
    c.pose(18, root=(0, 90, 0))
    c.move(0, "root", (0, 0, 0)).move(10, "root", (0, 0, 0.03)).move(18, "root", (0, 0, 0.03))
    clips.append((c, False))

    c = T.Clip(arm, "attack", 10, ["head", "hips"])  # peck
    c.pose(0, head=(0, 0, 0), hips=(0, 0, 0))
    c.pose(3, head=(50, 0, 0), hips=(30, 0, 0))
    c.pose(6, head=(10, 0, 0), hips=(10, 0, 0))
    c.pose(10, head=(0, 0, 0), hips=(0, 0, 0))
    clips.append((c, False))
    return clips


# --------------------------------------------------------------------- snake

SNAKE_SEGS = 9


def build_ular(name="ular"):
    seg_len = 0.17
    z = 0.045
    bones = [("root", (0, 0, 0), (0, 0, 0.05), None)]
    parent = "root"
    # head first (at -Y), body trailing along +Y
    y0 = -seg_len * 2
    for i in range(SNAKE_SEGS):
        name_i = "seg%d" % i
        bones.append((name_i, (0, y0 + i * seg_len, z), (0, y0 + (i + 1) * seg_len, z), parent))
        parent = name_i
    # the head hangs off the first segment
    bones.append(("head", (0, y0, z), (0, y0 - seg_len * 0.5, z), "seg0"))
    arm = T.build_custom_armature(name + "_rig", bones)
    skin_tex = T.mat("ular_skin", 0xFFFFFF, 0.5, image=stripes_texture("ular_bands", 0x3E8A3A, 0xE0C040))
    belly = T.mat("ular_belly", 0xE8E0A0, 0.6)
    tongue = T.mat("ular_tongue", 0xD02030, 0.5)
    white = T.mat("eye_white", 0xFFFFFF, 0.3, emit=0xFFFFFF, strength=0.3)
    pupil = T.mat("eye_dark", 0x1A120C, 0.3)
    P = []
    for i in range(SNAKE_SEGS):
        r = 0.045 * (1.0 - (i / SNAKE_SEGS) ** 1.6 * 0.8)
        r1 = 0.045 * (1.0 - ((i + 1) / SNAKE_SEGS) ** 1.6 * 0.8)
        a = Vector((0, y0 + i * seg_len - 0.02, z))
        b = Vector((0, y0 + (i + 1) * seg_len + 0.02, z))
        P.append(T.cyl("seg", a, b, r, skin_tex, r1=r1, bone="seg%d" % i, verts=12))
        P.append(T.sphere("joint", b, r1, skin_tex, "seg%d" % i, seg=10, rings=6))
    hc = Vector((0, y0 - 0.05, z + 0.01))
    P.append(T.sphere("head", hc, (0.06, 0.085, 0.045), skin_tex, "head", seg=14, rings=10))
    P.append(T.sphere("jaw", hc + Vector((0, -0.01, -0.02)), (0.05, 0.07, 0.025), belly, "head", seg=10, rings=6))
    P.append(T.cyl("tongue", hc + Vector((0, -0.08, -0.01)), hc + Vector((0, -0.13, -0.01)), 0.006, tongue, r1=0.004, bone="head", verts=4))
    eyes(P, hc, 0.06, 0.25, 0.6, white, pupil, "head", size=0.35, up=0.55)
    mesh = T.join(P, name)
    T.skin(mesh, arm)
    return arm


def ular_clips(arm):
    clips = []
    segs = ["seg%d" % i for i in range(SNAKE_SEGS)]

    def wave(clip, frames, amp, steps=8, lift=0.0):
        for k in range(steps + 1):
            f = int(frames * k / steps)
            ph = k / steps * math.tau
            rots = {s: (0, 0, amp * math.sin(ph - i * 0.9)) for i, s in enumerate(segs)}
            rots["head"] = (-lift, 0, -amp * 0.6 * math.sin(ph + 0.9))
            clip.pose(f, **rots)

    c = T.Clip(arm, "idle", 60)
    wave(c, 60, 6, lift=20)
    clips.append((c, True))
    c = T.Clip(arm, "walk", 32)
    wave(c, 32, 22, lift=8)
    clips.append((c, True))
    c = T.Clip(arm, "run", 16)
    wave(c, 16, 28, lift=5)
    clips.append((c, True))

    c = T.Clip(arm, "die", 24)
    c.pose(0)
    c.pose(24, root=(0, 160, 0), **{s: (0, 0, 12 if i % 2 else -12) for i, s in enumerate(segs)})
    c.move(0, "root", (0, 0, 0)).move(24, "root", (0, 0, 0.09))
    clips.append((c, False))

    c = T.Clip(arm, "attack", 14, ["head", "seg0", "seg1"])  # rear up and strike
    c.pose(0, head=(0, 0, 0), seg0=(0, 0, 0), seg1=(0, 0, 0))
    c.pose(5, head=(-10, 0, 0), seg0=(-40, 0, 0), seg1=(-25, 0, 0))
    c.pose(8, head=(20, 0, 0), seg0=(-15, 0, 0), seg1=(-10, 0, 0))
    c.pose(14, head=(0, 0, 0), seg0=(0, 0, 0), seg1=(0, 0, 0))
    clips.append((c, False))
    return clips


# --------------------------------------------------------------------- build

def _finish(name, arm, clips, preview, target, distance, height):
    names = []
    for clip, loop in clips:
        clip.build(loop=loop)
        names.append(clip.name)
    if preview:
        T.preview(preview, target=target, distance=distance, height=height, yaw=-35)
    path = T.export_glb("animal_" + name, [arm])
    return {"clips": names, "path": path}


def build(name, preview=None):
    T.clear_scene()
    T.reset_materials()
    if name in QUADS:
        q = QUADS[name]
        arm = build_quad(name, q)
        size = max(q.length, q.leg + q.body_r * 2)
        return _finish(name, arm, quad_clips(arm, q), preview, (0, 0, q.leg * 0.8), size * 2.2, size * 0.5)
    if name == "ayam":
        arm = build_ayam()
        return _finish(name, arm, ayam_clips(arm), preview, (0, 0, 0.18), 1.1, 0.25)
    if name == "burung":
        arm = build_burung()
        return _finish(name, arm, burung_clips(arm), preview, (0, 0, 0.07), 0.45, 0.1)
    if name == "ular":
        arm = build_ular()
        return _finish(name, arm, ular_clips(arm), preview, (0, 0.3, 0.05), 2.2, 0.8)
    raise KeyError(name)


ALL = ("ayam", "sapi", "kambing", "kucing", "anjing", "ular", "burung")


def build_all(preview_dir=None):
    import os
    out = {}
    for n in ALL:
        out[n] = build(n, os.path.join(preview_dir, "animal_%s.png" % n) if preview_dir else None)
    return out
