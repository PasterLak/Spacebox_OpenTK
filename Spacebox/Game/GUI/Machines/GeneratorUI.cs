using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI
{
    public sealed class GeneratorUI : IGameUiController
    {
        private ElectricalBlock generatorBlock;

        public void Reset()
        {
            generatorBlock = null;
        }
        private int power;
        private int consum;
        private string blockName = "Generator";

        public bool IsOpen => UIManager.IsOpen("generator") && generatorBlock != null;
        public ElectricalBlock Block => generatorBlock;
        public string BlockName => blockName;
        public int NetworkPower => power;
        public int NetworkConsumption => consum;

        public void Open(ElectricalBlock block, Astronaut astronaut, ref HitInfo hit)
        {
            generatorBlock = block;
            var pos = hit.blockPositionEntity;
            var id = hit.chunk.SpaceEntity.ElectricManager.GetNetworkId((pos.X, pos.Y, pos.Z));
            (power, consum) = hit.chunk.SpaceEntity.ElectricManager.GetNetworkPowerFlow(id);

            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            UIManager.Open("generator");
        }

        public void Close()
        {
            generatorBlock = null;
            UIManager.CloseTop();
        }

        public void OnGUI()
        {
            if (!IsOpen) return;
            if (Input.IsActionDown("inventory") || Input.IsKeyDown(Keys.Escape)) Close();
        }

        public float FreeShare => power <= 0 ? 0f : 1f - Math.Min(0.99f, consum / (float)power);

        public int RemainingPercent => power == 0 ? 100 : (int)(100 - consum / (float)power * 100);
    }
}
