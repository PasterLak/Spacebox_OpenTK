import os
import random
import sys
import numpy as np
from PIL import Image

player_dir = sys.argv[1]
gameset = sys.argv[2]
out_dir = os.path.join(gameset, "Models", "Armor")
os.makedirs(out_dir, exist_ok=True)

PIXEL = 1.0 / 32.0
TEX = 64


def load(path):
    verts, uvs, faces = [], [], []
    for line in open(path):
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "v":
            verts.append([float(x) for x in parts[1:4]])
        elif parts[0] == "vt":
            uvs.append([float(x) for x in parts[1:3]])
        elif parts[0] == "f":
            face = []
            for token in parts[1:]:
                ids = token.split("/")
                face.append((int(ids[0]) - 1, int(ids[1]) - 1))
            faces.append(face)
    return np.array(verts), np.array(uvs), faces


def components(verts, faces):
    parent = list(range(len(verts)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    for face in faces:
        for (a, _), (b, _) in zip(face, face[1:]):
            ra, rb = find(a), find(b)
            if ra != rb:
                parent[ra] = rb

    groups = {}
    for face in faces:
        groups.setdefault(find(face[0][0]), []).append(face)
    return list(groups.values())


def face_normal(verts, face):
    pts = verts[[v for v, _ in face]]
    normal = np.zeros(3)
    for i in range(len(pts)):
        normal += np.cross(pts[i], pts[(i + 1) % len(pts)])
    return normal / np.linalg.norm(normal)


def outward(verts, group):
    ids = sorted({v for face in group for v, _ in face})
    center = verts[ids].mean(axis=0)
    result = []
    for face in group:
        normal = face_normal(verts, face)
        face_center = verts[[v for v, _ in face]].mean(axis=0)
        if np.dot(normal, face_center - center) < 0:
            normal = -normal
        result.append((face, normal))
    return result, center


def shell(verts, group, keep=None):
    faces, center = outward(verts, group)
    vertex_normals = {}
    for face, normal in faces:
        for v, _ in face:
            known = vertex_normals.setdefault(v, [])
            if all(np.dot(normal, n) < 0.99 for n in known):
                known.append(normal)

    offset = {v: sum(normals) * PIXEL for v, normals in vertex_normals.items()}
    out = []
    for face, normal in faces:
        if keep is not None and not keep(face, normal, center):
            continue
        out.append(([(verts[v] + offset[v], t) for v, t in face], normal))
    return out


def write_obj(path, faces, uvs):
    lines = ["# Spacebox armor, generated from the astronaut model (1 texel outside the body)"]
    positions, texcoords, normals, polys = [], [], [], []
    for corners, normal in faces:
        normals.append(normal)
        poly = []
        for position, t in corners:
            positions.append(position)
            texcoords.append(uvs[t])
            poly.append((len(positions), len(texcoords), len(normals)))
        polys.append(poly)
    lines += [f"v {p[0]:.6f} {p[1]:.6f} {p[2]:.6f}" for p in positions]
    lines += [f"vt {t[0]:.6f} {t[1]:.6f}" for t in texcoords]
    lines += [f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}" for n in normals]
    lines += ["f " + " ".join(f"{a}/{b}/{c}" for a, b, c in poly) for poly in polys]
    open(path, "w", newline="\n").write("\n".join(lines) + "\n")


body_v, body_uv, body_f = load(os.path.join(player_dir, "Astronaut_Body_Fly.obj"))
helmet_v, helmet_uv, helmet_f = load(os.path.join(player_dir, "Astronaut_Helmet_Closed.obj"))

body_parts = components(body_v, body_f)
helmet_parts = components(helmet_v, helmet_f)


def part_center(verts, group):
    return verts[sorted({v for face in group for v, _ in face})].mean(axis=0)


def pick(verts, groups, predicate):
    return [g for g in groups if predicate(part_center(verts, g), len(g))]


torso = pick(body_v, body_parts, lambda c, n: abs(c[0]) < 0.05)
upper_arms = pick(body_v, body_parts, lambda c, n: abs(c[0]) > 0.3 and c[1] > -0.2)
lower_arms = pick(body_v, body_parts, lambda c, n: abs(c[0]) > 0.3 and c[1] <= -0.2)
thighs = pick(body_v, body_parts, lambda c, n: 0.05 < abs(c[0]) < 0.3 and n == 6)
boots = pick(body_v, body_parts, lambda c, n: 0.05 < abs(c[0]) < 0.3 and n > 6)
helmet_box = max(helmet_parts, key=len)


def helmet_keep(face, normal, center):
    face_center = helmet_v[[v for v, _ in face]].mean(axis=0)
    distance = np.dot(face_center - center, normal)
    ids = sorted({v for f in helmet_box for v, _ in f})
    extent = max(np.dot(helmet_v[i] - center, normal) for i in ids)
    is_outer = distance > extent - 1e-3
    is_front = normal[2] < -0.5
    is_bottom = normal[1] < -0.7
    return is_outer and not is_front and not is_bottom


slots = {
    "helmet": [(helmet_v, helmet_uv, helmet_box, helmet_keep)],
    "chest": [(body_v, body_uv, g, None) for g in torso],
    "shoulders": [(body_v, body_uv, g, None) for g in upper_arms],
    "arms": [(body_v, body_uv, g, None) for g in lower_arms],
    "legs": [(body_v, body_uv, g, None) for g in thighs],
    "boots": [(body_v, body_uv, g, None) for g in boots],
}

painted = []
for name, parts in slots.items():
    faces = []
    uv_source = None
    for verts, uvs, group, keep in parts:
        part_faces = shell(verts, group, keep)
        faces += part_faces
        uv_source = uvs
        for corners, _ in part_faces:
            painted.append([uvs[t] for _, t in corners])
    write_obj(os.path.join(out_dir, f"armor_{name}.obj"), faces, uv_source)
    print(f"armor_{name}.obj: {len(faces)} faces from {len(parts)} parts")


def ramp_of(path):
    pixels = [p[:3] for p in Image.open(path).convert("RGBA").getdata() if p[3] > 0]
    return sorted(pixels, key=lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2])


def sample(ramp, q):
    position = max(0.0, min(1.0, q)) * (len(ramp) - 1)
    low = int(position)
    high = min(low + 1, len(ramp) - 1)
    t = position - low
    return tuple(round(ramp[low][i] * (1 - t) + ramp[high][i] * t) for i in range(3))


def paint(ramp, seed):
    rnd = random.Random(seed)
    image = Image.new("RGBA", (TEX, TEX), (0, 0, 0, 0))
    px = image.load()
    for corners in painted:
        us = [c[0] * TEX for c in corners]
        vs = [(1 - c[1]) * TEX for c in corners]
        x0, x1 = int(round(min(us))), int(round(max(us)))
        y0, y1 = int(round(min(vs))), int(round(max(vs)))
        w, h = x1 - x0, y1 - y0
        for y in range(y0, y1):
            for x in range(x0, x1):
                if not (0 <= x < TEX and 0 <= y < TEX):
                    continue
                edge = x in (x0, x1 - 1) or y in (y0, y1 - 1)
                top = y == y0 + 1 or x == x0 + 1
                rivet = w >= 6 and h >= 6 and x in (x0 + 2, x1 - 3) and y in (y0 + 2, y1 - 3)
                if edge:
                    q = 0.12
                elif rivet:
                    q = 0.95
                elif top:
                    q = 0.78
                else:
                    q = 0.55 + rnd.choice((-0.05, 0.0, 0.0, 0.04))
                px[x, y] = (*sample(ramp, q), 255)
    return image


ingots = os.path.join(gameset, "Items", "Ingots")
for metal, ingot in (("aluminium", "aluIngot.png"), ("iron", "ironIngot.png"), ("titanium", "titIngot.png")):
    paint(ramp_of(os.path.join(ingots, ingot)), 7).save(os.path.join(out_dir, f"armor_{metal}.png"))
    print(f"armor_{metal}.png")
