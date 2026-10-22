# Spacebox modding guide

Spacebox content is data: JSON files plus PNG textures and OGG/WAV sounds. There are three kinds of mods:

| Kind | Folder | Picked | Saved with the world |
|---|---|---|---|
| Base GameSet | `Mods/GameSets/<Name>/` | per world (GameSet combo in Create World) | yes (`ModId`) |
| Extension GameSet | `Mods/Extensions/<Name>/` with `"Extends"` in `config.json` | per world (Extensions list in Create World) | yes (`Extensions`) |
| Texture pack | `Mods/TexturePacks/<Name>/` | globally in Mods -> Texture packs | no |

Examples shipped with the game: `Mods/Extensions/ExampleExtension` and `Mods/TexturePacks/ExamplePack`. A `.zip` or `.rar` dropped into `Mods/GameSets/` or `Mods/Extensions/` is extracted on the next start (extensions always end up in `Mods/Extensions/`).

## GameSet layout (folder format)

Every block and item has its own folder with its JSON, textures and sounds:

```
Mods/GameSets/MyMod/
  config.json
  icon.png
  Blocks/
    order.json                  optional: ["my_lamp", "my_panel"] = order in the creative menu
    Lights/my_lamp/             any grouping folders are allowed
      block.json                one block object
      myLamp.png                textures used by Textures in block.json
      Emissions/myLamp.png      glow map for the texture with the same name
      myLampPlace.ogg           sounds (Sounds.Place = "myLampPlace")
      blueprint.json            how to craft it (Product defaults to this block)
      recipes.json              machine recipes that use this block as input
  Items/
    my_gem/
      item.json
      myGem.png
  Recipes/furnace.json          { "Type": "furnace", "Recipes": [ ... ] }  all recipes of one machine
  Crafting/categories.json      { "Categories": [ ... ] }
```

- `block.json` / `item.json` hold exactly the object that used to be one array element of `blocks.json` / `items.json`. Field reference: `Spacebox/Game/Resource/Json/BlockJSON.cs`, `ItemJSON.cs` (or the GameSet editor, which lists every field).
- `blueprint.json`: `{ "Ingredients": [ { "Item": "iron_ingot", "Quantity": 2 } ], "Product": { "Quantity": 4 } }`.
- `recipes.json`: `[ { "Type": "crusher", "DurationSec": 4, "PowerPerTickRequared": 5, "Product": { "Item": "my_gem", "Quantity": 3 } } ]` (the ingredient defaults to the folder's entry).
- Texture and sound names are global per GameSet: the file name without extension is the name (`myLamp`), so keep names unique. Any PNG under `Blocks/` is a block texture, any PNG under `Items/` an item sprite, wherever it lies; shared textures can stay in a group folder. Folders starting with `_` are ignored.
- Old mods keep working: `blocks.json`, `items.json`, `crafting.json` and `recipes.json` (flat arrays) are still read and can be mixed with folders. Convert an old mod with the GameSet editor (Overview -> Convert to folder format) or `dotnet run --project Tools/Spacebox.GameSetTool -- convert GameSets/MyMod`; the converter checks that the loaded content stays identical before it changes anything.

## Extensions

An extension adds content to a base GameSet (usually `Default`) without copying it:

```json
{
  "ModId": "my_lamps",
  "ModName": "My Lamps",
  "Version": "1.0",
  "Extends": "default",
  "Includes": [ "my_metals" ]
}
```

- `Includes` loads other extensions first (they are added automatically when the world uses yours). Cycles, missing extensions and extensions of another base GameSet are reported in the menu and the world does not load.
- Extensions may contain blocks, items, textures, emission maps, sounds, blueprints, processing recipes and crafting categories, in the folder or the flat format, and `ItemsOnStart` (added to the base start items).
- Ids share the base GameSet's namespace (`default:my_lamp`), so you can use base items in recipes directly. Prefix your ids (`mylamps_...`) to avoid clashes with other extensions.
- An entry with an id that already exists patches it: listed fields replace the old values, nested objects are merged, arrays are replaced. Example: `{ "ID": "light_white", "Description": "New text" }` only changes the description. A blueprint with the same product or a recipe with the same machine and ingredient replaces the old one.
- Load order: base GameSet, then extensions (includes first, then the order chosen in Create World). Later extensions win.
- The extension list is chosen when a world is created and saved in `world.json` (`"Extensions": [ "my_lamps" ]`). Blocks and items of a missing extension would be unknown to the world, so the list cannot be changed in the menu and a world whose extensions are not installed does not load.
- Multiplayer: the server sends its GameSet hash including all extensions; clients without the same files download the server's GameSet and extensions automatically into `Multiplayer/Mods/<server>/`. Dedicated servers use the `Extensions` of their world's `world.json`.

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
