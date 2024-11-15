
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class AtlasTexture : IDisposable
    {
        private Dictionary<Texture2D, AtlasTextureInfo> Textures 
            = new Dictionary<Texture2D, AtlasTextureInfo>();

        private Dictionary<string, AtlasTextureInfo> ReadyTextures
            = new Dictionary<string, AtlasTextureInfo>();

        private bool PopulateFromTopToBottom;
        private string firsttextureName = "";

        public Vector2[] GetUVByName(string name)
        {
            if(ReadyTextures.ContainsKey(name))
            {
                return ReadyTextures[name].UV;
            }

         
            return ReadyTextures[firsttextureName].UV;
        }
        public Texture2D CreateTexture(string path, int blockSizePixels, bool populateFromTopToBottom)
        {
            PopulateFromTopToBottom = populateFromTopToBottom;
            var textures = CollectAllTextures(path);
           
            foreach (var tex in textures)
            {
                if(!Textures.ContainsKey(tex.Texture))
                {
                    AtlasTextureInfo info = new AtlasTextureInfo();
                    info.Name = tex.Name;
                    info.Size = new Vector2Byte(1, 1);
                    info.Texture = tex.Texture;
                    Textures.Add(tex.Texture, info);
                }
            }

            Debug.Log("textures: " + Textures.Count);


            var size = CalculateAtlasSize(blockSizePixels, CalculateBlocksNeeded());

            Texture2D atlas = PlaceTexturesInAtlas(size, blockSizePixels, populateFromTopToBottom);
            atlas.SaveToPng("atlas.png");

            CalculateUV(size.X, populateFromTopToBottom);

            foreach (var tex in ReadyTextures)
            {
                Debug.Log(tex.Value.ToString());
            }

            return atlas;
        }

        private Texture2D PlaceTexturesInAtlas(Vector2Byte atlasSizeBlocks, int blockSizePixels, bool fromTopToBottom)
        {
            if (atlasSizeBlocks.Y != atlasSizeBlocks.X)
            {
                Debug.Error("Atlas is not square. Using X value for both sides.");
                return new Texture2D(512, 512);
            }

            int sideBlocks = atlasSizeBlocks.X;
            int sidePixels = sideBlocks * blockSizePixels;

            Debug.Log($"atlas  sideBlocks: {sideBlocks}  sidePixels: {sidePixels} ");
            Texture2D atlas = new Texture2D(sidePixels, sidePixels);
            Color4[,] atlasPixels = new Color4[sidePixels, sidePixels];

            for (int i = 0; i < sidePixels; i++)
            {
                for (int p = 0; p < sidePixels; p++)
                {
                    atlasPixels[i, p] = new Color4(0, 0, 0, 0);
                }
            }

            List<Texture2D> textures = new List<Texture2D>();
            Debug.Log("atlas  : " + atlas.Width + " " + atlas.Height);
            foreach (var tex in Textures)
            {
                textures.Add(tex.Key);
            }

            if (fromTopToBottom)
            {
                PopulateAtlasFromTop(atlasPixels, textures, sideBlocks, blockSizePixels);
            }
            else
            {
                PopulateAtlasFromBottom(atlasPixels, textures, sideBlocks, blockSizePixels);
            }

            atlas.SetPixelsData(atlasPixels);
            atlas.UpdateTexture(true);

            return atlas;
        }

        private void PopulateAtlasFromTop(Color4[,] atlasPixels, List<Texture2D> textures, int sideBlocks, int blockSizePixels)
        {
            for (int y = 0; y < sideBlocks; y++)
            {
                for (int x = 0; x < sideBlocks; x++)
                {
                    int textureIndex = y * sideBlocks + x;

                    if (textureIndex < textures.Count)
                    {
                        Color4[,] texturePixels = textures[textureIndex].GetPixelData();
                        int pointerX = x * blockSizePixels;
                        int pointerY = y * blockSizePixels;

                        for (int y2 = 0; y2 < blockSizePixels; y2++)
                        {
                            for (int x2 = 0; x2 < blockSizePixels; x2++)
                            {
                                atlasPixels[pointerX + x2, pointerY + y2] = texturePixels[x2, y2];
                            }
                        }

                        AtlasTextureInfo atlasTextureInfo = Textures[textures[textureIndex]];
                        atlasTextureInfo.Position = new Vector2Byte((byte)x, (byte)y);

                        if (textureIndex == 0) firsttextureName = atlasTextureInfo.Name;
                        ReadyTextures.Add(atlasTextureInfo.Name, atlasTextureInfo);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void PopulateAtlasFromBottom(Color4[,] atlasPixels, List<Texture2D> textures, int sideBlocks, int blockSizePixels)
        {
            for (int y = 0; y < sideBlocks; y++)
            {
                for (int x = 0; x < sideBlocks; x++)
                {
                    int textureIndex = y * sideBlocks + x;

                    if (textureIndex < textures.Count)
                    {
                        Color4[,] texturePixels = textures[textureIndex].GetPixelData();
                        int pointerX = x * blockSizePixels;
                        int pointerY = (sideBlocks - 1 - y) * blockSizePixels;

                        for (int y2 = 0; y2 < blockSizePixels; y2++)
                        {
                            for (int x2 = 0; x2 < blockSizePixels; x2++)
                            {
                                atlasPixels[pointerX + x2, pointerY + y2] = texturePixels[x2, y2];
                            }
                        }

                        AtlasTextureInfo atlasTextureInfo = Textures[textures[textureIndex]];
                        atlasTextureInfo.Position = new Vector2Byte((byte)x, (byte)y);
                        ReadyTextures.Add(atlasTextureInfo.Name, atlasTextureInfo);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }


        private void CalculateUV(int sideBlocks, bool fromTopToBottom)
        {
            foreach(var data in ReadyTextures)
            {
                var pos = data.Value.Position;
                if(fromTopToBottom)
                {
                    var newPos = new Vector2Byte(pos.X, (byte)(sideBlocks - pos.Y));
                    data.Value.UV = UVAtlas.GetUVs(newPos);
                }
                else
                {
                    data.Value.UV = UVAtlas.GetUVs(pos);
                }
               
            }
        }

        private int CalculateBlocksNeeded()
        {
            int count = 0;

            foreach (var tex in Textures)
            {
                count += tex.Value.Size.X * tex.Value.Size.Y;
            }

            return count;
        }

        private Vector2Byte CalculateAtlasSize(int blockSizePixels, int blocksNeeded)
        {
          
            int sideBlocks = (int)MathHelper.Ceiling(MathHelper.Sqrt(blocksNeeded));
            int sidePixels = sideBlocks * blockSizePixels;

            int numberPowerOf2 = 1;

            while(sidePixels > numberPowerOf2)
            {
                numberPowerOf2 = numberPowerOf2 * 2;
            }

            byte sideBlocksAtlas = (byte)(numberPowerOf2 / blockSizePixels);
            Debug.Log($"Block size: {blockSizePixels}, blocksNeeded: {blocksNeeded}" );
            Debug.Log($"sideBlocks: {sideBlocks}, sidePixels: {sidePixels}");
            Debug.Log($"Texture size needed: {numberPowerOf2}, {numberPowerOf2} " +
                $"({sideBlocksAtlas},{sideBlocksAtlas})");


            return new Vector2Byte(sideBlocksAtlas, sideBlocksAtlas);
            
        }

      
        private TextureData[] CollectAllTextures(string path)
        {
          
            List<TextureData> data = new List<TextureData>();

            if (!Directory.Exists(path)) {

                Debug.Error($"Directory was not found: " + path);
                return new TextureData[0];
                //return (new Texture2D[0], new string[0]);
            }

            string[] files = Directory.GetFiles(path);

            foreach(string file in files)
            {
              
                TextureData textureData = new TextureData();
                textureData.Texture = new Texture2D(file, true, false);
                textureData.Name = Path.GetFileNameWithoutExtension(file);

                data.Add(textureData);
            }

            
            return data.ToArray();

        }
        private void LoadTexture()
        {

        }

        public void Dispose()
        {
            foreach(var t in Textures)
            {
                t.Key.Dispose();    
            }
        }

        private class TextureData
        {
            public string Name { get; set; }
            public Texture2D Texture { get; set; }
        }
        private class AtlasTextureInfo
        {
            public string Name { get; set; }
            public Vector2Byte Size {  get; set; }
            public Vector2Byte Position { get; set; }
            public Vector2[] UV { get; set; }
            public Texture2D Texture { get; set; }


            public override string ToString()
            {
                return $"Name:{Name}, Size:{Size}, Pos:{Position}";
            }
        }
    }
}
