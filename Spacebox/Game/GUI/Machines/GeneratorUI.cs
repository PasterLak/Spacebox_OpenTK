using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI
{
    public class GeneratorUI
    {
        private static ElectricalBlock generatorBlock;

        public static void Reset()
        {
            generatorBlock = null;
        }
        static int power;
        static int consum;
        static string blockName = "Generator";

        public static bool IsOpen => UIManager.IsOpen("generator") && generatorBlock != null;
        public static ElectricalBlock Block => generatorBlock;
        public static string BlockName => blockName;
        public static int NetworkPower => power;
        public static int NetworkConsumption => consum;

        public static void Initialize()
        {

        }

        public static void Open(ElectricalBlock block, Astronaut astronaut, ref HitInfo hit)
        {
            generatorBlock = block;
            var pos = hit.blockPositionEntity;
            var id = hit.chunk.SpaceEntity.ElectricManager.GetNetworkId((pos.X, pos.Y, pos.Z));
            (power, consum) = hit.chunk.SpaceEntity.ElectricManager.GetNetworkPowerFlow(id);

            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            UIManager.Open("generator");
        }

        public static void Close()
        {
            generatorBlock = null;
            UIManager.CloseTop();
        }

        public static void Update()
        {
            if (!IsOpen) return;
            if (Input.IsActionDown("inventory") || Input.IsKeyDown(Keys.Escape)) Close();
        }

        public static float FreeShare => power <= 0 ? 0f : 1f - Math.Min(0.99f, consum / (float)power);

        public static int RemainingPercent => power == 0 ? 100 : (int)(100 - consum / (float)power * 100);
    }
}
