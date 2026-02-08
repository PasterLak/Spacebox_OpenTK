using Engine;
using Spacebox.Client;
using Spacebox.Game.Player;
using SpaceNetwork;

namespace Client
{
    public class NetworkPlayerRegistry
    {
        private readonly Dictionary<int, RemoteAstronaut> _remotePlayers = new Dictionary<int, RemoteAstronaut>();
        private readonly int _localPlayerId;

        public event Action<RemoteAstronaut> OnPlayerJoined;
        public event Action<RemoteAstronaut> OnPlayerLeft;

        public NetworkPlayerRegistry(int localPlayerId)
        {
            _localPlayerId = localPlayerId;
        }

        public void AddOrUpdate(Player p)
        {
            if (p.ID == _localPlayerId) return;


            if (_remotePlayers.TryGetValue(p.ID, out var remote))
            {

                if (remote.TryGetComponent<NetworkNode3DComponent>(out var netTransform))
                {
                    netTransform.ReceiveState(p.Position.ToOpenTKVector3(), new OpenTK.Mathematics.Quaternion(p.Rotation.X, p.Rotation.Y,p.Rotation.Z,p.Rotation.W));
                }

                remote.UpdateNetworkData(p);
            }
            else
            {
                remote = new RemoteAstronaut(p);
                _remotePlayers[p.ID] = remote;
                OnPlayerJoined?.Invoke(remote);
            }
        }

        public void Remove(int id)
        {
            if (_remotePlayers.TryGetValue(id, out var remote))
            {
                OnPlayerLeft?.Invoke(remote);
                _remotePlayers.Remove(id);
            }
        }

        public bool TryGet(int id, out RemoteAstronaut player)
        {
            return _remotePlayers.TryGetValue(id, out player);
        }

        public List<RemoteAstronaut> GetAll() => _remotePlayers.Values.ToList();

        public void SyncFromList(Dictionary<int, Player> currentPlayers)
        {
            foreach (var kvp in currentPlayers)
            {
                AddOrUpdate(kvp.Value);
            }

            var idsToRemove = _remotePlayers.Keys
                .Where(k => !currentPlayers.ContainsKey(k))
                .ToList();

            foreach (var id in idsToRemove)
            {
                Remove(id);
            }
        }
    }
}