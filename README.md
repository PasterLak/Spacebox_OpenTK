<img src="https://github.com/user-attachments/assets/8695e4ae-769b-49fe-813e-81af0a431ea5" width="300" height="300" alt="Asteroid">


A voxel space-survival game called "Spacebox", developed with its own game engine on top of OpenTK (OpenGL 3.3, .NET 8).

## ⭐️ About the Game

You need to survive in an asteroid belt. Mine asteroids, smelt and process ores, build a base, power it, and build ships to fly between procedurally generated asteroids. Creatures hide deep inside some asteroids. Play alone, with friends in your network, or on a dedicated server. All content is data-driven, so blocks, items, recipes, generation and textures can be modded without programming.

## 🚀 Game Features

- **Six degrees of freedom:** move and roll in all directions in zero gravity; gravity generators give you a floor to walk on
- **Mining and processing:** drill tiers and ore hardness, furnace, crusher, disassembler and other machines, item pipes for logistics
- **Building:** blocks, multi-cell blocks, doors, windows, lights, signs and picture frames; creative fill and copy tools
- **Power:** generators and power blocks (cobalt, nuclear, ...), cables that connect automatically, oxygen and gravity generators
- **Ships:** build your own ship from blocks, fly it from a console, use thrusters, cannons, camera blocks and radar
- **Survival:** health, oxygen and power, armor and equipment, DNA reconstructors to respawn, tutorials that teach the mechanics
- **Procedural world:** biomes, asteroid shapes, caves, ore veins and loot; new-world generation options
- **Multiplayer:** host a game, open it to your LAN, or run a dedicated server with a web dashboard (players, inventories, live console, settings, 3D map, statistics, HTTPS)
- **Modding:** GameSets, extensions and texture packs, plus a built-in GameSet editor

## 🕹 Controls

Default keys (remappable in the options, press F6 in the game to see all of them):

| Action | Key |
| --- | --- |
| Move | W A S D |
| Up / down (zero gravity) | Space / Left Ctrl |
| Roll | Q / E |
| Sprint / boost | Left Shift |
| Crouch / slow walk (gravity) | Left Ctrl / Left Alt |
| Destroy block / shoot | Left mouse button |
| Place block / use | Right mouse button |
| Rotate block / block magnet | R / M |
| Zoom | Middle mouse button |
| Flashlight | F |
| Inventory | Tab |
| Drop item / delete item | G / X |
| Ships / waypoints / tutorial | B / N / J |
| Chat / command | T / `/` |
| Hide interface | F8 |
| Pause, save and options | Esc |

## 🧩 Modding

Content lives in `Mods/` next to the game:

- **GameSets** (`Mods/GameSets/`): a full content set in folder format, one folder per block and item with its JSON, textures, sounds and recipes. `Default` is the main game.
- **Extensions** (`Mods/Extensions/`): add or patch blocks, items, textures, sounds and recipes of a base GameSet, chosen per world in Create World. Example: `ExampleExtension`.
- **Texture packs** (`Mods/TexturePacks/`): override block, item and UI textures of any GameSet; several can be active at once. Example: `ExamplePack`.

Mods can be dropped in as folders or archives (.zip, .rar, .7z, .tar.*). Servers send their GameSet, extensions and texture packs to joining players. See the [modding guide](modding-guide.md) for the format.

## 🛠 Building from Source

Requires the .NET 8 SDK.

```bash
dotnet build Spacebox.sln                       # build everything
dotnet run --project Spacebox/Spacebox.csproj   # run the game
dotnet run --project Spacebox/Spacebox.csproj -- --editor Default   # open the GameSet editor
dotnet test Engine.Tests
dotnet test Spacebox.Tests

dotnet publish Spacebox/Spacebox.csproj -c Release -r linux-x64 --self-contained true --output ./publish   # also win-x64 / osx-x64
```

Release packages are built with `Spacebox/Build/BuildWindows.ps1`, `BuildLinux.ps1` and `BuildMacOS.ps1` (output in `Final/`).

## 🖥 Dedicated Server

The game, host mode and dedicated server are the same program.

- Windows: `start-server.bat` (or `Spacebox.exe --server`)
- Linux: `./start-server.sh` (or `./Spacebox --server`), no graphics card or desktop needed
- From source: `dotnet run --project Spacebox/Spacebox.csproj -- --server`

On the first start the server creates `Multiplayer/serverconfig.json` and a world in `Worlds/ServerWorld`. Players join on UDP port 7777; the web dashboard runs on TCP 7777 at `http://localhost:7777/dashboard` (register the admin with the setup key from `Multiplayer/Dashboard/setup-key.txt`). Type `stop` to save and stop. The full guide (configuration, port forwarding, HTTPS, commands) is shipped as `Multiplayer/Spacebox Server Guide.pdf`.

## 🔗 Links

- The Website and download: https://pasterlak.itch.io/spacebox
- Youtube: https://www.youtube.com/@SpaceboxGame
