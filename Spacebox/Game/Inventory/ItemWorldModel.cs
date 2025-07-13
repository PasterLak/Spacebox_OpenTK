using Engine;
using Engine.Components;
using Engine.Components.Debug;


namespace Spacebox.Game;

public class ItemWorldModel : Node3D
{
    public ItemWorldModel(string texturePath, float modelDepth = 0.5f)
    {
        var itemTexture = Resources.Load<Texture2D>(texturePath);
        itemTexture.FilterMode = FilterMode.Nearest;

        Name = "ItemWorldModel";
        Mesh item = ItemModelGenerator.GenerateMeshFromTexture(itemTexture,  modelDepth);
        
        var cm = AttachComponent(new ModelRendererComponent(new Model(item, new TextureMaterial(itemTexture))));
        //cm.Offset = new Vector3(-0.5f, -0.5f, -modelDepth/2f);
        AttachComponent(new AxesDebugComponent());
        AttachComponent(new OBBCollider());
       
    }

    public override void Render()
    {
        base.Render();
    }
}