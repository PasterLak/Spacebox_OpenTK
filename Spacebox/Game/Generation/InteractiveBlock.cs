using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;

namespace Spacebox.Game.Generation
{
    public class InteractiveBlock : Block
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public string HoverText = "Hover Text";

        public Action<Astronaut> OnUse;
        public Chunk chunk;
        private bool lasState;

        public Vector3 colorIfActive = new Vector3(0.7f, 0.4f, 0.2f) / 4f;
        public virtual void Use(Astronaut player)
        {
            OnUse?.Invoke(player);
        }

        public InteractiveBlock(Vector2 textureCoords, Vector3? color = null, float lightLevel = 0, Vector3? lightColor = null) 
            : base(textureCoords, color, lightLevel, lightColor)
        {
        }
        public InteractiveBlock(BlockData blockData) : base(blockData)
        {
            
            if (blockData.Name == "Radar")
            {
                OnUse += RadarWindow.Instance.Toggle;
            }

        }

        public void SetEmissionWithoutRedrawChunk(bool state)
        {
            enableEmission = state;
            lasState = state;

            if (state)
            {
                LightLevel = 15;
                LightColor = colorIfActive;

            }
            else
            {
                LightLevel = 0;
                LightColor = Vector3.Zero;
            }
        }
        public void SetEmission(bool state)
        {
            enableEmission = state;
            
            if(chunk != null && enableEmission != lasState)
            {
                if (state)
                {
                    LightLevel = 15;
                        LightColor = colorIfActive;
                    
                }
                else
                {
                    LightLevel = 0;
                    LightColor = Vector3.Zero;
                }
                chunk.GenerateMesh();
            }
            lasState = state;
        }


    }
}
