using Engine;
using Engine.Utils;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Resource;

public class GenerationLoader
{


    public static Generator Load(string modPath, string defaultModPath)
    {
        string generationPath = Path.Combine(modPath, "Generation");
        string defaultGenerationPath = Path.Combine(defaultModPath, "Generation");

        Generator generator = new Generator();

        Biome.Reset();
        AsteroidData.Reset();

        LoadAsteroids(generationPath, defaultGenerationPath, generator);
        //LoadStructures(generationPath, defaultGenerationPath);
        LoadBiomes(generationPath, defaultGenerationPath, generator);
        LoadGenerator(generationPath, defaultGenerationPath, generator);

        if (generator != null)
        {

            Debug.Success("[GenerationLoader] Generation data loaded successfully");
        }
        else
        {
            Debug.Error("[GenerationLoader] Failed to load generator configuration");
        }

        return generator;
    }

    private static void LoadGenerator(string modPath, string defaultModPath, Generator generator)
    {
        var path = GameSetLoader.GetFilePath(modPath, defaultModPath, "generator.json");
        if (path == null)
        {
            Debug.Error("[GenerationLoader] generator.json not found");
            return;
        }

        try
        {
            GeneratorJSON generatorJson = JsonFixer.LoadJsonSafe<GeneratorJSON>(path);
            if (generatorJson == null)
            {
                Debug.Error("[GenerationLoader] Failed to parse generator.json");
                return;
            }

            generator.Version = generatorJson.Version;
            generator.NoiseModifier = generatorJson.NoiseModifier;
            generator.MinAsteroidsInSector = generatorJson.MinAsteroidsInSector;
            generator.MaxAsteroidsInSector = generatorJson.MaxAsteroidsInSector;
            generator.Biomes = new Biome[generatorJson.Biomes.Length];
            generator.RejectionSamples = generatorJson.RejectionSamples;
            generator.MinDistanceBetweenAsteroids  = generatorJson.MinDistanceBetweenAsteroids;

            for (int i = 0; i < generatorJson.Biomes.Length; i++)
            {
                string biomeId = generatorJson.Biomes[i];
                if (generator.loadedBiomes.TryGetValue(biomeId, out Biome biome))
                {
                    generator.Biomes[i] = biome;
                    Debug.Log($"[GenerationLoader] Linked biome: {biomeId}");
                }
                else
                {
                    Debug.Error($"[GenerationLoader] Biome not found: {biomeId}");
                }
            }

            Debug.Success($"[GenerationLoader] Loaded generator v{generator.Version} with {generator.Biomes.Length} biomes");
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error loading generator: {ex.Message}");
        }
    }

    private static void LoadAsteroids(string modPath, string defaultModPath, Generator generator)
    {
        var path = GameSetLoader.GetFilePath(modPath, defaultModPath, "asteroids.json");
        if (path == null)
        {
            Debug.Error("[GenerationLoader] asteroids.json not found");
            return;
        }

        try
        {
            List<AsteroidJSON> asteroidsJson = JsonFixer.LoadJsonSafe<List<AsteroidJSON>>(path);
            if (asteroidsJson == null)
            {
                Debug.Error("[GenerationLoader] Failed to parse asteroids.json");
                return;
            }

            foreach (var asteroidJson in asteroidsJson)
            {
                if (string.IsNullOrEmpty(asteroidJson.Id))
                {
                    Debug.Error("[GenerationLoader] Asteroid missing ID, skipping");
                    continue;
                }

                if (generator.loadedAsteroids.ContainsKey(asteroidJson.Id))
                {
                    Debug.Error($"[GenerationLoader] Duplicate asteroid ID: {asteroidJson.Id}");
                    continue;
                }

                AsteroidData asteroid = ConvertAsteroid(asteroidJson);
                if (asteroid != null)
                {
                    generator.loadedAsteroids[asteroidJson.Id] = asteroid;
                    Debug.Log($"[GenerationLoader] Loaded asteroid: {asteroidJson.Id}");
                }
            }

            Debug.Success($"[GenerationLoader] Loaded {generator.loadedAsteroids.Count} asteroids");
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error loading asteroids: {ex.Message}");
        }
    }

    private static void LoadStructures(string modPath, string defaultModPath)
    {
        var path = GameSetLoader.GetFilePath(modPath, defaultModPath, "structures.json");
        if (path == null)
        {
            Debug.Log("[GenerationLoader] structures.json not found (optional)");
            return;
        }

        try
        {

            Debug.Log("[GenerationLoader] Structure loading not yet implemented");
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error loading structures: {ex.Message}");
        }
    }

    private static void LoadBiomes(string modPath, string defaultModPath, Generator generator)
    {
        var path = GameSetLoader.GetFilePath(modPath, defaultModPath, "biomes.json");
        if (path == null)
        {
            Debug.Error("[GenerationLoader] biomes.json not found");
            return;
        }

        try
        {
            List<BiomeJSON> biomesJson = JsonFixer.LoadJsonSafe<List<BiomeJSON>>(path);
            if (biomesJson == null)
            {
                Debug.Error("[GenerationLoader] Failed to parse biomes.json");
                return;
            }

            foreach (var biomeJson in biomesJson)
            {
                if (string.IsNullOrEmpty(biomeJson.Id))
                {
                    Debug.Error("[GenerationLoader] Biome missing ID, skipping");
                    continue;
                }

                if (generator.loadedBiomes.ContainsKey(biomeJson.Id))
                {
                    Debug.Error($"[GenerationLoader] Duplicate biome ID: {biomeJson.Id}");
                    continue;
                }

                Biome biome = ConvertBiome(biomeJson, generator);
                if (biome != null)
                {
                    generator.loadedBiomes[biomeJson.Id] = biome;
                    Debug.Log($"[GenerationLoader] Loaded biome: {biomeJson.Id}");
                }
            }

            Debug.Success($"[GenerationLoader] Loaded {generator.loadedBiomes.Count} biomes");
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error loading biomes: {ex.Message}");
        }
    }

    private static AsteroidData ConvertAsteroid(AsteroidJSON json)
    {
        try
        {
            var asteroid = new AsteroidData(json.Id, json.Name)
            {
                SizeInChunks = json.SizeInChunks,
                DensityThreshold = json.DensityThreshold,
                NoiseOctaves = json.NoiseOctaves,
                NoiseScale = json.NoiseScale,
                HasCavities = json.HasCavities,
                HasOreDeposits = json.HasOreDeposits,
                UsePerlinWorms = json.UsePerlinWorms,
                WormSettings = json.WormSettings,
                Layers = new AsteroidLayer[json.Layers.Length]
            };

            for (int i = 0; i < json.Layers.Length; i++)
            {
                asteroid.Layers[i] = ConvertLayer(json.Layers[i]);
            }

            return asteroid;
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error converting asteroid {json.Id}: {ex.Message}");
            return null;
        }
    }

    private static AsteroidLayer ConvertLayer(AsteroidLayerJSON json)
    {
        var layer = new AsteroidLayer
        {
            Name = json.Name,
            Ratio = json.Ratio,
            Veins = new AsteroidVein[json.Veins.Length]
        };

        string fullBlockId = GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, json.FillBlockID);
        var blockData = GameAssets.BlocksStr.ContainsKey(fullBlockId)
            ? GameAssets.BlocksStr[fullBlockId]
            : null;

        if (blockData != null)
        {
            layer.FillBlockID = blockData.Id;
        }
        else
        {
            Debug.Error($"[GenerationLoader] Block not found for layer {json.Name}: {json.FillBlockID}");
            layer.FillBlockID = 0;
        }

        for (int i = 0; i < json.Veins.Length; i++)
        {
            layer.Veins[i] = ConvertVein(json.Veins[i]);
        }

        return layer;
    }

    private static AsteroidVein ConvertVein(AsteroidVeinJSON json)
    {
        var vein = new AsteroidVein
        {
            SpawnChance = json.SpawnChance,
            MinVeinSize = json.MinVeinSize,
            MaxVeinSize = json.MaxVeinSize,
            CanSpawnNearVoid = json.CanSpawnNearVoid
        };

        string fullBlockId = GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, json.BlockId);
        var blockData = GameAssets.BlocksStr.ContainsKey(fullBlockId)
            ? GameAssets.BlocksStr[fullBlockId]
            : null;

        if (blockData != null)
        {
            vein.BlockId = blockData.Id;
        }
        else
        {
            Debug.Error($"[GenerationLoader] Vein block not found: {json.BlockId}");
            vein.BlockId = 0;
        }

        return vein;
    }

    private static Biome ConvertBiome(BiomeJSON json, Generator generator)
    {
        try
        {
            var biome = new Biome(json)
            {
                Asteroids = new AsteroidData[json.AsteroidIds.Length]
            };


            for (int i = 0; i < json.AsteroidIds.Length; i++)
            {
                string asteroidId = json.AsteroidIds[i];
                if (generator.loadedAsteroids.TryGetValue(asteroidId, out AsteroidData asteroid))
                {
                    biome.Asteroids[i] = asteroid;
                }
                else
                {
                    Debug.Error($"[GenerationLoader] Asteroid not found for biome {json.Id}: {asteroidId}");
                }
            }

            biome.Asteroids = biome.Asteroids.Where(a => a != null).ToArray();

            if (biome.Asteroids.Length == 0)
            {
                Debug.Error($"[GenerationLoader] Biome {json.Id} has no valid asteroids");
            }

            return biome;
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error converting biome {json.Id}: {ex.Message}");
            return null;
        }
    }



}