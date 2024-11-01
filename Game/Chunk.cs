using Spacebox.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Entities;
using System.Diagnostics;

namespace Spacebox.Game
{
    public class Chunk
    {
        public const sbyte Size = 16;

        public Vector3 Position { get; private set; }
        public Block[,,] Blocks { get; private set; }
        public bool ShowChunkBounds { get; set; } = true;
        public bool MeasureGenerationTime { get; set; } = true;

        private Mesh _mesh;

        public Chunk(Vector3 position)
        {
            Position = position;
            Blocks = new Block[Size, Size, Size];
        
            // GenerateBlocks(); 
            GenerateSphereBlocks(); 
            GenerateMesh();
        }


        private void GenerateSphereBlocks()
        {
            Vector3 center = new Vector3(Size / 2f, Size / 2f, Size / 2f);
            float radius = Size / 2f;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 position = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                        float distance = Vector3.Distance(position, center);

                        if (distance <= radius)
                        {
                            
                            Random random = new Random();

                            var r = random.Next(0, 10);

                            Vector2 textureCoords = Vector2.Zero;

                            if (r < 8)
                            {
                                if(distance < radius/2f)
                                Blocks[x, y, z] = GameBlocks.CreateFromId(2);
                                else
                                    Blocks[x, y, z] = GameBlocks.CreateFromId(1);
                            }
                            if (r == 8)
                            {
                                if (distance < radius / 2f)
                                    Blocks[x, y, z] = GameBlocks.CreateFromId(4);
                                else
                                    Blocks[x, y, z] = GameBlocks.CreateFromId(3);
                            }
                            if (r == 9)
                            {
                                if (distance < radius / 2f)
                                    Blocks[x, y, z] = GameBlocks.CreateFromId(2);
                                else
                                    Blocks[x, y, z] = GameBlocks.CreateFromId(5);
                            }

                           

                            //Vector3 color = new Vector3(1f , 1f , 1f ); // Белый цвет
                          
                           

                        }
                        else
                        {
                            // Вне сферы
                            Blocks[x, y, z] = GameBlocks.CreateFromId(0);
                        }
                    }
        }

        public void GenerateMesh()
        {
            Stopwatch stopwatch = null;
            if (MeasureGenerationTime)
            {
                stopwatch = Stopwatch.StartNew();
            }

            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint index = 0;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Block block = Blocks[x, y, z];
                        if (block.Type == BlockType.Air)
                            continue;

                        if (IsTransparentBlock(x - 1, y, z))
                            AddFace(vertices, indices, x, y, z, Face.Left, block, ref index);
                        if (IsTransparentBlock(x + 1, y, z))
                            AddFace(vertices, indices, x, y, z, Face.Right, block, ref index);
                        if (IsTransparentBlock(x, y - 1, z))
                            AddFace(vertices, indices, x, y, z, Face.Bottom, block, ref index);
                        if (IsTransparentBlock(x, y + 1, z))
                            AddFace(vertices, indices, x, y, z, Face.Top, block, ref index);
                        if (IsTransparentBlock(x, y, z - 1))
                            AddFace(vertices, indices, x, y, z, Face.Back, block, ref index);
                        if (IsTransparentBlock(x, y, z + 1))
                            AddFace(vertices, indices, x, y, z, Face.Front, block, ref index);
                    }

            _mesh = new Mesh(vertices.ToArray(), indices.ToArray());

            if (MeasureGenerationTime && stopwatch != null)
            {
                stopwatch.Stop();
                Console.WriteLine($"Chunk mesh generation time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }


        private bool IsTransparentBlock(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            if(Blocks[x, y, z].IsAir()) return true;

            return Blocks[x, y, z].IsTransparent;
        }

        private void AddFace(List<float> vertices, List<uint> indices, int x, int y, int z, Face face, Block block, ref uint index)
        {
            Vector3[] faceVertices = CubeMeshData.GetFaceVertices(face);
            Vector2[] faceUVs = UVAtlas.GetUVs((int)block.TextureCoords.X, (int)block.TextureCoords.Y);

            // Получаем уровень света и цвет освещения блока
            float currentLightLevel = block.LightLevel / 15f;
            Vector3 currentLightColor = block.LightColor;

            // Получаем соседний блок в направлении грани
            Vector3i neighborPos = new Vector3i(x, y, z) + GetFaceNormal(face);
            float neighborLightLevel = 0f;
            Vector3 neighborLightColor = Vector3.Zero;

            if (IsInRange(neighborPos.X, neighborPos.Y, neighborPos.Z))
            {
                Block neighborBlock = Blocks[neighborPos.X, neighborPos.Y, neighborPos.Z];
                neighborLightLevel = neighborBlock.LightLevel / 15f;
                neighborLightColor = neighborBlock.LightColor;
            }

            // Средний уровень света и цвет между блоками
            float averageLightLevel = (currentLightLevel + neighborLightLevel) / 2f;
            Vector3 averageLightColor = (currentLightColor * currentLightLevel + neighborLightColor * neighborLightLevel) / (currentLightLevel + neighborLightLevel + 0.001f);

            // Корректируем значение ambient-освещения
            Vector3 ambient = new Vector3(0.2f, 0.2f, 0.2f); // Настройте по своему усмотрению

            // Итоговый цвет вершины
            Vector3 vertexColor = block.Color * (averageLightColor + ambient);

            // Нормализуем цвет, чтобы значения не превышали 1.0
            vertexColor = Vector3.Clamp(vertexColor, Vector3.Zero, Vector3.One);

            for (int i = 0; i < faceVertices.Length; i++)
            {
                Vector3 vertex = faceVertices[i];
                vertices.Add(vertex.X + x);
                vertices.Add(vertex.Y + y);
                vertices.Add(vertex.Z + z);

                vertices.Add(faceUVs[i].X);
                vertices.Add(faceUVs[i].Y);

                vertices.Add(vertexColor.X);
                vertices.Add(vertexColor.Y);
                vertices.Add(vertexColor.Z);
            }

            uint[] faceIndices = { 0, 1, 2, 2, 3, 0 };
            for (int i = 0; i < faceIndices.Length; i++)
            {
                indices.Add(index + faceIndices[i]);
            }

            index += 4;
        }



        private Vector3i GetFaceNormal(Face face)
        {
            switch (face)
            {
                case Face.Left: return new Vector3i(-1, 0, 0);
                case Face.Right: return new Vector3i(1, 0, 0);
                case Face.Bottom: return new Vector3i(0, -1, 0);
                case Face.Top: return new Vector3i(0, 1, 0);
                case Face.Back: return new Vector3i(0, 0, -1);
                case Face.Front: return new Vector3i(0, 0, 1);
                default: return Vector3i.Zero;
            }
        }



        public void Shift(Vector3 shift)
        {

            Position -= shift;
        }

        public void Draw(Shader shader)
        {
            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);

            _mesh.Draw(shader);

            // Отрисовка границ чанка
            if (ShowChunkBounds && Spacebox.Common.Debug.ShowDebug)
            {
                Vector3 chunkMin = Position;
                Vector3 chunkMax = Position + new Vector3(Size);

                BoundingBox chunkBounds = BoundingBox.CreateFromMinMax(chunkMin, chunkMax);
                Spacebox.Common.Debug.DrawBoundingBox(chunkBounds, new Color4(0.5f, 0f, 0.5f, 1f)); // Фиолетовый цвет
            }
        }


        public void SetBlock(int x, int y, int z, Block block)
        {
            if (IsInRange(x, y, z))
            {
                Blocks[x, y, z] = block;

                // Если блок является источником света, задаем уровень света и цвет
                if (block.LightLevel > 0f)
                {
                    Blocks[x, y, z].LightLevel = block.LightLevel;
                    Blocks[x, y, z].LightColor = block.LightColor;
                }

                // После установки блока обновляем освещение
                PropagateLight();
                GenerateMesh();
            }
        }


        public void RemoveBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
            {
                Blocks[x, y, z] = new Block(BlockType.Air, new Vector2(0, 0));

                // После удаления блока обновляем освещение
                PropagateLight();
                GenerateMesh();
            }
        }

        public Block GetBlock(Vector3 pos)
        {
            return GetBlock((int)pos.X, (int)pos.Y, (int)pos.Z);
        }
        public Block GetBlock(int x, int y, int z)
        {
            if (IsInRange(x, y, z))
                return Blocks[x, y, z];
            else
                return new Block(BlockType.Air, new Vector2(0, 0));
        }

      

        private bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;
        }


        public bool Raycast(Ray ray, out Vector3 hitPosition, out Vector3i hitBlockPosition, out Vector3 hitNormal, float maxDistance = 100f)
        {
            hitPosition = Vector3.Zero;
            hitBlockPosition = new Vector3i(-1, -1, -1);
            hitNormal = Vector3.Zero;

            Vector3 rayOrigin = ray.Origin - Position; // Приводим начало луча к локальным координатам чанка
            Vector3 rayDirection = ray.Direction;

            // Начальные координаты блока
            int x = (int)Math.Floor(rayOrigin.X);
            int y = (int)Math.Floor(rayOrigin.Y);
            int z = (int)Math.Floor(rayOrigin.Z);

            // Дельты для движения по вокселям
            float deltaDistX = rayDirection.X == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.X);
            float deltaDistY = rayDirection.Y == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Y);
            float deltaDistZ = rayDirection.Z == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Z);

            int stepX = rayDirection.X < 0 ? -1 : 1;
            int stepY = rayDirection.Y < 0 ? -1 : 1;
            int stepZ = rayDirection.Z < 0 ? -1 : 1;

            float sideDistX = rayDirection.X == 0 ? float.MaxValue : (rayDirection.X < 0 ? (rayOrigin.X - x) : (x + 1.0f - rayOrigin.X)) * deltaDistX;
            float sideDistY = rayDirection.Y == 0 ? float.MaxValue : (rayDirection.Y < 0 ? (rayOrigin.Y - y) : (y + 1.0f - rayOrigin.Y)) * deltaDistY;
            float sideDistZ = rayDirection.Z == 0 ? float.MaxValue : (rayDirection.Z < 0 ? (rayOrigin.Z - z) : (z + 1.0f - rayOrigin.Z)) * deltaDistZ;

            float distanceTraveled = 0f;
            int side = -1; // 0 - X, 1 - Y, 2 - Z

            while (distanceTraveled < maxDistance)
            {
                // Проверяем, находится ли блок внутри границ чанка
                if (x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size)
                {
                    Block block = Blocks[x, y, z];
                    if (block.Type != BlockType.Air)
                    {
                        // Найден блок, возвращаем позицию
                        hitBlockPosition = new Vector3i(x, y, z);
                        hitPosition = ray.Origin + ray.Direction * distanceTraveled;

                        // Определяем нормаль столкновения на основе оси, по которой двигались
                        switch (side)
                        {
                            case 0:
                                hitNormal = new Vector3(-stepX, 0, 0);
                                break;
                            case 1:
                                hitNormal = new Vector3(0, -stepY, 0);
                                break;
                            case 2:
                                hitNormal = new Vector3(0, 0, -stepZ);
                                break;
                        }

                        return true;
                    }
                }
                else
                {
                    // Вышли за пределы чанка
                    return false;
                }

                // Определяем, по какой оси двигаться
                if (sideDistX < sideDistY)
                {
                    if (sideDistX < sideDistZ)
                    {
                        x += stepX;
                        distanceTraveled = sideDistX;
                        sideDistX += deltaDistX;
                        side = 0; // X ось
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2; // Z ось
                    }
                }
                else
                {
                    if (sideDistY < sideDistZ)
                    {
                        y += stepY;
                        distanceTraveled = sideDistY;
                        sideDistY += deltaDistY;
                        side = 1; // Y ось
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2; // Z ось
                    }
                }
            }

            return false;
        }

       

       /* public Dictionary<byte, Vector2> blocks = new Dictionary<byte, Vector2>
{
    {0, new Vector2(9, 0)}, // Новый блок света
    {1, new Vector2(1,2) }, {2, new Vector2(2,2)},
    {3, new Vector2(0,1)},{4, new Vector2(0,2)},
    {5, new Vector2(4,2)}, {6, new Vector2(4,0)},
    {7, new Vector2(5,0)},{8, new Vector2(6,0)},
    {9, new Vector2(8,0)}
};*/


        public void PropagateLight()
        {

            

            // Сброс уровня света и цвета всех блоков, кроме источников
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Block block = Blocks[x, y, z];
                        if (block.LightLevel < 15f)
                        {
                            block.LightLevel = 0f;
                            block.LightColor = Vector3.Zero;
                        }
                    }

            Queue<Vector3i> lightQueue = new Queue<Vector3i>();

            // Находим все источники света и добавляем их в очередь
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        if (Blocks[x, y, z].LightLevel > 0f)
                            lightQueue.Enqueue(new Vector3i(x, y, z));
                    }

            // BFS для распространения света
            while (lightQueue.Count > 0)
            {
                Vector3i pos = lightQueue.Dequeue();
                Block currentBlock = Blocks[pos.X, pos.Y, pos.Z];
                float lightLevel = currentBlock.LightLevel;

                if (lightLevel <= 0.1f)
                    continue;

                foreach (Vector3i offset in GetAdjacentOffsets())
                {
                    int nx = pos.X + offset.X;
                    int ny = pos.Y + offset.Y;
                    int nz = pos.Z + offset.Z;

                    if (!IsInRange(nx, ny, nz))
                        continue;

                    Block neighborBlock = Blocks[nx, ny, nz];

                    // Проверяем, можно ли свету пройти через этот блок
                    if (neighborBlock.Type == BlockType.Air || neighborBlock.IsTransparent)
                    {
                        float attenuation = 0.8f; // Коэффициент затухания света
                        float newLightLevel = lightLevel * attenuation;

                        Vector3 newLightColor = currentBlock.LightColor * attenuation;

                        // Если новый уровень света больше, чем текущий у соседнего блока
                        if (newLightLevel > neighborBlock.LightLevel)
                        {
                            neighborBlock.LightLevel = newLightLevel;
                            neighborBlock.LightColor = newLightColor;
                            Blocks[nx, ny, nz] = neighborBlock; // Обновляем блок
                            lightQueue.Enqueue(new Vector3i(nx, ny, nz));
                        }
                        else if (MathF.Abs(newLightLevel - neighborBlock.LightLevel) < 0.01f)
                        {
                            // Если уровни света примерно равны, смешиваем цвета
                            neighborBlock.LightColor += newLightColor;
                            neighborBlock.LightColor /= 2f;
                            Blocks[nx, ny, nz] = neighborBlock; // Обновляем блок
                        }
                    }
                }
            }
        }


        private List<Vector3i> GetAdjacentOffsets()
        {
            return new List<Vector3i>
    {
        new Vector3i(1, 0, 0),
        new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0),
        new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1),
        new Vector3i(0, 0, -1),
    };
        }


        public void Test(Astronaut player)
        {


            


            // Получаем направление взгляда игрока
            Vector3 rayOrigin = player.Position;
            Vector3 rayDirection = player.Front; // Направление взгляда игрока

            float maxDistance = 100f;
            Ray ray = new Ray(rayOrigin, rayDirection, maxDistance);

            Vector3 hitPosition;
            Vector3i hitBlockPosition;
            Vector3 hitNormal;

            if (Raycast(ray, out hitPosition, out hitBlockPosition, out hitNormal))
            {
                // Луч пересёк блок в чанке
                Vector3 worldBlockPosition = hitBlockPosition + Position;

                // Рисуем куб на месте пересечённого блока
                Spacebox.Common.Debug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f) + hitNormal,
                    Vector3.One * 1.01f), Color4.White);

                // Удаление блока при нажатии F
                if (Input.IsMouseButtonDown(MouseButton.Left))
                {
                    RemoveBlock(hitBlockPosition.X, hitBlockPosition.Y, hitBlockPosition.Z);
                    GenerateMesh();
                }

                if(Input.IsMouseButtonDown(MouseButton.Middle))
                {
                    Block b = GetBlock(hitBlockPosition);
                    Console.WriteLine(b + " Pos: " + hitBlockPosition);
                }

                // Добавление блока при нажатии G
                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    // Вычисляем позицию для размещения нового блока
                    Vector3i placeBlockPosition = hitBlockPosition + new Vector3i((int)hitNormal.X, (int)hitNormal.Y, (int)hitNormal.Z);

                    // Проверяем, что позиция внутри чанка и там нет блока
                    if (IsInRange(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z))
                    {
                        if (Blocks[placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z].Type == BlockType.Air)
                        {

                            Block newBlock = GameBlocks.CreateFromId(player.CurrentBlockId);

                            // Если выбран блок-источник света
                            if (player.CurrentBlockId == 9)
                            {
                                newBlock.LightLevel = 15f; // Максимальный уровень света
                                newBlock.LightColor = new Vector3(1.0f, 1.0f, 1.0f); // Белый свет
                            }
                            else if (player.CurrentBlockId == 0)
                            {
                                newBlock.LightLevel = 15f;
                                newBlock.LightColor = new Vector3(0f, 0.0f, 1.0f); // Красный свет
                            }

                            SetBlock(placeBlockPosition.X, placeBlockPosition.Y, placeBlockPosition.Z, newBlock);
                            GenerateMesh();
                        }
                    }
                }
            }
            else
            {
                // Луч не пересёк ни одного блока
                // Вычисляем точку на расстоянии 5 единиц вдоль луча
                float placeDistance = 5f;
                Vector3 placePosition = rayOrigin + rayDirection * placeDistance;
                Vector3 localPosition = placePosition - Position;

                // Вычисляем индексы блока
                int x = (int)Math.Floor(localPosition.X);
                int y = (int)Math.Floor(localPosition.Y);
                int z = (int)Math.Floor(localPosition.Z);

                // Рисуем рамку блока в этой позиции
                Vector3 worldBlockPosition = new Vector3(x, y, z) + Position;
                Spacebox.Common.Debug.DrawBoundingBox(new BoundingBox(worldBlockPosition + new Vector3(0.5f),
                    Vector3.One * 1.01f), Color4.Gray);

                // Добавление блока при нажатии G
                if (Input.IsMouseButtonDown(MouseButton.Right))
                {
                    // Проверяем, что позиция внутри чанка и там нет блока
                    if (IsInRange(x, y, z))
                    {
                        if (Blocks[x, y, z].Type == BlockType.Air)
                        {

                            Block newBlock = GameBlocks.CreateFromId(player.CurrentBlockId);

                            // Если выбран блок-источник света
                            if (player.CurrentBlockId == 9)
                            {
                                newBlock.LightLevel = 15f; // Максимальный уровень света
                                newBlock.LightColor = new Vector3(1.0f, 1.0f, 1.0f); // Белый свет
                            }
                            else if (player.CurrentBlockId == 0)
                            {
                                newBlock.LightLevel = 15f;
                                newBlock.LightColor = new Vector3(0, 0.0f, 1f); // Красный свет
                            }

                            SetBlock(x, y, z, newBlock);
                            GenerateMesh();
                        }
                    }
                }
            }
        }



    }
}
