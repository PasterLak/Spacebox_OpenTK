using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Engine;
using Spacebox.GUI;
using Engine.InputPro;
using Spacebox.Game.Physics;
namespace Spacebox.Game.Generation.Blocks
{

    public class InteractiveBlock : ElectricalBlock
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public const byte InteractionDistance = 3;
        public const byte InteractionDistanceSquared = InteractionDistance * InteractionDistance;
        public string HoverTextBlockName = "";
        public string HoverText = "Press RMB to use";
        public string HoverTextDeactivated = "No power";
        public Action<Astronaut> OnUse;
        public Chunk chunk;
        private bool lasState;

        public Vector3 colorIfActive = new Vector3(0.7f, 0.4f, 0.2f) / 4f;
        public virtual void Use(Astronaut player, ref HitInfo hit)
        {

            SetText();
            OnUse?.Invoke(player);
        }


        private void SetText()
        {
            var action = InputManager.Instance.GetAction("use");

            string keyName = "button";

            if ((action != null) && action.Bindings.Count > 0)
            {
                var key = action.Bindings[0];
                keyName = key.GetDisplayName();

                if(key is MouseKeyBinding)
                {
                    if(((MouseKeyBinding)key).Key == MouseButton.Right)
                    {
                        keyName = "RMB";
                    }
                    else if (((MouseKeyBinding)key).Key == MouseButton.Left)
                    {
                        keyName = "LMB";
                    }
                }
            }

            HoverText = "Press " + keyName + " to use\n";
            if (EFlags == ElectricalFlags.None)
            {
                CenteredText.SetText( 
                     HoverText + HoverTextBlockName);
                return;
            }
            CenteredText.SetText(IsActive ?   HoverText + HoverTextBlockName : HoverTextDeactivated);

        }
        private void OnHovered()
        {
            SetText();
            CenteredText.Show();
        }

        private void OnNotHovered()
        {

            //CenteredText.Hide();
        }

        public static void UpdateInteractive(InteractiveBlock block, Astronaut player, ref HitInfo hit)
        {
            var disSq = Vector3.DistanceSquared(player.Position, hit.position);

            if (disSq > InteractionDistanceSquared)
            {
                block.OnNotHovered();
            }
            else
            {
                block.OnHovered();
                if (ToggleManager.OpenedWindowsCount == 0)
                {
                    if (Input.IsActionDown("use"))
                    {
                        block.chunk = hit.chunk;
                        block.Use(player, ref  hit);

                    }
                }
            }
        }


        public InteractiveBlock(BlockJSON blockData) : base(blockData)
        {
            EFlags = ElectricalFlags.None;
            MaxPower = 300;
            ConsumptionRate = 0;
            CurrentPower = 0;
            EnableEmission = true;
        }

        public void SetEmissionWithoutRedrawChunk(bool state)
        {
            EnableEmission = state;
            lasState = state;
        }
        public void SetEmission(bool state)
        {

            EnableEmission = state;

            if (chunk != null && EnableEmission != lasState)
            {
                //chunk.GenerateMesh(); MarkNeedsRegenerate

                chunk.NeedsToRegenerateMesh = true;
            }
            lasState = state;
        }


    }
}
