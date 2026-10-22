using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Respawn;

public readonly record struct ReconstructionGrant(ReconstructorKey Key, Vector3 SpawnPosition, Quaternion Rotation, short BlockId, Vector3 Center);
