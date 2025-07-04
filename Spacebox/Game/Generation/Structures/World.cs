﻿using Engine;
using Engine.GUI;
using Engine.Physics;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;


/*
  positionWorld
  positionEntity
  positionChunk
 */

namespace Spacebox.Game.Generation
{
    public interface ISaveLoad
    {
        void Save();
        void Load();
    }
    public class WorldSaveLoader
    {
        private World world;
        public WorldSaveLoader(World world) { this.world = world; }

    }
    public class World : IDisposable, ISpaceStructure
    {
        public static World Instance;

        public const int SizeSectors = 4;
        public Astronaut Player { get; private set; }

        public static WorldLoader.LoadedWorld Data { get; private set; }
        public static DropEffectManager DropEffectManager;
        public static BlockDestructionManager DestructionManager;
        public static int Seed { get; private set; }
        public static Sector? CurrentSector { get; private set; }

        private readonly Octree<Sector> worldOctree;


        public World(Astronaut player)
        {
            Instance = this;
            Player = player;

            worldOctree = new Octree<Sector>(
                Sector.SizeBlocks * SizeSectors,
                Vector3.Zero, Sector.SizeBlocks, 1.0f);

            Load();

            DropEffectManager = new DropEffectManager(player);
            DestructionManager = new BlockDestructionManager(player);

            player.OnMoved += OnPlayerMoved;

            Overlay.AddElement(new WorldOverlayElement(this));

        }

        public void Save()
        {
            Player.Save();
            WorldSaveLoad.SaveWorld(Data.WorldFolderPath);
            WorldInfoSaver.Save(Data.Info);

            var screenSize = Window.Instance.ClientSize;
            string path = Path.Combine(Data.WorldFolderPath, "preview.jpg");

            FramebufferCapture.SaveWorldPreview(screenSize, path);

        }

        public void Load()
        {
            Vector3i initialSectorIndex = GetSectorIndex(Player.Position);

            CurrentSector = LoadSector(initialSectorIndex);
            CurrentSector.SpawnPlayerNearAsteroid(Player, new Random(Seed));
            if (CurrentSector == null) Debug.Error("No current sector");
        }

        public static void LoadWorldInfo(string worldName)
        {
            Data = WorldLoader.LoadWorldByName(worldName);
            Seed = int.Parse(Data.Info.Seed);
            if (Data == null)
            {
                Debug.Log("Data not found!");
                //Random = new Random();
            }
            else
            {
                //Random = new Random(Seed);
            }

        }

        private void OnPlayerMoved(Astronaut player)
        {
            if (CurrentSector == null) return;
            Vector3i sectorToCheck = GetSectorIndex(Player.Position);

            if (CurrentSector.PositionIndex == sectorToCheck) return;

            UnloadSector();

            CurrentSector = LoadSector(sectorToCheck);
        }

        public void Update()
        {
            DropEffectManager.Update();
            DestructionManager.Update();
            worldOctree.DrawDebug();

            CurrentSector?.Update();


            if (Input.IsKeyDown(Keys.KeyPad8))
            {

                if (CurrentSector.TryGetNearestEntity(Player.Position, out var ent))
                {
                    if (ent.IsPositionInChunk(Player.Position, out var chunk))
                    {
                        //chunk.ClearChunk();
                        ent.RemoveChunk(chunk);
                        // chunk.GenerateMesh();
                    }
                }

            }

        }

        public void Render(BlockMaterial material)
        {

            CurrentSector?.Render(material);


            DropEffectManager.Render();
            DestructionManager.Render();

        }

        private Sector LoadSector(Vector3i sectorIndex)
        {
            Sector newSector = null;

            Vector3 sectorPosition = GetSectorPosition(sectorIndex);
            newSector = new Sector(sectorPosition, sectorIndex, this);
            CurrentSector = newSector;

            Vector3 sectorCenter = sectorPosition + new Vector3(Sector.SizeBlocksHalf);
            Vector3 sectorSize = new Vector3(Sector.SizeBlocks, Sector.SizeBlocks, Sector.SizeBlocks);

            BoundingBox sectorBounds = new BoundingBox(sectorCenter, sectorSize);

            worldOctree.Add(newSector, sectorBounds);


            return newSector;
        }

        private void UnloadSector()
        {
            if (CurrentSector == null) return;

            worldOctree.Remove(CurrentSector, CurrentSector.BoundingBox);
            CurrentSector = null;
        }

        public Vector3i GetSectorIndex(Vector3 position)
        {
            int x = (int)Math.Floor(position.X / Sector.SizeBlocks);
            int y = (int)Math.Floor(position.Y / Sector.SizeBlocks);
            int z = (int)Math.Floor(position.Z / Sector.SizeBlocks);
            return new Vector3i(x, y, z);
        }

        private Vector3 GetSectorPosition(Vector3i index)
        {
            return new Vector3(
                index.X * Sector.SizeBlocks,
                index.Y * Sector.SizeBlocks,
                index.Z * Sector.SizeBlocks
            );
        }

        public bool IsColliding(Vector3 pos, BoundingVolume volume, out CollideInfo collideInfo)
        {
            if (CurrentSector == null)
            {
                collideInfo = new CollideInfo();
                return false;

            }

            return CurrentSector.IsColliding(pos, volume, out collideInfo);
        }

        public void Dispose()
        {
            Data = null;
            DropEffectManager.Dispose();
            DropEffectManager = null;
            DestructionManager.Dispose();
            DestructionManager = null;
            CurrentSector?.Dispose();
            CurrentSector = null;
        }
    }
}