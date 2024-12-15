
using System.Globalization;
using OpenTK.Mathematics;

namespace Spacebox.Common.Utils
{
    public static class ObjLoader
    {
        public static (float[] vertices, int[] indices) Load(string path)
        {
            var positions = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertices = new List<float>();
            var indices = new List<int>();
            var vertexIndices = new Dictionary<string, int>();
            int index = 0;

            foreach (var line in File.ReadLines(path))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "v":
                        positions.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                        ));
                        break;
                    case "vt":
                        texCoords.Add(new Vector2(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            1 - float.Parse(parts[2], CultureInfo.InvariantCulture)
                        ));
                        break;
                    case "vn":
                        normals.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                        ));
                        break;
                    case "f":
                        for (int i = 1; i + 2 <= parts.Length - 1; i++)
                        {
                            int[] faceIndices = new int[3];
                            for (int j = 0; j < 3; j++)
                            {
                                string key = parts[j == 0 ? 1 : i + j];
                                if (!vertexIndices.TryGetValue(key, out int idx))
                                {
                                    var v = key.Split('/');
                                    var position = positions[int.Parse(v[0]) - 1];
                                    var texCoord = texCoords.Count > 0 && v.Length > 1 && !string.IsNullOrEmpty(v[1]) ? texCoords[int.Parse(v[1]) - 1] : Vector2.Zero;
                                    var normal = normals.Count > 0 && v.Length > 2 && !string.IsNullOrEmpty(v[2]) ? normals[int.Parse(v[2]) - 1] : Vector3.UnitY;
                                    vertices.Add(position.X);
                                    vertices.Add(position.Y);
                                    vertices.Add(position.Z);
                                    vertices.Add(normal.X);
                                    vertices.Add(normal.Y);
                                    vertices.Add(normal.Z);
                                    vertices.Add(texCoord.X);
                                    vertices.Add(texCoord.Y);
                                    idx = index;
                                    vertexIndices[key] = idx;
                                    index++;
                                }
                                faceIndices[j] = idx;
                            }
                            indices.Add(faceIndices[0]);
                            indices.Add(faceIndices[1]);
                            indices.Add(faceIndices[2]);
                        }
                        break;
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }
    }
}
