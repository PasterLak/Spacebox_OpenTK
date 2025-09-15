
namespace Spacebox.Game.Generation.Tools
{
    public static class NBTKey
    {
        public static class SECTOR
        {
            public const string index_x = "index_x";
            public const string index_y = "index_y";
            public const string index_z = "index_z";
        }

        public static class ENTITY
        {
            public const string id = "id";
            public const string name = "name";
            public const string world_x = "world_x";
            public const string world_y = "world_y";
            public const string world_z = "world_z";

            public const string rotation_x = "rotation_x";
            public const string rotation_y = "rotation_y";
            public const string rotation_z = "rotation_z";
            public const string rotation_w = "rotation_w";

            public const string chunks = "chunks";
        }

        public static class CHUNK
        {

            public const string index_x = "index_x";
            public const string index_y = "index_y";
            public const string index_z = "index_z";

            public const string palette_blocks = "palette_blocks";
            public const string palette_items = "palette_items";
            public const string blocks = "blocks";
            public const string rotations = "rotations";

            public const string storages = "storages";
            public const string processing_blocks = "processing_blocks";
        }

        public static class STORAGE
        {
            public const string storage = "storage";
            public const string position_chunk = "position_chunk";
            public const string size_xy = "size_xy";
            public const string name = "name";

            public const string slots_data = "slots_data";
         
        }
    }
}
