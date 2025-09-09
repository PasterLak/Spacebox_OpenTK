using Engine;

namespace Spacebox.Game.Resource
{
    public class ItemMaterial : TextureMaterial
    {
        private Texture2D? _emission;

        public Texture2D? Emission
        {
            get => _emission;
            set
            {
                _emission = value;
                if (_emission != null)
                {
                    ReplaceTexture("texture1", _emission);
                }
            }
        }

        public ItemMaterial(Texture2D texture, Texture2D emission) :
            base(texture, Resources.Load<Shader>("Shaders/itemModel"))
        {
            RenderMode = RenderMode.Opaque;

            //RenderFace = RenderFace.Both;
            Emission = emission;
           
        }

        public Action Action;

        protected override void UpdateDynamicUniforms()
        {
            base.UpdateDynamicUniforms();

            Action?.Invoke();
        }
       
    }
}
