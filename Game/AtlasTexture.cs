
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

     
        private string firsttextureName = "";

        public int BlockSizePixels { get; set; }
        public int SizeBlocks { get; set; }

        public Vector2[] GetUVByName(string name)
        {
         
            if(ReadyTextures.ContainsKey(name))
            {
                return ReadyTextures[name].UV;
            }

            return ReadyTextures[firsttextureName].UV;
        }
        public Vector2Byte GetUVIndexByName(string name)
        {
         
            if (ReadyTextures.ContainsKey(name))
            {
                return ReadyTextures[name].Position;
            }


            return ReadyTextures[firsttextureName].Position;
        }
        public Texture2D CreateTexture(string path, int blockSizePixels, bool populateFromTopToBottom)
        {
            
            var textures = CollectAllTextures(path);
            BlockSizePixels = blockSizePixels;
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

            var size = CalculateAtlasSize(blockSizePixels, CalculateBlocksNeeded());
            SizeBlocks = size.X;
            Texture2D atlas = PlaceTexturesInAtlas(size, blockSizePixels, populateFromTopToBottom);
            
            //atlas.FlipY();
            atlas.UpdateTexture(true);
            
 
            CalculateUV(size.X, populateFromTopToBottom);


            foreach (var tex in Textures)
            {
                tex.Key.Dispose();
            }
            Textures = null;

            foreach(var t in ReadyTextures)
            {
                if(t.Value.Texture != null)
                {
                    t.Value.Texture.Dispose();
                    t.Value.Texture = null;
                }
               
            }
           
            return atlas;
        }

        public Texture2D CreateEmission(string path)
        {
            Texture2D output = null;

            var sidePixels = SizeBlocks * BlockSizePixels;

            TextureData[] emissionTextures = CollectAllTextures(path, true);

            Dictionary<string, TextureData> emissions = new Dictionary<string, TextureData>();

            foreach(var et in  emissionTextures)
            {
                emissions.Add(et.Name, et);
            }
          
            Color4[,] atlasPixels = new Color4[sidePixels, sidePixels];

            for (int i = 0; i < sidePixels; i++)
            {
                for (int p = 0; p < sidePixels; p++)
                {
                    atlasPixels[i, p] = new Color4(0, 0, 0, 0);
                }
            }

            foreach (var tex in ReadyTextures)
            {
                if (!emissions.ContainsKey(tex.Key)) continue;

                var pointerX = tex.Value.Position.X * BlockSizePixels;
                var pointerY = tex.Value.Position.Y * BlockSizePixels;

                var sizeX = tex.Value.Size.X * BlockSizePixels;
                var sizeY = tex.Value.Size.Y * BlockSizePixels;

                Color4[,] blockPixels = emissions[tex.Key].Texture.GetPixelData();

                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        atlasPixels[pointerX + x, pointerY + y] = blockPixels[x, y];
                    }
                }
               
            }

            output =  new Texture2D(sidePixels, sidePixels);

            output.SetPixelsData(atlasPixels);
            
            output.UpdateTexture(true);

            return output;
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
            atlas.FlipY();
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


        private void CalculateUV(int sideBlocks, bool fromTopToBottom)
        {
            foreach (var data in ReadyTextures)
            {
                var pos = data.Value.Position;
                if (fromTopToBottom)
                {
                  
                    var newPos = new Vector2Byte(pos.X, (byte)(sideBlocks - pos.Y - 1));
                    data.Value.UV = UVAtlas.GetUVs(newPos, sideBlocks);
                }
                else
                {
                  
                    data.Value.UV = UVAtlas.GetUVs(pos, sideBlocks);
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
       
            return new Vector2Byte(sideBlocksAtlas, sideBlocksAtlas);
            
        }

      
        private TextureData[] CollectAllTextures(string path, bool flipY = false)
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
                if(flipY)
                textureData.Texture.FlipY();
                textureData.Name = Path.GetFileNameWithoutExtension(file).ToLower();

                data.Add(textureData);
            }

            
            return data.ToArray();

        }

        public void Dispose()
        {
            if(Textures == null) return;

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
                return $"Name:{Name}, Size:{Size}, Pos:{Position}, UV:{UV[0]} to {UV[2]}";
            }
        }
    }
}
