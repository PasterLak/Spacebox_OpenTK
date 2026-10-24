# Spacebox modding guide

Spacebox content is data: JSON files plus PNG textures and OGG/WAV sounds. There are three kinds of mods:

| Kind | Folder | Picked | Saved with the world |
|---|---|---|---|
| Base GameSet | `Mods/GameSets/<Name>/` | per world (GameSet combo in Create World) | yes (`ModId`) |
| Extension GameSet | `Mods/Extensions/<Name>/` with `"Extends"` in `config.json` | per world (Extensions list in Create World) | yes (`Extensions`) |
| Texture pack | `Mods/TexturePacks/<Name>/` | globally in Mods -> Texture packs | no |

Examples shipped with the game: `Mods/Extensions/ExampleExtension` and `Mods/TexturePacks/ExamplePack`. See "Creating a GameSet" below for `Mods/GameSets/Empty`, the base every GameSet is built on. A mod archive (`.zip`, `.rar`, `.7z`, `.tar`, `.tar.gz`/`.tgz`, `.tar.bz2`/`.tbz2`, `.tar.xz`/`.txz` or `.tar.zst`) dropped into `Mods/GameSets/` or `Mods/Extensions/` is extracted on the next start (extensions always end up in `Mods/Extensions/`).

## GameSet layout (folder format)

Every block and item has its own folder with its JSON, textures and sounds:

```
Mods/GameSets/MyMod/
  config.json
  icon.png
  Blocks/
    order.json                  optional: ["my_lamp", "my_panel"] = order in the creative menu
    Lights/my_lamp/             any grouping folders are allowed
      my_lamp.json              one block object, file named like its folder (block.json works too)
      myLamp.png                textures used by Textures in my_lamp.json
      Emissions/myLamp.png      glow map for the texture with the same name
      myLampPlace.ogg           sounds (Sounds.Place = "myLampPlace"; machines also Sounds.Working loop and Sounds.Finished)
      blueprint.json            how to craft it (Product defaults to this block)
      recipes.json              machine recipes that use this block as input
  Items/
    my_gem/
      my_gem.json
      myGem.png
  Recipes/furnace.json          { "Type": "furnace", "Recipes": [ ... ] }  all recipes of one machine
  Crafting/categories.json      { "Categories": [ ... ] }
```

- The entry file is named like its folder (`my_lamp/my_lamp.json`, so a search for the id finds it) or `block.json` / `item.json`; if both exist, the named one is used. It holds exactly the object that used to be one array element of `blocks.json` / `items.json`. Field reference: `Spacebox/Game/Resource/Json/BlockJSON.cs`, `ItemJSON.cs` (or the GameSet editor, which lists every field).
- `blueprint.json`: `{ "Ingredients": [ { "Item": "iron_ingot", "Quantity": 2 } ], "Product": { "Quantity": 4 } }`.
- `recipes.json`: `[ { "Type": "crusher", "DurationSec": 4, "PowerPerTickRequared": 5, "Product": { "Item": "my_gem", "Quantity": 3 } } ]` (the ingredient defaults to the folder's entry).
- Furnaces and crushers work without electricity unless their block JSON has `"Processing": { "Power": 30 }` (EU); then they only run while their network is powered.
- Block components: `"Components": [ ... ]` combines behaviours on one block without code. It works on `"Type": "modular"` (a block made only of components) and adds to `furnace`, `crusher` and `generator`. Components: `processing` (machine, settings in `Processing`: `Power`, `RecipeType` = the `Type` of the recipes it uses, required for `modular`), `storage` (slots, size from `StorageSize`; next to `processing` it feeds the input and collects the products), `generator` (`Generator.Power`), `consumer` (`Consumer.Power`, default 20 EU) and `light` (glows with `LightColor` only while powered). Examples: a smelter with a buffer `{ "Type": "modular", "Components": ["processing", "storage"], "Processing": { "RecipeType": "furnace", "Power": 20 }, "StorageSize": { "X": 4, "Y": 2 } }`, a lamp that needs power `{ "Type": "modular", "Components": ["consumer", "light"], "LightColor": { "R": 255, "G": 220, "B": 160 } }`. Unknown names are skipped with a warning.
- `Crafting/categories.json`: each category can set `"Group"` (for example `Blocks`, `Items`, `Ships`); groups are the tabs at the top of the crafting window. Without a group, row 1 goes to Blocks and row 2 to Items.
- Mobs: `DarkSightRange` (blocks; a player with the flashlight off is only noticed this close, 0 = light makes no difference), `WatchChance` (% chance to only watch a player until hurt), `ReturnDistance` (after fleeing, fly back home when farther than this; 0 = off).
- Texture and sound names are global for the whole stack (base GameSet, Default fallback and all extensions): the file name without extension is the name (`myLamp`). A file with a name that already exists replaces it everywhere (later layers win), so prefix your names. Any PNG under `Blocks/` is a block texture, any PNG under `Items/` an item sprite, wherever it lies; shared textures can stay in a group folder. Folders starting with `_` are ignored.
- Ids: lower-case letters, digits and `_`, 2-64 characters, not starting with a digit. Invalid characters are replaced; a duplicate id is skipped with an error in the log, so give every entry its own id. JSON property names are case-insensitive. `Recipes/<machine>.json` needs `"Type"`; ids in `order.json` that don't exist are ignored.
- `config.json` needs a unique `ModId` (without one the folder name is used). Two GameSets with the same id are reported and only the first one is used.
- Removing or renaming a block or item id turns the placed blocks and items of existing worlds into air/nothing on the next save. Keep old ids in released mods.
- Archives: a `.zip`, `.rar`, `.7z`, `.tar`, `.tar.gz`/`.tgz`, `.tar.bz2`/`.tbz2`, `.tar.xz`/`.txz` or `.tar.zst` in `Mods/GameSets/` or `Mods/Extensions/` is extracted on the next start into a folder named after its `ModName` (or the archive name when it has none). If that folder holds the same mod (same `ModId`) it is updated, otherwise the new one gets a numbered folder; `Default` is never replaced.
- Old mods keep working: `blocks.json`, `items.json`, `crafting.json` and `recipes.json` (flat arrays) are still read and can be mixed with folders. Convert an old mod with the GameSet editor (Overview -> Convert to folder format) or `dotnet run --project Tools/Spacebox.GameSetTool -- convert Mods/GameSets/MyMod`; the converter checks that the loaded content stays identical before it changes anything.

## Player start

`Player/spawn_parameters.json` (base GameSet):

```json
{
  "GiveItemsOnStart": [ { "ID": "aluminium_drill", "Count": 1, "PutIn": "hand", "SlotId": 0 }, { "ID": "aluminium_flashlight", "Count": 1, "PutIn": "equipment" } ],
  "SpawnSettings": { "PreferredSpawnBiome": "", "MinDistanceFromCenter": 0, "MaxDistanceFromCenter": 0, "MinAsteroidRadius": 24, "ExcludedAsteroids": [ "asteroid_icy" ], "HealthOnSpawn": 100, "PowerOnSpawn": 100, "OxygenOnSpawn": 100 }
}
```

- `PutIn`: `auto` (equipment slot if it fits, else the hotbar), `hand` (hotbar slot `SlotId`, selected), `hotbar`, `inventory`, `equipment`.
- Spawn: new players start next to an asteroid of the start sector that is in the distance range and the preferred biome (falls back to any in range, then any), at least `MinAsteroidRadius` big and not excluded. Health, power and oxygen are percent of the maximum, used for new players and respawns.
- Without the file the old `config.json` `ItemsOnStart` and `generator.json` `PlayerSpawn` still work. Extensions add start items with `ItemsOnStart` in their `config.json`.
- All time settings are seconds (weapon and cannon `ReloadTime` too: 0.5 = two shots per second).

## Game modes

`GameModes/*.json` defines the game modes of a GameSet. Ids 0, 1 and 2 are Survival, Creative and Spectator; their files can rename them and change their rules, but not their base. Ids 3-31 are new modes built on one of the three bases (its movement, inventory and interactions) with their own rules. Missing rules use the base's values, a missing folder keeps the three built-in modes.

```json
{
  "Id": 3,
  "Key": "explorer",
  "Name": "Explorer",
  "Tag": "E",
  "TagColor": "#C08A2E",
  "Base": "survival",
  "Rules": { "BreakBlocks": false, "PlaceBlocks": false }
}
```

`Tag` (up to 3 letters, default: first letter of the name) and `TagColor` (`#RRGGBB`, default by base) mark worlds of this mode in the world list. Rules (all true/false; missing ones use the base's value):

| Rule | Meaning | Survival / Creative / Spectator |
|---|---|---|
| `BreakBlocks`, `PlaceBlocks`, `UseWeapons` | break, place, shoot | yes / yes / no |
| `TakeDamage` | damage from mobs, weapons, players, explosions, falls, suffocation | yes / no / no |
| `NeedOxygen` | oxygen is used up outside air | yes / no / no |
| `Collide` | bump into blocks (off = fly through) | yes / yes / no |
| `UseGravity` | gravity generators pull the player | yes / yes / no |
| `MobsAttack` | mobs chase and bite the player | yes / no / no |
| `ConsumeItems` | placing, eating, repairing, spawning and gifts use items up; the server checks the inventory | yes / no / no |
| `DropItemsOnDeath` | the inventory drops on death (unless the server keeps inventories) | yes / no / no |
| `UseItems` | use and drop hotbar items | yes / yes / no |
| `CreativeInventory` | creative window with every item | no / yes / no |
| `InstantBreak` | blocks break at once instead of drilling | no / yes / no |
| `BreakUnbreakable` | break normally unbreakable blocks | no / yes / no |
| `AreaTools` | erase and fill areas | no / yes / no |
| `ShipLibrary` | place saved ships | no / yes / no |
| `FreeFlashlight`, `FullRadar` | flashlight and full radar without items | no / no / yes |
| `CountStatistics`, `StartItems` | count statistics, give start items | yes / yes / no |
| `HotbarSlots`, `HotbarRows` | slots per hotbar row (1-20) and rows (1-3); number keys pick the first 10 slots, the mouse wheel all of them; items in slots a mode does not have move to the inventory or drop | 10 / 15 / 10 slots, 1 row |

The base still decides movement (walking/flying, spectator camera), the inventory layout and the interactions; the server checks breaking, placing, shooting, item use, area tools, saved ships and unbreakable blocks with the player's rules. Switch with `gm <id or key>` (e.g. `gm explorer`) or on the dashboard.

## Creating a GameSet

Mods > Create opens a window with two choices:

- **New GameSet:** a complete GameSet of your own in `Mods/GameSets/<Name>/` with `"Extends": "empty"` in its `config.json`. It is built on `Empty`, so it works from the start, and it is listed as a GameSet (not as an extension) in Create World. You add every block, item, asteroid and so on yourself.
- **Extension:** adds or changes content of an existing GameSet (`Default` or one of your own) in `Mods/Extensions/<Name>/`; pick "Any GameSet (general pack)" for a pack that works with every GameSet (`"Extends": "*"`). The editor opens a general pack on top of Default. See Extensions.

Both write `config.json` and an icon and open the GameSet editor.

`Mods/GameSets/Empty` holds what the game itself needs (interface, footstep, pickup, crafting and default block sounds, `Optional/lighting.json`, the three game modes) and no content: worlds made with it are empty space. It is read-only in the editor and cannot be extended. `Default` is built on Empty too (`"Extends": "empty"`).

Missing files come from the GameSet it is built on: a GameSet built on Empty takes missing sounds, textures, `Generation`, `Mobs`, `Projectiles`, `Tutorials`, `GameModes`, `Loot`, `Optional/lighting.json` and blocks, items, crafting or recipes from Empty. An existing folder, even an empty one, keeps that part empty. Old GameSets without `Extends` keep taking missing files from Default (and from Empty after it).

## Extensions

An extension adds content to a base GameSet (usually `Default`) without copying it:

```json
{
  "ModId": "my_lamps",
  "ModName": "My Lamps",
  "Version": "1.0",
  "Extends": "default",
  "MinBaseVersion": "0.1.5",
  "Includes": [ "my_metals" ]
}
```

- **Which GameSets:** `"Extends": "default"` (one GameSet), `["default", "my_set"]` (only these) or `"*"` (any GameSet; Empty can never be extended). An extension can also extend another extension (`"Extends": "weapons_pack"`): it works wherever its parent works, the parent is turned on automatically and loads first, and `MinBaseVersion`/`MaxBaseVersion` check the parent's `Version`. Loops (`a` extends `b` extends `a`) are refused.
- **Versions:** `MinBaseVersion` and optional `MaxBaseVersion` compare against the base GameSet's `Version` number by number (`0.1.10` > `0.1.9`). An extension that does not fit is shown in red with the reason in Create World and cannot be picked; a world or server that uses it does not load. Mods > Create fills in the base's current version.
- **Everything can be overridden:** an extension can add, patch and remove blocks, items, blueprints, recipes, crafting categories, projectiles, mobs, loot tables, asteroids, biomes, tutorials and game modes, and patch every settings file (`Generation/generator.json`, `ships.json`, `Player/spawn_parameters.json`, `Optional/lighting.json`, `disassembly.json`, `OxygenConsumeInterval` in its `config.json`), plus all textures, sounds, models and pictures.
  - Files in folders (`Projectiles/`, `Mobs/`, `Generation/Loot/`, `Generation/Asteroids/`, `Generation/Biomes/`, `Tutorials/`, `GameModes/`) are matched by file name: a file with the same name as a base file patches it field by field, a new name adds one.
  - Settings files are patched field by field; nested objects merge, lists are replaced (copy the whole list, e.g. `Biomes` in generator.json, when you change it). `disassembly.json` `Items` are merged by `Item`.
  - `"Remove": true` removes an entry: `{ "ID": "glass", "Remove": true }` in a block or item, a folder file containing only `{ "Remove": true }`, or `{ "Item": "pistol", "Remove": true }` in disassembly `Items`. Worlds with a removed block placed show it as unknown.
- **Adapter folders:** `Bases/<GameSet id>/` inside an extension is loaded right after the extension, only when the world uses that GameSet. It has the normal layout (Blocks/, Items/, Recipes/, Projectiles/, ...). Use it for recipes and patches that need items of one GameSet. In the GameSet editor, the toolbar's **Target** selector of an extension switches between "All GameSets" (the extension folder) and "Only <GameSet>" (its `Bases/<id>/` folder, previewed on that GameSet).

- `Includes` loads other extensions first (they are added automatically when the world uses yours). Cycles, missing extensions and extensions of another base GameSet are reported in the menu and the world does not load.
- Extensions may contain blocks, items, textures, emission maps, sounds, blueprints, processing recipes and crafting categories, in the folder or the flat format, and `ItemsOnStart` (added to the base start items).
- **Ids get the extension's namespace automatically:** a new block or item `my_lamp` in `my_lamps` becomes `my_lamps:my_lamp`, so two extensions can both have a `laser` without clashing. Short ids in an extension's own files (recipes, blueprints, drops, ammo) look in the extension first, then in the extensions it extends or includes, then in the base GameSet, so `iron_ingot` still means `default:iron_ingot`. An entry whose short id already exists in the base or a parent (`stone`) patches it; write `default:marble` or `other_pack:laser` to name a namespace yourself. The `give` command and old saves also accept `default:my_lamp` for an extension item when no base item has that id (worlds made before this change keep loading).
- **Everything else is namespaced too:** new crafting categories (`my_lamps:lights`, items of the extension find their own category first), projectiles (`weapons_pack:laser`, weapons and cannons of the pack find their own first), loot tables, mobs and every new file in a folder (`Projectiles/`, `Mobs/`, `Generation/Loot/`, ...). A file with the name of a base or parent file still patches it. `spawnmob laser_bug` finds a pack mob when its short id is unique.
- An entry with an id that already exists patches it: listed fields replace the old values, nested objects are merged, arrays are replaced. Example: `{ "ID": "light_white", "Description": "New text" }` only changes the description. A blueprint with the same product or a recipe with the same machine and ingredient replaces the old one.
- When an extension changes an entry that another extension added (same id), the log warns about it: use your own id prefix unless you really mean to patch it. Extensions that extend or include the other extension patch its entries without a warning.
- **General packs for every GameSet** (for example a weapons pack): use `"Extends": "*"`, bring everything the pack needs (projectiles, sounds, textures, its own crafting category) and use only your own items in its recipes; put recipes with items of a specific GameSet into `Bases/<id>/`. Recipes and loot entries that still name a missing item are skipped with a message in the log. Ids are namespaced per pack automatically (`weapons_pack:laser`), so packs never overwrite each other's entries.
- Load order: base GameSet, then extensions (includes first, then the order chosen in Create World). Later extensions win.
- The extension list is chosen when a world is created and saved in the world's `world.nbt` (`Extensions` list; worlds from older builds still have it in `world.json` until they are saved). Blocks and items of a missing extension would be unknown to the world, so the list cannot be changed in the menu and a world whose extensions are not installed does not load.
- Multiplayer: the server sends its GameSet hash including all extensions; clients without the same files download the server's GameSet and extensions automatically into `Multiplayer/Mods/<server>/`. Dedicated servers use the `Extensions` of their world's `world.json`.

## Generation

`Generation/generator.json`, `Generation/Biomes/*.json` and `Generation/Asteroids/*.json` of the base GameSet (extensions cannot change them). Every field is optional; missing fields keep the old behaviour. Distances are in blocks from the world origin (0, 0, 0); `Max...` 0 means no limit.

- **World generator version:** new worlds store `GeneratorVersion` 2 in `world.nbt` and use all options below. Worlds without it (made before this version) keep generating exactly as before, so changing these files never breaks the asteroids of old worlds. Asteroids also store the biome and distance they were made with, so their untouched parts always regenerate the same.
- **generator.json:** `MinAsteroidsInSector`, `MaxAsteroidsInSector`, `MinDistanceBetweenAsteroids`, `RejectionSamples`, `BiomesMapNoiseFrequency`, `Biomes` (ids, the order matters for the noise), `FallbackBiome` (used where no listed biome is allowed at that distance; without it that space stays empty), `PlayerSpawn`.
- **Biome:** `SpawnChance` (share of the biome noise), `MinDistanceFromCenter` / `MaxDistanceFromCenter`, `Density` (percent of asteroids kept, default 100, above 100 makes it denser), `MinDistanceBetweenAsteroids` (wider spacing in this biome), `RadiusMultiplier` (default 1), `OreMultiplier` (scales every vein chance, default 1), `WormSettings` (replaces the asteroid's tunnel settings), `Asteroids` ({ asteroid id: weight }).
- **Asteroid:** `MinRadius`, `MaxRadius`, `DensityThreshold`, `NoiseOctaves`, `NoiseScale`, `UsePerlinWorms` + `WormSettings`, `HasOreDeposits`, `MinDistanceFromCenter` / `MaxDistanceFromCenter` (the biome only picks it in that range), `HollowCorePercent` (0-90: the inner part of the radius is empty), `Cavities` { `MinCount`, `MaxCount`, `MinRadius`, `MaxRadius` } (round caves; `HasCavities: true` without it gives 1-3 small caves), `Layers`, `Mobs`.
- **Asteroid shape** (new worlds): `"Shape": { "Chance": 70, "Stretch": 0.35, "Warp": 0.3, "Roughness": 0.22, "Lobes": { "Chance": 65, "MinCount": 1, "MaxCount": 3, "MinSize": 0.3, "MaxSize": 0.6 }, "Double": { "Chance": 10, "MinSize": 0.5, "MaxSize": 0.9, "Neck": 0.5 }, "Torus": { "Chance": 3, "MinTube": 0.25, "MaxTube": 0.35 }, "Bite": { "Chance": 12, "MinSize": 0.35, "MaxSize": 0.6 }, "Craters": { "MinCount": 0, "MaxCount": 4, "MinSize": 0.1, "MaxSize": 0.25 }, "RemoveFloatingBlocks": true }`. `Chance` is the percent of asteroids of this type that get a random form; the others stay classic noisy balls. Every shaped asteroid rolls its own form from its seed: a stretched blob with extra lobes, sometimes two bodies joined by a neck (`Double`) or a ring (`Torus`), then a Pac-Man mouth or a big bite (`Bite`) and surface craters. Sizes are shares of the asteroid radius, chances are percent. `Warp` bends the outline, `Roughness` is the surface noise (with `NoiseScale`/`NoiseOctaves`). `RemoveFloatingBlocks` keeps only the largest connected piece (asteroids up to 256 blocks across). Layers, ore depth and mob depth follow the shape. Without `Shape` the asteroid stays a noisy ball.
- **Layer:** `FillBlockID`, `Ratio` (share of the depth from the surface to the core; the first layer is the outer one), `Veins`.
- **Vein:** `BlockId`, `SpawnChance` (per block, out of 1 000 000), `MinVeinSize`, `MaxVeinSize`, `CanSpawnNearVoid`, `MinDepth` / `MaxDepth` (0 surface ... 1 core), `MinDistanceFromCenter` / `MaxDistanceFromCenter`, `NearCavesOnly` (only within 2 blocks of a cave or tunnel), `BiomeMultipliers` ({ biome id: percent }).
- **Mobs in asteroids:** `"Mobs": [ { "Mob": "mineral_parasite", "Chance": 100, "MaxPerChunk": 1, "MaxPerEntity": 4, "Layers": [ 1, 2 ], "MinDepth": 0, "MaxDepth": 1, "MaxLightLevel": 0, "Biomes": [ "dense_cluster" ], "MinDistanceFromCenter": 0, "MaxDistanceFromCenter": 0 } ]`. A mob listed by any asteroid only spawns in asteroids that list it (in all worlds); a mob listed nowhere spawns on every asteroid like before. Unset values come from the mob's own `Spawn` section, which keeps the global rules (`Enabled`, `MaxTotal`, `Interval`, `Chance`, player distances, `DespawnDistance`). `Layers` are indexes into the asteroid's `Layers` (0 = outer), empty = all.
- The GameSet editor's Generation panel shows the biome map of any sector for new worlds (or "Old worlds"), with distance rings of the biomes.

## Texture packs

```
Mods/TexturePacks/MyPack/
  pack.json        { "Id": "my_pack", "Name": "My Pack", "Description": "...", "Author": "...", "Version": "1.0" }
  icon.png
  Blocks/lightRock.png      replaces the block texture "lightRock" of every GameSet and extension
  Items/ironIngot.png       replaces the item sprite "ironIngot"
  Emissions/furnace.png     replaces the glow map "furnace"
  UI/slot.png               replaces Resources/Textures/UI/slot.png (same relative path)
```

- Only names that exist are replaced; folders inside `Blocks/` and `Items/` are free.
- Enable packs in Mods -> Texture packs. Several packs can be active; the top one wins. The list is stored in `Resources/Settings.json` (`texture_packs.enabled`) and applies when a world is loaded.
- Multiplayer: a server can share texture packs. A dedicated server lists them in `Multiplayer/serverconfig.json` (`"TexturePacks": [ "my_pack" ]`), a host shares the packs it has enabled. Joining players download them with the GameSet and see them below their own enabled packs; they are used only while playing on that server.
