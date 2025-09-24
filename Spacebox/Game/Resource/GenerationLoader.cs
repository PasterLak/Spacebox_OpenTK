using Engine;
using Engine.Utils;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Resource;

public class GenerationLoader
{


    public static WorldGenerator Load(string modPath, string defaultModPath)
    {
        string generationPath = Path.Combine(modPath, "Generation");
        string defaultGenerationPath = Path.Combine(defaultModPath, "Generation");

        WorldGenerator generator = new WorldGenerator();

        Biome.Reset();
        AsteroidData.Reset();

        LoadAsteroids(generationPath, defaultGenerationPath, generator);
        //LoadStructures(generationPath, defaultGenerationPath);
        LoadBiomes(generationPath, defaultGenerationPath, generator);
        LoadGenerator(generationPath, defaultGenerationPath, generator);

        if (generator != null)
        {

           // Debug.Success("[GenerationLoader] Generation data loaded successfully");
        }
        else
        {
            Debug.Error("[GenerationLoader] Failed to load generator configuration");
        }

        generator.CalculateBiomeProbabilities();

        return generator;
    }

    private static void LoadGenerator(string modPath, string defaultModPath, WorldGenerator generator)
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
            generator.BiomesMapNoiseFrequency = generatorJson.BiomesMapNoiseFrequency;

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

            //Debug.Success($"[GenerationLoader] Loaded generator v{generator.Version} with {generator.Biomes.Length} biomes");
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error loading generator: {ex.Message}");
        }
    }

    private static void LoadAsteroids(string modPath, string defaultModPath, WorldGenerator generator)
    {
        string asteroidPath = Path.Combine(modPath, "Asteroids");
        string defaultAsteroidPath = Path.Combine(defaultModPath, "Asteroids");

        if (Directory.Exists(modPath))
        {
            Debug.Log($"[GenerationLoader] Asteroid directory not found: {modPath}");
            LoadAsteroidsFromDirectory(asteroidPath, generator);
        } else 
        LoadAsteroidsFromDirectory(defaultAsteroidPath, generator);

        //Debug.Success($"[GenerationLoader] Loaded {generator.loadedAsteroids.Count} asteroids");
    }

    private static void LoadAsteroidsFromDirectory(string directoryPath, WorldGenerator generator)
    {
        if (!Directory.Exists(directoryPath))
        {
            Debug.Log($"[GenerationLoader] Asteroid directory not found: {directoryPath}");
            return;
        }

        try
        {
            var jsonFiles = Directory.GetFiles(directoryPath, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    AsteroidJSON asteroidJson = JsonFixer.LoadJsonSafe<AsteroidJSON>(filePath);
                    if (asteroidJson == null)
                    {
                        Debug.Error($"[GenerationLoader] Failed to parse asteroid file: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(asteroidJson.Id))
                    {
                        Debug.Error($"[GenerationLoader] Asteroid missing ID in file: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    if (generator.loadedAsteroids.ContainsKey(asteroidJson.Id))
                    {
                        Debug.Log($"[GenerationLoader] Asteroid {asteroidJson.Id} already loaded, skipping duplicate");
                        continue;
                    }

                    AsteroidData asteroid = ConvertAsteroid(asteroidJson);
                    if (asteroid != null)
                    {
                        generator.loadedAsteroids[asteroidJson.Id] = asteroid;
                       // Debug.Log($"[GenerationLoader] Loaded asteroid: {asteroidJson.Id} from {Path.GetFileName(filePath)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GenerationLoader] Error loading asteroid file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error reading asteroid directory {directoryPath}: {ex.Message}");
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

    private static void LoadBiomes(string modPath, string defaultModPath, WorldGenerator generator)
    {
        string biomePath = Path.Combine(modPath, "Biomes");
        string defaultBiomePath = Path.Combine(defaultModPath, "Biomes");

        LoadBiomesFromDirectory(biomePath, generator);
        LoadBiomesFromDirectory(defaultBiomePath, generator);
    }

    private static void LoadBiomesFromDirectory(string directoryPath, WorldGenerator generator)
    {
        if (!Directory.Exists(directoryPath))
        {
            Debug.Log($"[GenerationLoader] Biome directory not found: {directoryPath}");
            return;
        }

        try
        {
            var jsonFiles = Directory.GetFiles(directoryPath, "*.json");

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    BiomeJSON biomeJson = JsonFixer.LoadJsonSafe<BiomeJSON>(filePath);
                    if (biomeJson == null)
                    {
                        Debug.Error($"[GenerationLoader] Failed to parse biome file: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(biomeJson.Id))
                    {
                        Debug.Error($"[GenerationLoader] Biome missing ID in file: {Path.GetFileName(filePath)}");
                        continue;
                    }

                    if (generator.loadedBiomes.ContainsKey(biomeJson.Id))
                    {
                        Debug.Log($"[GenerationLoader] Biome {biomeJson.Id} already loaded, skipping duplicate");
                        continue;
                    }

                    if (biomeJson.MinDistanceFromCenter == 0 && biomeJson.MinDistanceFromCenter == biomeJson.MaxDistanceFromCenter)
                    {
                        biomeJson.MaxDistanceFromCenter = int.MaxValue;
                    }

                    Biome biome = ConvertBiome(biomeJson, generator);
                    if (biome != null)
                    {
                        generator.loadedBiomes[biomeJson.Id] = biome;
                        Debug.Log($"[GenerationLoader] Loaded biome: {biomeJson.Id} from {Path.GetFileName(filePath)}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Error($"[GenerationLoader] Error loading biome file {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Error($"[GenerationLoader] Error reading biome directory {directoryPath}: {ex.Message}");
        }
    }

    private static AsteroidData ConvertAsteroid(AsteroidJSON json)
    {
        try
        {
            var asteroid = new AsteroidData(json.Id, json.Name)
            {
                
                MinRadius = json.MinRadius,
                MaxRadius = json.MaxRadius,
                DensityThreshold = json.DensityThreshold,
                NoiseOctaves = json.NoiseOctaves,
                NoiseScale = json.NoiseScale,
                HasCavities = json.HasCavities,
                HasOreDeposits = json.HasOreDeposits,
                UsePerlinWorms = json.UsePerlinWorms,
                WormSettings = json.WormSettings,
                Layers = new AsteroidLayer[json.Layers.Length]
            };

            if (asteroid.UsePerlinWorms)
            {
                if (asteroid.WormSettings.MinCount > asteroid.WormSettings.MaxCount)
                {
                    var max = asteroid.WormSettings.MinCount;

                    asteroid.WormSettings.MinCount = asteroid.WormSettings.MaxCount;
                    asteroid.WormSettings.MaxCount = max;

                    Debug.Error("[GenerationLoader] asteroid.WormSettings.MinCount > asteroid.WormSettings.MaxCount");
                }
            }

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

    private static Biome ConvertBiome(BiomeJSON json, WorldGenerator generator)
    {
        try
        {
            var biome = new Biome(json);

            foreach (var asteroidChance in json.Asteroids)
            {
                string asteroidId = asteroidChance.Key;
                byte chance = asteroidChance.Value;

                if (generator.loadedAsteroids.TryGetValue(asteroidId, out AsteroidData asteroid))
                {
                    biome.AsteroidChances[asteroid] = chance;
                }
                else
                {
                    Debug.Error($"[GenerationLoader] Asteroid not found for biome {json.Id}: {asteroidId}");
                }
            }

            if (biome.AsteroidChances.Count == 0)
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