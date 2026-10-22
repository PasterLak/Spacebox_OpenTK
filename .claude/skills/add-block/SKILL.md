---
name: add-block
description: Add a new block type to Spacebox — a data-only block (JSON + texture) or a block with new behaviour (Block subclass registered in BlockFactory). Use when the user asks to add, create or define a block.
---

# Add a block

## 1. Decide: data-only or new behaviour

- Existing `Type` values (`block`, `mineral`, `interactive`, `storage`, `craftingtable`, `furnace`, `crusher`, `disassembler`, `analyzer`, `radar`, `generator`, `consumer`, `cable`) cover the needed behaviour → data only, skip step 3.
- Otherwise → new `Block` subclass (step 3).

## 2. JSON entry

Default uses the folder format (see `modding-guide.md`): create `Spacebox/Mods/GameSets/Default/Blocks/<Group>/<id>/block.json` with one object (fields come from `Spacebox/Game/Resource/Json/BlockJSON.cs`) and add the id to `Blocks/order.json` where it should appear (unlisted entries come after the listed ones). Old GameSets may still use a flat `blocks.json` array; both formats can be mixed.

```json
{
  "ID": "my_block",
  "Name": "My Block",
  "Description": "…",
  "Type": "block",
  "Category": "",
  "PowerToDrill": 1,
  "Mass": 1,
  "Durability": 5,
  "Textures": { "Active": { "All": "myBlock" } },
  "Sounds": { "Place": "blockPlaceLight" },
  "Drop": "$self",
  "DropQuantity": 1
}
```

- `ID` must be unique, snake_case. The mod id prefix is added by the loader.
- `Textures.Active` / `Textures.Inactive` accept `All` or per face `Up, Down, Left, Right, Forward, Back`. `Inactive` is used by machines when off.
- Optional: `Sounds.Footsteps` (list of GameSet sound names played as footsteps in gravity, e.g. `footstepGlass1`-`4`; default stone on natural blocks, metal otherwise), `IsTransparent`, `LightColor {r,g,b}` (emissive), `FrontDirection`, `Efficiency`, `StorageSize` (storage only), `Door { MoveDirection, Speed, OpenTime, AutoClose }` (type `door`), `Generator { Power }` (type `generator`), `Size {x,y,z}` (multi-cell block, 1–16 per axis; x = width, y = height, z = depth relative to the front face). For multi-cell faces, a face-sized texture (e.g. 64×64 px on a 2×2 face of a 32 px block) spans the face and a one-cell texture repeats per cell; the pixels per cell are detected per block from its textures (any resolution works), override with `CellPixels` when every face of the block should repeat a large texture, or set `"TextureFit": true` to stretch each face texture once over the whole face. Electrical multi-cell blocks work (only the origin cell generates/consumes); storage and processing machines are still per cell.
- `"Unbreakable": true` works on any block: only creative players and commands can remove it. Type `invisible` makes a solid block that is only drawn while a player holds an invisible block (Default `barrier`).
- Mass and Durability are stored in 8 bits each (0–255).

## 3. New behaviour (only if needed)

1. Create `Spacebox/Game/Generation/Blocks/<Name>Block.cs` deriving from the closest base (`InteractiveBlock`, `ElectricalBlock`, `ResourceProcessingBlock`, `ConsumerBlock`, …). Follow the constructor pattern of a sibling block (`(BlockJSON data)`).
2. Register it in `Spacebox/Game/Resource/Loaders/BlockFactory.cs` with a lowercase key: `{ "mytype", data => new MyTypeBlock(data) }`.
3. If it needs extra JSON fields, add them to a dedicated data class (like `StorageBlockData`) instead of growing `BlockJSON` with type-specific fields.
4. If it has UI, follow an existing window in `Spacebox/Game/GUI` (e.g. the analyzer UI).

## 4. Texture

- PNG in the block's entry folder (`Blocks/<Group>/<id>/`), emission maps in `<id>/Emissions/` (32 px is the standard; other sizes are packed too, but isometric icons need all faces of a block to have the same square size). Any PNG under `Blocks/` is a block texture, so shared textures can stay in a group folder.
- The file name matches the name used in `Textures` (case-insensitive) and must be unique in the GameSet. Sounds used only by this block can live in the entry folder too.

## 5. Optional

- Crafting: `<id>/blueprint.json` (one blueprint; `Product` defaults to this block); processing where this block is the ingredient: `<id>/recipes.json` (array with `Type`), or the machine file `Recipes/<type>.json`.
- World generation: reference it from an asteroid preset in `Generation/Asteroids/*.json`.

## 6. Verify

- `dotnet build Spacebox/Spacebox.csproj`
- `dotnet test Spacebox.Tests`
- Tell the user to check it in game (creative inventory) — loading errors are logged as `[GameSetLoader]` / `[BlocksLoader]`.
