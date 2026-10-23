Empty GameSet
=============

A starting point for a GameSet of your own. Worlds made with it are empty space:
no asteroids, no blocks, no items, no crafting, no mobs and no tutorials.
Nothing is taken from the Default GameSet, only interface textures and sounds.

Copy this folder, rename it, change ModId and ModName in config.json and add content
with the GameSet editor (Mods > GameSet editor) or by hand. See modding-guide.md.

Files that keep the GameSet empty:
- blocks.json, items.json: empty lists. Delete them when you add block or item folders.
- Crafting/categories.json, Recipes/recipes.json: no crafting and no machine recipes.
- Generation/generator.json: no asteroids in any sector.
- Player/spawn_parameters.json: players start with nothing.
- GameModes/: Survival, Creative and Spectator.
