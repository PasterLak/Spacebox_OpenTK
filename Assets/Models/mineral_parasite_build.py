import math
import sys

import bpy
from mathutils import Vector

TEXTURE_SIZE = 64.0

BODY = (0, 0, 32, 32)
MOUTH = (32, 0, 64, 32)
SPIKE = (0, 32, 16, 64)
BACK = (16, 32, 32, 48)
LIP = (32, 32, 48, 48)

HALF = 0.36
LIP_DEPTH = 0.05
LIP_WIDTH = 0.07
MAX_EXTENT = 0.5


def args():
    argv = sys.argv[sys.argv.index("--") + 1:]
    return {"texture": argv[0], "obj": argv[1], "blend": argv[2], "preview": argv[3] if len(argv) > 3 else None}


def rect_uv(rect):
    x0, y0, x1, y1 = rect
    u0, u1 = x0 / TEXTURE_SIZE, x1 / TEXTURE_SIZE
    v0, v1 = 1.0 - y1 / TEXTURE_SIZE, 1.0 - y0 / TEXTURE_SIZE
    return [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]


class MeshBuilder:
    def __init__(self):
        self.verts = []
        self.faces = []
        self.uvs = []

    def quad(self, corners, rect):
        self.face(corners, rect_uv(rect))

    def face(self, corners, uvs):
        start = len(self.verts)
        self.verts.extend(tuple(c) for c in corners)
        self.faces.append(tuple(range(start, start + len(corners))))
        self.uvs.append(uvs)

    def box(self, center, size, rects):
        c = Vector(center)
        h = Vector(size) * 0.5
        x, y, z = h
        p = lambda sx, sy, sz: c + Vector((sx * x, sy * y, sz * z))
        self.quad([p(-1, -1, -1), p(1, -1, -1), p(1, -1, 1), p(-1, -1, 1)], rects["front"])
        self.quad([p(1, 1, -1), p(-1, 1, -1), p(-1, 1, 1), p(1, 1, 1)], rects["back"])
        self.quad([p(-1, 1, -1), p(-1, -1, -1), p(-1, -1, 1), p(-1, 1, 1)], rects["side"])
        self.quad([p(1, -1, -1), p(1, 1, -1), p(1, 1, 1), p(1, -1, 1)], rects["side"])
        self.quad([p(-1, -1, 1), p(1, -1, 1), p(1, 1, 1), p(-1, 1, 1)], rects["top"])
        self.quad([p(-1, 1, -1), p(1, 1, -1), p(1, -1, -1), p(-1, -1, -1)], rects["bottom"])

    def spike(self, base, direction, length, width, twist=0.0):
        d = Vector(direction).normalized()
        helper = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
        a = d.cross(helper).normalized()
        b = d.cross(a).normalized()
        cos_t, sin_t = math.cos(twist), math.sin(twist)
        a, b = a * cos_t + b * sin_t, b * cos_t - a * sin_t

        base = Vector(base)
        w = width * 0.5
        ring = [base + (a * sx + b * sy) * w for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        apex = base + d * length

        x0, y0, x1, y1 = SPIKE
        u0, u1 = x0 / TEXTURE_SIZE, x1 / TEXTURE_SIZE
        top, bottom = 1.0 - y0 / TEXTURE_SIZE, 1.0 - y1 / TEXTURE_SIZE
        for i in range(4):
            j = (i + 1) % 4
            self.face([ring[i], ring[j], apex], [(u0, bottom), (u1, bottom), ((u0 + u1) * 0.5, top)])
        self.face([ring[3], ring[2], ring[1], ring[0]], [(u0, bottom), (u1, bottom), (u1, bottom + 0.05), (u0, bottom + 0.05)])


def build_mesh():
    m = MeshBuilder()

    body = {"front": MOUTH, "back": BACK, "side": BODY, "top": BODY, "bottom": BODY}
    m.box((0, 0, 0), (HALF * 2, HALF * 2, HALF * 2), body)

    lip = {"front": LIP, "back": LIP, "side": LIP, "top": LIP, "bottom": LIP}
    y = -HALF - LIP_DEPTH * 0.5
    span = HALF * 2
    m.box((0, y, HALF - LIP_WIDTH * 0.5), (span, LIP_DEPTH, LIP_WIDTH), lip)
    m.box((0, y, -HALF + LIP_WIDTH * 0.5), (span, LIP_DEPTH, LIP_WIDTH), lip)
    m.box((HALF - LIP_WIDTH * 0.5, y, 0), (LIP_WIDTH, LIP_DEPTH, span - LIP_WIDTH * 2), lip)
    m.box((-HALF + LIP_WIDTH * 0.5, y, 0), (LIP_WIDTH, LIP_DEPTH, span - LIP_WIDTH * 2), lip)

    front = -HALF - LIP_DEPTH
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.spike((sx * (HALF - 0.04), front, sz * (HALF - 0.04)), (sx * 0.8, -1.0, sz * 0.8), 0.24, 0.09, 0.3)
    for axis in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        sx, sz = axis
        m.spike((sx * (HALF - 0.03), front, sz * (HALF - 0.03)), (sx * 1.1, -0.6, sz * 1.1), 0.2, 0.08, 0.6)

    m.spike((0.12, 0.05, HALF), (0.3, 0.4, 1.0), 0.22, 0.1, 0.2)
    m.spike((-0.15, 0.18, HALF), (-0.4, 0.2, 1.0), 0.17, 0.08, 0.9)
    m.spike((HALF, 0.15, 0.1), (1.0, 0.5, 0.3), 0.18, 0.09, 0.4)
    m.spike((-HALF, 0.1, -0.12), (-1.0, 0.4, -0.2), 0.16, 0.08, 1.2)
    m.spike((0.1, HALF, -0.08), (0.2, 1.0, -0.3), 0.15, 0.08, 0.7)
    m.spike((-0.12, -0.1, -HALF), (-0.2, -0.3, -1.0), 0.12, 0.07, 0.5)

    extent = max(max(abs(c) for c in v) for v in m.verts)
    scale = min(1.0, MAX_EXTENT / extent)
    verts = [tuple(c * scale for c in v) for v in m.verts]

    mesh = bpy.data.meshes.new("MineralParasite")
    mesh.from_pydata(verts, [], m.faces)
    mesh.update()

    uv_layer = mesh.uv_layers.new(name="UVMap")
    loop = 0
    for face_uvs in m.uvs:
        for uv in face_uvs:
            uv_layer.data[loop].uv = uv
            loop += 1

    return mesh


def build_material(texture_path):
    image = bpy.data.images.load(texture_path)
    material = bpy.data.materials.new("MineralParasite")
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = "Closest"
    material.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.8
    return material


def render_preview(obj, path):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.film_transparent = False
    scene.world = scene.world or bpy.data.worlds.new("World")

    cam_data = bpy.data.cameras.new("PreviewCamera")
    cam = bpy.data.objects.new("PreviewCamera", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (1.4, -2.0, 1.1)
    direction = Vector((0, 0, 0)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    paths = args()
    bpy.ops.wm.read_factory_settings(use_empty=True)

    mesh = build_mesh()
    mesh.materials.append(build_material(paths["texture"]))
    obj = bpy.data.objects.new("MineralParasite", mesh)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    bpy.ops.wm.obj_export(
        filepath=paths["obj"],
        export_selected_objects=True,
        export_uv=True,
        export_normals=True,
        export_materials=False,
        export_triangulated_mesh=True,
        forward_axis="NEGATIVE_Z",
        up_axis="Y",
    )

    bpy.ops.wm.save_as_mainfile(filepath=paths["blend"])

    if paths["preview"]:
        render_preview(obj, paths["preview"])


main()
