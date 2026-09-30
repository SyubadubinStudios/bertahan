"""Renders UI images: character portraits, zombie and weapon icons and the logo,
all with transparent backgrounds, into src/Bertahan/Assets/UI."""

import math
import os

import bpy
from mathutils import Vector

import btk as T
import characters as C
import weapons as W
import zombies as Z

UI = os.path.join(T.PROJECT, "src", "Bertahan", "Assets", "UI")


def _setup(res):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.film_transparent = True
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.view_settings.view_transform = 'Standard'
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.8, 0.82, 0.9, 1)
    bg.inputs[1].default_value = 0.7
    return scene


def _camera(target, offset, lens=50, ortho=None):
    data = bpy.data.cameras.new("ui_cam")
    data.lens = lens
    if ortho:
        data.type = 'ORTHO'
        data.ortho_scale = ortho
    cam = T.link(bpy.data.objects.new("ui_cam", data))
    cam.location = Vector(target) + Vector(offset)
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.camera = cam
    return cam


def _lights():
    key = bpy.data.lights.new("key", 'SUN')
    key.energy = 3.2
    k = T.link(bpy.data.objects.new("key", key))
    k.rotation_euler = (math.radians(55), 0, math.radians(-35))
    rim = bpy.data.lights.new("rim", 'SUN')
    rim.energy = 2.0
    rim.color = (1.0, 0.85, 0.6)
    r = T.link(bpy.data.objects.new("rim", rim))
    r.rotation_euler = (math.radians(60), 0, math.radians(150))


def _render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def portraits():
    os.makedirs(UI, exist_ok=True)
    out = []
    for name, spec in C.FAMILY.items():
        T.clear_scene()
        T.reset_materials()
        _setup((320, 320))
        C.resolve_fabrics(name, spec)
        spec.weapons = False
        arm, mesh = C.build_human(name, spec)
        head = Vector((0, 0, spec.height - spec.head * 0.62))
        k = spec.head / 0.5
        _camera(head, (0.4 * k, -1.95 * k, 0.18 * k), lens=60)
        _lights()
        path = os.path.join(UI, f"portrait_{name}.png")
        _render(path)
        out.append(path)
        # full body card for the character select screen
        _setup((360, 560))
        for o in [o for o in bpy.data.objects if o.name == "ui_cam"]:
            bpy.data.objects.remove(o, do_unlink=True)
        _camera((0, 0, spec.height * 0.5), (0.9, -3.6, 0.25), lens=50)
        path = os.path.join(UI, f"card_{name}.png")
        _render(path)
        out.append(path)
    return out


def zombie_icons():
    os.makedirs(UI, exist_ok=True)
    specs = Z.specs()
    out = []
    for name in Z.NAMES:
        T.clear_scene()
        T.reset_materials()
        _setup((256, 256))
        if name == "pocong":
            Z.build_pocong(export=False)
            height = 1.95
        else:
            s = specs[name]
            Z.resolve(name, s)
            C.build_human(name, s)
            height = s.height
        _camera((0, 0, height * 0.5), (0.8, -height * 2.1 - 0.6, 0.3), lens=50)
        _lights()
        path = os.path.join(UI, f"zombie_{name}.png")
        _render(path)
        out.append(path)
    return out


def weapon_icons():
    os.makedirs(UI, exist_ok=True)
    out = []
    for key, fn in W.BUILDERS.items():
        T.clear_scene()
        T.reset_materials()
        _setup((192, 192))
        obj = fn("icon_" + key)
        obj.rotation_euler = (math.radians(-40), 0, 0)
        bpy.context.view_layer.update()
        corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
        lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
        hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
        centre = (lo + hi) / 2
        size = max(hi.y - lo.y, hi.z - lo.z)
        _camera(centre, (3.0, 0.0, 0.0), ortho=size * 1.1)
        _lights()
        path = os.path.join(UI, f"weapon_{key}.png")
        _render(path)
        out.append(path)
    return out


def logo():
    os.makedirs(UI, exist_ok=True)
    T.clear_scene()
    T.reset_materials()
    _setup((1400, 420))
    face = T.mat("logo_face", 0xFFC93C, 0.35, emit=0xFF9A1F, strength=0.25)
    side = T.mat("logo_side", 0x8C2A1C, 0.5)
    goo = T.mat("logo_goo", 0x7BD13B, 0.3, emit=0x4FA01E, strength=0.3)
    curve = bpy.data.curves.new("logo", 'FONT')
    curve.body = "BERTAHAN"
    curve.size = 1.0
    curve.extrude = 0.14
    curve.bevel_depth = 0.035
    curve.bevel_resolution = 2
    curve.offset = 0.02
    curve.space_character = 1.08
    curve.align_x = 'CENTER'
    curve.align_y = 'CENTER'
    text = T.link(bpy.data.objects.new("logo", curve))
    text.rotation_euler = (math.radians(90), 0, 0)
    text.data.materials.append(face)
    text.data.materials.append(side)
    curve.materials[0] = face
    # dark outline: a fatter copy of the text just behind it
    outline_curve = curve.copy()
    outline_curve.offset = 0.075
    outline_curve.extrude = 0.08
    outline_curve.bevel_depth = 0.0
    outline = T.link(bpy.data.objects.new("logo_outline", outline_curve))
    outline.rotation_euler = text.rotation_euler
    outline.location = (0, 0.12, 0)
    outline_curve.materials.clear()
    outline_curve.materials.append(side)
    # slime drips hanging from the letters, irregular like real goo
    import random
    rng = random.Random(3)
    for i in range(10):
        x = -2.55 + i * 0.56 + rng.uniform(-0.12, 0.12)
        length = rng.uniform(0.12, 0.42)
        T.cyl("drip", (x, -0.2, -0.32), (x, -0.2, -0.32 - length), 0.05, goo, r1=0.032, verts=10)
        T.sphere("drop", (x, -0.2, -0.34 - length), 0.065, goo, seg=10, rings=6)
        T.sphere("blob", (x, -0.2, -0.33), (0.13, 0.06, 0.06), goo, seg=10, rings=6)
    _camera((0, 0, -0.05), (0, -12.0, 0.6), lens=60)
    _lights()
    path = os.path.join(UI, "logo.png")
    _render(path)
    return [path]


def render_all():
    return {"portraits": len(portraits()), "zombies": len(zombie_icons()), "weapons": len(weapon_icons()), "logo": logo()}
