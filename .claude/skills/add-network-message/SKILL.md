---
name: add-network-message
description: Add a new multiplayer network message (SpaceNetwork) with client and server handling. Use when the user asks to sync something new over the network or add a packet/message.
---

# Add a network message

## 1. Message class

Create `SpaceNetwork/Messages/<Name>Message.cs` deriving from `BaseMessage`, following `FlashlightMessage.cs`:

- Public fields, a parameterless constructor (required — messages are created with `Activator.CreateInstance`) and optionally a convenience constructor.
- `WriteData(NetOutgoingMessage)` and `Read(NetIncomingMessage)` must write and read the same fields in the same order.
- Include `SenderId` when other clients need to know who sent it.

Registration is automatic: `MessageRegistry` assigns ids to every `BaseMessage` type by reflection. Client and server therefore must be built from the same `SpaceNetwork` version; never register ids by hand.

## 2. Client → send

Add a `Send<Name>(...)` one-liner to `Spacebox/Client/ClientNetwork.cs` next to `SendFlashlightState`, choosing the delivery method:
- `ReliableOrdered` for state changes (blocks, chat, items),
- `UnreliableSequenced` for frequent transform-like updates.

## 3. Server

In `CommonLibrary/MessageProcessor.cs`:
- If the server only relays it to everyone, add it to the `BroadcastRaw` branch (with `BlockDestroyedMessage`, `FlashlightMessage`, …).
- If the server must validate or change state, add its own branch and handler method.

## 4. Client → receive

Add `{ typeof(<Name>Message), Handle<Name> }` to the handler map in `Spacebox/Client/ClientPacketHandler.cs` and implement `Handle<Name>`. Ignore messages whose `SenderId` is the local player when they are broadcast back. Touch GL or scene objects only on the main thread.

## 5. Verify

- `dotnet build Spacebox/Spacebox.csproj` and `dotnet build SpaceServer/SpaceServer.csproj`.
- Ask the user to test with a local `SpaceServer` and two clients.
