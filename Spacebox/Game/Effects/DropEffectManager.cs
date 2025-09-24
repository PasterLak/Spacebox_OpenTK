using Engine;
using Engine.Audio;
using Engine.Components;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Effects
{
    public class DropEffectManager : Component
    {
        private readonly Pool<Drop> _dropPool;
        private readonly HashSet<Drop> _activeDrops = new HashSet<Drop>();
        private readonly List<Drop> _nearbyDrops = new List<Drop>(32);
        private readonly List<Drop> _movingDrops = new List<Drop>(64);

        private readonly Dictionary<short, ParticleSystem> _particleSystems = new Dictionary<short, ParticleSystem>();
        private readonly AudioSource[] _pickupSounds = new AudioSource[3];
        private readonly Dictionary<Drop, Particle> _dropToParticle = new Dictionary<Drop, Particle>();
        private readonly Astronaut _player;
        private readonly float _maxSpeed;
        private readonly float _moveDistanceSquared;
        private readonly float _pickupDistanceSquared;
        private readonly float _mergeDistanceSquared;
        private readonly float _mergeDistance;

        private PointOctree<Drop> _octree;
        private Random random = new Random();
        public DropEffectManager(
            Astronaut player,
            float maxSpeed = 20f,
            float moveDistance = 2f,
            float pickupDistance = 0.3f,
            float mergeDistance = 1f)
        {
            _player = player;
            _maxSpeed = maxSpeed;
            _moveDistanceSquared = moveDistance * moveDistance;
            _pickupDistanceSquared = pickupDistance * pickupDistance;
            _mergeDistanceSquared = mergeDistance * mergeDistance;
            this._mergeDistance = mergeDistance;
            _octree = new PointOctree<Drop>(8192, Vector3.Zero, 1);

            _dropPool = new Pool<Drop>(
                initialCount: 256,
                initializeFunc: drop => drop,
                onTakeFunc: null,
                resetFunc: drop => drop.Reset(),
                isActiveFunc: drop => drop.IsActive,
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

        public void DropItem(Vector3 position, Vector3 direction, float speed, Item item, int quantity, float pickupDelay = 0.5f, float deceleration = 5f)
        {
            var drop = _dropPool.Take();
            drop.Initialize(position, item, quantity, 60,  pickupDelay);
            drop.Velocity = direction.Normalized() * speed;
            drop.IsMovingToPlayer = false;
            drop.IsThrown = true; 
            drop.Deceleration = deceleration;

            _activeDrops.Add(drop);
            _movingDrops.Add(drop);

            EnsureParticleSystemForItem(item.Id);
            CreateParticle(position, item.Id, drop);
        }

        public void DropStorage(Storage storage, Vector3 position, float speed = 2f, float spreadRadius = 0.1f, float pickupDelay = 2f, float deceleration = 4f)
        {
            if (storage == null || !storage.HasAnyItems()) return;

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

                DropItem(dropPosition, direction, speed, item, totalQuantity, pickupDelay, deceleration);
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

        public void DropBlock(Vector3 position, Color3Byte color, Block block, float pickupDelay = 0f)
        {
            var blockData = GameAssets.GetBlockDataById(block.Id);
            if (blockData.Id == 0 || blockData.Drop.Count <= 0) return;

            var dropPosition = position + Vector3.One * 0.5f;
            var item = blockData.Drop.Item;
            var quantity = blockData.Drop.Count;

            if (TryMergeWithNearbyDrop(dropPosition, item, quantity))
                return;

            var drop = _dropPool.Take();
            drop.Initialize(dropPosition, item, quantity, 30, pickupDelay);

            _activeDrops.Add(drop);
            _octree.Add(drop, dropPosition);

            EnsureParticleSystem(item.Id, blockData, color);
            CreateParticle(dropPosition, item.Id, drop);

          
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
                        nearbyDrop.AddQuantity(quantity, 60);

                        if (_dropToParticle.TryGetValue(nearbyDrop, out var particle))
                        {
                            particle.Position = midPoint;
                        }

                        return true;
                    }
                }
            }

            return false;
        }
        private void EnsureParticleSystem(short itemId, BlockJSON blockData, Color3Byte color)
        {
            if (_particleSystems.ContainsKey(itemId)) return;

            var texture = GameAssets.ItemIcons[itemId];
            var emitter = new PointEmitter()
            {
                LifeMin = 20,
                LifeMax = 20,
                StartSizeMin = 0.2f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.2f,
                EndSizeMax = 0.2f,
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

        private void EnsureParticleSystemForItem(short itemId)
        {
            if (_particleSystems.ContainsKey(itemId)) return;

            var texture = GameAssets.ItemIcons[itemId];
            var emitter = new PointEmitter()
            {
                LifeMin = 20,
                LifeMax = 20,
                StartSizeMin = 0.2f,
                StartSizeMax = 0.2f,
                EndSizeMin = 0.2f,
                EndSizeMax = 0.2f,
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
            particle.StartSize = particle.EndSize = particle.Size = 0.2f;
            particle.ColorStart = particle.ColorEnd = particle.Color = new Vector4(1f);
            particleSystem.PushParticle(particle);

            _dropToParticle[drop] = particle;
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
                drop.Update(Time.Delta);

                if (drop.IsExpired)
                {
                    toRemove.Add(drop);
                    continue;
                }

           
                if (!wasStopped && drop.IsStopped && drop.IsThrown)
                {
                    justStopped.Add(drop);
                    continue;
                }

              
                if (!drop.CanBePickedUp) continue;

                var distanceSquared = (drop.Position - targetPosition).LengthSquared;

                if (ShouldPickup(distanceSquared))
                {
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

        private void HandleDropStopped(Drop drop)
        {
            _movingDrops.Remove(drop);

            if (TryMergeStoppedDrop(drop))
            {
                RemoveDrop(drop);
                return;
            }

            _octree.Add(drop, drop.Position);
            drop.IsThrown = false;
        }

        private bool TryMergeStoppedDrop(Drop stoppedDrop)
        {
            _nearbyDrops.Clear();
            _octree.GetNearbyNonAlloc(stoppedDrop.Position, _mergeDistance, _nearbyDrops);

            foreach (var nearbyDrop in _nearbyDrops)
            {
                if (nearbyDrop.Info.item.Id == stoppedDrop.Info.item.Id && !nearbyDrop.IsMovingToPlayer)
                {
                    var distanceSquared = (nearbyDrop.Position - stoppedDrop.Position).LengthSquared;
                    if (distanceSquared <= _mergeDistanceSquared)
                    {
                        var midPoint = (nearbyDrop.Position + stoppedDrop.Position) * 0.5f;
                        nearbyDrop.Position = midPoint;
                        nearbyDrop.AddQuantity(stoppedDrop.Info.quantity, 60);

                        if (_dropToParticle.TryGetValue(nearbyDrop, out var particle))
                        {
                            particle.Position = midPoint;
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

                if (TryMergeStoppedDrop(drop))
                {
                    return;
                }

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
            }
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
            var quantity = drop.Info.quantity;
            var addedQuantity = 0;

            while (quantity > 0)
            {
                var amountToAdd = Math.Min(quantity, 255);
                if (_player.Panel.TryAddItem(drop.Info.item, (byte)amountToAdd))
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

            if (quantity > 0)
            {
                drop.Info = new Drop.DropInfo { item = drop.Info.item, quantity = quantity };
                StopDropMovement(drop);
            }
            else
            {
                drop.Info = new Drop.DropInfo { item = drop.Info.item, quantity = 0 };
            }
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
            _activeDrops.Remove(drop);

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
                _dropPool.Release(drop);
            }
            _activeDrops.Clear();
            _movingDrops.Clear();
            _dropToParticle.Clear();

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