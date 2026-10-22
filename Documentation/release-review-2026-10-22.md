# Release review — 2026-10-22 (game version 0.1.5, branch `test-0.1.3`)

Four read-only reviews (multiplayer, game logic, engine, modding) and live tests (single player, dedicated server + client, LAN host + client on one PC). Everything marked **fixed** is committed, with tests; build and both test suites are green (Engine.Tests 517, Spacebox.Tests 1387).

## 1. Fixed in this round

| Area | Fix | Commit |
|---|---|---|
| Server stop | `stop`, `restart`, dashboard, Ctrl+C/SIGTERM **and closing the server window** first ask every player for fresh data (up to 2.5 s), then save. Closing the window/taskkill used to save **nothing**. A host leaving its world does the same. | c983050a, 6889b3fe |
| Client leave | A leaving client waits up to 2 s for the server to confirm its player data. Before, the last ≤10 s were lost → **item duplication** by putting items in a chest/dropping/dying and leaving. | 77453e19 |
| Loading entities | Clients buffer edits, bulk edits, damage, repairs, explosions and block states for an asteroid/ship they are still streaming and replay them after it loads (were lost until rejoin). | 77453e19 |
| Far areas | The server keeps block edits for entities it hasn't loaded yet (15 s, 256/player) and replays them in order; placements that expire are refunded. | c983050a |
| Other sectors | Drops, physics bodies, mobs, ship carry speed, spacer loot and `protect` use the sector at the position instead of the host's sector. | c983050a |
| Old entity layout | Worlds already marked 0.1.5 with entities in `Sectors/` get a zip backup in `Worlds/_backups/` before the in-place move (restore on failure). | c983050a |
| UI scale | Pixel font is never scaled below 1 (800x600/1024x768 are crisp now); above 1280 px it scales in 0.25 steps. | c983050a |
| Network misc | Ship rest state repeated 5 ticks; peers that never join are kicked (30 s / 200 s while logging in); GameSet package built on a worker (no freeze on first join); block states skip untouched asteroid chunks; no item use while a chest change is pending (dup window); "Open to network" hidden for remote clients. | 77453e19, 6889b3fe |
| Engine | WAV files opened read-only (silent sounds on read-only installs); console log bounded (5000) and thread-safe, no log per disposed GL object; archive extraction limits (2 GB / 100k entries, traversal check also on Linux); shader hot reload can't break loading, off in Final; GL disposal every frame + on quit; `Resources` short names only match bare names (the root cause of the GameSet icon mix-up), failed loads not retried every frame; agent API only for local tools; worker pool safe after shutdown. | f52562c7 |
| Mods | RAR zip-slip fixed; archive folder names sanitized, never replace `Default`, only update a folder of the same `ModId`; items case-insensitive; Default fallback for textures/sounds (missing place sound crashed); one bad blueprint no longer empties crafting; `ModId` falls back to the folder name; warnings for duplicate GameSet ids and extensions patching each other. | e6a02fcc |
| Game | Unreadable player save no longer mixes start items with half-loaded items (kept as `localplayer.unreadable_*.json`); pickup into inventory with a chest open; static UI references cleared between worlds; duplicate entity load no longer leaves a ghost (gravity/DNA/machines). | cf6080c7 |
| Docs | modding-guide: id rules, global asset names, archive installs, extension limits; spec updated. | a917af6c |

Live-verified: dedicated join, leave-within-1 s saves position, window close saves world + player and exits cleanly, client sees "Server stopped"; LAN host ↔ client block edits both ways, client leave saved on the host; 800x600 menus readable.

## 2. Decisions needed before the release

1. **Mod format — lock it now** (changes later break mods/worlds):
   - **Extension id namespace.** Extension content is saved as `default:<id>` (base namespace). *Recommendation:* keep it and declare it permanent in the guide (already documented as "share the base namespace"); per-extension namespaces would need a world upgrade step later.
   - **Version fields.** Add `FormatVersion` (or `MinGameVersion`) to `config.json` / `pack.json`, and store the GameSet + extension versions in `world.json`. Defaults make it non-breaking. *Recommendation: add before release* (small) — today `Dependencies` is not read at all.
   - **Invalid/duplicate ids.** Currently auto-cleaned/renamed to `_1` (order-dependent, saved into worlds). *Recommendation:* reject such entries with an error in the next version; it is documented now.
2. **Cheating / trust.** *Done after this review: the server now counts the items of every survival player (see spec §8 "Item authority"); block hardness/instant mining and `IsAlive` are still client-decided.* Original note: Player JSON (inventory), block placement ids, instant removal, drops and `IsAlive` are trusted from the client. Fine for friends/LAN; public servers need server-side inventory. Decide if 0.1.5 is "trusted players only" (state it in the server guide).
3. **Asteroids as ships.** Any ship part on an asteroid makes it flyable (ShipBody). Refuse ship parts on procedural asteroids, or keep it as a feature?
4. **Fall damage from carried velocity.** Flying into a gravity field at speed counts as a fall. Start the fall from the speed at entry?
5. **Shared texture packs.** Dedicated: `TexturePacks` in `serverconfig.json`. **Hosts share the packs they have enabled** (my choice) — clients see them below their own packs. Alternative: hosts share nothing, or clients get an opt-out.
6. Smaller design calls: mobs eat blocks inside claims; `storage_item_delete` deletes whole stacks without confirmation in survival; container spill drops live 60 s (death drops follow the server rule); furnace/crusher `FuelStorage` exists but is never used; explosions don't damage a docked neighbouring ship.

## 3. Known issues not fixed (ranked)

| Severity | Issue | Effort |
|---|---|---|
| medium | `AudioSource`: every `Play()` starts a monitor thread; `Dispose` joins it on the main thread (up to 100 ms per playing source on scene switch). One shared poll in `AudioDevice.Update` would fix it. | M |
| medium | Unknown block ids in a world (removed/renamed mod content) silently become air on the next save; `WorldValidator` only runs during version upgrades. Run it on every load and warn/ask. | S |
| medium | Saves rewrite every entity touched in the session (chunk `IsModified` never cleared after save) — slower saves on big worlds, no data loss. | S |
| low | Ship mount refused by the server: the client still thinks it pilots (silent); the host bypasses the pilot-seat check. | S |
| low | Client `Shutdown` can fire `Stopped`/`DisconnectedFromServer` twice on a voluntary leave (harmless today). | S |
| low | Explosive whose blast is cancelled by protection keeps durability 0. | S |
| low | Mods only: multi-cell storages/machines keep items per cell; exploding power block can drop itself; generator `Power` > ~16k overflows. | M |
| low | Editor: deleting an entry leaves its blueprint/recipes/textures/order entry; entry-folder `recipes.json` not shown; renaming ids doesn't update references. | M |
| low | Texture packs: HD override drops the base emission map (size mismatch); `Emissions/` inside pack `Blocks/` are read as block textures. | S |
| low | Default ships three empty `Generation/Structures/*.json` and `UI/settings.json` starting with `{ // TODO`. | XS |
| low | Inventory/creative/equipment windows overlap at 4:3 small resolutions (slot size = 6% of height, positions by width) — pre-existing layout, not the font. | S |
| low | `Resources/Textures/white.png` is missing (fallback texture used, error logged at startup). | XS |
| test | `ChatCommandRoutingTests.Run_CapturesOnlyLinesFromTheCommandThread` failed once in a full run, then passed 3×: flaky. | S |
| info | The server simulates/meshes chunks around every remote player → CPU/RAM grow with players spread out. Measure with 8–16 players. | — |
| info | `Spacebox/Build/asset-keep.txt` comment still says `GameSets/<set>/` (protected folder, cosmetic). | XS |

## 4. Modding system verdict

Merge/patch/stack logic, extension resolution, texture packs and the shipped content are consistent (scan of Default + ExampleExtension: no missing textures/sounds/ids/recipes). The folder layout (`Mods/GameSets`, `Mods/Extensions`, `Mods/TexturePacks`) and server sync (GameSet + extensions + shared packs, hashed and cached in `Multiplayer/Mods/<server>/`) work end to end. **You don't need to change existing GameSets, textures or extensions.** Only the decisions in 2.1 should be made before release so the format can stay stable afterwards.

## 5. Release checklist (see the `release` skill)

1. Decide 2.1–2.5; implement the version fields if chosen.
2. Keep a small played 0.1.5 world as fixture before bumping the version (`Spacebox.Tests/Fixtures/Worlds/0.1.5/` already exists — refresh it if the format changed since).
3. Test the folder migration on a **real old install** (copy of a previous release folder with `GameSets/`, `TexturePacks/`, root `serverconfig.json`, `dashboard/`, `client.json`): start once, check `Mods/`, `Multiplayer/`, `Debug/command_history.txt`, and that the server config/admin login survived.
4. Untested here: LAN discovery between two PCs (broadcast), dedicated server on Linux (`start-server.sh`, xvfb), joining over the internet with a real latency, 4+ players.
5. Build packages (`BuildWindows.ps1`, `BuildLinux.ps1`), start the Final build, load a world, check the log for missing textures/sounds; check `--server` headless.
6. Server guide PDF is current (rebuilt with the new folders).

## 6. Worth doing later

- Server-authoritative inventory and block rules (anti-cheat) before public servers.
- Mod-level id renames (`"Renames"` in `config.json`) so mod updates don't destroy placed blocks.
- Extensions for projectiles, mobs, loot and generation; a way to remove base entries.
- Move the game UI to CanvasUI (resolution-independent layouts, validated by the layout tests).
- Client opt-out for server texture packs; show the shared packs in the join screen.
- Dashboard: show players' sectors and server load per area.
