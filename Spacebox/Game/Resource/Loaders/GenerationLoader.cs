using Engine;
using Engine.Utils;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource;

public class GenerationLoader
{


    public const string AsteroidsFolder = "Generation/Asteroids";
    public const string BiomesFolder = "Generation/Biomes";
    public const string GeneratorFile = "Generation/generator.json";

    public static WorldGenerator Load(GameSetFiles files)
    {
        WorldGenerator generator = new WorldGenerator();

        Biome.Reset();
        AsteroidData.Reset();

        LoadAsteroids(files.Folder(AsteroidsFolder, false), generator);
        //LoadStructures(generationPath, defaultGenerationPath);
        LoadBiomes(files.Folder(BiomesFolder, false), generator);
        LoadGenerator(files.File(GeneratorFile), generator);

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

    private static void LoadGenerator(LayeredFile file, WorldGenerator generator)
    {
        if (file == null)
        {
            Debug.Error("[GenerationLoader] generator.json not found");
            return;
        }

        try
        {
            GeneratorJSON generatorJson = GameSetFiles.Deserialize<GeneratorJSON>(file.Data);
            if (generatorJson == null)
            {
                Debug.Error("[GenerationLoader] Failed to parse generator.json");
                return;
            }

            generator.MinAsteroidsInSector = generatorJson.MinAsteroidsInSector;
            generator.MaxAsteroidsInSector = generatorJson.MaxAsteroidsInSector;
            generator.Biomes = new Biome[generatorJson.Biomes.Length];
            generator.RejectionSamples = generatorJson.RejectionSamples;
            generator.MinDistanceBetweenAsteroids  = generatorJson.MinDistanceBetweenAsteroids;
            generator.BiomesMapNoiseFrequency = generatorJson.BiomesMapNoiseFrequency;
            generator.PlayerSpawn = PlayerSpawnRules.FromJson(generatorJson.PlayerSpawn);
            generator.FallbackBiome = LinkFallbackBiome(generatorJson.FallbackBiome, generator);

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

    private static Biome LinkFallbackBiome(string biomeId, WorldGenerator generator)
    {
        if (string.IsNullOrWhiteSpace(biomeId)) return null;
        if (generator.loadedBiomes.TryGetValue(biomeId, out Biome biome)) return biome;

        Debug.Error($"[GenerationLoader] Fallback biome not found: {biomeId}");
        return null;
    }

    private static void LoadAsteroids(IReadOnlyList<LayeredFile> files, WorldGenerator generator)
    {
        LoadAsteroidsFromFiles(files, generator);

        //Debug.Success($"[GenerationLoader] Loaded {generator.loadedAsteroids.Count} asteroids");
    }

    private static void LoadAsteroidsFromFiles(IReadOnlyList<LayeredFile> files, WorldGenerator generator)
    {
        try
        {
            foreach (var file in files)
            {
                string filePath = file.Source;
                try
                {
                    AsteroidJSON asteroidJson = GameSetFiles.Deserialize<AsteroidJSON>(file.Data);
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
            Debug.Error($"[GenerationLoader] Error reading asteroids: {ex.Message}");
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

    private static void LoadBiomes(IReadOnlyList<LayeredFile> files, WorldGenerator generator)
    {
        try
        {
            foreach (var file in files)
            {
                string filePath = file.Source;
                try
                {
                    BiomeJSON biomeJson = GameSetFiles.Deserialize<BiomeJSON>(file.Data);
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
            Debug.Error($"[GenerationLoader] Error reading biomes: {ex.Message}");
        }
    }

    public static AsteroidData ConvertAsteroid(AsteroidJSON json)
    {
        try
        {
            var asteroid = new AsteroidData(json.Id, json.Name)
            {
                
                MinRadius = AsteroidRadius.Clamp(json.MinRadius),
                MaxRadius = Math.Max(AsteroidRadius.Clamp(json.MinRadius), AsteroidRadius.Clamp(json.MaxRadius)),
                DensityThreshold = json.DensityThreshold,
                NoiseOctaves = json.NoiseOctaves,
                NoiseScale = json.NoiseScale,
                HasCavities = json.HasCavities,
                HasOreDeposits = json.HasOreDeposits,
                UsePerlinWorms = json.UsePerlinWorms,
                WormSettings = json.WormSettings,
                Distance = new DistanceRange(json.MinDistanceFromCenter, json.MaxDistanceFromCenter),
                HollowCorePercent = Math.Min(json.HollowCorePercent, (byte)90),
                Cavities = NormalizeCavities(json.Cavities),
                Shape = json.Shape,
                Mobs = ConvertMobRules(json),
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

    private static AsteroidCavitiesJSON NormalizeCavities(AsteroidCavitiesJSON json)
    {
        if (json == null) return null;

        return new AsteroidCavitiesJSON
        {
            MinCount = Math.Min(json.MinCount, json.MaxCount),
            MaxCount = Math.Max(json.MinCount, json.MaxCount),
            MinRadius = Math.Max((byte)1, Math.Min(json.MinRadius, json.MaxRadius)),
            MaxRadius = Math.Max((byte)1, Math.Max(json.MinRadius, json.MaxRadius))
        };
    }

    private static AsteroidMobRule[] ConvertMobRules(AsteroidJSON json)
    {
        if (json.Mobs == null || json.Mobs.Length == 0) return new AsteroidMobRule[0];

        var rules = new List<AsteroidMobRule>(json.Mobs.Length);
        foreach (var mob in json.Mobs)
        {
            if (mob == null || string.IsNullOrWhiteSpace(mob.Mob)) continue;

            string mobId = GameSetLoader.ResolveName(BaseShortId(mob.Mob), null, GameAssets.Mobs.ContainsKey, GameAssets.Mobs.Keys);
            if (!GameAssets.Mobs.ContainsKey(mobId))
            {
                Debug.Warning($"[GenerationLoader] Asteroid {json.Id}: unknown mob '{mob.Mob}'");
                continue;
            }

            rules.Add(AsteroidMobRule.FromJson(mob, mobId));
        }
        return rules.ToArray();
    }

    private static string BaseShortId(string id)
    {
        string trimmed = id.Trim().ToLowerInvariant();
        int colon = trimmed.IndexOf(':');
        if (colon < 0 || GameAssets.Mobs.ContainsKey(trimmed)) return trimmed;
        string shortId = trimmed.Substring(colon + 1);
        bool isBase = string.Equals(trimmed.Substring(0, colon), GameSetLoader.ModInfo?.ModId ?? "default", StringComparison.OrdinalIgnoreCase);
        return isBase ? shortId : trimmed;
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
        var blockData = GameAssets.TryGetBlockByFullID(fullBlockId, out var fillBlock) ? fillBlock : null;

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
            CanSpawnNearVoid = json.CanSpawnNearVoid,
            MinDepth = Math.Clamp(Math.Min(json.MinDepth, json.MaxDepth), 0f, 1f),
            MaxDepth = Math.Clamp(Math.Max(json.MinDepth, json.MaxDepth), 0f, 1f),
            Distance = new DistanceRange(json.MinDistanceFromCenter, json.MaxDistanceFromCenter),
            NearCavesOnly = json.NearCavesOnly,
            BiomeMultipliers = new Dictionary<string, int>(json.BiomeMultipliers ?? new Dictionary<string, int>(), StringComparer.OrdinalIgnoreCase)
        };

        string fullBlockId = GameSetLoader.CombineId(GameSetLoader.ModInfo.ModId, json.BlockId);
        var blockData = GameAssets.TryGetBlockByFullID(fullBlockId, out var veinBlock) ? veinBlock : null;

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