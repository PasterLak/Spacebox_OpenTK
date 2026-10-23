Empty GameSet
=============

The base of every GameSet. Worlds made with it are empty space: no asteroids,
no blocks, no items, no crafting, no mobs and no tutorials. It holds the sounds and
settings the game itself needs (interface, footsteps, pickup, default block sounds, lighting).

Empty is read-only. To make your own GameSet use Mods > Create > New GameSet: it creates
a GameSet with "Extends": "empty" in its config.json. Everything it does not have itself
comes from Empty, so it works from the start. Default is built on Empty the same way.
Empty cannot be extended; extensions extend Default or your own GameSet. See modding-guide.md.

Files that keep the GameSet empty:
- blocks.json, items.json: empty lists. Delete them when you add block or item folders.
- Crafting/categories.json, Recipes/recipes.json: no crafting and no machine recipes.
- Generation/generator.json: no asteroids in any sector.
- Player/spawn_parameters.json: players start with nothing.
- GameModes/: Survival, Creative and Spectator.
