using Engine;
using Engine.Audio;
using Engine.Components;
using Engine.Light;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Effects
{
    public class DropManager : Component
    {
        private readonly Pool<Drop> _dropPool;
        private readonly HashSet<Drop> _activeDrops = new HashSet<Drop>();
        private readonly List<Drop> _nearbyDrops = new List<Drop>(32);
        private readonly List<Drop> _movingDrops = new List<Drop>(64);

        private readonly Dictionary<short, ParticleSystem> _particleSystems = new Dictionary<short, ParticleSystem>();
        private readonly AudioSource[] _pickupSounds = new AudioSource[3];
        private readonly Dictionary<Drop, Particle> _dropToParticle = new Dictionary<Drop, Particle>();
        private readonly Dictionary<Drop, PointLight> _dropToLight = new Dictionary<Drop, PointLight>();
        private readonly LocalAstronaut _player;
        private readonly float _maxSpeed;
        private readonly float _moveDistanceSquared;
        private readonly float _pickupDistanceSquared;
        private readonly float _mergeDistanceSquared;
        private readonly float _mergeDistance;

        public const int DropLifeTime = 60;
        private const float DropLightInterval = 0.25f;
        private const float PickupRequestTimeout = 1.5f;

        private readonly Dictionary<uint, Drop> _networkDrops = new Dictionary<uint, Drop>();
        private IDropReplicator _replicator;
        private uint _nextNetId = 1;

        public IReadOnlyCollection<Drop> NetworkDrops => _networkDrops.Values;
        private bool IsRemote => _replicator != null && !_replicator.IsAuthority;
        private bool IsAuthority => _replicator != null && _replicator.IsAuthority;

        public IDropReplicator Replicator
        {
            get => _replicator;
            set
            {
                _replicator = value;
                if (!IsAuthority) return;

                foreach (var drop in _activeDrops)
                {
                    if (drop.NetId == 0) AssignNetId(drop);
                }
            }
        }
        private LightPool<PointLight> _lightPool;

        private PointOctree<Drop> _octree;
        private Random random = new Random();
        public DropManager(
            LocalAstronaut player,
            float maxSpeed = 20f,
            float moveDistance = 2f,
            float pickupDistance = 0.3f,
            float mergeDistance = 0.75f)
        {
            _player = player;
            _maxSpeed = maxSpeed;
            _moveDistanceSquared = moveDistance * moveDistance;
            _pickupDistanceSquared = pickupDistance * pickupDistance;
            _mergeDistanceSquared = mergeDistance * mergeDistance;
            this._mergeDistance = mergeDistance;
            _octree = new PointOctree<Drop>(8192, Vector3.Zero, 1);
            _lightPool = new LightPool<PointLight>(8);

            _dropPool = new Pool<Drop>(
                initialCount: 256,
                initializeFunc: drop => drop,
                onTakeFunc: null,
                resetFunc: drop => drop.Reset(),
             
                setActiveFunc: (drop, active) => drop.IsActive = active,
                autoExpand: true
            );
        }

        public override void Start()
        {
            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            for (int i = 0; i < _pickupSounds.Length; i++)
            {
                _pickupSounds[i] = new AudioSource(Resources.Load<AudioClip>("pickupDefault"));
                _pickupSounds[i].Volume = 0.8f;
            }
        }

        public void DropItemSlot(Vector3 position, Vector3 direction, float speed, ItemSlot itemSlot,  float pickupDelay = 0.5f, float deceleration = 5f, SpaceEntity sourceEntity = null)
        {
            DropItem(position, direction, speed, itemSlot.Item, itemSlot.Count, pickupDelay, deceleration, sourceEntity);
        }

        public void DropItem(Vector3 position, Vector3 direction, float speed, Item item, int quantity, float pickupDelay = 0.5f, float deceleration = 5f, SpaceEntity sourceEntity = null)
        {
            if (item == null || quantity <= 0) return;

            var velocity = direction.LengthSquared > 1e-8f ? direction.Normalized() * speed : Vector3.Zero;

            if (IsRemote)
            {
                _replicator.RequestSpawn(item, quantity, position, velocity, pickupDelay, deceleration, sourceEntity);
                return;
            }

            var drop = SpawnItemDrop(position, velocity, item, quantity, pickupDelay, deceleration, sourceEntity);
            RegisterSpawn(drop, velocity, false, default);
        }

        public void SpawnReplicated(uint netId, Item item, int quantity, Vector3 position, Vector3 velocity, float pickupDelay, float deceleration, SpaceEntity sourceEntity, bool fromBlock, Color3Byte color)
        {
            if (item == null || _networkDrops.ContainsKey(netId)) return;

            var drop = fromBlock
                ? SpawnBlockDrop(position, item, quantity, pickupDelay, sourceEntity, color)
                : SpawnItemDrop(position, velocity, item, quantity, pickupDelay, deceleration, sourceEntity);

            drop.NetId = netId;
            _networkDrops[netId] = drop;
        }

        public bool TryGetNetworkDrop(uint netId, out Drop drop)
        {
            return _networkDrops.TryGetValue(netId, out drop);
        }

        public void RemoveReplicated(uint netId)
        {
            if (_networkDrops.TryGetValue(netId, out var drop)) RemoveDrop(drop);
        }

        public void ApplyReplicatedQuantity(uint netId, int quantity, Vector3 position)
        {
            if (!_networkDrops.TryGetValue(netId, out var drop)) return;

            drop.Info = new Drop.DropInfo { item = drop.Info.item, quantity = quantity };
            drop.Position = position;
        }

        public bool TryClaim(uint netId, Vector3? claimerPosition, float maxDistance, out Item item, out int quantity, out Vector3 position)
        {
            item = null;
            quantity = 0;
            position = Vector3.Zero;

            if (!_networkDrops.TryGetValue(netId, out var drop) || !drop.CanBePickedUp) return false;
            if (claimerPosition.HasValue && Vector3.DistanceSquared(claimerPosition.Value, drop.Position) > maxDistance * maxDistance) return false;

            item = drop.Info.item;
            quantity = drop.Info.quantity;
            position = drop.Position;
            RemoveDrop(drop);
            return true;
        }

        public void GrantPickup(Item item, int quantity, Vector3 position)
        {
            int left = AddToPlayer(item, quantity);
            if (left > 0) DropItem(_player.Position, _player.Front, 1f, item, left, 2f, 4f);
        }

        private Drop SpawnItemDrop(Vector3 position, Vector3 initialVelocity, Item item, int quantity, float pickupDelay, float deceleration, SpaceEntity sourceEntity)
        {
            var drop = _dropPool.Take();
            drop.Initialize(position, item, quantity, DropLifeTime, pickupDelay, sourceEntity);

            drop.Velocity = initialVelocity;
            drop._initialVelocity = initialVelocity;
            drop.IsMovingToPlayer = false;
            drop.IsThrown = true;
            drop.Deceleration = deceleration;

            _activeDrops.Add(drop);
            _movingDrops.Add(drop);

            EnsureParticleSystemItem(item.Id);
            CreateParticle(position, item.Id, drop);
            return drop;
        }

        private void RegisterSpawn(Drop drop, Vector3 velocity, bool fromBlock, Color3Byte color)
        {
            if (!IsAuthority) return;

            AssignNetId(drop);
            _replicator.OnSpawned(drop, velocity, fromBlock, color);
        }

        private void AssignNetId(Drop drop)
        {
            drop.NetId = _nextNetId++;
            _networkDrops[drop.NetId] = drop;
        }

        public void DropStorage(Storage storage, Vector3 position, float speed = 2f, float spreadRadius = 0.1f, float pickupDelay = 2f, float deceleration = 4f, SpaceEntity sourceEntity = null)
        {
            if (storage == null || !storage.HasAnyItems() || IsRemote) return;

            var itemGroups = new Dictionary<short, int>();
            foreach (var slot in storage.GetAllSlots())
            {
                if (slot.HasItem && slot.Item != null)
                {
                    if (!itemGroups.TryGetValue(slot.Item.Id, out var count))
                        itemGroups[slot.Item.Id] = 0;
                    itemGroups[slot.Item.Id] += slot.Count;
                }
            }

            foreach (var kvp in itemGroups)
            {
                var itemId = kvp.Key;
                var totalQuantity = kvp.Value;
                var item = GetItemFromSlot(storage, itemId);
                if (item == null) continue;

                var dropPosition = position + new Vector3(
                    (float)(random.NextDouble() * 2.0 - 1.0) * spreadRadius,
                    (float)(random.NextDouble() * 2.0 - 1.0) * spreadRadius,
                    (float)(random.NextDouble() * 2.0 - 1.0) * spreadRadius
                );

                var direction = new Vector3(
                    (float)(random.NextDouble() * 2.0 - 1.0),
                    (float)(random.NextDouble() * 2.0 - 1.0),
                    (float)(random.NextDouble() * 2.0 - 1.0)
                ).Normalized();

                DropItem(dropPosition, direction, speed, item, totalQuantity, pickupDelay, deceleration, sourceEntity);
            }

            storage.Clear();
        }

        private Item GetItemFromSlot(Storage storage, short itemId)
        {
            foreach (var slot in storage.GetAllSlots())
            {
                if (slot.HasItem && slot.Item != null && slot.Item.Id == itemId)
                {
                    return slot.Item;
                }
            }
            return null;
        }

        public void DropBlock(Vector3 position, Color3Byte color, Block block, SpaceEntity entity, float pickupDelay = 0f)
        {
            if (IsRemote) return;

            var blockData = GameAssets.GetBlockDataById(block.Id);
            if (blockData.Id == 0 || blockData.Drop.Count <= 0) return;

            var dropPosition = position + Vector3.One * 0.5f;
            var item = blockData.Drop.Item;
            var quantity = blockData.Drop.Count;

            if (TryMergeWithNearbyDrop(dropPosition, item, quantity))
                return;

            var drop = SpawnBlockDrop(dropPosition, item, quantity, pickupDelay, entity, color);
            RegisterSpawn(drop, Vector3.Zero, true, color);
        }

        private Drop SpawnBlockDrop(Vector3 dropPosition, Item item, int quantity, float pickupDelay, SpaceEntity entity, Color3Byte color)
        {
            var drop = _dropPool.Take();
            drop.Initialize(dropPosition, item, quantity, DropLifeTime, pickupDelay, entity);

            _activeDrops.Add(drop);
            _octree.Add(drop, dropPosition);

            if (item is BlockItem blockItem)
                EnsureParticleSystemBlock(item.Id, GameAssets.GetBlockDataById(blockItem.BlockId), color);
            else
                EnsureParticleSystemItem(item.Id);
            CreateParticle(dropPosition, item.Id, drop);
            return drop;
        }

        private bool TryMergeWithNearbyDrop(Vector3 position, Item item, int quantity)
        {
            _nearbyDrops.Clear();
            _octree.GetNearbyNonAlloc(position, _mergeDistance, _nearbyDrops);

            foreach (var nearbyDrop in _nearbyDrops)
            {
                if (nearbyDrop.Info.item.Id == item.Id && !nearbyDrop.IsMovingToPlayer)
                {
                    var distanceSquared = (nearbyDrop.Position - position).LengthSquared;
                    if (distanceSquared <= _mergeDistanceSquared)
                    {
                        var midPoint = (nearbyDrop.Position + position) * 0.5f;
                        nearbyDrop.Position = midPoint;
                        nearbyDrop.AddQuantity(quantity, DropLifeTime);
                        if (IsAuthority && nearbyDrop.NetId != 0) _replicator.OnQuantityChanged(nearbyDrop);

                        if (_dropToParticle.TryGetValue(nearbyDrop, out var particle))
                        {
                            particle.Position = midPoint;
                        }

                        if (_dropToLight.TryGetValue(nearbyDrop, out var light))
                        {
                            light.Position = midPoint;
                        }

                        return true;
                    }
                }
            }

            return false;
        }
        private void EnsureParticleSystemBlock(short itemId, BlockData blockData, Color3Byte color)
        {
            if (_particleSystems.ContainsKey(itemId)) return;

            var texture = GameAssets.GetItemIconTexture(itemId);
            var emitter = new PointEmitter()
            {
                LifeMin = 20,
                LifeMax = 20,
                ColorStart = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f),
                ColorEnd = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f),
                SpeedMin = 0,
                SpeedMax = 0,
            };

            var particleSystem = new ParticleSystem(new ParticleMaterial(texture), emitter)
            {
                Rate = 0,
                Max = 1000,
                Space = SimulationSpace.World,
                UseManually = true
            };

            particleSystem.Renderer.SetFlip(false, true);
            _particleSystems[itemId] = particleSystem;
        }

        private void EnsureParticleSystemItem(short itemId)
        {
            if (_particleSystems.ContainsKey(itemId)) return;

            var texture = GameAssets.GetItemIconTexture(itemId);
            var emitter = new PointEmitter()
            {
                LifeMin = 20,
                LifeMax = 20,
                ColorStart = new Vector4(1f),
                ColorEnd = new Vector4(1f),
                SpeedMin = 0,
                SpeedMax = 0,
            };

            var particleSystem = new ParticleSystem(new ParticleMaterial(texture), emitter)
            {
                Rate = 0,
                Max = 1000,
                Space = SimulationSpace.World,
                UseManually = true
            };

            particleSystem.Renderer.SetFlip(false, true);
            _particleSystems[itemId] = particleSystem;
        }

        private void CreateParticle(Vector3 position, short itemId, Drop drop)
        {
            if (!_particleSystems.TryGetValue(itemId, out var particleSystem)) return;

            var particle = particleSystem.CreateParticle(position, Vector3.Zero, 20f);
            particle.StartSize = particle.EndSize = particle.Size = drop.IsBlock ? 0.2f : 0.3f;
            particle.ColorStart = particle.ColorEnd = particle.Color = new Vector4(1f);
            particleSystem.PushParticle(particle);

            _dropToParticle[drop] = particle;

            if (drop.Info.item.IsLuminous)
            {
                var light = _lightPool.Take();
                light.Position = position;
                light.Range = 3f;
                light.Diffuse = new Vector3(
                    drop.Info.item.Color.R / 255f,
                    drop.Info.item.Color.G / 255f,
                    drop.Info.item.Color.B / 255f
                );
                light.Specular = Vector3.Zero;
                light.Enabled = true;

                _dropToLight[drop] = light;
            }
        }

        public override void OnUpdate()
        {
            if (_activeDrops.Count == 0) return;

            var targetPosition = _player.Position - _player.Up * 0.3f;
            var toRemove = new List<Drop>();
            var justStopped = new List<Drop>();

            foreach (var drop in _activeDrops)
            {
                var wasStopped = drop.IsStopped;
                drop.Update();

                if (drop.IsExpired && !IsRemote)
                {
                    toRemove.Add(drop);
                    continue;
                }


                if (!wasStopped && drop.IsStopped && drop.IsThrown)
                {
                    justStopped.Add(drop);
                    continue;
                }


                if (!drop.CanBePickedUp || _player.IsFrozen) continue;

                var distanceSquared = (drop.Position - targetPosition).LengthSquared;


                if (ShouldPickup(distanceSquared))
                {
                    if (IsRemote)
                    {
                        RequestPickup(drop);
                        continue;
                    }

                    ProcessPickup(drop);
                    if (drop.Info.quantity == 0)
                    {
                        toRemove.Add(drop);
                    }
                }

                else if (drop.IsStopped && ShouldMoveTowardsPlayer(distanceSquared) && !drop.IsMovingToPlayer)
                {
                    StartMovingToPlayer(drop, targetPosition);
                }
                else if (drop.IsMovingToPlayer)
                {
                    MoveTowardsPlayer(drop, targetPosition);
                }
            }

            foreach (var drop in justStopped)
            {
                HandleDropStopped(drop);
            }

            foreach (var drop in toRemove)
            {
                RemoveDrop(drop);
            }

            UpdateParticleSystems();
        }

        public void PushDrops(Func<Vector3, Vector3?> getPushedPosition, Vector3 velocity)
        {
            foreach (var drop in _activeDrops)
            {
                if (drop.IsMovingToPlayer) continue;

                var pushed = getPushedPosition(drop.Position);
                if (!pushed.HasValue) continue;

                PushDrop(drop, pushed.Value, velocity);
            }
        }

        public void PushDropsFrom(Vector3 center, float radius, float strength)
        {
            float radiusSquared = radius * radius;

            foreach (var drop in _activeDrops)
            {
                if (drop.IsMovingToPlayer) continue;

                var offset = drop.Position - center;
                float distanceSquared = offset.LengthSquared;
                if (distanceSquared >= radiusSquared) continue;

                float distance = MathF.Sqrt(distanceSquared);
                var direction = distance > 1e-3f ? offset / distance : Vector3.UnitY;
                PushDrop(drop, drop.Position, drop.Velocity + direction * (strength * (1f - distance / radius)));
            }
        }

        private void PushDrop(Drop drop, Vector3 position, Vector3 velocity)
        {
            if (drop.IsStopped)
            {
                _octree.Remove(drop);
                if (!_movingDrops.Contains(drop)) _movingDrops.Add(drop);
            }

            drop.Push(position, velocity);
        }

        private void RequestPickup(Drop drop)
        {
            if (drop.NetId == 0) return;

            drop.PickupRequestTimer -= Time.Delta;
            if (drop.PickupRequestTimer > 0f) return;

            drop.PickupRequestTimer = PickupRequestTimeout;
            _replicator.RequestPickup(drop);
        }

        private void HandleDropStopped(Drop drop)
        {
            _movingDrops.Remove(drop);

            if (!IsRemote && TryMergeStoppedDrop(drop))
            {
                RemoveDrop(drop);
                return;
            }

            if (!_octree.GetAll().Contains(drop))
            {
                _octree.Add(drop, drop.Position);
            }
            drop.IsThrown = false;
        }

        private bool TryMergeStoppedDrop(Drop stoppedDrop)
        {
            _nearbyDrops.Clear();
            _octree.GetNearbyNonAlloc(stoppedDrop.Position, _mergeDistance, _nearbyDrops);

            foreach (var nearbyDrop in _nearbyDrops)
            {
                if (nearbyDrop.Info.item.Id == stoppedDrop.Info.item.Id &&
                    !nearbyDrop.IsMovingToPlayer &&
                    nearbyDrop != stoppedDrop)
                {
                    var distanceSquared = (nearbyDrop.Position - stoppedDrop.Position).LengthSquared;
                    if (distanceSquared <= _mergeDistanceSquared)
                    {
                        var midPoint = (nearbyDrop.Position + stoppedDrop.Position) * 0.5f;
                        nearbyDrop.Position = midPoint;
                        nearbyDrop.AddQuantity(stoppedDrop.Info.quantity, 60);
                        if (IsAuthority && nearbyDrop.NetId != 0) _replicator.OnQuantityChanged(nearbyDrop);

                        if (_dropToParticle.TryGetValue(nearbyDrop, out var particle))
                        {
                            particle.Position = midPoint;
                        }

                        if (_dropToLight.TryGetValue(nearbyDrop, out var light))
                        {
                            light.Position = midPoint;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private void StopDropMovement(Drop drop)
        {
            if (drop.IsMovingToPlayer)
            {
                drop.IsMovingToPlayer = false;
                drop.Velocity = Vector3.Zero;
                _movingDrops.Remove(drop);

                drop.PickupDelay = 3f;
                drop.ResetPickupTimer();

                _octree.Add(drop, drop.Position);
            }
        }

        private void StartMovingToPlayer(Drop drop, Vector3 targetPosition)
        {
            drop.IsMovingToPlayer = true;
            _octree.Remove(drop);
            _movingDrops.Add(drop);

            var direction = (targetPosition - drop.Position).Normalized();
            drop.Velocity = direction * _maxSpeed;
        }

        private void MoveTowardsPlayer(Drop drop, Vector3 targetPosition)
        {
            var direction = (targetPosition - drop.Position).Normalized();
            drop.Velocity = direction * _maxSpeed;
        }

        private void UpdateParticleSystems()
        {
            foreach (var particleSystem in _particleSystems.Values)
            {
                particleSystem.Update();
            }

            foreach (var drop in _activeDrops)
            {
                if (_particleSystems.TryGetValue(drop.Info.item.Id, out var particleSystem))
                {
                    UpdateParticlePositions(particleSystem, drop);
                }
            }
        }

        private void UpdateParticlePositions(ParticleSystem particleSystem, Drop drop)
        {
            if (_dropToParticle.TryGetValue(drop, out var particle))
            {
                particle.Position = drop.Position;
                UpdateDropLight(drop, particle);
            }

            if (_dropToLight.TryGetValue(drop, out var light))
            {
                light.Position = drop.Position;
            }
        }

        private static void UpdateDropLight(Drop drop, Particle particle)
        {
            drop.LightSampleTimer -= Time.Delta;
            if (drop.LightSampleTimer > 0f) return;

            drop.LightSampleTimer = DropLightInterval;
            drop.BlockLight = BlockLightSampler.Sample(drop.Position, drop.SourceEntity);
            particle.Color = new Vector4(Vector3.One + drop.BlockLight, 1f);
        }

        public override void OnRender()
        {
            foreach (var particleSystem in _particleSystems.Values)
            {
                particleSystem.Render();
            }
        }

        private bool ShouldPickup(float distanceSquared) => distanceSquared < _pickupDistanceSquared;
        private bool ShouldMoveTowardsPlayer(float distanceSquared) => distanceSquared < _moveDistanceSquared;

        private void ProcessPickup(Drop drop)
        {
            int before = drop.Info.quantity;
            int quantity = AddToPlayer(drop.Info.item, before);

            if (quantity > 0)
            {
                drop.Info = new Drop.DropInfo { item = drop.Info.item, quantity = quantity };
                StopDropMovement(drop);
                if (quantity != before && IsAuthority && drop.NetId != 0) _replicator.OnQuantityChanged(drop);
            }
            else
            {
                drop.Info = new Drop.DropInfo { item = drop.Info.item, quantity = 0 };
            }
        }

        private int AddToPlayer(Item item, int quantity)
        {
            var addedQuantity = 0;

            while (quantity > 0)
            {
                var amountToAdd = Math.Min(quantity, 255);

                if (_player.Panel.TryAddItem(item, (byte)amountToAdd))
                {
                    addedQuantity += amountToAdd;
                    quantity -= amountToAdd;
                }
                else
                {
                    break;
                }
            }

            if (addedQuantity > 0)
            {
                _player.PlayerStatistics.ItemsPickedUp += addedQuantity;
                PlayPickupSound();
            }

            return quantity;
        }
        private void PlayPickupSound()
        {
            var availableSound = FindAvailableSound();
            if (availableSound != null)
            {
                availableSound.Play();
            }
            else
            {
                _pickupSounds[0].Stop();
                _pickupSounds[0].Play();
            }
        }

        private AudioSource FindAvailableSound()
        {
            foreach (var sound in _pickupSounds)
            {
                if (!sound.IsPlaying) return sound;
            }
            return null;
        }



        private void RemoveDrop(Drop drop)
        {
            if (!_activeDrops.Remove(drop)) return;

            if (drop.NetId != 0)
            {
                _networkDrops.Remove(drop.NetId);
                if (IsAuthority) _replicator.OnRemoved(drop);
            }

            if (drop.IsMovingToPlayer)
            {
                _movingDrops.Remove(drop);
            }
            else
            {
                _octree.Remove(drop);
            }

            if (_dropToParticle.TryGetValue(drop, out var particle))
            {
                RemoveParticleFromSystem(particle, drop.Info.item.Id);
                _dropToParticle.Remove(drop);
            }

            if (_dropToLight.TryGetValue(drop, out var light))
            {
                _lightPool.PutBack(light);
                _dropToLight.Remove(drop);
            }

            _dropPool.Release(drop);
        }
        private void RemoveParticleFromSystem(Particle particle, short itemId)
        {
            if (_particleSystems.TryGetValue(itemId, out var particleSystem))
            {
                particleSystem.RemoveParticle(particle);
            }
        }


        public override void OnDetached()
        {
            base.OnDetached();

            foreach (var drop in _activeDrops)
            {
                if (_dropToLight.TryGetValue(drop, out var light))
                {
                    _lightPool.PutBack(light);
                }
                _dropPool.Release(drop);
            }
            _activeDrops.Clear();
            _networkDrops.Clear();
            _replicator = null;
            _movingDrops.Clear();
            _dropToParticle.Clear();
            _dropToLight.Clear();

            foreach (var particleSystem in _particleSystems.Values)
            {
                particleSystem.Destroy();
            }
            _particleSystems.Clear();

            foreach (var sound in _pickupSounds)
            {
                sound?.Dispose();
            }
        }

        private void CreateLoadedDrop(string itemIdString, int quantity, Vector3 position)
        {
            var item = GameAssets.GetItemByFullID(itemIdString);
            if (item == null)
            {
                Debug.Error($"[DropManager.CreateLoadedDrop] Item {itemIdString} not found and skipped");
                return;
            }

            var drop = _dropPool.Take();
            drop.Initialize(position, item, quantity, DropLifeTime);

            _activeDrops.Add(drop);
            _octree.Add(drop, position);

            EnsureParticleSystemItem(item.Id);
            CreateParticle(position, item.Id, drop);
            RegisterSpawn(drop, Vector3.Zero, false, default);
        }

        public void SaveDrops(string filePath)
        {
            if (_activeDrops.Count == 0)
            {
                return;
            }
            var dropData = DropSaveManager.SerializeDrops(_activeDrops);
            DropSaveManager.SaveToFile(dropData, filePath);
        }

        public void LoadDrops(string filePath)
        {
            var dropData = DropSaveManager.LoadFromFile(filePath);
            foreach (var data in dropData)
            {
                CreateLoadedDrop(data.ItemId, data.Quantity, data.Position);
            }
        }

        public void DebugPrintInfo()
        {
            Debug.Log($"=== DROP SYSTEM DEBUG INFO ===");
            Debug.Log($"Active Drops: {_activeDrops.Count}");
            Debug.Log($"Moving Drops: {_movingDrops.Count}");
            Debug.Log($"Pool Available: {_dropPool.AvailableObjects}");
            Debug.Log($"Pool Total: {_dropPool.TotalObjects}");
            Debug.Log($"Particle Systems: {_particleSystems.Count}");
            Debug.Log($"Drop-Particle Mappings: {_dropToParticle.Count}");

            var itemCounts = new Dictionary<short, int>();
            var totalQuantity = 0;

            foreach (var drop in _activeDrops)
            {
                var itemId = drop.Info.item.Id;
                if (!itemCounts.ContainsKey(itemId))
                    itemCounts[itemId] = 0;

                itemCounts[itemId] += drop.Info.quantity;
                totalQuantity += drop.Info.quantity;
            }

            Debug.Log($"Total Items in World: {totalQuantity}");

            foreach (var kvp in itemCounts)
            {
                Debug.Log($"  Item ID {kvp.Key}: {kvp.Value} units");
            }

            foreach (var kvp in _particleSystems)
            {
                Debug.Log($"ParticleSystem {kvp.Key}: {kvp.Value.ParticlesCount} active particles");
            }

            var expiredCount = 0;
            var canPickupCount = 0;

            foreach (var drop in _activeDrops)
            {
                if (drop.IsExpired) expiredCount++;
                if (drop.CanBePickedUp) canPickupCount++;
            }

            Debug.Log($"Drops ready for pickup: {canPickupCount}");
            Debug.Log($"Drops with pickup delay: {_activeDrops.Count - canPickupCount}");
            Debug.Log($"Expired drops (cleanup pending): {expiredCount}");
            Debug.Log($"=== END DEBUG INFO ===");
        }
    }
}