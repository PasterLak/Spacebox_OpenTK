# Ships: building, flying, guns — design notes

Design for player-built ships in Spacebox. Part 1 describes the target mechanics in detail (physics formulas, blocks, console, binding, boarding, collisions, multiplayer). Part 2 is what Spacebox has today. Part 3 is the implementation plan with work packages an agent can pick up.

---

## Part 1 — Target mechanics

### 1.1 Ships are ordinary space entities

- There is no separate "ship" type. A ship is a `SpaceEntity` that contains thruster blocks.
- Special blocks register themselves with their entity when they are placed and unregister when removed (consoles, thrusters, cannons, cameras, lights).
- **Building:** a placed block joins the closest entity if that entity has a block within 16 units; otherwise a **new entity** is created at the block position. An entity whose block count reaches 0 is removed.
- Placing is refused if a player overlaps the block.
- Basic version: a ship cut in two stays one rigid body. Splitting is a later improvement (WP10).

### 1.2 Rigid-body physics of an entity

State per entity: `Position`, `Velocity` (world space, units/s), `Orientation` (quaternion), `AngularVelocity` (vector in **entity-local** space, rad/s), `Mass`, diagonal `Inertia` (`Vector3`).

Mass properties:
- Every block has a mass from data (default 1, clamp 0.1–100). Chunks keep total mass and a running center of mass.
- Entity inertia = per chunk inertia plus the parallel-axis term per axis (`m · d.x²`, …) around the entity center of mass.
- The transform rotates **around the center of mass**: `world = Position + R · (local − COM)`. When the COM shifts (block added/removed), `Position` moves by the rotated COM delta so the ship does not jump.

Forces (`ApplyForce(pointInEntity, forceInEntity, dt)`):
```
velocity        += rotate(force, orientation) / mass * dt   // linear, world space
torque           = cross(point - COM, force) * dt           // entity space
angularVelocity += torque / inertia (per axis)              // angular, entity space
thrustThisTick   = true
```
Integration, every physics tick:
- Caps: speed ≤ 20 u/s, angular speed ≤ 1.5 rad/s.
- **Passive damping only when no force was applied this tick**: linear −0.25 u/s² (snap to 0 below 0.025), angular −0.03 rad/s² (snap below 0.003). A ship coasts slowly to a stop when nobody holds a key.
- `Position += Velocity·dt`; `Orientation *= axisAngle(normalize(ω), |ω|·dt)` (post-multiply = local axis); renormalize.
- `PointVelocity(worldPoint) = Velocity + R·cross(ω, local − COM)`: velocity of a point on the hull, used when a character leaves the ship and for projectiles fired from it.

### 1.3 Power

- Every ship block has a power value: positive = generator, negative = consumer. Adjacent powered blocks form a network.
- If the network sum ≥ 0 all its blocks are active, otherwise all are inactive. Inactive blocks show the off texture and **do nothing when triggered**. A console that loses power drops its pilot.
- Spacebox's `ElectricNetworkManager` already implements this rule (generation ≥ consumption → `IsActive`).

### 1.4 Trigger blocks (things a console can drive)

Common contract:
- `Trigger(dt)` — called **every tick** while the bound key is held.
- `Release()` — when the key is released or the pilot leaves.
- `Aim(lookRotationInEntity)` — for aimable blocks.
- `Controller` — the console the block is bound to. Removing the block unbinds it.

**Thruster:** data `Force`. Thrust direction = block-local down (nozzle side) rotated by the block orientation. While triggered: `ship.ApplyForce(blockCenterInEntity, thrustDir · Force, dt)`. Because the force acts at the block position, off-center thrusters also create torque — that is how ships turn; no separate gyroscope needed. Flames are drawn for every triggered thruster.

**Cannon:** data `ReloadTime` (ms, ≥ 50), `BarrelLength`, `Projectile`.
- Fires when active and reloaded: direction = block up (barrel axis) rotated by block orientation and by the current aim; muzzle = block center + dir · `BarrelLength`, transformed to world; spawns the projectile; restarts reload.
- Aiming turns the barrel toward where the pilot looks. The barrel is drawn as a separate model with its own transform, not baked into the chunk mesh.
- Projectiles must inherit the ship's point velocity at the muzzle and ignore the firing ship for a short time.

**Camera:** a view point fixed to the ship. Its camera sits at the block's world position, oriented by `blockOrientation · shipOrientation`, so the view moves and rotates with the ship; the pilot's mouse rotates the view inside that frame.

### 1.5 Control console

Data: `Buttons` (1–16 key groups). Each group = `{ Color, Key }`; default colors red, green, light blue, yellow, violet, orange, brown, turquoise, pink, dark blue.
- Bindings: color → set of trigger blocks. A block belongs to at most one group of one console. The camera is a separate single binding.
- **Mount** (use on the console): pilot velocity set to zero, held item cleared, pilot attached to the ship. Use again → unmount, which also releases every bound block.
- **Each tick while mounted:**
  - update the bound camera;
  - for each group i: pressed = key held (or bit i in the network button mask); pressed → set bit i and `Trigger` all blocks of that color; otherwise `Release` them;
  - weapon consoles additionally `Aim` all bound cannons with the pilot's current view rotation.
- While mounted the pilot cannot walk/fly, use tools or open the inventory; the HUD shows a speed indicator instead of the hotbar.
- Pilot view = the console's camera if one is bound and powered, otherwise the pilot's own eyes. Rendering and aiming use that view.

### 1.6 Binding workflow

- **Edit** action on a console gives the player a binding tool for that console. All bound blocks are outlined in their group color, the console itself in blue.
- Left click on a valid block of the **same entity**: unbound → first color; bound → next color; after the last color → unbound (cycle). Valid targets: any trigger block except cameras (weapon console: cannons only).
- **Edit** again on the same console opens a window to assign a key to each color; a button there switches to a camera binding tool (click a camera block to bind/unbind it).

### 1.7 Players on and around ships

- A character has `BoardedEntity`, `LocalPosition`, `LocalVelocity` (entity-local).
- **Boarding with hysteresis:** not boarded → board the entity whose blocks overlap the character sphere scaled ×2.5; boarded → leave when the sphere scaled ×5 no longer touches it.
- On board: local position/velocity = world values transformed into entity space, view orientation made relative to the ship. On leave: `Velocity += PointVelocity(position)` (momentum kept), orientation back to world.
- **Update while boarded:** integrate in entity space, then `Position = ship.ToWorld(LocalPosition)`, `Velocity = ship.DirectionToWorld(LocalVelocity)`. The character moves and rotates rigidly with the ship; the camera turns with it (option: keep the horizon fixed).
- Player thrust (5 u/s² per direction, max 4 u/s) is applied in local space when boarded.
- **Collision character ↔ ship:** sphere vs block boxes in entity space, sub-stepped by relative speed (`|v_char − v_ship| · dt / 0.5`), push-out averaged over touching blocks, velocity into the normal removed; impacts above a speed threshold hurt. When boarded, the push-out changes `LocalPosition`.

### 1.8 Ship ↔ ship / asteroid collisions

- Broad phase: bounding spheres (sweep-and-prune or spatial octree) → candidate pairs.
- Narrow phase: oriented chunk boxes vs chunk boxes, then single blocks.
- Resolution: for every overlapping block pair an impulse along the pair normal using both masses and inertias (`j = −v_rel·n / (1/m1 + 1/m2 + angular terms)`), applied to velocity and angular velocity; then positional correction (overlap = 1.7321 − center distance, split by mass ratio, averaged over contacts). Static asteroids = infinite mass.
- Skip pairs where both bodies are at rest.

### 1.9 Multiplayer

- The server simulates. It sends new entities to players within 512 units and removes them beyond 768. Every network cycle it sends `EntityState(position, orientation, COM, tick)` plus the **active button mask of every console**.
- Clients buffer states and interpolate between the last two (with COM correction: `position + R · (COM_now − COM_snapshot)`). They apply the received button masks to their consoles so thruster flames and turret animations match.
- Pilot input: the client sends the console button bits (and analog look) every tick; the server runs the console update with them.
- Binding, unbinding and key changes are **requests** to the server, which applies them and broadcasts the result. Mount state changes are broadcast.
- Boarded players send `BoardedEntityId`, local position and local velocity instead of world values.

### 1.10 Saving

Per console: position in entity space, color → key list, bound blocks as (color, position), bound camera position. Bindings are restored by cell position after the blocks are loaded.

---

## Part 2 — What Spacebox has today

| Area | Today | Gap for ships |
|---|---|---|
| Entity | `SpaceEntity : SpatialCell`; `PositionWorld` **fixed** (protected set); `Orientation` + `SetOrientation` (rotates around `PositionWorld`); `Mass`, `CenterOfMass` from chunks | No velocity / angular velocity, no integration, no inertia, rotation not around COM |
| Chunks | `Chunk.PositionWorld` and `BoundingBox` stored in **world space** (unrotated entity space); render model = `Translate(chunk − entity) · RotationMatrix · Translate(RenderSpace(entity))` | Must become entity-local so a moving entity does not rebuild chunks |
| Sector | `sectorOctree` (`PointOctree<SpaceEntity>`) keyed by world position; entities belong to one sector | Moving entities need octree updates and sector transfer |
| Math helpers | `ToEntitySpace`, `ToWorldSpace`, `WorldToEntityLocal`, `EntityLocalToWorld`, `DirectionTo…`, `Raycast`/`IsColliding` with world inputs | Good base; must use a dynamic transform |
| Player | `MovementMode` (zero-g, world space), `GravityWalker` (entity-local position + `EntityBoxCollider.Sweep`), kinematic `SphereBody` | Zero-g mode has no boarded frame; `GravityWalker.SyncLocal` re-derives the local position whenever the world position changed externally |
| Moving geometry precedent | `DoorSystem` moves cells, pushes player/bodies/drops along the slide | Same push logic needed for hulls |
| Power | `ElectricNetworkManager` per entity, `IsActive` on blocks | Ready to gate thrusters/cannons/consoles |
| Blocks | `basic_engine`, `basic_cannon` exist in `blocks.json` **without behaviour**; multi-cell blocks, `BlockRotationHelper` directions | Need `Type`s + classes |
| Weapons | `Projectile`, `ProjectilesPool`, `CombatNetwork` (authoritative, lag compensated), `WeaponItem` | Reuse for cannons |
| Network | `NetworkVariable<T>`, `[Rpc]`, `INetMessage`, `EntitySync`, `BlockStateNetwork`, `NetworkTransform` (snapshot interpolation 0.1 s) | Need ship state stream + pilot input |
| Saving | NBT per entity; asteroids save only changed chunks; `IBlockSerializer` per block type | Add velocity, console bindings |
| Coordinates | `RenderSpace` origin rebasing for large worlds | Needed for fast ships |

---

## Part 3 — Implementation plan

Goal: ships built anywhere, thrusters push at their position, consoles bind thrusters/cannons/cameras to keys, players ride ships, server authoritative — plus splitting, velocity-inheriting projectiles and automatic flight controls.

### 3.0 Status

WP1–WP9 are implemented (`Spacebox/Game/Ships/`, `Spacebox/Network/ShipNetwork.cs`, ship blocks in `Game/Generation/Blocks/`, data in GameSet `blocks.json` / `ships.json`). Differences from the plan below:

- WP1 kept chunk world caches: `SpaceEntity.SetPose` shifts them instead of switching chunks to local origins. Rotation stays around `PositionWorld`; `ShipBody` keeps the center of mass fixed while rotating.
- WP4 binding happens from the seat (`ShipBindingTool`), not with a held tool item.
- WP6 uses hull face samples against blocks of nearby entities with substeps instead of block-vs-block SAT.
- Not done yet (WP10): ship splitting, players pushed by ships they are not standing on, damage from collisions.

### 3.1 Principles

- **Entity-local everything.** Chunks, blocks, oxygen, electricity, doors and light already work per entity. Remove the remaining world-space caches so moving an entity is *only* a transform change (no remesh, no relight).
- **One rigid body per entity.** Only player-built entities move; asteroids and generated structures are static (`IsStatic`, infinite mass), so they never drift and collisions with them are one-sided.
- **Data-driven blocks.** New block `Type`s with JSON sections, like `Door`, `Pipe`, `Gravity`.
- **Server simulates, clients interpolate.** Pilot sends input; server integrates; clients render snapshots.
- **Per-entity systems** (like `DoorSystem`, `OxygenSystem`): `ShipPhysics`, `ShipControlSystem` on `SpaceEntity`.

### 3.2 Work packages (in order)

#### WP1 — Movable entity core
- Make the `SpaceEntity` transform dynamic: `Position` (keep `Vector3` + `RenderSpace`, or `Vector3d` if far travel needs it), `Orientation`, rotation **around the center of mass** (§1.2), COM-shift compensation.
- `Chunk`: store `LocalOrigin` (index × 32) instead of `PositionWorld`; bounding boxes in entity-local space; render model = `Translate(LocalOrigin − COM) · R · Translate(RenderSpace.ToRender(Position))`. Audit every use of `Chunk.PositionWorld`, `SpaceEntity.PositionWorld`, `BoundingBox`, `GeometryBoundingBox`, `VoxelPhysics.IsColliding(volume, blocks, PositionWorld, …)`.
- Sector bookkeeping: when an entity moves, update `sectorOctree` (remove/add after moving more than a threshold) and transfer it between sectors (`Sector.TransferEntity`), including the save folder.
- Tests (`Spacebox.Tests`): world↔local round trips with rotation around COM; a COM shift keeps the world positions of blocks unchanged.

#### WP2 — Rigid-body physics (`ShipPhysics`, per entity)
- State and formulas from §1.2. Inertia per chunk: sum of block masses at block centers with the parallel-axis term to the COM, kept incrementally in `Chunk.RecalculateMass`.
- `ApplyForceAtLocal(point, force)` accumulates force and torque for the tick.
- Fixed-step integration on the server (`Time.OnTick` or an own 60 Hz accumulator), semi-implicit Euler, quaternion integration with renormalize.
- Tunables in a GameSet `ships.json` (defaults in code): `MaxSpeed` (20), `MaxAngularSpeed` (1.5), `LinearDamping` / `AngularDamping` applied only without thrust (0.25 / 0.03), optional "inertial dampeners" mode that actively counters drift with thrusters.
- `GetPointVelocity(worldPoint)` for boarding/leaving, drops and projectiles.
- Tests: torque from an off-center force, damping, caps, inertia sums.

#### WP3 — Ship blocks
Common base `ShipTriggerBlock : InteractiveBlock` (or interface `IShipTrigger { void Trigger(float dt); void Release(); void Aim(Quaternion lookInEntity); ShipConsoleBlock Controller; }`), gated by `IsActive` (power).

| Type (`blocks.json` `Type`) | Class | JSON section | Behaviour |
|---|---|---|---|
| `thruster` | `ThrusterBlock` | `Thruster { Force, Power }` | While triggered: `ship.ApplyForceAtLocal(cellCenter, thrustDir·Force)`; `thrustDir` from the block front via `BlockRotationHelper`; flame particles/emission while triggered |
| `shipconsole` | `ShipConsoleBlock` | `Console { Buttons (1–16), Power }` | Mountable seat, key groups, bindings, camera |
| `cannon` | `CannonBlock` | `Cannon { Projectile, ReloadTime, BarrelLength, MaxYaw, MaxPitch, TurnSpeed, Power }` | Fires at the muzzle; aims toward the pilot's view within limits; rotating barrel model |
| `shipcamera` | `ShipCameraBlock` | `ShipCamera { Fov }` | View point for the pilot |

Use the existing `basic_engine` / `basic_cannon` textures for the first data entries. Register types in `BlockFactory`, data classes in `BlocksLoader` (pattern of `DisassemblerBlockData`), crafting + disassembly entries.

#### WP4 — Console, bindings, flight input (`ShipControlSystem`)
- Bindings stored **by cell** in the entity's `ShipControlSystem`: `console cell → { groups[(colorIndex, InputAction/key)], bound trigger cells per group, camera cell }`. Cells survive saves and network; resolve to blocks lazily (like `DoorSystem`).
- **Automatic flight groups:** when a pilot mounts, classify every powered thruster by thrust direction relative to the console facing (forward/back/left/right/up/down) and by torque sign (pitch/yaw/roll). Map `move_forward`, `move_back`, strafe, up/down, roll and mouse look to them automatically. Manual color groups (§1.5) remain for cannons, doors and special thrusters.
- Mount/unmount via `use` on the console (`InteractiveBlock.Use`); the pilot is locked to the seat (position = console cell + seat offset, entity-local), tools disabled, HUD swaps to a ship HUD (speed, angular speed, power, bound groups).
- Binding tool: an `InteractionMode` (`InteractionBindShipBlocks`) entered by the console's edit action; left click cycles groups (§1.6); outlines in group colors (`VisualDebug`-style boxes or a selector mesh). Key assignment window `ShipConsoleUI` (ImGui, static UI wired in `BaseSpaceScene`) with a "bind camera" button.
- Cannon aiming: each tick the console passes the pilot's view rotation (entity space) to bound cannons (`Aim`); cannons turn with `TurnSpeed` and clamp to `MaxYaw` / `MaxPitch` around their front.

#### WP5 — Players aboard
- A **boarded frame** for the local player: `BoardedEntity`, `LocalPosition`, `LocalVelocity`, used by both `MovementMode` (zero-g) and `GravityWalker` (already entity-local — make it read the shared boarded state instead of re-deriving it from the world position each frame).
- Boarding hysteresis and momentum rules from §1.7.
- Each frame: integrate locally, then `world = entity.ToWorld(local)`; camera frame = entity orientation (option: keep the horizon fixed when walking under gravity).
- Collisions: keep `EntityBoxCollider` in entity space; sub-step by relative velocity. `SphereBody` bodies, drops and mobs get the same boarded frame (drops lying in a moving ship must move with it — extend `DropManager` and `Mob` with a boarded entity reference).
- Remote players: `AstronautNetwork` sends `BoardedEntityId` + local position/velocity when boarded so they stay glued to the ship on every client.

#### WP6 — Collisions between entities
- Rules from §1.8. Start with sphere-per-block contacts (distance < 1.7321) and move to oriented boxes if needed.
- Optional block damage above an impact speed (`Chunk.DamageBlock`) and a spark effect.
- Ship vs non-boarded player: push like doors do (`DoorSystem.PushPlayer` logic).

#### WP7 — Cannons and projectiles
- Spawn through the existing authoritative `CombatNetwork` path (server spawns, broadcasts `ProjectileSpawnMessage`).
- Projectile velocity = muzzle direction · speed **+ ship point velocity at the muzzle**; ignore the firing entity for the first meters/ms; reload and power use from JSON; muzzle flash + sound at the block; reuse projectile JSON fields such as `ImpactSphere` / `ImpactExplosionRadius`.

#### WP8 — Multiplayer
- `ShipStateMessage { EntityId, Tick, Position, Orientation, Velocity, AngularVelocity, CenterOfMass }` at 10–20 Hz to players within view distance (reuse the `WorldObservers` radius); clients keep a snapshot buffer and interpolate with the same delay as `NetworkTransform` (0.1 s), extrapolating briefly on packet loss; COM correction as in §1.9.
- Pilot input: `ShipPilotInputMessage { EntityId, ConsoleCell, Buttons (bitmask), Analog (move/roll/look) }` client → server every tick while mounted; the server checks that the sender is the mounted pilot.
- The server sends the active trigger mask per console with the state so flames and turret animations match.
- Mount/unmount, binding and key changes: request → server applies → broadcast (`ShipControlNetwork`), like `DoorNetwork` / `StorageNameNetwork`.
- Block edits on moving ships keep using `WorldEditNetwork` with entity-local cells (already local).

#### WP9 — Saving
- Entity NBT: `velocity`, `angular_velocity`, `is_static` (default: generated asteroids static, player-built dynamic). Old saves load with zero velocity.
- `ShipConsoleSerializer` (`IBlockSerializer`): groups (color index, action/key), bound cells per group, camera cell (§1.10). Cannon reload timer optional.
- Add a `VersionConverter` step only if the chunk position change (WP1) alters the file format; prefer keeping files compatible.

#### WP10 — Later
- **Ship splitting:** after a block removal, flood-fill connectivity (`AirFloodFill`-style pooled buffers) and move disconnected parts into new entities with inherited velocity.
- Docking connectors, landing gear (magnetic lock to an asteroid = temporary static joint).
- Autopilot to tags/waypoints using the automatic flight groups.
- Gyroscope block (pure torque) for ships that turn badly with thrusters only.

### 3.3 Starting values for balancing

| Value | Start | Note |
|---|---|---|
| Max ship speed | 20 u/s | a fast-travel mode could raise it |
| Max angular speed | 1.5 rad/s | |
| Passive linear damping | 0.25 u/s² (only without thrust) | snap to 0 below 0.025 |
| Passive angular damping | 0.03 rad/s² | snap below 0.003 |
| Player thrust / max speed | 5 u/s² / 4 u/s | |
| Block mass | 0.1–100, default 1 | |
| Cannon reload | ≥ 50 ms, default 1000 ms | |
| Console buttons | 1–16 | |
| Boarding radius / leave radius | 2.5× / 5× character radius | |
| Entity send / remove distance (MP) | 512 / 768 units | |
| New entity join distance | closest block ≤ 16 units | |

### 3.4 Risks / things to verify first

- `Chunk.PositionWorld` and world-space bounding boxes are used widely (meshing seeds, generation, LOD, culling, lighting, drops). WP1 is the largest refactor; do it behind tests and keep asteroids static so generation code keeps working with a fixed transform.
- Float precision when ships fly far: rely on `RenderSpace` and keep physics positions relative to the sector.
- Oxygen, electricity, doors and lights already run on entity-local indices — verify none of them caches world positions (`OxygenSystem` uses entity indices; `DoorSystem.Render` uses `ToWorldSpace`).
- Performance: integrating a few dozen dynamic entities is cheap; the collision narrow phase must be limited to nearby pairs and occupied chunks.
