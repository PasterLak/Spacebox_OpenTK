using Engine;
using Engine.Audio;
using Engine.Light;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network;
using Spacebox.Network.Combat;

namespace Spacebox.Game.Player;

public class Projectile : Node3D
{
    public ProjectileParameters Parameters { get; private set; }
    public Vector3 SpawnPosition { get; private set; }

    public Action<Projectile> OnSpawn;
    public Action<Projectile> OnDespawn;

    public static LightPool<PointLight> PointLightsPool;

    private float distanceTraveled = 0;
    private byte currentRicochets = 0;
    private byte currentDamage = 0;
    private byte blocksDestroyedThisShot = 0;
    private bool isHitWritten = false;
    private bool isExplosionWritten = false;

    private LineRenderer lineRenderer;
    private Ray ray;

    private AudioSource ricochetSound;
    private AudioSource hitSound;
    private AudioSource explosionSound;

    private PointLight light;
    private bool useLight = true;
    private LocalAstronaut? astronaut;

    private const int MaxDamageForSound = 100;
    private const float MaxFastForward = 0.5f;
    private const float FastForwardStep = 1f / 60f;
    private const float MinRayLength = 0.05f;

    private bool _isAuthoritative = true;
    private int _ownerPeer;
    private float _rewind;
    private float _speed;
    private SpaceEntity? _ignoredEntity;
    private float _ignoreTime;

    public bool IsAuthoritative => _isAuthoritative;
    public int OwnerPeer => _ownerPeer;

    public bool HasOwer(out LocalAstronaut? owner)
    {
        owner = astronaut;
        return astronaut != null;
    }

    public const float VisualBlendTime = 0.15f;
    public const float MaxVisualOffset = 3f;

    private Vector3 _visualOffset;
    private float _visualBlend;

    public static float VisualOffsetFactor(float blendLeft) => Math.Clamp(blendLeft / VisualBlendTime, 0f, 1f);

    public void SetVisualOrigin(Vector3 muzzleWorld)
    {
        var offset = muzzleWorld - SpawnPosition;
        if (offset.LengthSquared > MaxVisualOffset * MaxVisualOffset) return;

        _visualOffset = offset;
        _visualBlend = VisualBlendTime;
        ApplyVisualOffset();
    }

    public void SetRollReference(Vector3 up)
    {
        lineRenderer.UpReference = up;
    }

    private void ApplyVisualOffset()
    {
        lineRenderer.Position = _visualOffset * VisualOffsetFactor(_visualBlend);
    }

    public Projectile()
    {
        lineRenderer = new LineRenderer();
        lineRenderer.Color = Color4.Blue;
        lineRenderer.Thickness = 0.2f;
        AddChild(lineRenderer);

        if (PointLightsPool == null)
        {
            PointLightsPool = new LightPool<PointLight>(8);
        }
    }

    public Projectile Initialize(Ray ray, ref ProjectileParameters parameters, LocalAstronaut? owner, bool useLight = true)
    {
        return Initialize(ray, ref parameters, owner, Engine.Networking.NetworkManager.Instance.LocalPeerId, true, 0f, useLight);
    }

    public Projectile Initialize(Ray ray, ref ProjectileParameters parameters, LocalAstronaut? owner, int ownerPeer, bool isAuthoritative, float rewind, bool useLight = true)
    {
        _ownerPeer = ownerPeer;
        _isAuthoritative = isAuthoritative;
        _rewind = rewind;
        this.useLight = useLight;
        this.ray = ray;
        this.ray.Length = 1f;
        this.Parameters = parameters;
        _speed = parameters.Speed;
        _ignoredEntity = null;
        _ignoreTime = 0f;

        Enabled = true;
        currentDamage = parameters.DamageBlocks;
        blocksDestroyedThisShot = 0;
        isHitWritten = false;
        isExplosionWritten = false;
        SpawnPosition = ray.Origin;
        Position = ray.Origin;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
        astronaut = owner;
        _visualOffset = Vector3.Zero;
        _visualBlend = 0f;
        lineRenderer.Position = Vector3.Zero;
        lineRenderer.UpReference = Vector3.UnitY;

        SetLineRenderer();
        SetSounds();
        SetLight(parameters.Color3);

        OnSpawn?.Invoke(this);
        return this;
    }

    public void SetCarrier(Vector3 carrierVelocity, SpaceEntity? ignoredEntity, float ignoreTime)
    {
        var velocity = ray.Direction * Parameters.Speed + carrierVelocity;
        float speed = velocity.Length;
        if (speed > 1e-4f)
        {
            ray.Direction = velocity / speed;
            _speed = speed;
            SetLineRenderer();
        }

        _ignoredEntity = ignoredEntity;
        _ignoreTime = ignoreTime;
    }

    private bool RaycastVoxels(out HitInfo hit)
    {
        var ignored = _ignoreTime > 0f ? _ignoredEntity : null;
        return World.SectorAt(ray.Origin).Raycast(ray, out hit, ignored);
    }

    private void SetLight(Vector3 color)
    {
        if (!useLight) return;

        light = PointLightsPool.Take();
        light.Range = 4;
        light.Diffuse = color;
        light.Specular = Vector3.Zero;
        light.Enabled = true;
    }

    private void SetLineRenderer()
    {
        lineRenderer.Thickness = Parameters.Thickness;
        lineRenderer.Color = Parameters.Color;
        lineRenderer.ClearPoints();
        lineRenderer.AddPoint(Vector3.Zero);
        lineRenderer.AddPoint(ray.Direction * Parameters.Length);
    }

    private void SetSounds()
    {
        if (ricochetSound == null)
        {
            ricochetSound = new AudioSource(Resources.Load<AudioClip>("ricochet"));
            ricochetSound.Setup3D(5.0f, 200.0f, 2.0f);
        }
        if (hitSound == null)
        {
            hitSound = new AudioSource(Resources.Load<AudioClip>("hitBlock"));
            hitSound.Setup3D(10.0f, 400.0f, 1.5f);
        }
        if (explosionSound == null)
        {
            explosionSound = new AudioSource(Resources.Load<AudioClip>("arExplosion"));
            explosionSound.Setup3D(20.0f, 800.0f, 0.5f);
        }
    }

    public override void Update()
    {
        if (!Enabled) return;

        base.Update();
        Step(Time.Delta);

        if (_visualBlend <= 0f) return;
        _visualBlend -= Time.Delta;
        ApplyVisualOffset();
    }

    public void FastForward(float seconds)
    {
        float remaining = Math.Clamp(seconds, 0f, MaxFastForward);
        while (remaining > 0f && Enabled)
        {
            float delta = MathF.Min(remaining, FastForwardStep);
            Step(delta);
            remaining -= delta;
        }
    }

    private void Step(float deltaTime)
    {
        float step = _speed * deltaTime;
        if (_ignoreTime > 0f) _ignoreTime -= deltaTime;
        ray.Length = MathF.Max(step, MinRayLength);

        if (CheckPlayerCollision()) return;
        if (CheckDynamicCollision()) return;
        if (CheckVoxelCollision()) return;

        Vector3 movement = ray.Direction * step;
        Position += movement;
        ray.Origin += movement;
        distanceTraveled += step;

        if (useLight)
        {
            light.Position = Position;
        }

        if (distanceTraveled >= Parameters.MaxTravelDistance)
            HandleMaxDistanceReached();
    }

    private bool CheckPlayerCollision()
    {
        if (!PlayerHitboxes.Raycast(ray.Origin, ray.Direction, ray.Length, _ownerPeer, _isAuthoritative ? _rewind : 0f, out var hit)) return false;

        if (RaycastVoxels(out var voxelHit) &&
            Vector3.DistanceSquared(ray.Origin, voxelHit.hitPosition) < hit.Distance * hit.Distance)
            return false;

        ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.Point, Parameters.ID);
        ProcessHitStats(hit.Point);
        astronaut?.PlayerStatistics.RecordEntityDamage(Parameters.Damage);

        if (_isAuthoritative)
            CombatNetwork.ApplyPlayerHit(hit.Peer, _ownerPeer, Parameters.Damage, hit.IsHeadshot);

        Enabled = false;
        OnDespawn?.Invoke(this);
        return true;
    }



    private bool CheckDynamicCollision()
    {
        if (!World.Instance.Physics.Raycast(ray, astronaut?.Body, out var hit)) return false;
        if (hit.Body.Owner is Astronaut || hit.Body.Owner is not IDamageable damageable) return false;

        if (RaycastVoxels(out var voxelHit) &&
            Vector3.DistanceSquared(ray.Origin, voxelHit.hitPosition) < hit.Distance * hit.Distance)
            return false;

        HandleDynamicHit(damageable, hit.Point);
        return true;
    }

    private bool CheckVoxelCollision()
    {
        if (RaycastVoxels(out var hit))
        {
            if (TryTriggerReactiveBlock(hit)) return true;
            if (TryRicochet(hit)) return true;

            HandleVoxelHit(hit);
            return true;
        }
        return false;
    }

    private bool TryTriggerReactiveBlock(HitInfo hit)
    {
        if (!_isAuthoritative || hit.block is not IProjectileReactive reactive || !reactive.OnProjectileHit(hit)) return false;

        ProcessHitStats(hit.hitPosition);
        ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.hitPosition + hit.normalWorld * 0.1f, Parameters.ID);
        Enabled = false;
        OnDespawn?.Invoke(this);
        return true;
    }

    private void HandleMaxDistanceReached()
    {
        Enabled = false;

        if (Parameters.DamageBlocks >= 50)
        {
            PlayExplosionSound(Position);
        }

        OnDespawn?.Invoke(this);
    }

    private void HandleDynamicHit(IDamageable damagable, Vector3 hitPos)
    {
        if (_isAuthoritative) damagable.TakeDamage(this);
        ProcessHitStats(hitPos);
        astronaut?.PlayerStatistics.RecordEntityDamage(Parameters.Damage);

        ProjectileHitEffectsManager.Instance.PlayHitEffect(hitPos, Parameters.ID);

        Enabled = false;
        if (Parameters.DamageBlocks >= 50)
        {
            PlayExplosionSound(Position);
        }
        OnDespawn?.Invoke(this);
    }

    private bool TryRicochet(HitInfo hit)
    {
        if (Parameters.RicochetAngle <= 0 || currentRicochets >= Parameters.PossibleRicochets) return false;

        var angle = Ray.CalculateIncidentAngle(ray, hit.normalWorld);
        if (angle > Parameters.RicochetAngle) return false;

        ray = ray.CalculateRicochetRay(hit.hitPosition, hit.normalWorld, ray.Length);
        PlayRicochetSound(hit.hitPosition);

        if (astronaut != null)
        {
            astronaut.PlayerStatistics.ProjectilesRicocheted++;
        }

        SetLineRenderer();
        currentRicochets++;

        if (currentRicochets == 5)
        {
            ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.hitPosition, Parameters.ID);

            Enabled = false;
            if (Parameters.DamageBlocks >= 50)
            {
                PlayExplosionSound(hit.hitPosition);
            }
            OnDespawn?.Invoke(this);
            return true;
        }

        return true;
    }

    private void HandleVoxelHit(HitInfo hit)
    {
        ProcessHitStats(hit.hitPosition);

        Vector3 effectPos = hit.hitPosition + hit.normalWorld * 0.1f;

        if (!_isAuthoritative)
        {
            if (Parameters.Penetration >= hit.block.Durability)
                ProjectileHitEffectsManager.Instance.PlayHitEffect(effectPos, Parameters.ID);
            else
                ProjectileHitEffectsManager.Instance.PlayNoPenetrationEffect(effectPos);

            Enabled = false;
            OnDespawn?.Invoke(this);
            return;
        }

        if (Parameters.Penetration >= hit.block.Durability)
        {
            byte blockDurability = hit.block.Durability;
            bool destroyed = ApplyBlockDamage(hit);

            if (destroyed)
            {
                if (currentDamage > blockDurability)
                {
                    currentDamage -= blockDurability;
                }
                else
                {
                    currentDamage = 0;
                }

                blocksDestroyedThisShot++;

                if (blocksDestroyedThisShot >= Parameters.MaximumBlocksCanDestroy || currentDamage <= 0)
                {
                    ProjectileHitEffectsManager.Instance.PlayHitEffect(effectPos, Parameters.ID);

                    Enabled = false;
                    if (Parameters.DamageBlocks >= 50)
                    {
                        PlayExplosionSound(hit.hitPosition);
                    }
                    OnDespawn?.Invoke(this);
                }

            }
            else
            {

                ProjectileHitEffectsManager.Instance.PlayHitEffect(effectPos, Parameters.ID);

                Enabled = false;
                if (Parameters.DamageBlocks >= 50)
                {
                    PlayExplosionSound(hit.hitPosition);
                }
                OnDespawn?.Invoke(this);
            }
        }
        else
        {

            ProjectileHitEffectsManager.Instance.PlayNoPenetrationEffect(effectPos);

            Enabled = false;
            if (Parameters.DamageBlocks >= 50)
            {
                PlayExplosionSound(hit.hitPosition);
            }
            OnDespawn?.Invoke(this);
        }
    }
    private bool ApplyBlockDamage(HitInfo hit)
    {
        short blockId = hit.block?.Id ?? 0;
        bool destroyed = hit.chunk.DamageBlock(hit, currentDamage, Parameters.DropBlock);
        if (destroyed) WorldEditNetwork.NotifyRemoved(hit.chunk, hit.blockPositionIndex);
        else WorldEditNetwork.NotifyDamaged(hit.chunk, hit.blockPositionIndex, blockId, currentDamage);

        if (astronaut != null)
        {
            astronaut.PlayerStatistics.BlockDamageDealt += currentDamage;
            if (destroyed)
            {
                astronaut.PlayerStatistics.BlocksDestroyed++;
            }
        }

        if (currentDamage >= 50)
        {
            PlayExplosionSound(hit.hitPosition);
        }

        return destroyed;
    }

    private void ProcessHitStats(Vector3 hitPos)
    {
        if (astronaut != null)
        {
            if (!isHitWritten)
            {
                isHitWritten = true;
                astronaut.PlayerStatistics.ShotsHit++;
            }
        }
        PlayHitAudio(hitPos);
    }

    private void PlayHitAudio(Vector3 pos)
    {
        hitSound.Position = pos;
        var dmg = MathF.Min(Parameters.DamageBlocks, MaxDamageForSound);
        hitSound.SetPitchByValue(MaxDamageForSound - dmg, 0, MaxDamageForSound, 0.5f, 1f);
        hitSound.Play();
    }

    private void PlayRicochetSound(Vector3 pos)
    {
        ricochetSound.Position = pos;
        var dmg = MathF.Min(Parameters.DamageBlocks, MaxDamageForSound);
        ricochetSound.SetPitchByValue(MaxDamageForSound - dmg, 0, MaxDamageForSound, 0.5f, 1f);
        ricochetSound.Play();
    }

    private void PlayExplosionSound(Vector3 pos)
    {
        explosionSound.Position = pos;
        explosionSound.SetPitchByValue(Parameters.MaxTravelDistance + 5 - distanceTraveled, 0, Parameters.MaxTravelDistance, 0.8f, 1f);
        explosionSound.Play();

        if (astronaut != null && !isExplosionWritten)
        {
            isExplosionWritten = true;
            astronaut.PlayerStatistics.ExplosionsCaused++;
        }
    }

    public override void Render()
    {
        if (!Enabled) return;
        base.Render();
        lineRenderer.Render();
    }

    public override void Destroy()
    {
        base.Destroy();

        lineRenderer?.Destroy();
        light?.Destroy();
        hitSound?.Dispose();
        ricochetSound?.Dispose();
        explosionSound?.Dispose();
    }
    public void Reset()
    {
        if (useLight && PointLightsPool != null)
        {
            PointLightsPool.PutBack(light);
        }

        Enabled = false;
        Rotation = Vector3.Zero;
        distanceTraveled = 0;
        currentRicochets = 0;
        currentDamage = 0;
        blocksDestroyedThisShot = 0;
        isHitWritten = false;
        isExplosionWritten = false;
        Scale = Vector3.One;
        ray = new Ray(Vector3.Zero, Vector3.Zero, 0f);
        lineRenderer.ClearPoints();
    }
}