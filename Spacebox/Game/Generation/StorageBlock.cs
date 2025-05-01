using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation
{
    public class StorageBlock : InteractiveBlock
    {
        public Storage? Storage { get; set; }
        public int PositionIndex { get; private set; } = -1;
        public StorageBlock(BlockData blockData) : base(blockData)
        {
            Storage = new Storage(8, 3);
        }

        public void SetPositionInEntity(Vector3i positionEntitySpace)
        {

            PositionIndex = positionEntitySpace.X * positionEntitySpace.Y * positionEntitySpace.Z;

        }

        private void OnStorageDataWasChanged(Storage storage)
        {
            if(chunk != null)
            {
                chunk.IsModified = true;
            }
        }

        public override void Use(Astronaut player)
        {
            
            if (PositionIndex == -1) return;



            if (World.CurrentSector.TryGetNearestEntity(player.Position, out var entity))
            {

            }
            else return;

            try
            {
                var storage = WorldSaveLoad.LoadStorage(PositionIndex, entity);

                if(storage != null)
                {
                    Storage = storage;
                    Storage.OnDataWasChanged += OnStorageDataWasChanged;
                }
                else
                {
                    Debug.Error("Loaded storage was null! PosInEntity: " + PositionIndex);
                }
            }
            catch (Exception e)
            {
                Debug.Error("Error loading storage: " + e);
                return;
            }

            if (Storage == null) return;
            base.Use(player);

            StorageUI.OpenStorage(this, player);

        }
    }
}
