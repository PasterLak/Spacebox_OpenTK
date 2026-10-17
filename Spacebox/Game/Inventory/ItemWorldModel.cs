using Engine;
using Engine.Components;
using Engine.Components.Debug;

namespace Spacebox.Game
{
    public class ItemWorldModel : Node3D
    {
        public bool ShowModel = false;
        private ModelRendererComponent modelRendererComponent;

        public ItemWorldModel()
        {
            Name = "ItemWorldModel";
           
            modelRendererComponent = AttachComponent(new ModelRendererComponent(null));
           

            AttachComponent(new AxesDebugComponent());
            AttachComponent(new OBBCollider());
        }

        public void ChangeModelTo(Item item)
        {
            if (item == null)
            {
                Debug.Error($"ItemWorldModel: Item is null.");
                return;
            }
            var model = GameAssets.GetItemWorldModelById(item.Id);
            if (modelRendererComponent != null)
            {
                modelRendererComponent.Model = model;
            }
        }

        public override void Render()
        {
            if (!ShowModel) return;
            base.Render();
        }
    }
}