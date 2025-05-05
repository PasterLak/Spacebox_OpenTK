using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks
{
    public class StorageBlock : InteractiveBlock
    {
        private Storage? _storage;
        public Storage? Storage
        {
            get => _storage;
            set
            {
                _storage = value;
                _storage.OnDataWasChanged += OnStorageDataWasChanged;
            }
        }
        public ushort PositionIndex { get; private set; } = 0;

        public StorageBlock(BlockData blockData) : base(blockData)
        {

        }

        public void SetPositionInChunk(Vector3Byte positionChunk)
        {

            PositionIndex = (ushort)(positionChunk.Z * Chunk.Size * Chunk.Size + positionChunk.Y * Chunk.Size + positionChunk.X);
        }

        public static Vector3Byte PositionIndexToPositionInChunk(ushort positionIndex)
        {
            Vector3Byte pos = new Vector3Byte(0, 0, 0);

            pos.X = (byte)(positionIndex % Chunk.Size);
            pos.Y = (byte)(positionIndex / Chunk.Size % Chunk.Size);
            pos.Z = (byte)(positionIndex / (Chunk.Size * Chunk.Size));

            return pos;

        }



        private void OnStorageDataWasChanged(Storage storage)
        {
            if (chunk != null)
            {
                chunk.SpaceEntity.SetModified();
                chunk.IsModified = true;
            }
        }

        public override void Use(Astronaut player)
        {

            if (Storage == null)
            {
                Storage = new Storage(8, 3);
            }

            base.Use(player);

            StorageUI.OpenStorage(this, player);

        }
    }
}
