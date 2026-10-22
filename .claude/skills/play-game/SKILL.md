---
name: play-game
description: Launch Spacebox and drive the running game through its agent API (HTTP on localhost) to test a change in the real game - load a test world, read player state and the block under the crosshair, move with A* goto, look at points, scan blocks and entities, press keys and input actions, read every ImGui window's text and click buttons by label, run console commands, take screenshots. Use whenever a change should be checked in the running game instead of only by tests.
---

# Play the game through the agent API

The game has a localhost-only HTTP API (`Spacebox/Agent/`, engine side `Engine/Automation/`). It is compiled into Debug, Release and Profile builds, never into Final, and only starts with `--agent`. Drive it with `Tools/agent/game.sh` (curl wrapper, JSON out). Test in **Release**: Debug is too slow.

## Start and stop

```bash
Tools/agent/game.sh start                 # build Release, launch, create/load world "AgentWorld" (Creative), wait until loaded
Tools/agent/game.sh start Test --fresh --mode survival --seed 42   # new world from scratch
Tools/agent/game.sh start --no-build      # skip the build
Tools/agent/game.sh menu                  # launch to the main menu (API on, no world)
Tools/agent/game.sh stop                  # quit and wait for the process to exit
```

- The running game locks `Spacebox/bin/Release/net8.0/Spacebox.exe`: **stop the game before `dotnet build -c Release`**, then start again.
- Worlds are in `Spacebox/bin/Release/net8.0/Worlds/<name>`. `--fresh` deletes and recreates the world.
- Port: 47800 (`SPACEBOX_AGENT_PORT` changes it). `SPACEBOX_AGENT_CONFIG=Debug` uses the Debug build.
- A new world may open the tutorial prompt: `game.sh click "No, thanks"`.
- Don't minimize the game window: ImGui text capture and screenshots need rendered frames.

## Commands

| Command | What it does |
|---|---|
| `state` | scene, `worldReady`, cursor (`Grabbed` = game, `Normal` = UI), open UI windows, player (position, front, yaw/pitch, velocity, game mode, health/power/oxygen, held item), crosshair `target` block, navigation |
| `look` / `lookat x y z` / `turn yaw pitch` / `turnby dyaw dpitch` | crosshair raycast; turn the camera (yaw 0 = -Z, 90 = +X; pitch up positive) |
| `goto x y z [speed]`, `nav`, `navstop` | fly along an A* path through free cells (kinematic, ignores physics; solid goal → nearest free cell, state `partial`) |
| `block x y z`, `blocks [radius] [idFilter]`, `entities [radius]` | block at a point; non-air blocks around the player (counts by id + nearest list with world centers); asteroids/ships nearest first |
| `inv`, `select <0-9>` | hotbar and inventory contents; select hotbar slot |
| `ui [window]`, `uiflat` | all text ImGui drew this frame per window (top window first), with screen rects |
| `canvas [1]`, `winsize <w> <h>` | CanvasUI (retained UI) layers with the headless layout check (issues with control paths and rects; `1` lists every control), and window resizing to check other resolutions; start the UI showcase with `game.sh menu --ui-showcase` |
| `click <text> [window] [index]`, `hover <text>`, `clickxy x y` | click/hover ImGui text (exact match first, then contains); needs a UI open (cursor not grabbed) |
| `key <Key> [tap\|hold\|release] [frames]` | keyboard (`E`, `Escape`, `Tab`, `Enter`, `Space`, `LeftShift`, `F3`, `1`..`9`) |
| `action <name> [tap\|hold\|release] [frames]` | input action from `Resources/default_input.json` (`forward`, `up`, `inventory`, `use`, `block_place`, `block_destroy`, `shoot`, …; list: `get /input/actions`) |
| `mouse [left\|right] [click\|hold\|release] [frames]`, `type <text>`, `release` | mouse buttons (left = drill/destroy), text into the focused ImGui field, release everything |
| `cmd <console command>` | run a debug console command, returns its output (`cmd help`, `cmd "give default:iron_ore 10"`, `cmd "tp x y z"`, `cmd "gm survival"`) |
| `log [since]` | console messages with sequence numbers |
| `shot [path]` | PNG of the next frame incl. UI → prints the path; read it with the Read tool |
| `wait <ms>`, `waitfor world\|idle\|nav [timeoutSec]` | sleep; wait until the world is loaded / injected input finished and not navigating / navigation finished |
| `help`, `get <path> k=v…`, `post <path> k=v…` | full route list with parameters; raw calls |

## Typical flows

```bash
G=Tools/agent/game.sh
$G start --no-build && $G click "No, thanks"
$G blocks 32 iron                            # find ore, take a "center"
$G goto 7082.7 4499.2 7933.3 && $G waitfor nav
$G lookat 7082.7 4499.2 7933.3               # crosshair on it, "usable" tells if it is in reach
$G mouse left hold 120 && $G waitfor idle    # drill it
$G inv
$G action inventory && $G wait 200 && $G ui Inventory && $G key Escape
$G shot                                      # visual check
```

## Notes

- Input goes into OpenTK's keyboard/mouse state at the start of each frame, so game code, InputPro actions and ImGui all see it. A `tap` is down for 1 frame. A UI click takes 3 frames: wait ~200 ms before reading the result.
- `ui` decodes glyphs from ImGui draw data. Image-only buttons (item slots, icons) have no text: use `inv`/`state` for data, `shot` + `clickxy` for clicks. Text clipped at a window edge can be missing.
- `state.target.usable` means the block is within the interaction distance.
- The window doesn't need focus. Moving the real mouse over the game window overrides the injected cursor position.
- New endpoints: add an `IAgentModule` in `Spacebox/Agent/Modules/`, register it in `AgentModuleCatalog`, run game code on the main thread (`router.Get/Post` already do; async handlers use `MainThread.Run`), add a `game.sh` shortcut and a row here.
