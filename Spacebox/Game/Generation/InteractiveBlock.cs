﻿using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Player;
using Spacebox.Game.Resources;
using Engine;
using Spacebox.GUI;
namespace Spacebox.Game.Generation
{

    public interface IInteractiveBlock
    {
        public Keys KeyToUse { get; set; }
        public string HoverText{ get; set; }
        public Action<Astronaut> OnUse { get; set; }
        public const byte InteractionDistance = 3;
        public const byte InteractionDistanceSquared = (InteractionDistance * InteractionDistance);


        public void Use(Astronaut a);
        void OnHovered();
        void OnNotHovered();
        void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk, Vector3 hitPos);


    }
    public class InteractiveBlock : Block
    {

        public Keys KeyToUse { get; private set; } = Keys.F;
        public const byte InteractionDistance = 3;
        public const byte InteractionDistanceSquared = (InteractionDistance * InteractionDistance);
        public string HoverText = "Press RMB to use";

        public Action<Astronaut> OnUse;
        public Chunk chunk;
        private bool lasState;

        public Vector3 colorIfActive = new Vector3(0.7f, 0.4f, 0.2f) / 4f;
        public virtual void Use(Astronaut player)
        {
            CenteredText.SetText(HoverText);
            OnUse?.Invoke(player);
        }

        private void OnHovered()
        {
            CenteredText.SetText(HoverText);
            CenteredText.Show();
        }

        private void OnNotHovered()
        {

            CenteredText.Hide();
        }

        public static void UpdateInteractive(InteractiveBlock block, Astronaut player, Chunk chunk, Vector3 hitPos)
        {
            var disSq = Vector3.DistanceSquared(player.Position, hitPos);

            if (disSq > InteractionDistanceSquared)
            {
                block.OnNotHovered();
            }
            else
            {
                block.OnHovered();
                if (ToggleManager.OpenedWindowsCount == 0)
                {
                    if (Input.IsMouseButtonDown(MouseButton.Right))
                    {
                        block.chunk = chunk;
                        block.Use(player);

                    }
                }
            }
        }


        public InteractiveBlock(BlockData blockData) : base(blockData)
        {
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

                chunk.MarkNeedsRegenerate();
            }
            lasState = state;
        }


    }
}
