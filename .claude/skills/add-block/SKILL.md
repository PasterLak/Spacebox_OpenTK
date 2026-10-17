---
name: add-block
description: Add a new block type to Spacebox — a data-only block (JSON + texture) or a block with new behaviour (Block subclass registered in BlockFactory). Use when the user asks to add, create or define a block.
---

# Add a block

## 1. Decide: data-only or new behaviour

- Existing `Type` values (`block`, `mineral`, `interactive`, `storage`, `craftingtable`, `furnace`, `crusher`, `disassembler`, `analyzer`, `radar`, `generator`, `consumer`, `cable`) cover the needed behaviour → data only, skip step 3.
- Otherwise → new `Block` subclass (step 3).

## 2. JSON entry

Add an object to `Spacebox/GameSets/Default/blocks.json` (fields come from `Spacebox/Game/Resource/BlockJSON.cs`):

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
- Optional: `IsTransparent`, `LightColor {r,g,b}` (emissive), `FrontDirection`, `Efficiency`, `StorageSize` (storage only).
- Mass and Durability are stored in 8 bits each (0–255).

## 3. New behaviour (only if needed)

1. Create `Spacebox/Game/Generation/Blocks/<Name>Block.cs` deriving from the closest base (`InteractiveBlock`, `ElectricalBlock`, `ResourceProcessingBlock`, `ConsumerBlock`, …). Follow the constructor pattern of a sibling block (`(BlockJSON data)`).
2. Register it in `Spacebox/Game/Resource/BlockFactory.cs` with a lowercase key: `{ "mytype", data => new MyTypeBlock(data) }`.
3. If it needs extra JSON fields, add them to a dedicated data class (like `StorageBlockData`) instead of growing `BlockJSON` with type-specific fields.
4. If it has UI, follow an existing window in `Spacebox/Game/GUI` (e.g. the analyzer UI).

## 4. Texture

- PNG, `BlockSize` px square (32 by default, see `GameSets/Default/config.json`), in `Spacebox/GameSets/Default/Blocks/`.
- The file name matches the name used in `Textures` (case-insensitive).

## 5. Optional

- Crafting: add a blueprint to `crafting.json`; processing output: `recipes.json`.
- World generation: reference it from an asteroid preset in `Generation/Asteroids/*.json`.

## 6. Verify

- `dotnet build Spacebox/Spacebox.csproj`
- `dotnet test Spacebox.Tests`
- Tell the user to check it in game (creative inventory) — loading errors are logged as `[GameSetLoader]` / `[BlocksLoader]`.
