using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public readonly record struct ReconstructorKey(long EntityId, Vector3i Cell);
