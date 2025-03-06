using Engine;

namespace Spacebox.Game.Resource
{
    public class ItemMaterial : TextureMaterial
    {
 
        public ItemMaterial(Texture2D texture) :
            base(texture, Resources.Load<Shader>("Shaders/itemModel"))
        {
            RenderMode = RenderMode.Opaque;

            //RenderFace = RenderFace.Both;
        }

        public Action Action; 
         
        protected override void SetMaterialProperties()
        {
            base.SetMaterialProperties();

            Action?.Invoke();
        }
    }
}
