using Engine;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Editor.Preview;

public sealed class BlockPreviewContent : IPreviewContent
{
    private const string Shader = "Resources/Shaders/movingBlock";

    private readonly SimpleBlock _block;

    public Vector3 Center { get; }
    public float Radius { get; }
    public bool IsReady => true;

    public BlockPreviewContent(BlockData data, BlockState state)
    {
        var (vertices, indices) = MultiblockPreviewMesh.Build(data, state);
        var material = new TextureMaterial(GameAssets.BlocksTexture, Resources.Load<Shader>(Shader, true));
        _block = new SimpleBlock(material, Vector3.Zero) { DisposesTexture = false };
        _block.SetCustomMesh(vertices, indices);
        _block.ExtraLight = Vector3.One;

        var size = new Vector3(data.Size.X, data.Size.Y, data.Size.Z);
        Center = (size - Vector3.One) * 0.5f;
        Radius = size.Length * 0.5f;
    }

    public void Update(float deltaTime)
    {
    }

    public void Render()
    {
        _block.Render();
    }

    public void Dispose()
    {
        _block.Destroy();
    }
}
