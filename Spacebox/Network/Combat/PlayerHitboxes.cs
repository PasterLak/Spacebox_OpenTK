using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Network.Combat;

public readonly struct PlayerHit
{
    public readonly int Peer;
    public readonly Vector3 Point;
    public readonly float Distance;
    public readonly bool IsHeadshot;

    public PlayerHit(int peer, Vector3 point, float distance, bool isHeadshot)
    {
        Peer = peer;
        Point = point;
        Distance = distance;
        IsHeadshot = isHeadshot;
    }
}

public static class PlayerHitboxes
{
    public const float MaxRewind = 0.4f;

    private const float HeadRadius = 0.28f;
    private const float BodyRadius = 0.35f;
    private const float BodyTop = 0.32f;
    private const float BodyBottom = 1.45f;
    private const float HistoryLength = 1f;

    private static readonly Dictionary<int, List<State>> _history = new Dictionary<int, List<State>>();
    private static readonly List<int> _stalePeers = new List<int>();

    public static void Record()
    {
        float now = (float)Time.Total;

        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            var astronautNetwork = networkObject.GetBehaviour<AstronautNetwork>();
            if (astronautNetwork == null || networkObject.Owner is not Astronaut) continue;

            GetAuthoritativeState(astronautNetwork, out var position, out var rotation);

            if (!_history.TryGetValue(networkObject.OwnerPeer, out var states))
            {
                states = new List<State>(64);
                _history[networkObject.OwnerPeer] = states;
            }

            states.Add(new State(now, position, rotation));
            while (states.Count > 2 && now - states[0].Time > HistoryLength) states.RemoveAt(0);
        }

        _stalePeers.Clear();
        foreach (var peer in _history.Keys)
        {
            if (FindPlayer(peer) == null) _stalePeers.Add(peer);
        }
        for (int i = 0; i < _stalePeers.Count; i++) _history.Remove(_stalePeers[i]);
    }

    public static void Clear()
    {
        _history.Clear();
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, float length, int ignorePeer, float rewind, out PlayerHit hit)
    {
        hit = default;
        float best = length;
        bool found = false;
        float time = (float)Time.Total - Math.Clamp(rewind, 0f, MaxRewind);

        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            int peer = networkObject.OwnerPeer;
            if (peer == ignorePeer) continue;

            var astronautNetwork = networkObject.GetBehaviour<AstronautNetwork>();
            if (astronautNetwork == null || !astronautNetwork.IsAlive || networkObject.Owner is not Astronaut astronaut) continue;

            GetState(peer, astronaut, rewind > 0f, time, out var position, out var rotation);
            var up = Vector3.Transform(Vector3.UnitY, rotation);

            if (RaySphere(origin, direction, position, HeadRadius, out float headDistance) && headDistance < best)
            {
                best = headDistance;
                hit = new PlayerHit(peer, origin + direction * headDistance, headDistance, true);
                found = true;
            }

            var top = position - up * BodyTop;
            var bottom = position - up * BodyBottom;
            if (RayCapsule(origin, direction, top, bottom, BodyRadius, out float bodyDistance) && bodyDistance < best)
            {
                best = bodyDistance;
                hit = new PlayerHit(peer, origin + direction * bodyDistance, bodyDistance, false);
                found = true;
            }
        }

        return found;
    }

    public static Astronaut FindPlayer(int peer)
    {
        foreach (var networkObject in NetworkManager.Instance.Spawner.Objects)
        {
            if (networkObject.OwnerPeer == peer && networkObject.Owner is Astronaut astronaut && networkObject.GetBehaviour<AstronautNetwork>() != null)
                return astronaut;
        }
        return null;
    }

    private static void GetAuthoritativeState(AstronautNetwork network, out Vector3 position, out Quaternion rotation)
    {
        if (network.IsOwner && network.Owner is Astronaut astronaut)
        {
            position = astronaut.Position;
            rotation = astronaut.GetRotation();
            return;
        }

        position = network.TargetPosition;
        rotation = network.TargetRotation;
    }

    private static void GetState(int peer, Astronaut astronaut, bool rewind, float time, out Vector3 position, out Quaternion rotation)
    {
        if (rewind && _history.TryGetValue(peer, out var states) && states.Count > 0)
        {
            Sample(states, time, out position, out rotation);
            return;
        }

        position = astronaut.Position;
        rotation = astronaut.GetRotation();
    }

    private static void Sample(List<State> states, float time, out Vector3 position, out Quaternion rotation)
    {
        if (time <= states[0].Time)
        {
            position = states[0].Position;
            rotation = states[0].Rotation;
            return;
        }

        for (int i = 1; i < states.Count; i++)
        {
            if (time > states[i].Time) continue;

            var a = states[i - 1];
            var b = states[i];
            float t = Math.Clamp((time - a.Time) / MathF.Max(b.Time - a.Time, 1e-4f), 0f, 1f);
            position = Vector3.Lerp(a.Position, b.Position, t);
            rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
            return;
        }

        position = states[^1].Position;
        rotation = states[^1].Rotation;
    }

    private static bool RaySphere(Vector3 origin, Vector3 direction, Vector3 center, float radius, out float distance)
    {
        distance = 0f;
        var offset = origin - center;
        float b = Vector3.Dot(offset, direction);
        float c = Vector3.Dot(offset, offset) - radius * radius;
        if (c > 0f && b > 0f) return false;

        float discriminant = b * b - c;
        if (discriminant < 0f) return false;

        distance = MathF.Max(0f, -b - MathF.Sqrt(discriminant));
        return true;
    }

    private static bool RayCapsule(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, float radius, out float distance)
    {
        distance = 0f;
        var axis = b - a;
        var offset = origin - a;

        float axisLengthSquared = Vector3.Dot(axis, axis);
        float axisDotDirection = Vector3.Dot(axis, direction);
        float axisDotOffset = Vector3.Dot(axis, offset);
        float directionDotOffset = Vector3.Dot(direction, offset);
        float offsetLengthSquared = Vector3.Dot(offset, offset);

        float qa = axisLengthSquared - axisDotDirection * axisDotDirection;
        float qb = axisLengthSquared * directionDotOffset - axisDotOffset * axisDotDirection;
        float qc = axisLengthSquared * offsetLengthSquared - axisDotOffset * axisDotOffset - radius * radius * axisLengthSquared;
        float h = qb * qb - qa * qc;

        if (h >= 0f && MathF.Abs(qa) > 1e-6f)
        {
            float t = (-qb - MathF.Sqrt(h)) / qa;
            float y = axisDotOffset + t * axisDotDirection;
            if (t >= 0f && y > 0f && y < axisLengthSquared)
            {
                distance = t;
                return true;
            }
        }

        bool hitA = RaySphere(origin, direction, a, radius, out float distanceA);
        bool hitB = RaySphere(origin, direction, b, radius, out float distanceB);
        if (!hitA && !hitB) return false;

        distance = hitA && hitB ? MathF.Min(distanceA, distanceB) : hitA ? distanceA : distanceB;
        return true;
    }

    private readonly struct State
    {
        public readonly float Time;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public State(float time, Vector3 position, Quaternion rotation)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
        }
    }
}
