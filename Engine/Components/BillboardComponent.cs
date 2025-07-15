using Engine.Utils;
using Engine;
using OpenTK.Mathematics;
using Engine.Components;

public class BillboardComponent : Component
{
    Mesh mesh = GenMesh.CreateQuad();
    public BillboardMaterial Material { get; private set; }


    public BillboardComponent(Texture2D texture, float w = 1, float h = 1)
    {
        texture.FilterMode = FilterMode.Nearest;
        Material = new BillboardMaterial(texture, new Vector2(w, h));

    }

    public BillboardComponent(BillboardMaterial material, float w = 1, float h = 1)
    {
        Material = material;
        material.Size = new Vector2(w, h);
    }

    public override void OnRender()
    {
        var cam = Camera.Main;
        if (cam == null) return;

        Material.Apply(Owner.GetRenderModelMatrix());
        mesh.Render();
    }
}
