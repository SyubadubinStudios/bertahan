"""Static props for the four levels. Origin at the ground centre, fronts face -Y
(which becomes +Z in the game)."""

import math
import random

import bpy
import numpy as np
from mathutils import Vector

import btk as T


# ------------------------------------------------------------------ textures

def tex_roof(name, base):
    b = np.array(T.hexf(base))

    def fn(u, v):
        row = np.floor(v * 10)
        wave = np.abs(np.sin((u * 12 + row * 0.5) * math.pi))
        k = 0.72 + 0.28 * wave
        k = np.where(((v * 10) % 1) < 0.1, 0.55, k)
        return b[0] * k, b[1] * k, b[2] * k
    return T.make_image(name, 128, fn)


def tex_planks(name, base):
    b = np.array(T.hexf(base))
    rng = np.random.default_rng(3)
    tones = rng.uniform(0.8, 1.1, 16)

    def fn(u, v):
        idx = np.floor(u * 8).astype(int) % 16
        k = tones[idx] * (0.93 + 0.07 * np.sin(v * 60 + idx))
        k = np.where(((u * 8) % 1) < 0.06, 0.5, k)
        return b[0] * k, b[1] * k, b[2] * k
    return T.make_image(name, 128, fn)


def tex_plaster(name, base):
    b = np.array(T.hexf(base))
    rng = np.random.default_rng(11)
    noise = rng.random((128, 128))
    noise = (noise + np.roll(noise, 2, 0) + np.roll(noise, 2, 1)) / 3

    def fn(u, v):
        k = 0.9 + 0.12 * noise
        stain = np.clip((0.18 - v) * 4, 0, 1) * 0.25   # dirty bottom
        k = k - stain
        return b[0] * k, b[1] * k, b[2] * k
    return T.make_image(name, 128, fn)


def tex_stripes(name, a, b_):
    ca, cb = np.array(T.hexf(a)), np.array(T.hexf(b_))

    def fn(u, v):
        m = ((u * 6) % 1) < 0.5
        col = np.where(m[..., None], ca, cb)
        return col[..., 0], col[..., 1], col[..., 2]
    return T.make_image(name, 64, fn)


def tex_stone(name, base):
    b = np.array(T.hexf(base))
    rng = np.random.default_rng(5)
    noise = rng.random((128, 128))
    noise = (noise + np.roll(noise, 1, 0) + np.roll(noise, 1, 1) + np.roll(noise, 3, 0)) / 4

    def fn(u, v):
        k = 0.78 + 0.35 * noise
        moss = (noise > 0.62) & (v < 0.5)
        r, g, bb = b[0] * k, b[1] * k, b[2] * k
        return np.where(moss, 0.3, r), np.where(moss, 0.45, g), np.where(moss, 0.22, bb)
    return T.make_image(name, 128, fn)


def text_mesh(name, text, size, material, loc, rot=(math.pi / 2, 0, 0), extrude=0.02):
    """Converts a Blender text object into a mesh object (for signs)."""
    curve = bpy.data.curves.new(name, 'FONT')
    curve.body = text
    curve.size = size
    curve.extrude = extrude
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    obj = T.link(bpy.data.objects.new(name, curve))
    obj.location = loc
    obj.rotation_euler = rot
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph))
    mesh_obj = T.link(bpy.data.objects.new(name + "_mesh", mesh))
    mesh_obj.matrix_world = obj.matrix_world
    bpy.data.objects.remove(obj, do_unlink=True)
    mesh.materials.clear()
    mesh.materials.append(material)
    with bpy.context.temp_override(active_object=mesh_obj, selected_editable_objects=[mesh_obj], selected_objects=[mesh_obj]):
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return mesh_obj


# ------------------------------------------------------------------ materials

def M():
    return {
        "wood": T.mat("wood", 0x8B5A33, 0.8),
        "wood_dark": T.mat("wood_dark", 0x5A3A22, 0.85),
        "wood_light": T.mat("wood_light", 0xB88452, 0.8),
        "tile": T.mat("roof_tile", 0xFFFFFF, 0.75, image=tex_roof("roof_tile_tex", 0xB5532F)),
        "tile_dark": T.mat("roof_tile_dark", 0xFFFFFF, 0.75, image=tex_roof("roof_dark_tex", 0x6E3A2A)),
        "zinc": T.mat("roof_zinc", 0xFFFFFF, 0.45, 0.6, image=tex_roof("roof_zinc_tex", 0x9AA4A8)),
        "thatch": T.mat("thatch", 0xFFFFFF, 0.95, image=tex_roof("thatch_tex", 0xB8954E)),
        "planks": T.mat("planks", 0xFFFFFF, 0.85, image=tex_planks("planks_tex", 0x8E5E36)),
        "bamboo": T.mat("bamboo", 0x9CB548, 0.6),
        "bamboo_dry": T.mat("bamboo_dry", 0xC9B26A, 0.7),
        "glass": T.mat("window_glass", 0x2E4A5C, 0.15, 0.2),
        "glass_lit": T.mat("window_lit", 0xFFD98A, 0.4, emit=0xFFC66A, strength=2.5),
        "white": T.mat("paint_white", 0xF1EEE6, 0.8),
        "concrete": T.mat("concrete", 0xA9A49A, 0.9),
        "dark": T.mat("dark_metal", 0x2B2E33, 0.5, 0.7),
        "metal": T.mat("metal", 0x9098A0, 0.35, 0.9),
        "red": T.mat("paint_red", 0xC4302B, 0.6),
        "green": T.mat("paint_green", 0x2E8B57, 0.6),
        "leaf": T.mat("leaf", 0x4C9A3A, 0.7),
        "leaf_dark": T.mat("leaf_dark", 0x2F6E2C, 0.75),
        "leaf_banana": T.mat("leaf_banana", 0x6DB33F, 0.6),
        "trunk": T.mat("trunk", 0x6B4A2E, 0.9),
        "trunk_palm": T.mat("trunk_palm", 0x8A7355, 0.9),
        "stone": T.mat("stone", 0xFFFFFF, 0.9, image=tex_stone("stone_tex", 0x8C8C84)),
        "rubber": T.mat("rubber", 0x1C1C1E, 0.8),
        "lamp": T.mat("lamp_glow", 0xFFF2C8, 0.3, emit=0xFFE6A8, strength=12.0),
        "flame": T.mat("flame", 0xFF9A2A, 0.5, emit=0xFF8A1A, strength=10.0),
        "dirt": T.mat("dirt", 0x6E5236, 0.95),
    }


# ------------------------------------------------------------------ houses

def rumah(name, wall_color, roof="tile", w=4.2, d=3.6, h=2.7, seed=0):
    m = M()
    wall = T.mat(name + "_wall", 0xFFFFFF, 0.85, image=tex_plaster(name + "_wall_tex", wall_color))
    trim = T.mat(name + "_trim", {0: 0x3E6FA8, 1: 0x2F7D4E, 2: 0x9C3B2B, 3: 0x6A4A8C}[seed % 4], 0.6)
    P = [
        T.box("base", (0, 0, 0.15), (w + 0.2, d + 0.2, 0.3), m["concrete"]),
        T.box("walls", (0, 0, 0.3 + h / 2), (w, d, h), wall),
        T.gable_roof("roof", w, d, 1.5, 0.45, m[roof], loc=(0, 0, 0.3 + h), rot=(0, 0, 0)),
    ]
    # gable triangles fill
    for sx in (-1, 1):
        P.append(T.poly_prism("gable", [(-d / 2, 0), (d / 2, 0), (0, 1.45)], -0.05, 0.05, wall,
                              loc=(sx * (w / 2 - 0.05), 0, 0.3 + h), rot=(math.pi / 2, 0, math.pi / 2)))
    # door and windows on the front (-Y)
    fy = -d / 2 - 0.02
    P.append(T.box("door", (0.0, fy, 0.3 + 1.05), (0.95, 0.06, 2.1), m["wood_dark"]))
    P.append(T.box("door_frame", (0.0, fy - 0.01, 0.3 + 1.1), (1.15, 0.04, 2.25), trim))
    P.append(T.sphere("knob", (0.3, fy - 0.05, 1.3), 0.04, m["metal"], seg=6, rings=4))
    for sx in (-1, 1):
        x = sx * (w / 2 - 0.95)
        P.append(T.box("win_frame", (x, fy - 0.01, 1.75), (1.0, 0.05, 1.05), trim))
        P.append(T.box("win", (x, fy - 0.03, 1.75), (0.8, 0.04, 0.85), m["glass_lit"] if seed % 2 == 0 and sx > 0 else m["glass"]))
        P.append(T.box("win_bar", (x, fy - 0.06, 1.75), (0.05, 0.03, 0.85), trim))
        P.append(T.box("shutter", (x - sx * 0.62, fy - 0.03, 1.75), (0.35, 0.04, 0.95), trim, rot=(0, 0, sx * 0.3)))
    # porch roof and posts
    P.append(T.box("porch_roof", (0, -d / 2 - 0.8, 0.3 + h - 0.1), (w * 0.8, 1.7, 0.08), m[roof], rot=(-0.18, 0, 0)))
    for sx in (-1, 1):
        P.append(T.box("post", (sx * w * 0.36, -d / 2 - 1.5, 0.3 + h / 2 - 0.2), (0.14, 0.14, h - 0.3), m["wood"]))
    P.append(T.box("porch_floor", (0, -d / 2 - 0.8, 0.12), (w * 0.8, 1.7, 0.24), m["concrete"]))
    P.append(T.box("step", (0, -d / 2 - 1.8, 0.06), (1.2, 0.4, 0.12), m["concrete"]))
    # side details: potted plant and a bench
    P.append(T.box("bench", (-w * 0.25, -d / 2 - 0.5, 0.45), (1.2, 0.35, 0.08), m["wood"]))
    for sx in (-1, 1):
        P.append(T.box("bench_leg", (-w * 0.25 + sx * 0.5, -d / 2 - 0.5, 0.33), (0.06, 0.3, 0.3), m["wood_dark"]))
    P.append(T.cyl("pot", (w * 0.32, -d / 2 - 0.4, 0.24), (w * 0.32, -d / 2 - 0.4, 0.55), 0.16, T.mat("clay", 0xB5652F, 0.8), r1=0.2, verts=10))
    P.append(T.sphere("plant", (w * 0.32, -d / 2 - 0.4, 0.8), (0.3, 0.3, 0.35), m["leaf"], seg=10, rings=6))
    P.append(T.box("chimney", (w * 0.25, d * 0.15, 0.3 + h + 1.1), (0.3, 0.3, 0.8), m["concrete"]))
    return T.join(P, name)


def rumah_panggung(name="rumah_panggung"):
    m = M()
    w, d, h, lift = 4.5, 3.8, 2.4, 1.2
    P = [T.box("floor", (0, 0, lift), (w + 0.3, d + 1.6, 0.18), m["wood"]),
         T.box("walls", (0, 0.4, lift + h / 2), (w, d, h), m["planks"]),
         T.gable_roof("roof", w, d, 1.6, 0.6, m["tile_dark"], loc=(0, 0.4, lift + h))]
    for sx in (-1, 1):
        P.append(T.poly_prism("gable", [(-d / 2, 0), (d / 2, 0), (0, 1.55)], -0.05, 0.05, m["planks"],
                              loc=(sx * (w / 2 - 0.05), 0.4, lift + h), rot=(math.pi / 2, 0, math.pi / 2)))
    for x in (-w / 2, 0, w / 2):
        for y in (-d / 2 - 0.4, d / 2 + 0.4):
            P.append(T.box("stilt", (x, y, lift / 2), (0.2, 0.2, lift), m["wood_dark"]))
    fy = 0.4 - d / 2 - 0.02
    P.append(T.box("door", (0.6, fy, lift + 1.0), (0.9, 0.06, 1.9), m["wood_dark"]))
    P.append(T.box("win", (-1.2, fy - 0.02, lift + 1.4), (0.9, 0.05, 0.8), m["glass"]))
    P.append(T.box("win_frame", (-1.2, fy - 0.01, lift + 1.4), (1.05, 0.04, 0.95), m["wood_light"]))
    # veranda railing
    for i in range(9):
        x = -w / 2 + i * w / 8
        P.append(T.box("rail_post", (x, -d / 2 - 0.35, lift + 0.4), (0.06, 0.06, 0.7), m["wood_light"]))
    P.append(T.box("rail", (0, -d / 2 - 0.35, lift + 0.75), (w, 0.08, 0.06), m["wood_light"]))
    # stairs
    for i in range(5):
        P.append(T.box("stair", (1.6, -d / 2 - 0.8 - i * 0.28, lift - 0.2 - i * 0.24), (0.9, 0.3, 0.06), m["wood"]))
    return T.join(P, name)


def masjid(name="masjid"):
    m = M()
    wall = T.mat("masjid_wall", 0xFFFFFF, 0.85, image=tex_plaster("masjid_wall_tex", 0xE9F1E6))
    dome = T.mat("masjid_dome", 0x2FA37C, 0.45, 0.2)
    gold = T.mat("gold", 0xE0B040, 0.35, 0.8)
    P = [T.box("base", (0, 0, 0.2), (8.4, 8.4, 0.4), m["concrete"]),
         T.box("hall", (0, 0, 2.4), (7.5, 7.5, 4.0), wall),
         T.box("roof1", (0, 0, 4.55), (8.2, 8.2, 0.3), dome),
         T.box("drum", (0, 0, 5.2), (4.6, 4.6, 1.0), wall),
         T.sphere("dome", (0, 0, 5.8), (2.6, 2.6, 2.6), dome, seg=24, rings=14),
         T.cyl("spire", (0, 0, 8.3), (0, 0, 9.3), 0.08, gold, r1=0.02, verts=8),
         T.torus("crescent", (0, 0, 9.5), 0.22, 0.05, gold, rot=(math.pi / 2, 0, 0), seg=16, ring_seg=5)]
    # arched door and windows (approximated with rounded boxes)
    P.append(T.box("door", (0, -3.78, 1.6), (1.6, 0.1, 2.6), m["wood_dark"]))
    P.append(T.cyl("door_arch", (0, -3.72, 2.9), (0, -3.85, 2.9), 0.8, m["wood_dark"], verts=16))
    for x in (-2.6, 2.6):
        P.append(T.box("win", (x, -3.78, 2.4), (1.0, 0.1, 1.6), m["glass"]))
        P.append(T.cyl("win_arch", (x, -3.72, 3.2), (x, -3.85, 3.2), 0.5, m["glass"], verts=12))
    # minaret
    mx, my = 4.8, 3.2
    P += [T.cyl("minaret", (mx, my, 0.4), (mx, my, 9.0), 0.7, wall, r1=0.6, verts=12),
          T.cyl("balcony", (mx, my, 7.2), (mx, my, 7.5), 0.95, dome, verts=12),
          T.sphere("minaret_dome", (mx, my, 9.2), 0.75, dome, seg=14, rings=10),
          T.cyl("minaret_spire", (mx, my, 9.9), (mx, my, 10.6), 0.05, gold, r1=0.01, verts=6)]
    # beduk (drum) on the porch
    P.append(T.cyl("beduk", (-3.2, -4.6, 0.9), (-2.2, -4.6, 0.9), 0.45, m["wood"], verts=14))
    P.append(T.box("beduk_stand", (-2.7, -4.6, 0.3), (1.2, 0.6, 0.6), m["wood_dark"]))
    return T.join(P, name)


def ruko(name, color, sign_text, seed=0):
    """Two storey shophouse for Pasar Lama."""
    m = M()
    wall = T.mat(name + "_wall", 0xFFFFFF, 0.85, image=tex_plaster(name + "_wall_tex", color))
    awn = T.mat(name + "_awning", 0xFFFFFF, 0.7, image=tex_stripes(name + "_awn_tex", [0xD63B30, 0x2F6FB5, 0x2E9B57][seed % 3], 0xF2EEE2))
    sign = T.mat(name + "_sign", [0xF2C94C, 0xE56B2F, 0x3FA7D6][seed % 3], 0.6)
    ink = T.mat("sign_ink", 0x1D1D22, 0.6)
    w, d, h = 4.0, 5.0, 6.4
    P = [T.box("body", (0, 0, h / 2), (w, d, h), wall),
         T.box("roof", (0, 0.3, h + 0.15), (w + 0.2, d + 0.6, 0.3), m["concrete"]),
         T.box("shutter", (0, -d / 2 - 0.02, 1.4), (w * 0.8, 0.05, 2.8), m["metal"]),
         T.box("awning", (0, -d / 2 - 0.7, 3.1), (w + 0.1, 1.5, 0.06), awn, rot=(-0.25, 0, 0)),
         T.box("sign", (0, -d / 2 - 0.08, 3.9), (w * 0.85, 0.08, 0.7), sign),
         text_mesh("sign_text", sign_text, 0.38, ink, (0, -d / 2 - 0.14, 3.9)),
         T.box("balcony", (0, -d / 2 - 0.3, 4.3), (w * 0.9, 0.6, 0.12), m["concrete"])]
    for x in (-1.1, 1.1):
        P.append(T.box("win2", (x, -d / 2 - 0.02, 5.2), (1.1, 0.05, 1.2), m["glass_lit"] if (seed + (x > 0)) % 2 else m["glass"]))
    for i in range(12):
        P.append(T.box("shutter_line", (0, -d / 2 - 0.05, 0.2 + i * 0.22), (w * 0.8, 0.02, 0.03), m["dark"]))
    return T.join(P, name)


def kios(name="kios", seed=0):
    m = M()
    awn = T.mat(name + "_awning", 0xFFFFFF, 0.7, image=tex_stripes(name + "_awn_tex", [0xE2553E, 0x3A86C8, 0xE0A92E][seed % 3], 0xF4F0E4))
    P = [T.box("counter", (0, -0.2, 0.5), (2.4, 1.0, 1.0), m["planks"]),
         T.box("awning", (0, -0.4, 2.35), (2.8, 1.8, 0.05), awn, rot=(-0.2, 0, 0))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.box("pole", (sx * 1.2, sy * 0.6 - 0.2, 1.2), (0.08, 0.08, 2.4), m["wood"]))
    fruit = [0xF2C230, 0xE5542B, 0x6DB33F, 0xF28C28, 0x9B3BB5]
    rng = random.Random(seed)
    for i in range(4):
        x = -0.85 + i * 0.57
        P.append(T.cyl("basket", (x, -0.3, 1.0), (x, -0.3, 1.18), 0.22, m["bamboo_dry"], r1=0.26, verts=10))
        c = T.mat("fruit%d" % (i % 5), fruit[(i + seed) % 5], 0.5)
        for k in range(5):
            P.append(T.sphere("fruit", (x + rng.uniform(-0.12, 0.12), -0.3 + rng.uniform(-0.12, 0.12), 1.24 + rng.uniform(0, 0.06)), 0.07, c, seg=8, rings=5))
    P.append(T.box("crate_under", (0.7, -0.9, 0.2), (0.5, 0.4, 0.4), m["wood_light"]))
    return T.join(P, name)


def gapura(name="gapura"):
    m = M()
    board = T.mat("sign_board", 0x6B4526, 0.8)
    text = T.mat("sign_text_cream", 0xF4E3B5, 0.6)
    P = []
    for sx in (-1, 1):
        for k in range(3):
            x = sx * (2.6 + k * 0.18)
            P.append(T.cyl("pillar", (x, 0, 0), (x, 0, 4.4 - k * 0.2), 0.1, m["bamboo"], verts=10))
        # fence wings
        for i in range(8):
            x = sx * (3.3 + i * 0.25)
            P.append(T.cyl("wing", (x, 0.05, 0), (x, 0.05, 2.2 + 0.2 * math.sin(i)), 0.06, m["bamboo_dry"], verts=6))
        P.append(T.box("wing_rail", (sx * 4.2, 0.05, 1.5), (2.1, 0.08, 0.08), m["bamboo_dry"]))
    P.append(T.box("beam", (0, 0, 4.25), (6.2, 0.25, 0.2), m["bamboo_dry"]))
    P.append(T.box("board", (0, -0.12, 3.75), (4.2, 0.12, 0.8), board, bevel=0.04))
    P.append(text_mesh("title", "KAMPUNG DAMAI", 0.5, text, (0, -0.2, 3.75), extrude=0.03))
    # red and white flags
    for sx in (-1, 1):
        P.append(T.cyl("flag_pole", (sx * 2.9, 0, 4.4), (sx * 2.9, 0, 5.4), 0.025, m["wood"], verts=6))
        P.append(T.box("flag_r", (sx * 2.9 + 0.25, 0, 5.28), (0.5, 0.02, 0.16), m["red"]))
        P.append(T.box("flag_w", (sx * 2.9 + 0.25, 0, 5.12), (0.5, 0.02, 0.16), m["white"]))
    return T.join(P, name)


def pos_ronda(name="pos_ronda"):
    m = M()
    P = [T.box("floor", (0, 0, 0.55), (2.4, 2.0, 0.12), m["planks"]),
         T.gable_roof("roof", 2.4, 2.0, 0.9, 0.35, m["zinc"], loc=(0, 0, 2.5))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.box("post", (sx * 1.1, sy * 0.9, 1.25), (0.1, 0.1, 2.5), m["wood"]))
    P.append(T.box("back", (0, 0.95, 1.1), (2.3, 0.06, 1.0), m["planks"]))
    # kentongan: hollow log alarm hanging at the front
    P.append(T.cyl("kentongan", (-0.9, -1.0, 1.1), (-0.9, -1.0, 1.9), 0.13, m["wood_light"], verts=10))
    P.append(T.box("kentongan_slot", (-0.9, -1.13, 1.5), (0.05, 0.02, 0.5), m["wood_dark"]))
    P.append(T.box("sign", (0.4, -1.0, 2.2), (1.1, 0.05, 0.3), m["white"]))
    P.append(text_mesh("sign_text", "POS RONDA", 0.2, T.mat("sign_ink", 0x1D1D22, 0.6), (0.4, -1.04, 2.2), extrude=0.01))
    return T.join(P, name)


def gubuk(name="gubuk"):
    m = M()
    P = [T.box("floor", (0, 0, 0.7), (2.2, 2.2, 0.1), m["bamboo_dry"]),
         T.gable_roof("roof", 2.2, 2.2, 1.1, 0.5, m["thatch"], loc=(0, 0, 2.3))]
    for sx in (-1, 1):
        for sy in (-1, 1):
            P.append(T.cyl("post", (sx * 1.0, sy * 1.0, 0), (sx * 1.0, sy * 1.0, 2.3), 0.07, m["bamboo_dry"], verts=8))
    return T.join(P, name)


def sumur(name="sumur"):
    m = M()
    P = [T.cyl("ring", (0, 0, 0), (0, 0, 0.8), 0.7, m["stone"], verts=16),
         T.cyl("water", (0, 0, 0.5), (0, 0, 0.75), 0.6, T.mat("well_water", 0x1E3B4A, 0.1), verts=16),
         T.gable_roof("roof", 1.6, 1.4, 0.6, 0.2, m["tile"], loc=(0, 0, 2.2))]
    for sx in (-1, 1):
        P.append(T.box("post", (sx * 0.75, 0, 1.2), (0.1, 0.1, 1.9), m["wood"]))
    P.append(T.cyl("axle", (-0.75, 0, 1.7), (0.75, 0, 1.7), 0.05, m["wood_dark"], verts=8))
    P.append(T.cyl("bucket", (0.1, 0, 1.05), (0.1, 0, 1.3), 0.13, m["metal"], r1=0.15, verts=10))
    return T.join(P, name)


# ------------------------------------------------------------------ nature

def pohon_pisang(name="pohon_pisang"):
    m = M()
    P = [T.cyl("trunk", (0, 0, 0), (0, 0, 2.2), 0.16, T.mat("banana_trunk", 0x7E8E4A, 0.8), r1=0.1, verts=10)]
    for i in range(7):
        a = i / 7 * math.tau
        tilt = 0.5 + (i % 3) * 0.2
        base = Vector((0, 0, 2.1))
        tip = base + Vector((math.cos(a) * 1.8, math.sin(a) * 1.8, 0.6 - tilt * 0.8))
        mid = (base + tip) / 2 + Vector((0, 0, 0.3))
        leaf = T.sphere("leaf", mid, (1.0, 0.28, 0.03), m["leaf_banana"], seg=12, rings=6,
                        rot=(0, -math.atan2(tip.z - base.z, 1.8), a))
        P.append(leaf)
        P.append(T.cyl("rib", base, tip, 0.02, T.mat("banana_rib", 0x9FBF5A, 0.7), verts=4))
    P.append(T.sphere("bunch", (0.25, 0, 1.8), (0.14, 0.14, 0.25), T.mat("banana_fruit", 0x9ACD32, 0.6), seg=8, rings=6))
    P.append(T.cyl("heart", (0.25, 0, 1.55), (0.3, 0, 1.2), 0.07, T.mat("banana_heart", 0x8B2A4A, 0.6), r1=0.0, verts=8))
    return T.join(P, name)


def pohon_kelapa(name="pohon_kelapa", lean=0.25):
    m = M()
    P = []
    pts = [Vector((math.sin(t * 1.2) * lean * t * 2, 0, t * 6.0)) for t in [i / 6 for i in range(7)]]
    for i in range(6):
        P.append(T.cyl("trunk", pts[i], pts[i + 1], 0.2 - i * 0.015, m["trunk_palm"], r1=0.19 - i * 0.015, verts=10))
        P.append(T.torus("ring", pts[i], 0.2 - i * 0.015, 0.025, m["trunk"], seg=10, ring_seg=4))
    top = pts[-1]
    for i in range(9):
        a = i / 9 * math.tau
        tip = top + Vector((math.cos(a) * 2.6, math.sin(a) * 2.6, -1.1))
        mid = (top + tip) / 2 + Vector((0, 0, 0.55))
        P.append(T.cyl("frond_a", top, mid, 0.05, m["leaf"], r1=0.04, verts=4, smooth=False))
        P.append(T.cyl("frond_b", mid, tip, 0.04, m["leaf"], r1=0.0, verts=4, smooth=False))
        for k in range(5):
            t = 0.2 + k * 0.16
            p = top.lerp(mid, t * 2) if t < 0.5 else mid.lerp(tip, (t - 0.5) * 2)
            side = Vector((-math.sin(a), math.cos(a), 0)) * 0.55
            P.append(T.cyl("leaflet", p, p + side + Vector((0, 0, -0.25)), 0.035, m["leaf_dark"], r1=0.0, verts=3, smooth=False))
            P.append(T.cyl("leaflet", p, p - side + Vector((0, 0, -0.25)), 0.035, m["leaf_dark"], r1=0.0, verts=3, smooth=False))
    for i in range(4):
        a = i / 4 * math.tau + 0.4
        P.append(T.sphere("coconut", top + Vector((math.cos(a) * 0.22, math.sin(a) * 0.22, -0.25)), 0.16, T.mat("coconut", 0x5E7A2A, 0.6), seg=8, rings=6))
    return T.join(P, name)


def pohon(name="pohon_mangga", scale=1.0, seed=1):
    m = M()
    rng = random.Random(seed)
    s = scale
    P = [T.cyl("trunk", (0, 0, 0), (0, 0, 2.6 * s), 0.28 * s, m["trunk"], r1=0.18 * s, verts=10)]
    for i in range(3):
        a = i * 2.1
        P.append(T.cyl("branch", (0, 0, 2.2 * s), (math.cos(a) * 0.9 * s, math.sin(a) * 0.9 * s, 3.2 * s), 0.1 * s, m["trunk"], r1=0.06 * s, verts=6))
    for i in range(7):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(0.3, 1.2) * s
        P.append(T.sphere("canopy", (math.cos(a) * r, math.sin(a) * r, (3.4 + rng.uniform(-0.3, 0.9)) * s),
                          rng.uniform(1.0, 1.5) * s, m["leaf"] if i % 2 else m["leaf_dark"], seg=12, rings=8))
    return T.join(P, name)


def pohon_beringin(name="pohon_beringin"):
    m = M()
    rng = random.Random(4)
    bark = T.mat("banyan_bark", 0x4E3B2C, 0.95)
    P = []
    for i in range(5):
        a = i / 5 * math.tau
        P.append(T.cyl("trunk", (math.cos(a) * 0.5, math.sin(a) * 0.5, 0), (math.cos(a) * 0.2, math.sin(a) * 0.2, 5.0), 0.45, bark, r1=0.3, verts=8))
    for i in range(5):
        a = i / 5 * math.tau + 0.3
        P.append(T.cyl("limb", (0, 0, 4.2), (math.cos(a) * 3.5, math.sin(a) * 3.5, 6.2), 0.3, bark, r1=0.14, verts=6))
    for i in range(12):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(1.0, 4.2)
        P.append(T.sphere("canopy", (math.cos(a) * r, math.sin(a) * r, rng.uniform(6.0, 8.2)), rng.uniform(1.8, 2.8),
                          m["leaf_dark"] if i % 3 else T.mat("leaf_deep", 0x24522A, 0.8), seg=12, rings=8))
    # hanging aerial roots
    for i in range(22):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(1.2, 4.0)
        top = Vector((math.cos(a) * r, math.sin(a) * r, rng.uniform(5.2, 6.2)))
        P.append(T.cyl("aerial_root", top, top + Vector((rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), -rng.uniform(2.5, 5.0))),
                       0.04, bark, r1=0.02, verts=4, smooth=False))
    return T.join(P, name)


def rumpun_bambu(name="rumpun_bambu"):
    m = M()
    rng = random.Random(9)
    P = []
    for i in range(11):
        a = rng.uniform(0, math.tau)
        r = rng.uniform(0, 0.6)
        base = Vector((math.cos(a) * r, math.sin(a) * r, 0))
        tip = base + Vector((math.cos(a) * rng.uniform(0.6, 1.8), math.sin(a) * rng.uniform(0.6, 1.8), rng.uniform(5.5, 7.5)))
        P.append(T.cyl("culm", base, tip, 0.07, m["bamboo"], r1=0.04, verts=6))
        for k in range(3):
            p = base.lerp(tip, 0.55 + k * 0.15)
            P.append(T.sphere("foliage", p + Vector((rng.uniform(-0.4, 0.4), rng.uniform(-0.4, 0.4), 0.2)), (0.7, 0.7, 0.35),
                              m["leaf"], seg=8, rings=5))
    return T.join(P, name)


def semak(name="semak", seed=2):
    m = M()
    rng = random.Random(seed)
    P = []
    for i in range(5):
        P.append(T.sphere("bush", (rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), rng.uniform(0.3, 0.55)),
                          rng.uniform(0.4, 0.65), m["leaf"] if i % 2 else m["leaf_dark"], seg=10, rings=6))
    if seed % 2:
        flower = T.mat("flower", 0xF25C8C, 0.5)
        for i in range(6):
            P.append(T.sphere("flower", (rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6), rng.uniform(0.6, 0.9)), 0.06, flower, seg=6, rings=4))
    return T.join(P, name)


def rumput(name="rumput"):
    m = M()
    rng = random.Random(3)
    P = []
    for i in range(9):
        a = rng.uniform(0, math.tau)
        base = Vector((rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25), 0))
        P.append(T.cyl("blade", base, base + Vector((math.cos(a) * 0.15, math.sin(a) * 0.15, rng.uniform(0.35, 0.6))), 0.03,
                       m["leaf"] if i % 2 else T.mat("grass_light", 0x7DB84A, 0.7), r1=0.0, verts=3, smooth=False))
    return T.join(P, name)


def padi(name="padi"):
    """A clump of rice plants (instanced hundreds of times in the paddies)."""
    m = M()
    rng = random.Random(8)
    P = []
    for i in range(7):
        a = i / 7 * math.tau
        tip = Vector((math.cos(a) * 0.14, math.sin(a) * 0.14, rng.uniform(0.55, 0.75)))
        P.append(T.cyl("stalk", (0, 0, 0), tip, 0.025, T.mat("rice_green", 0x7CB342, 0.7), r1=0.0, verts=3, smooth=False))
    P.append(T.cyl("grain", (0, 0, 0.5), (0.18, 0.05, 0.62), 0.03, T.mat("rice_grain", 0xD8C35A, 0.7), r1=0.01, verts=4))
    return T.join(P, name)


def batu(name="batu", seed=1):
    m = M()
    rng = random.Random(seed)
    P = [T.sphere("rock", (rng.uniform(-0.2, 0.2), 0, 0.2), (rng.uniform(0.4, 0.6), rng.uniform(0.35, 0.5), rng.uniform(0.25, 0.4)),
                  m["stone"], seg=8, rings=5, smooth=False) for _ in range(2)]
    return T.join(P, name)


# ------------------------------------------------------------------ village clutter

def pagar_bambu(name="pagar_bambu"):
    m = M()
    P = [T.box("rail", (0, 0, 0.5), (2.0, 0.06, 0.06), m["bamboo_dry"]), T.box("rail2", (0, 0, 1.0), (2.0, 0.06, 0.06), m["bamboo_dry"])]
    for i in range(9):
        x = -0.9 + i * 0.225
        P.append(T.cyl("stake", (x, 0.04, 0), (x, 0.04, 1.2 + 0.08 * math.sin(i * 1.7)), 0.045, m["bamboo"] if i % 3 else m["bamboo_dry"], r1=0.03, verts=6))
    return T.join(P, name)


def barikade(name="barikade"):
    """Czech-hedgehog style wooden barricade with barbed wire."""
    m = M()
    P = []
    for i, a in enumerate((0.0, 2.1, 4.2)):
        P.append(T.cyl("x_beam", (math.cos(a) * -0.8, math.sin(a) * -0.8, 0.0), (math.cos(a) * 0.8, math.sin(a) * 0.8, 1.2), 0.07, m["wood"], verts=6))
    P.append(T.torus("wire", (0, 0, 0.7), 0.7, 0.012, m["metal"], rot=(math.pi / 2, 0, 0), seg=18, ring_seg=3))
    P.append(T.torus("wire2", (0, 0, 0.7), 0.7, 0.012, m["metal"], rot=(math.pi / 2, 0, math.pi / 2), seg=18, ring_seg=3))
    return T.join(P, name)


def motor(name="motor", color=0xC62828):
    m = M()
    body = T.mat(name + "_paint", color, 0.35, 0.3)
    P = [T.cyl("wheel_f", (0.06, -0.62, 0.3), (-0.06, -0.62, 0.3), 0.3, m["rubber"], verts=14),
         T.cyl("wheel_r", (0.07, 0.6, 0.3), (-0.07, 0.6, 0.3), 0.3, m["rubber"], verts=14),
         T.cyl("hub_f", (0.07, -0.62, 0.3), (-0.07, -0.62, 0.3), 0.12, m["metal"], verts=10),
         T.cyl("hub_r", (0.08, 0.6, 0.3), (-0.08, 0.6, 0.3), 0.12, m["metal"], verts=10),
         T.box("body", (0, 0.25, 0.62), (0.34, 0.9, 0.42), body, bevel=0.1),
         T.box("floor", (0, -0.2, 0.38), (0.3, 0.5, 0.08), m["dark"]),
         T.box("front", (0, -0.55, 0.75), (0.3, 0.2, 0.75), body, rot=(-0.3, 0, 0), bevel=0.06),
         T.box("seat", (0, 0.35, 0.88), (0.3, 0.7, 0.12), m["rubber"], bevel=0.05),
         T.cyl("handle", (-0.36, -0.68, 1.12), (0.36, -0.68, 1.12), 0.025, m["dark"], verts=6),
         T.sphere("headlight", (0, -0.7, 0.95), (0.1, 0.05, 0.08), m["lamp"], seg=8, rings=5),
         T.box("mirror_l", (0.34, -0.66, 1.3), (0.1, 0.02, 0.06), m["metal"]),
         T.box("mirror_r", (-0.34, -0.66, 1.3), (0.1, 0.02, 0.06), m["metal"]),
         T.box("tail", (0, 0.78, 0.7), (0.2, 0.05, 0.06), m["red"])]
    return T.join(P, name)


def gerobak(name="gerobak"):
    m = M()
    P = [T.box("bed", (0, 0, 0.75), (1.4, 2.2, 0.1), m["planks"])]
    for sx in (-1, 1):
        P.append(T.box("side", (sx * 0.7, 0, 1.0), (0.06, 2.2, 0.45), m["planks"]))
        P.append(T.torus("wheel", (sx * 0.82, 0.2, 0.55), 0.5, 0.06, m["wood_dark"], rot=(0, math.pi / 2, 0), seg=18, ring_seg=5))
        for k in range(6):
            a = k / 6 * math.pi
            P.append(T.cyl("spoke", (sx * 0.82, 0.2 - math.cos(a) * 0.5, 0.55 - math.sin(a) * 0.5), (sx * 0.82, 0.2 + math.cos(a) * 0.5, 0.55 + math.sin(a) * 0.5), 0.025, m["wood"], verts=4))
        P.append(T.cyl("handle", (sx * 0.5, -1.1, 0.8), (sx * 0.45, -2.2, 0.55), 0.04, m["wood"], verts=6))
    P.append(T.box("front", (0, -1.1, 1.0), (1.4, 0.06, 0.45), m["planks"]))
    P.append(T.sphere("sack", (0.2, 0.4, 1.0), (0.35, 0.45, 0.25), T.mat("sack", 0xC8B48A, 0.95), seg=10, rings=6))
    P.append(T.sphere("sack2", (-0.25, -0.3, 0.98), (0.3, 0.35, 0.22), T.mat("sack", 0xC8B48A, 0.95), seg=10, rings=6))
    return T.join(P, name)


def lampu_jalan(name="lampu_jalan"):
    m = M()
    P = [T.cyl("pole", (0, 0, 0), (0, 0, 5.0), 0.09, m["dark"], r1=0.06, verts=8),
         T.cyl("arm", (0, 0, 4.8), (0, -1.0, 5.1), 0.05, m["dark"], verts=6),
         T.box("head", (0, -1.05, 5.05), (0.35, 0.5, 0.12), m["dark"], bevel=0.03),
         T.box("bulb", (0, -1.05, 4.97), (0.25, 0.38, 0.04), m["lamp"])]
    return T.join(P, name)


def tiang_listrik(name="tiang_listrik"):
    m = M()
    P = [T.cyl("pole", (0, 0, 0), (0, 0, 7.5), 0.14, m["concrete"], r1=0.1, verts=8),
         T.box("cross", (0, 0, 7.1), (1.6, 0.1, 0.1), m["dark"])]
    for x in (-0.7, 0, 0.7):
        P.append(T.cyl("insulator", (x, 0, 7.15), (x, 0, 7.35), 0.05, T.mat("insulator", 0x8FA7A8, 0.3), verts=6))
    P.append(T.cyl("transformer", (0, 0.25, 5.8), (0, 0.25, 6.6), 0.25, m["metal"], verts=10))
    return T.join(P, name)


def peti(name="peti"):
    m = M()
    P = [T.box("crate", (0, 0, 0.35), (0.7, 0.7, 0.7), m["wood_light"])]
    for z in (0.08, 0.62):
        P.append(T.box("band", (0, 0, z), (0.72, 0.72, 0.08), m["wood"]))
    P.append(T.box("diag", (0, -0.36, 0.35), (0.08, 0.02, 0.9), m["wood"], rot=(0, 0.8, 0)))
    return T.join(P, name)


def tong(name="tong", color=0x2F6FB5):
    m = M()
    c = T.mat(name + "_paint", color, 0.5, 0.4)
    P = [T.cyl("drum", (0, 0, 0), (0, 0, 0.9), 0.3, c, verts=14)]
    for z in (0.2, 0.7):
        P.append(T.torus("hoop", (0, 0, z), 0.305, 0.02, m["dark"], seg=14, ring_seg=4))
    return T.join(P, name)


def ban_bekas(name="ban_bekas"):
    m = M()
    P = [T.torus("tire%d" % i, (0.05 * (i % 2), 0, 0.12 + i * 0.22), 0.34, 0.11, m["rubber"], seg=16, ring_seg=6) for i in range(3)]
    return T.join(P, name)


def tumpukan_kayu(name="tumpukan_kayu"):
    m = M()
    rng = random.Random(12)
    P = []
    for i in range(12):
        a = rng.uniform(0, math.pi)
        p = Vector((rng.uniform(-0.8, 0.8), rng.uniform(-0.5, 0.5), 0.1 + (i // 4) * 0.18))
        d = Vector((math.cos(a), math.sin(a), rng.uniform(-0.1, 0.2))) * rng.uniform(0.8, 1.4)
        P.append(T.box("plank", p, (0.18, d.length, 0.06), m["wood"] if i % 2 else m["wood_dark"], rot=(0, 0, a + math.pi / 2)))
    return T.join(P, name)


def karung(name="karung"):
    m = M()
    sack = T.mat("sack", 0xC8B48A, 0.95)
    P = [T.sphere("sack%d" % i, (i * 0.55 - 0.55, 0, 0.22 + (0.35 if i == 1 else 0)), (0.3, 0.42, 0.22), sack, seg=10, rings=6) for i in range(3)]
    return T.join(P, name)


# ------------------------------------------------------------------ graveyard and dukun lair

def nisan(name="nisan_a", style=0):
    m = M()
    stone = m["stone"]
    white = T.mat("grave_white", 0xE4E1D6, 0.8)
    P = []
    if style == 0:  # Islamic grave: mound with a pair of stones and a cloth
        P.append(T.sphere("mound", (0, 0, 0), (0.5, 1.0, 0.25), m["dirt"], seg=10, rings=6))
        for y in (-0.8, 0.8):
            P.append(T.box("stone", (0, y, 0.35), (0.28, 0.12, 0.6), white, bevel=0.05))
            P.append(T.sphere("cap", (0, y, 0.66), (0.14, 0.06, 0.08), white, seg=8, rings=5))
        P.append(T.sphere("flowers", (0, 0, 0.22), (0.3, 0.6, 0.06), T.mat("petals", 0xE45C8C, 0.7), seg=8, rings=4))
    elif style == 1:  # rounded slab
        P.append(T.box("slab", (0, 0, 0.45), (0.7, 0.18, 0.9), stone, bevel=0.06))
        P.append(T.cyl("top", (0, -0.09, 0.9), (0, 0.09, 0.9), 0.35, stone, verts=14))
        P.append(T.box("base", (0, 0.25, 0.08), (0.9, 1.4, 0.16), stone))
    else:  # wooden cross
        P.append(T.box("post", (0, 0, 0.6), (0.1, 0.08, 1.2), m["wood_dark"]))
        P.append(T.box("bar", (0, 0, 0.9), (0.55, 0.08, 0.1), m["wood_dark"]))
        P.append(T.sphere("mound", (0, 0.5, 0), (0.45, 0.9, 0.2), m["dirt"], seg=10, rings=6))
    return T.join(P, name)


def gerbang_kubur(name="gerbang_kubur"):
    m = M()
    P = []
    for sx in (-1, 1):
        P.append(T.box("pillar", (sx * 1.6, 0, 1.3), (0.5, 0.5, 2.6), m["stone"]))
        P.append(T.cyl("cap", (sx * 1.6, 0, 2.6), (sx * 1.6, 0, 3.0), 0.3, m["stone"], r1=0.05, verts=8))
        for k in range(4):
            P.append(T.cyl("bar", (sx * (0.25 + k * 0.3), 0, 0), (sx * (0.25 + k * 0.3), 0, 2.2), 0.025, m["dark"], verts=6))
    P.append(T.box("arch", (0, 0, 2.4), (3.0, 0.2, 0.15), m["dark"]))
    return T.join(P, name)


def lilin(name="lilin"):
    m = M()
    wax = T.mat("wax", 0xF3EAD0, 0.6)
    P = []
    for i, (x, y, h) in enumerate(((0, 0, 0.3), (0.12, 0.06, 0.2), (-0.1, 0.08, 0.25))):
        P.append(T.cyl("candle", (x, y, 0), (x, y, h), 0.035, wax, verts=8))
        P.append(T.sphere("flame", (x, y, h + 0.04), (0.02, 0.02, 0.04), m["flame"], seg=6, rings=4))
    return T.join(P, name)


def altar_dukun(name="altar_dukun"):
    m = M()
    cloth = T.mat("altar_cloth", 0x5E1A24, 0.9)
    P = [T.box("table", (0, 0, 0.45), (1.8, 0.9, 0.9), m["wood_dark"]),
         T.box("cloth", (0, 0, 0.91), (1.9, 1.0, 0.04), cloth),
         T.sphere("skull", (0, 0, 1.08), (0.14, 0.13, 0.14), T.mat("skull", 0xEDE6CF, 0.6), seg=10, rings=8),
         T.cyl("pot", (0.5, 0, 0.93), (0.5, 0, 1.15), 0.14, T.mat("clay", 0xB5652F, 0.8), r1=0.1, verts=10),
         T.sphere("smoke", (0.5, 0, 1.3), 0.1, T.mat("incense", 0xB6FF8A, 0.5, emit=0x7CFF4A, strength=3.0), seg=8, rings=5)]
    for x in (-0.7, -0.45, 0.75):
        P.append(T.cyl("candle", (x, 0.2, 0.93), (x, 0.2, 1.2), 0.03, T.mat("wax", 0xF3EAD0, 0.6), verts=6))
        P.append(T.sphere("flame", (x, 0.2, 1.24), (0.02, 0.02, 0.04), m["flame"], seg=6, rings=4))
    return T.join(P, name)


def orang_sawah(name="orang_sawah"):
    """Scarecrow in a caping hat, a friendly landmark in the paddies."""
    m = M()
    shirt = T.mat("scare_shirt", 0x3C6EA8, 0.9)
    P = [T.cyl("post", (0, 0, 0), (0, 0, 2.0), 0.05, m["wood"], verts=6),
         T.cyl("arms", (-0.8, 0, 1.45), (0.8, 0, 1.45), 0.04, m["wood"], verts=6),
         T.box("shirt", (0, 0, 1.3), (0.55, 0.25, 0.6), shirt),
         T.sphere("head", (0, 0, 1.85), 0.2, T.mat("sack", 0xC8B48A, 0.95), seg=10, rings=8),
         T.cyl("caping", (0, 0, 2.0), (0, 0, 2.25), 0.5, m["bamboo_dry"], r1=0.02, verts=16)]
    for sx in (-1, 1):
        P.append(T.box("sleeve", (sx * 0.55, 0, 1.43), (0.5, 0.2, 0.18), shirt))
    return T.join(P, name)


# ------------------------------------------------------------------ pickups

def nasi_bungkus(name="pickup_nasi"):
    leaf = T.mat("banana_leaf", 0x4E9A3A, 0.6)
    paper = T.mat("wrap_paper", 0xE8DCC0, 0.8)
    P = [T.box("pack", (0, 0, 0.1), (0.3, 0.22, 0.14), leaf, bevel=0.05),
         T.box("paper", (0, 0, 0.18), (0.22, 0.16, 0.02), paper),
         T.torus("rubber_band", (0, 0, 0.1), 0.16, 0.01, T.mat("band_red", 0xD43C3C, 0.5), rot=(math.pi / 2, 0, 0), seg=12, ring_seg=3)]
    return T.join(P, name)


def jamu(name="pickup_jamu"):
    glass = T.mat("jamu_bottle", 0xE0A030, 0.2, emit=0xFFB040, strength=0.6)
    cap = T.mat("jamu_cap", 0x2E7D32, 0.5)
    P = [T.cyl("bottle", (0, 0, 0), (0, 0, 0.22), 0.07, glass, verts=12),
         T.cyl("neck", (0, 0, 0.22), (0, 0, 0.3), 0.07, glass, r1=0.03, verts=12),
         T.cyl("cap", (0, 0, 0.3), (0, 0, 0.35), 0.035, cap, verts=10),
         T.box("label", (0, -0.068, 0.12), (0.1, 0.01, 0.08), T.mat("label", 0xF4E9C8, 0.8))]
    return T.join(P, name)


def kotak_peluru(name="pickup_ammo"):
    m = M()
    P = [T.box("box", (0, 0, 0.12), (0.4, 0.26, 0.24), T.mat("ammo_green", 0x4B5E2F, 0.6), bevel=0.02),
         T.box("lid_band", (0, 0, 0.24), (0.42, 0.28, 0.04), m["dark"]),
         T.box("stripe", (0, -0.131, 0.12), (0.3, 0.01, 0.06), T.mat("ammo_yellow", 0xE8C04A, 0.5))]
    for i in range(3):
        P.append(T.cyl("shell", (-0.1 + i * 0.1, 0, 0.26), (-0.1 + i * 0.1, 0, 0.36), 0.02, T.mat("brass", 0xD4A437, 0.3, 0.9), r1=0.012, verts=6))
    return T.join(P, name)


def peti_molotov(name="pickup_molotov"):
    m = M()
    P = [T.box("crate", (0, 0, 0.12), (0.45, 0.35, 0.24), m["wood_light"], bevel=0.015)]
    for i, x in enumerate((-0.12, 0.0, 0.12)):
        P.append(T.cyl("bottle", (x, 0, 0.2), (x, 0, 0.42), 0.04, T.mat("bottle_glass", 0x3F8F4F, 0.15), verts=8))
        P.append(T.cyl("wick", (x, 0, 0.42), (x, 0, 0.48), 0.012, T.mat("wick_cloth", 0xE9E1CF, 0.9), verts=5))
    return T.join(P, name)


# ------------------------------------------------------------------ registry

PROPS = {
    "rumah_kuning": lambda: rumah("rumah_kuning", 0xF2D06B, seed=0),
    "rumah_biru": lambda: rumah("rumah_biru", 0x8EC3E6, seed=1),
    "rumah_hijau": lambda: rumah("rumah_hijau", 0xA8D89A, seed=2),
    "rumah_merah": lambda: rumah("rumah_merah", 0xE8A69A, roof="zinc", seed=3),
    "rumah_panggung": rumah_panggung,
    "masjid": masjid,
    "ruko_a": lambda: ruko("ruko_a", 0xE6D3A3, "TOKO MAKMUR", 0),
    "ruko_b": lambda: ruko("ruko_b", 0xB9D7C9, "WARUNG BU SRI", 1),
    "ruko_c": lambda: ruko("ruko_c", 0xE8B9A0, "APOTEK SEHAT", 2),
    "kios_a": lambda: kios("kios_a", 0),
    "kios_b": lambda: kios("kios_b", 1),
    "kios_c": lambda: kios("kios_c", 2),
    "gapura": gapura,
    "pos_ronda": pos_ronda,
    "gubuk": gubuk,
    "sumur": sumur,
    "pohon_pisang": pohon_pisang,
    "pohon_kelapa": pohon_kelapa,
    "pohon_mangga": pohon,
    "pohon_beringin": pohon_beringin,
    "rumpun_bambu": rumpun_bambu,
    "semak_a": lambda: semak("semak_a", 2),
    "semak_b": lambda: semak("semak_b", 3),
    "rumput": rumput,
    "padi": padi,
    "batu_a": lambda: batu("batu_a", 1),
    "batu_b": lambda: batu("batu_b", 2),
    "pagar_bambu": pagar_bambu,
    "barikade": barikade,
    "motor_merah": lambda: motor("motor_merah", 0xC62828),
    "motor_biru": lambda: motor("motor_biru", 0x1E6FC2),
    "gerobak": gerobak,
    "lampu_jalan": lampu_jalan,
    "tiang_listrik": tiang_listrik,
    "peti": peti,
    "tong_biru": lambda: tong("tong_biru", 0x2F6FB5),
    "tong_merah": lambda: tong("tong_merah", 0xC0392B),
    "ban_bekas": ban_bekas,
    "tumpukan_kayu": tumpukan_kayu,
    "karung": karung,
    "nisan_a": lambda: nisan("nisan_a", 0),
    "nisan_b": lambda: nisan("nisan_b", 1),
    "nisan_c": lambda: nisan("nisan_c", 2),
    "gerbang_kubur": gerbang_kubur,
    "lilin": lilin,
    "altar_dukun": altar_dukun,
    "orang_sawah": orang_sawah,
    "pickup_nasi": nasi_bungkus,
    "pickup_jamu": jamu,
    "pickup_ammo": kotak_peluru,
    "pickup_molotov": peti_molotov,
}


def build(name, export=True):
    T.clear_scene()
    T.reset_materials()
    obj = PROPS[name]()
    return T.export_glb("prop_" + name, [obj], animations=False) if export else obj


def build_all(names=None):
    out = {}
    for name in names or PROPS:
        out[name] = build(name)
    return out
