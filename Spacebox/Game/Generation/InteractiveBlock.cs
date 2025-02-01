using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;
using Engine;
namespace Spacebox.Game.Generation
{
    public class InteractiveBlock : Block
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public const byte InteractionDistance = 3;
        public const byte InteractionDistanceSquared = (InteractionDistance * InteractionDistance);
        public string HoverText = "Hover Text";

        public Action<Astronaut> OnUse;
        public Chunk chunk;
        private bool lasState;

        public Vector3 colorIfActive = new Vector3(0.7f, 0.4f, 0.2f) / 4f;
        public virtual void Use(Astronaut player)
        {
            OnUse?.Invoke(player);
        }

        public InteractiveBlock(BlockData blockData) : base(blockData)
        {
        }

        public void SetEmissionWithoutRedrawChunk(bool state)
        {
            EnableEmission = state;
            lasState = state;

            if (state)
            {
              //  LightLevel = 15;
              //  LightColor = colorIfActive;

            }
            else
            {
              //  LightLevel = 0;
               // LightColor = Vector3.Zero;
            }
        }
        public void SetEmission(bool state)
        {

            EnableEmission = state;
            
            if(chunk != null && EnableEmission != lasState)
            {
                if (state)
                {
                   // LightLevel = 15;
                    //    LightColor = colorIfActive;
                    
                }
                else
                {
                  //  LightLevel = 0;
                  //  LightColor = Vector3.Zero;
                }
                chunk.GenerateMesh();
            }
            lasState = state;
        }


    }
}
