using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public readonly record struct BrokenMultiblock(Vector3i Cell, Block Block);
