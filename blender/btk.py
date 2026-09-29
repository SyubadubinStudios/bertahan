"""Bertahan ToolKit: helpers shared by the Blender asset scripts.

Blender conventions used everywhere: Z is up, characters face -Y, the
character's left side is +X (bones suffixed _L). The glTF exporter turns this
into Y up, facing +Z, which is what the game expects.
"""

import math
import os

import bmesh
import bpy
import numpy as np
from mathutils import Euler, Matrix, Quaternion, Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(ROOT)
MODELS = os.path.join(PROJECT, "src", "Bertahan", "Assets", "Models")
FPS = 30


# --------------------------------------------------------------------------- scene

def clear_scene():
    """Removes every object and orphan datablock so each build starts clean."""
    if bpy.context.object and bpy.context.object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions,
                       bpy.data.materials, bpy.data.images, bpy.data.curves):
        for block in list(collection):
            collection.remove(block)
    scene = bpy.context.scene
    scene.render.fps = FPS
    return scene


def link(obj):
    bpy.context.scene.collection.objects.link(obj)
    return obj


# ----------------------------------------------------------------------- materials

_MATS = {}


def hex_rgb(value):
    """sRGB hex to linear RGB, what Principled BSDF expects."""
    def lin(c):
        c /= 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (lin((value >> 16) & 255), lin((value >> 8) & 255), lin(value & 255))


def mat(name, color, rough=0.75, metal=0.0, emit=None, strength=1.0, image=None, alpha=1.0):
    """Cached Principled material. `color` is an sRGB hex int."""
    key = name
    if key in _MATS and _MATS[key].name in bpy.data.materials:
        return _MATS[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    rgb = hex_rgb(color)
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    if emit is not None:
        bsdf.inputs["Emission Color"].default_value = (*hex_rgb(emit), 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    if alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
        m.surface_render_method = 'BLENDED'
    if image is not None:
        tex = m.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = image
        m.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    _MATS[key] = m
    return m


def reset_materials():
    _MATS.clear()


def make_image(name, size, fn):
    """Procedural texture: fn(u, v) -> (r, g, b) arrays in 0..1 (sRGB), packed into the .blend/.glb."""
    img = bpy.data.images.new(name, size, size, alpha=False)
    v, u = np.mgrid[0:size, 0:size] / float(size)
    r, g, b = fn(u, v)
    px = np.ones((size, size, 4), dtype=np.float32)
    px[..., 0], px[..., 1], px[..., 2] = np.clip(r, 0, 1), np.clip(g, 0, 1), np.clip(b, 0, 1)
    img.pixels.foreach_set(px.ravel())
    img.pack()
    return img


def hexf(value):
    return (((value >> 16) & 255) / 255.0, ((value >> 8) & 255) / 255.0, (value & 255) / 255.0)


def mix(a, b, t):
    a, b = hexf(a), hexf(b)
    return tuple(a[i] * (1 - t) + b[i] * t for i in range(3))


# ------------------------------------------------------------------------- meshes

def _bm():
    """New bmesh with a UV layer, so the create_* operators write UVs."""
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    return bm


def _finish(name, bm, material, bone, smooth, matrix):
    bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = link(bpy.data.objects.new(name, mesh))
    if material is not None:
        mesh.materials.append(material)
    for poly in mesh.polygons:
        poly.use_smooth = smooth
    if bone:
        group = obj.vertex_groups.new(name=bone)
        group.add(range(len(mesh.vertices)), 1.0, 'REPLACE')
    return obj


def _trs(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    return (Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4()
            @ Matrix.Diagonal((*scale, 1.0)))


def sphere(name, loc, scale, material, bone=None, seg=16, rings=10, rot=(0, 0, 0), smooth=True):
    """Ellipsoid; `scale` is the radius per axis (or a float)."""
    if isinstance(scale, (int, float)):
        scale = (scale, scale, scale)
    bm = _bm()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0, calc_uvs=True)
    return _finish(name, bm, material, bone, smooth, _trs(loc, rot, scale))


def box(name, loc, size, material, bone=None, rot=(0, 0, 0), bevel=0.0, smooth=False):
    """Box with full `size` extents, optionally with bevelled edges."""
    bm = _bm()
    bmesh.ops.create_cube(bm, size=1.0, calc_uvs=True)
    if bevel > 0:
        rel = bevel / max(min(size), 1e-4)
        bmesh.ops.bevel(bm, geom=bm.edges[:] + bm.verts[:], offset=min(rel, 0.45), segments=2,
                        affect='EDGES', profile=0.5)
    return _finish(name, bm, material, bone, smooth or bevel > 0, _trs(loc, rot, size))


def cyl(name, p0, p1, r0, material, r1=None, bone=None, verts=12, smooth=True, cap=True):
    """Tapered cylinder (cone when r1 == 0) running from p0 to p1."""
    r1 = r0 if r1 is None else r1
    p0, p1 = Vector(p0), Vector(p1)
    axis = p1 - p0
    length = axis.length
    bm = _bm()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=verts, radius1=r0,
                          radius2=r1, depth=length, calc_uvs=True)
    # create_cone is centred on the origin along Z.
    rot = Vector((0, 0, 1)).rotation_difference(axis.normalized()).to_matrix().to_4x4()
    m = Matrix.Translation((p0 + p1) * 0.5) @ rot
    return _finish(name, bm, material, bone, smooth, m)


def torus(name, loc, major, minor, material, bone=None, rot=(0, 0, 0), seg=16, ring_seg=8):
    bm = bmesh.new()
    for i in range(seg):
        a = 2 * math.pi * i / seg
        for j in range(ring_seg):
            b = 2 * math.pi * j / ring_seg
            r = major + minor * math.cos(b)
            bm.verts.new((r * math.cos(a), r * math.sin(a), minor * math.sin(b)))
    bm.verts.ensure_lookup_table()
    for i in range(seg):
        for j in range(ring_seg):
            a = i * ring_seg + j
            b = ((i + 1) % seg) * ring_seg + j
            c = ((i + 1) % seg) * ring_seg + (j + 1) % ring_seg
            d = i * ring_seg + (j + 1) % ring_seg
            bm.faces.new((bm.verts[a], bm.verts[b], bm.verts[c], bm.verts[d]))
    return _finish(name, bm, material, bone, True, _trs(loc, rot))


def poly_prism(name, points2d, z0, z1, material, bone=None, loc=(0, 0, 0), rot=(0, 0, 0), smooth=False):
    """Extrudes a 2D outline (XY, counter clockwise) from z0 to z1."""
    bm = bmesh.new()
    bottom = [bm.verts.new((x, y, z0)) for x, y in points2d]
    top = [bm.verts.new((x, y, z1)) for x, y in points2d]
    bm.faces.new(list(reversed(bottom)))
    bm.faces.new(top)
    n = len(points2d)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((bottom[i], bottom[j], top[j], top[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv = bm.loops.layers.uv.new()
    for face in bm.faces:
        for loop in face.loops:
            co = loop.vert.co
            loop[uv].uv = (co.x + co.z, co.y + co.z)
    return _finish(name, bm, material, bone, smooth, _trs(loc, rot))


def gable_roof(name, width, depth, height, overhang, material, loc=(0, 0, 0), rot=(0, 0, 0), thickness=0.08):
    """Two sloped slabs meeting at a ridge along X."""
    w = width / 2 + overhang
    d = depth / 2 + overhang
    pts = [(-d, 0.0), (d, 0.0), (0.0, height)]
    bm = bmesh.new()
    t = thickness
    # outline in the YZ plane, extruded along X
    outline = [(-d - t, -t * 0.5), (0.0, height), (d + t, -t * 0.5), (d + t, t * 0.8), (0.0, height + t * 1.6), (-d - t, t * 0.8)]
    left = [bm.verts.new((-w, y, z)) for y, z in outline]
    right = [bm.verts.new((w, y, z)) for y, z in outline]
    bm.faces.new(left)
    bm.faces.new(list(reversed(right)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((left[i], left[j], right[j], right[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv = bm.loops.layers.uv.new()
    for face in bm.faces:
        for loop in face.loops:
            co = loop.vert.co
            loop[uv].uv = (co.x, co.y + co.z)
    del pts
    return _finish(name, bm, material, None, False, _trs(loc, rot))


def join(objects, name):
    """Joins meshes into one object named `name` (materials and vertex groups kept)."""
    objects = [o for o in objects if o is not None]
    target = objects[0]
    with bpy.context.temp_override(active_object=target, selected_objects=objects,
                                   selected_editable_objects=objects):
        bpy.ops.object.join()
    target.name = name
    target.data.name = name
    return target


def origin_to(obj, point=(0, 0, 0)):
    """Moves the object origin to `point` (world) without moving the geometry."""
    offset = Vector(point) - obj.matrix_world.translation
    obj.data.transform(Matrix.Translation(-offset))
    obj.matrix_world.translation += offset


# --------------------------------------------------------------------------- rigs

HUMANOID_BONES = [
    "root", "hips", "spine", "chest", "head",
    "upperarm_L", "forearm_L", "hand_L", "upperarm_R", "forearm_R", "hand_R",
    "thigh_L", "shin_L", "foot_L", "thigh_R", "shin_R", "foot_R",
]


def humanoid_joints(height=1.7, head=0.5, shoulder=0.26, hip_w=0.11, leg=0.62, arm=0.55, torso=None):
    """Joint positions for a chibi humanoid. Returns a dict of name -> Vector."""
    hip_z = leg
    neck_z = height - head * 0.92
    torso = torso or (neck_z - hip_z)
    chest_z = hip_z + torso * 0.55
    sh_z = neck_z - 0.05
    j = {
        "root": Vector((0, 0, 0)),
        "hip": Vector((0, 0, hip_z)),
        "chest": Vector((0, 0, chest_z)),
        "neck": Vector((0, 0, neck_z)),
        "top": Vector((0, 0, height)),
    }
    for side, s in (("L", 1), ("R", -1)):
        j["shoulder_" + side] = Vector((s * shoulder, 0, sh_z))
        j["elbow_" + side] = Vector((s * (shoulder + 0.04), 0.02, sh_z - arm * 0.5))
        j["wrist_" + side] = Vector((s * (shoulder + 0.06), 0.0, sh_z - arm * 0.92))
        j["fingers_" + side] = Vector((s * (shoulder + 0.065), 0.0, sh_z - arm * 1.1))
        j["hipj_" + side] = Vector((s * hip_w, 0, hip_z))
        j["knee_" + side] = Vector((s * hip_w, -0.01, hip_z * 0.5))
        j["ankle_" + side] = Vector((s * hip_w, 0.0, 0.07))
        j["toe_" + side] = Vector((s * hip_w, -0.16, 0.03))
    return j


def build_armature(name, j):
    """Creates the standard humanoid armature from `humanoid_joints` output."""
    data = bpy.data.armatures.new(name)
    arm = link(bpy.data.objects.new(name, data))
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    eb = data.edit_bones

    def bone(bname, head, tail, parent=None):
        b = eb.new(bname)
        b.head, b.tail = head, tail
        b.roll = 0.0
        if parent:
            b.parent = eb[parent]
            b.use_connect = False
        return b

    bone("root", j["root"], j["root"] + Vector((0, 0, 0.2)))
    bone("hips", j["hip"], j["hip"] + (j["chest"] - j["hip"]) * 0.5, "root")
    bone("spine", j["hip"] + (j["chest"] - j["hip"]) * 0.5, j["chest"], "hips")
    bone("chest", j["chest"], j["neck"], "spine")
    bone("head", j["neck"], j["top"], "chest")
    for s in ("L", "R"):
        bone("upperarm_" + s, j["shoulder_" + s], j["elbow_" + s], "chest")
        bone("forearm_" + s, j["elbow_" + s], j["wrist_" + s], "upperarm_" + s)
        bone("hand_" + s, j["wrist_" + s], j["fingers_" + s], "forearm_" + s)
        bone("thigh_" + s, j["hipj_" + s], j["knee_" + s], "hips")
        bone("shin_" + s, j["knee_" + s], j["ankle_" + s], "thigh_" + s)
        bone("foot_" + s, j["ankle_" + s], j["toe_" + s], "shin_" + s)
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm


def build_custom_armature(name, bones):
    """bones: list of (name, head, tail, parent) for creatures that are not humanoid."""
    data = bpy.data.armatures.new(name)
    arm = link(bpy.data.objects.new(name, data))
    bpy.context.view_layer.objects.active = arm
    arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for bname, head, tail, parent in bones:
        b = data.edit_bones.new(bname)
        b.head, b.tail, b.roll = Vector(head), Vector(tail), 0.0
        if parent:
            b.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    return arm


def skin(mesh_obj, arm):
    """Parents the mesh to the armature with an Armature modifier (vertex groups already set)."""
    mesh_obj.parent = arm
    mod = mesh_obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = arm
    return mesh_obj


def attach_to_bone(obj, arm, bone):
    """Bone-parents `obj` keeping its world transform (exported as a child of the joint)."""
    world = obj.matrix_world.copy()
    obj.parent = arm
    obj.parent_type = 'BONE'
    obj.parent_bone = bone
    bpy.context.view_layer.update()
    obj.matrix_world = world
    return obj


# ---------------------------------------------------------------------- animation

class Clip:
    """Keyframes pose bones of `arm` into a new action.

    Rotations are Euler angles in degrees about the *armature* axes of the rest
    pose (X: pitch, forward swing is negative for limbs hanging down; Y: roll;
    Z: yaw), which is far easier to author than bone-local rotations.
    """

    def __init__(self, arm, name, frames, bones=None):
        self.arm = arm
        self.name = name
        self.frames = frames
        self.bones = bones  # None: key every bone (base clips must reset all bones)
        self.keys = {}  # frame -> {bone: (rx, ry, rz)}
        self.locs = {}  # frame -> {bone: (x, y, z)} armature space offsets
        self.offsets = {}  # bone -> (rx, ry, rz) added to every key (posture)

    def pose(self, frame, **rots):
        self.keys.setdefault(frame, {}).update(rots)
        return self

    def move(self, frame, bone, offset):
        self.locs.setdefault(frame, {})[bone] = offset
        return self

    def _local_quat(self, pbone, rx, ry, rz):
        rest = pbone.bone.matrix_local.to_3x3()
        world = Euler((math.radians(rx), math.radians(ry), math.radians(rz)), 'XYZ').to_matrix()
        return (rest.inverted() @ world @ rest).to_quaternion()

    def build(self, loop=True):
        """Keys every listed bone at every authored frame. Bones not mentioned at
        a frame hold their previous value; looping clips repeat frame 0 at the end."""
        arm = self.arm
        arm.animation_data_create()
        action = bpy.data.actions.new(self.name)
        action.use_fake_user = True
        arm.animation_data.action = action
        names = self.bones or [b.name for b in arm.pose.bones]
        loc_bones = {n for d in self.locs.values() for n in d}
        if self.bones is None:
            loc_bones |= {"root", "hips"} & set(arm.pose.bones.keys())
        rot = {n: (0.0, 0.0, 0.0) for n in names}
        loc = {n: (0.0, 0.0, 0.0) for n in loc_bones}
        snapshots = []
        for f in sorted(set(self.keys) | set(self.locs) | {0}):
            rot.update({n: r for n, r in self.keys.get(f, {}).items() if n in rot})
            loc.update(self.locs.get(f, {}))
            snapshots.append((f, dict(rot), dict(loc)))
        if loop and snapshots[-1][0] != self.frames:
            snapshots.append((self.frames, snapshots[0][1], snapshots[0][2]))
        for f, rots, locs in snapshots:
            for n in names:
                pb = arm.pose.bones[n]
                pb.rotation_mode = 'QUATERNION'
                off = self.offsets.get(n, (0.0, 0.0, 0.0))
                pb.rotation_quaternion = self._local_quat(pb, *(r + o for r, o in zip(rots[n], off)))
                pb.keyframe_insert("rotation_quaternion", frame=f + 1)
            for n in loc_bones:
                pb = arm.pose.bones[n]
                rest = pb.bone.matrix_local.to_3x3()
                pb.location = rest.inverted() @ Vector(locs[n])
                pb.keyframe_insert("location", frame=f + 1)
        arm.animation_data.action = None
        for pb in arm.pose.bones:
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)
        return action


# ------------------------------------------------------------------------- export

def export_glb(name, objects, animations=True):
    os.makedirs(MODELS, exist_ok=True)
    path = os.path.join(MODELS, name + ".glb")
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        for child in obj.children_recursive:
            child.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.gltf(
        filepath=path,
        export_format='GLB',
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_animations=animations,
        export_animation_mode='ACTIONS',
        export_force_sampling=False,
        export_optimize_animation_size=False,
        export_anim_slide_to_zero=True,
        export_skins=True,
        export_morph=False,
        export_image_format='AUTO',
        export_materials='EXPORT',
    )
    return path


def preview(path, target=(0, 0, 0.8), distance=4.0, height=1.2, yaw=-25.0, res=(800, 600), lens=50):
    """Quick EEVEE render from the front (-Y side) for checking an asset."""
    scene = bpy.context.scene
    for o in [o for o in bpy.data.objects if o.name.startswith("_preview")]:
        bpy.data.objects.remove(o, do_unlink=True)
    cam_data = bpy.data.cameras.new("_preview_cam")
    cam_data.lens = lens
    cam = link(bpy.data.objects.new("_preview_cam", cam_data))
    a = math.radians(yaw)
    tgt = Vector(target)
    cam.location = tgt + Vector((math.sin(a) * distance, -math.cos(a) * distance, height))
    cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    sun_data = bpy.data.lights.new("_preview_sun", 'SUN')
    sun_data.energy = 3.5
    sun = link(bpy.data.objects.new("_preview_sun", sun_data))
    sun.rotation_euler = (math.radians(50), 0, math.radians(-30))
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    bg.inputs[0].default_value = (0.55, 0.62, 0.72, 1)
    bg.inputs[1].default_value = 0.9
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.filepath = path
    scene.render.image_settings.file_format = 'PNG'
    scene.view_settings.view_transform = 'Standard'
    bpy.ops.render.render(write_still=True)
    for o in (cam, sun):
        bpy.data.objects.remove(o, do_unlink=True)
    return path


def save_blend(name):
    path = os.path.join(ROOT, name + ".blend")
    bpy.ops.wm.save_as_mainfile(filepath=path, copy=True)
    return path
