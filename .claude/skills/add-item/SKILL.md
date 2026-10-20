---
name: add-item
description: Add a new item to Spacebox (plain item, drill, weapon, consumable) or a new item type. Use when the user asks to add, create or define an item, tool, weapon or consumable.
---

# Add an item

## 1. JSON entry

Add an object to `Spacebox/GameSets/Default/items.json`. `Type` selects the DTO in `Spacebox/Game/Resource/Json/ItemJSON.cs`:

| Type | DTO | Extra fields |
|---|---|---|
| `item` (default) | `ItemJSON` | — |
| `drill` | `DrillItemJSON` | `Tier`, `PowerUsage`, `Range`, `DrillColor {r,g,b}` |
| `weapon` | `WeaponItemJSON` | see the class (projectile, ammo, …) |
| `consumable` | `ConsumableItemJSON` | see the class |

Common fields: `ID` (unique, snake_case), `Name`, `Description`, `Category`, `Sprite`, `MaxStack` (clamped to 255), `ModelDepth`.

## 2. Sprite

PNG in `Spacebox/GameSets/Default/Items/`, file name = `Sprite` value. Weapons may also need a projectile in `Projectiles/` (`ProjectileJSON`).

## 3. New item type (only if no existing type fits)

1. Add a DTO deriving from `ItemJSON` in `ItemJSON.cs` and an `Item` subclass next to the existing ones.
2. Add a `case` in the item switch in `GameSetLoader` (`Spacebox/Game/Resource/Loaders/GameSetLoader.cs`, the `"weapon"`/`"drill"`/`"consumable"` switch) and a matching `Register<Type>Item` method.
3. If the item needs its own interaction, add an `Interaction*` class in `Spacebox/Game/Player/Interactions/` and select it in `InteractionHandler`.

## 4. Optional

Crafting blueprint in `crafting.json`, processing recipe in `recipes.json`, starting item in `config.json` → `ItemsOnStart`, loot in `Generation/Loot/*.json`.

## 5. Verify

`dotnet build Spacebox/Spacebox.csproj`, then ask the user to check it in game.
