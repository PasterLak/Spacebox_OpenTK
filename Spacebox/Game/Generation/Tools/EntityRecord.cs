using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Tools;

public readonly record struct EntityRecord(long Id, string Name, Vector3i Sector, Vector3 Local);
