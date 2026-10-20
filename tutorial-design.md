# Tutorial system — design & content

## Context

New players start a survival world with a drill, furnace, crafting table and flashlight, and nothing tells them what to do. We want a 7 Days to Die style guided tutorial:
- a compact quest panel in the top-right corner;
- tasks with live counters, a progress bar, and finished tasks green and crossed out;
- small "step complete" moments that make learning feel like a game.

The tutorial should be:
- **data-driven:** JSON in the GameSet, because tasks name GameSet items;
- **easy to write:** many options, item icons;
- **optional:** a toggle on the welcome screen; the player can stop it at any time;
- **long:** first steps (moving, keys, blocks, magnet, resources, crafting, the next drill) lead on to machines, survival, base building and ships.

After approval, the first implementation step copies this document into the repo as `tutorial-design.md`, like `ships-design.md`.

---

## 1. Player experience

### Welcome screen
`WelcomeUI` (shown once for a new local world) gets a checkbox **"Play tutorial"**: on by default in survival, off in creative. It is the player's choice; "Start" applies it.

Multiplayer joiners never see the welcome screen. A new player without saved tutorial state gets a small card in the tutorial spot: "New here? Start the tutorial  [Yes] [No]".

### Panel (top-right, compact, about 22% of the screen width)
```
┌───────────────────────────────────┐
│ ★ GETTING STARTED          3 / 9  │  chapter title + step counter
│ ████████████░░░░░░░░░░░░░░░       │  chapter progress bar
│ ⛏ Mine your first ore             │  step title (+ optional icon)
│ Hold LMB with the drill on an     │  short text, keys from the
│ ore block. Ores glow in color.    │  real bindings ({key:...})
│ ✔ ̶E̶q̶u̶i̶p̶ ̶t̶h̶e̶ ̶d̶r̶i̶l̶l̶          (green)   │  done = green + strike-through
│ ☐ [icon] Aluminium ore     2 / 5  │  item icon + live counter
│ ☐ [icon] Cobaltite         0 / 3  │
│ Hint: (J) collapse · Esc→Tutorial │  footer only while collapsed or for the first 10 s
└───────────────────────────────────┘
```
- **Collapsed mode** (toggle key, default `J`): one line, "★ 3/9 Mine your first ore 2/5".
- **On task complete:** the row flashes green, the strike-through animates left to right, and a soft "tick" sound plays.
- **On step complete:** the panel pulses and a short toast "Step complete! +reward" slides in under the panel (about 2 s). Then the next step fades in.
- **On chapter complete:** a larger centred toast ("Chapter complete: Resources", with its icon) and a jingle.
- **Visibility:** hidden while `Settings.ShowInterface` is false, when the death screen or loading screen is up, and while a full-screen UI is open. While the inventory, crafting or processing window is open it shows dimmed, because those windows are often what the step asks for.
- **Position:** it moves below the chat if the chat is docked on the right and open.
- **Style:** existing `Theme` colours and `GameMenu.DrawElementColors` frame; item icons via `Item.Icon` (`AtlasSprite.Draw`).

### Control
- **Pause menu:** a "Tutorial" entry with Resume, Collapse, **Stop tutorial** (with confirm), Restart, and Skip step (only for steps marked `Skippable`).
- **Leaving early:** stopping keeps the progress, so "Resume" continues where the player left.

---

## 2. Architecture (all new code in `Spacebox/Game/Tutorial/`, pure logic testable without GL)

| Piece | Responsibility |
|---|---|
| `TutorialJSON` DTOs (`TutorialJSON`, `ChapterJSON`, `StepJSON`, `TaskJSON`, `RewardJSON`) | file format, every field has a default (old or partial files load) |
| `TutorialLoader` | loads `GameSets/<set>/Tutorials/*.json` (falls back to Default like the other loaders); validates ids and item/block references with `Debug.Warning`; called from `GameSetLoader.Load` after items and crafting |
| `TutorialDefinition` / `TutorialChapter` / `TutorialStep` / `TutorialTaskDefinition` | resolved, immutable model (item ids → `Item`) |
| `ITutorialTask` + `TutorialTaskFactory` | task runtime: `Start(ctx)`, `Update(ctx, dt)`, `OnActivity(in PlayerActivityEvent)`, `Current`/`Target`, `IsDone`, `Label`. The factory maps the JSON `Type` string → constructor (same pattern as `BlockFactory` / `MobBrainFactory`); a new task type is a new class + one registration line |
| `PlayerActivity` (`Game/Player/`) | static hub `event Action<PlayerActivityEvent>`; the struct holds `Kind`, `Id`, `Count`. Raised at the call sites that already record statistics. Reusable later for achievements |
| `ITutorialContext` + `LocalTutorialContext` | everything tasks may query: item counts (hotbar + inventory + equipment), stats, `UIManager.IsOpen`, input actions, piloting state, gravity, flashlight, game mode. Tests use a fake |
| `TutorialRunner` | state machine: active tutorial/chapter/step, creates tasks, advances, gives rewards, `Stop`/`Resume`/`Restart`/`Skip`, raises events for the UI |
| `TutorialProgress` | save DTO in `PlayerData.Tutorial` (nullable, so old saves load): `TutorialId`, `Enabled`, `Collapsed`, `ChapterIndex`, `StepIndex`, completed step ids, per-task progress |
| `TutorialText` | token replacement in titles and texts: `{key:forward}` → the current binding (`InputBinding.GetDisplayName`), `{item:aluminium_ore}` → item name, `{block:furnace}`, `{count}`/`{target}` |
| `TutorialUI` + `TutorialToast` | static UI (`Init`/`OnGUI`/`Dispose`, wired in `BaseSpaceScene`), sounds on `AudioChannel.UI` |

### Activity events to raise (one line each, next to existing statistics code)
| Kind | Call site |
|---|---|
| `BlockPlaced` (block id) | `InteractionPlaceBlock` (the `BlocksPlaced++` lines) |
| `BlockDestroyed` (block id) | `BlockDestroyer.Destroy` |
| `ItemPickedUp` (item id, n) | `DropManager` pickup (`ItemsPickedUp +=`) |
| `ItemCrafted` (item id, n) | `CraftingGUI` (`ItemsCrafted +=`) |
| `ItemProcessed` (output id, n, machine type) | the `AddProcessedItems` call site |
| `ItemUsed` (item id) | `LocalAstronaut.NotifyItemUsed` (already exists) |
| `ItemEquipped` (item id) | `PlayerEquipment.OnChanged` diff |
| `ActionPressed` (action name) | `TutorialRunner` polls `Input.IsActionDown` only for the actions current tasks need (no global hook) |
| `MagnetToggled`, `FlashlightToggled` | `BlockSelector` magnet toggle, flashlight toggle |
| `ConsoleMounted`, `ShipBound`, `ShipCameraUsed`, `CannonFired` | `ShipPilot.Mounted`, `ShipBindingTool`, `ShipPilot.IsUsingCamera`, `ShipWeapons` |
| `BlockUsed` (block id) | `InteractiveBlock` use (open furnace, crafting table, storage …) |
| `WaypointCreated` | `TagCommand` / tag creation |

### Task types (JSON `Type`)
| Type | Options | Completes when |
|---|---|---|
| `press` | `Action`, `Count` | the action is pressed N times |
| `hold` | `Action`, `Seconds` | the action is held for X seconds in total |
| `move` | `Distance` | the player travels X blocks after the step starts |
| `look` | `Degrees` | the camera turns X degrees in total |
| `place_block` | `Block?`, `Count` | blocks are placed (any block, or one id) |
| `destroy_block` | `Block?`, `Count` | blocks are mined |
| `pickup` | `Item`, `Count` | the item is picked up |
| `have_item` | `Item`, `Count` | the inventory contains it (polled; good for "get 5 ore") |
| `craft` | `Item`, `Count` | the item is crafted |
| `process` | `Item`, `Machine?`, `Count` | the output is taken from a furnace or crusher |
| `use_item` | `Item?` | a consumable is used |
| `create_waypoint` | `Count` | a waypoint is added in the waypoint window (M) or with `tag create <name>` |
| `equip` | `Item?`, `Slot?` | equipment is equipped |
| `open_ui` | `Window` (`inventory`, `crafting`, `pause` …) | the window is open |
| `use_block` | `Block` | that block's UI is opened |
| `select_hotbar` | `Item` | the item is in the selected hotbar slot |
| `toggle` | `What` (`magnet`, `flashlight`) + `State` | the state matches |
| `stat` | `Stat` (property name of `PlayerStatistics`), `Delta` | the stat grows by Delta during the step |
| `pilot` | `Flight?` | the player sits at a console (flight or weapon) |
| `bind_ship_part` | `Count` | ship parts are bound |
| `ship_camera` | — | the player views through a ship camera |
| `ship_travel` | `Distance` | `DistanceInShips` grows |
| `enter_gravity` | — | the player stands in a gravity field |
| `wait` | `Seconds` | auto-continue (for pure "read me" steps) |
| `all_of` / `any_of` | `Tasks: [...]` | composite (e.g. "mine aluminium **or** cobaltite") |

Adding a type: a class implementing `ITutorialTask` + `TutorialTaskFactory.Register("name", json => new X(json))`.

### JSON format (`GameSets/Default/Tutorials/basics.json`)
```json
{
  "Id": "basics",
  "Name": "Spacebox basics",
  "GameModes": ["Survival"],
  "Order": 0,
  "Chapters": [
    {
      "Id": "first_steps",
      "Title": "First steps",
      "Icon": "aluminium_drill",
      "Reward": [{ "Item": "power", "Count": 2 }],
      "Steps": [
        {
          "Id": "move",
          "Title": "Learn to fly",
          "Text": "You float in zero gravity. Use {key:forward}{key:left}{key:backward}{key:right} to move, {key:up}/{key:down} to rise and sink.",
          "Icon": "ui:keyboard",
          "Tasks": [
            { "Type": "press", "Action": "forward", "Label": "Fly forward ({key:forward})" },
            { "Type": "press", "Action": "up", "Label": "Fly up ({key:up})" },
            { "Type": "move", "Distance": 20, "Label": "Travel {target} blocks" }
          ],
          "Hint": "Hold {key:sprint} to fly faster.",
          "Skippable": true
        }
      ]
    }
  ]
}
```
More step options:
- `Optional` (the step can be skipped);
- `RequiresGameMode`;
- `Delay` before the step shows;
- `CompleteSound`;
- `Reward` per step or per chapter (`[{Item, Count}]`, given to the hotbar/inventory, dropped if full);
- `HideTasksUntilStart`.

Task options:
- `Label` (supports tokens);
- `Icon` (an item id, or `ui:<UIAtlas name>`; it defaults to the task's item);
- `Optional` (shown as "bonus", not required).

`Icon` resolution:
- an item or block id → `Item.Icon`;
- `ui:name` → `UIAtlas.Get(name)`;
- none → no icon.

### Integration points (files to touch)
- `Spacebox/Game/GUI/Screens/WelcomeUI.cs`: "Play tutorial" checkbox → `TutorialRunner.Start(...)`.
- `Spacebox/Game/Player/PlayerSaveLoadManager.cs`: `PlayerData.Tutorial`, save and load.
- `Spacebox/Scenes/BaseSpaceScene.cs`: `TutorialUI` Init/OnGUI/Dispose; runner update in `Update`; reset in `UnloadContent`.
- `Spacebox/Game/Resource/Loaders/GameSetLoader.cs`: `TutorialLoader.Load`.
- `Spacebox/Game/GUI/Screens/PauseUI.cs`: the Tutorial menu.
- Activity call sites listed above.
- `Resources/default_input.json`: the new `tutorial` action (key J). Check during implementation that existing user configs get the new default action (InputPro keeps actions on reload).
- `Spacebox/Editor/Data/GameSetCatalog.cs`: entry type `tutorials` with the DTO schema, so tutorials are editable in the GameSet editor (not done yet; tutorials are edited as JSON files).
- Multiplayer: everything is client-side; progress lives in the player JSON that the server already stores. The GameSet package already ships the whole folder, so remote players get the same tutorial; this will be verified.

### Tests (`Spacebox.Tests/Tutorial/`)
- loader: defaults, unknown types/items → warnings, Default tutorials parse and all ids resolve;
- each task type with a fake context and fake events;
- runner: advance, optional tasks, composite tasks, skip/stop/resume/restart, rewards given once, progress round-trip through JSON (old player save without `Tutorial` still loads);
- `TutorialText` token replacement.

---

## 3. Tutorial content (Default GameSet, survival)

The plan is balanced to the real progression: the aluminium drill (tier 1) mines aluminium, cobaltite, ice and light rock; the furnace needs no power; the cobalt drill needs a drill bit, 2 cobalt ingots and 2 energy cells.

The actual steps are in `GameSets/Default/Tutorials/basics.json`. Rewards build an aluminium armor set, so they never replace something a task asks for.

| # | Chapter | Steps | Reward |
|---|---|---|---|
| 1 | First steps | look around, fly (forward/up/down, 20 blocks), roll, flashlight, controls overlay ({inputOverlay}) | Med kit |
| 2 | Your toolbelt | hotbar, inventory, place the crafting table, rotate + magnet, place the furnace | — |
| 3 | Mining | mine 10 blocks, collect aluminium ore and cobaltite, collect ice | Aluminium helmet |
| 4 | Smelting and crafting | use the furnace (aluminium ingots), smelt cobalt, open the crafting table, energy cells, drill bit, **Cobalt Drill** | Aluminium chest armor |
| 5 | Staying alive | oxygen (water → compressed oxygen → use), power (liquid hydrogen → power → use), craft and equip a power generator, waypoint | Aluminium legs |
| 6 | Iron age and machines | mine iron, basic machine, crusher (craft, place, crush), crate, power grid (power block, cables, light) | Aluminium boots |
| 7 | Better drills | **Iron Drill**, thulium (ore → crusher dust → furnace ingot), thulium lenses and energy cells, **Triple Iron Drill**, titanium and uranium ore | Aluminium shoulders + arms |
| 8 | Build a base *(optional)* | 20 blocks, door and window, oxygen generator, DNA reconstructor | 2× blue light |
| 9 | Your first ship | how ships work (read), 4 thrusters, ship console, take the seat, bind parts, fly 100 blocks | Iron helmet |
| 10 | Armed and dangerous *(optional)* | ship camera, weapon console + cannon | Microchip |
| 11 | Beyond *(optional)* | gravity generator (needs titanium and uranium from chapter 7), radar | — |

**Creative** gets its own short file `creative.json`: fly, creative inventory, eraser, fill, ship building, flight.

---

## Implementation map (done)

| Part | Files |
|---|---|
| Format and model | `Game/Tutorial/TutorialJSON.cs`, `TutorialModel.cs`, `TutorialParser.cs` (pure, warnings list), `TutorialTaskLabels.cs` (default labels) |
| Loading | `TutorialLoader.cs` (`GameSets/<set>/Tutorials/*.json`, falls back to Default, skipped on headless; warns about unknown input actions and stats), `TutorialLibrary.cs` |
| Tasks | `Game/Tutorial/Tasks/` (`ITutorialTask`, `TutorialTask` base with latching progress, `TutorialTasks.cs`, `TutorialTaskFactory.cs`) |
| Runtime | `TutorialRunner.cs` (steps, game-mode filter, rewards once per step/chapter even after restart, skip/stop/resume/restart), `TutorialProgress.cs` (saved in `PlayerData.Tutorial`), `TutorialService.cs` (player glue, rewards, `PlayerActivity` subscription) |
| Game queries | `ITutorialContext.cs`, `LocalTutorialContext.cs` (also resolves `{key:}`/`{item:}`), `PlayerStatReader.cs`, `TutorialKeyNames.cs`, `TutorialText.cs` |
| Events | `Game/Player/PlayerActivity.cs` (block events are converted to the block's item id) |
| UI | `Game/Tutorial/UI/` (`TutorialUI` panel and sounds, `TutorialToasts`, `TutorialMenuUI` in the pause menu, `TutorialPromptUI` for multiplayer joiners, `TutorialDraw` helpers) |
| Content | `GameSets/Default/Tutorials/basics.json` (survival, 11 chapters), `creative.json` |
| Tests | `Spacebox.Tests/Tutorial/` |

### Content rules (checked by tests)
- Rewards are never items that a task of the same tutorial asks for (the Default rewards build an aluminium armor set).
- Every `craft` task has a blueprint, every `process` task a recipe on the given machine, every block task an existing block.
- Ores are only requested after the tutorial crafted (or the start items contain) a drill with a high enough tier.
- Restarting or starting the tutorial again never gives a reward twice (`TutorialProgress.Rewarded` survives restart and `StartOver`).

### Writing a tutorial
1. Add `GameSets/<set>/Tutorials/<name>.json` (format above). Item and block ids are GameSet ids (`aluminium_ore`) or full ids (`default:aluminium_ore`).
2. Keep texts short (2 lines). Use `{key:action}` instead of key names, so remapped keys stay correct.
3. Start the game: warnings about unknown ids, actions or task types are written to the console (`[Tutorial] ...`).

### Adding a task type
1. A class deriving from `TutorialTask` (override `Update` for polled state or `OnActivity` for events; call `SetProgress`/`AddProgress`).
2. `TutorialTaskFactory.Register("my_type", d => new MyTask(d))` or an entry in its dictionary; a default label in `TutorialTaskLabels`.
3. New game state goes into `ITutorialContext` (+ `LocalTutorialContext`, + the test fake); new events into `PlayerActivityKind` and a `PlayerActivity.Raise` at the call site.

## 4. Implementation order
1. Copy this design to `tutorial-design.md`; write the DTOs + loader + model + tests.
2. `PlayerActivity` hub + raise calls; `ITutorialContext`; task types + factory + tests.
3. `TutorialRunner` + `TutorialProgress` save/load + tests.
4. `TutorialUI` (panel, collapse, toasts, strike-through, icons, sounds) + welcome toggle + pause menu + key.
5. Default `Tutorials/basics.json` (chapters 1–10) and `creative.json`; a test that all ids resolve.
6. Multiplayer join prompt; GameSet editor catalog entry; `DONE.txt`, `spec.md`, `Spacebox/CLAUDE.md` ("Where to add things": new tutorial task type / new tutorial).

## Verification
- `dotnet build Spacebox/Spacebox.csproj`, `dotnet test Engine.Tests`, `dotnet test Spacebox.Tests` (new tutorial tests: loader, every task type, runner, save round-trip, Default content ids).
- Manual: new survival world → toggle on → play chapters 1–4 to the cobalt drill. Check counters, green strike-through, toasts and rewards; stop/resume from the pause menu; reload the world (progress kept); load an old world (no tutorial, no errors).
- Multiplayer: host + client, the client gets the join prompt, progress persists after rejoin.
- Headless server starts without loading tutorial UI.
