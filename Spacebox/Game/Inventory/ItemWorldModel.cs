using Engine;
using Engine.Components;
using Engine.Components.Debug;


namespace Spacebox.Game;

public class ItemWorldModel : Node3D
{
    private ModelRendererComponent modelRendererComponent;
    public ItemWorldModel(string itemId, float modelDepth = 0.5f)
    {
        //var itemTexture = Resources.Load<Texture2D>(texturePath);
        //itemTexture.FilterMode = FilterMode.Nearest;
      
        Name = "ItemWorldModel";
       // Mesh item = ItemModelGenerator.GenerateMeshFromTexture(itemTexture,  modelDepth);

        var item = GameAssets.GetItemByFullID(itemId);

        modelRendererComponent = AttachComponent(new ModelRendererComponent(null));
        ChangeModelTo(item);

        //cm.Offset = new Vector3(-0.5f, -0.5f, -modelDepth/2f);
        AttachComponent(new AxesDebugComponent());
        AttachComponent(new OBBCollider());
       
    }

    public void ChangeModelTo(Item item)
    {
        var model = GameAssets.GetItemWorldModelById(item.Id);
        modelRendererComponent.Model = model;
    }

    public override void Render()
    {
        base.Render();
    }
}