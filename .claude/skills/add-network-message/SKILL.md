---
name: add-network-message
description: Sync something new over the network (multiplayer): a new INetMessage with server and client handling, or an RPC / NetworkVariable on a NetworkBehaviour. Use when the user asks to sync something new over the network or add a packet/message.
---

# Add a network message

The game is client, host and dedicated server in one executable (`Engine/Networking` + `Spacebox/Network`, `spec.md` §8). The server is authoritative.

## 0. Pick the mechanism

- State of a spawned network object (player, mob, spacer) → `NetworkVariable<T>` on its `NetworkBehaviour` (see `AstronautNetwork._flashlight`).
- A call on a network object → `[Rpc(RpcTarget.Server | Owner | Clients | ClientsExceptOwner | All)]` method on a `NetworkBehaviour` (see `Spacebox/Network/Mobs/MobNetwork.cs`; `RequireOwnership`, `Delivery`).
- Everything else (world, blocks, ships, UI) → an `INetMessage` struct, as below.

## 1. Message struct

In the feature's network class (`Spacebox/Network/<Feature>Network.cs`), add a `public struct <Name>Message : INetMessage`:

- `Serialize(NetWriter writer)` and `Deserialize(NetReader reader)` write and read the same fields in the same order (see `ShipShotMessage` in `ShipNetwork.cs`).
- Ids come from the type's full name (`NetMessageRegistry.GetId<T>`), so renaming the type breaks compatibility between versions.

## 2. Register the handler

In the network class's `Register(NetworkManager network)`:

```csharp
network.Messages.Register<NameMessage>(OnName);
```

The handler is `static void OnName(int sender, NameMessage message)`. A new network class is registered in `SpaceboxNetwork.Initialize`.

## 3. Send

- Client → server: `NetworkManager.Instance.SendToServer(message, DeliveryMode.Reliable)`.
- Server → everyone: `SendToClients(message, mode, exceptPeer)`; to one peer: `Send(peer, message, mode)`.
- `DeliveryMode`: `Reliable` for state changes, `Sequenced` or `Unreliable` for frequent updates (aim, poses).
- Check `network.IsServer` before authoritative changes and `network.IsRemoteClient` before sending requests; in single player the local game is the server, so apply locally instead of sending.

## 4. Server handling

The server validates the request (sender owns or may use the target, values in range), changes the state and broadcasts the result. Never trust client data for damage, items or blocks. Use `PlayerRegistry` / `PlayerHitboxes.FindPlayer(peer)` to map peers to players.

## 5. Client handling

Apply broadcast results on clients; skip messages that the local player already applied (`sender`/peer checks). Touch GL or scene objects only on the main thread (`MainThreadDispatcher` for worker results).

## 6. Verify

- `dotnet build Spacebox/Spacebox.csproj`, `dotnet test Engine.Tests`, `dotnet test Spacebox.Tests`.
- Serialization round trips can be unit tested with `NetWriter`/`NetReader` in `Spacebox.Tests/Network/`.
- Ask the user to test with a host (or `--server`) and a second client.
- If players or server admins see the change (commands, config, ports), update `Documentation/server-guide/server-guide.html`.
