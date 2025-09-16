using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
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
                Name = _storage.Name;
            }
        }
        public ushort PositionIndex { get; private set; } = 0;
       
        public string Name
        {
            get => _storage?.Name ?? ""; 
            set
            {
                if (_storage == null) return;

                _storage.Name = string.Empty;
                _storage.Name = value;

                if(_storage.Name == "" || _storage.Name == " " || _storage.Name == _blockData.Name || _storage.Name == "Storage")
                {
                    HoverTextBlockName = "";
                }
                else
                HoverTextBlockName = ("\n\n"+ _storage.Name);
                
                if(chunk != null)
                chunk.IsModified = true;
            }
        }
        private BlockData _blockData;
        public StorageBlock(BlockData blockData) : base(blockData)
        {
            _blockData = blockData;
           
        }

        public bool NeedsToSaveName(out string name)
        {
            name = _storage.Name;
            if (_storage.Name == "" || _storage.Name == " " || _blockData.Name == _storage.Name || _storage.Name == "Storage")
            {
                return false;
            }
           
            return true;
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

        public override void Use(Astronaut player, ref HitInfo hit)
        {

            if (Storage == null)
            {
                Storage = new Storage(8, 3);
            }

            base.Use(player, ref hit);

            StorageUI.OpenStorage(this, player);

        }
    }
}
