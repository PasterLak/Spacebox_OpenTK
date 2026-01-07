using Engine;
using Engine.Audio;
using Engine.Light;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player;

public class Projectile : Node3D
{
    public ProjectileParameters Parameters { get; private set; }
    public Vector3 SpawnPosition { get; private set; }

    public Action<Projectile> OnSpawn;
    public Action<Projectile> OnDespawn;

    public static PointLightsPool PointLightsPool;

    private float distanceTraveled = 0;
    private byte currentRicochets = 0;
    private byte currentDamage = 0;
    private bool canRicochet = false;

    private LineRenderer lineRenderer;
    private Ray ray;

    private AudioSource ricochetSound;
    private AudioSource hitSound;
    private AudioSource explosionSound;

    private PointLight light;
    private bool useLight = true;
    private LocalAstronaut? astronaut;

    private const int MaxDamageForSound = 100;

    public Projectile()
    {
        lineRenderer = new LineRenderer();
        lineRenderer.Color = Color4.Blue;
        lineRenderer.Thickness = 0.2f;
        AddChild(lineRenderer);

        if (PointLightsPool == null)
        {
            PointLightsPool = new PointLightsPool(8);
        }
    }

    public Projectile Initialize(Ray ray, ref ProjectileParameters parameters, LocalAstronaut? owner, bool useLight = true)
    {
        this.useLight = useLight;
        this.ray = ray;
        this.ray.Length = 1f;
        this.Parameters = parameters;

        Enabled = true;
        currentDamage = parameters.DamageBlocks;
        canRicochet = parameters.RicochetAngle > 0;
        SpawnPosition = ray.Origin;
        Position = ray.Origin;
        Rotation = Vector3.Zero;
        Scale = Vector3.One;
        astronaut = owner;

        SetLineRenderer();
        SetSounds();

        if (useLight)
        {
            light = PointLightsPool.Take();
            light.Range = 4;
            light.Diffuse = parameters.Color3;
            light.Specular = Vector3.Zero;
            light.Enabled = true;
        }

        OnSpawn?.Invoke(this);
        return this;
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

        float step = Parameters.Speed * Time.Delta;
        Vector3 movement = ray.Direction * step;

        Position += movement;
        ray.Origin += movement;
        distanceTraveled += step;

        if (useLight)
        {
            light.Position = Position;
        }

        if (distanceTraveled >= Parameters.MaxTravelDistance)
        {
            HandleMaxDistanceReached();
            return;
        }

        if (CheckDynamicCollision()) return;
        if (CheckVoxelCollision()) return;
    }

    private void HandleMaxDistanceReached()
    {
        Enabled = false;
        if (currentDamage >= 50)
        {
            PlayExplosionSound(Position);
        }
        OnDespawn?.Invoke(this);
    }

    private bool CheckDynamicCollision()
    {
        List<Node3D> hitObjects = new List<Node3D>();
        if (World.Instance.RaycastDynamicObjects(ray, out var dist, hitObjects) && hitObjects.Count > 0)
        {
            var spacer = hitObjects[0] as Spacer;
            if (spacer != null)
            {
                HandleDynamicHit(spacer, ray.GetPoint(dist));
                return true;
            }
        }
        return false;
    }

    private void HandleDynamicHit(Spacer spacer, Vector3 hitPos)
    {
        spacer.Hit(this);
        ProjectileHitEffectsManager.Instance.PlayHitEffect(hitPos, Parameters.ID);
        ProcessHitStats(hitPos);

        Enabled = false;
        OnDespawn?.Invoke(this);
    }

    private bool CheckVoxelCollision()
    {
        if (World.CurrentSector.Raycast(ray, out var hit))
        {
            if (TryRicochet(hit)) return true;

            HandleVoxelHit(hit);
            return true;
        }
        return false;
    }

    private bool TryRicochet(HitInfo hit)
    {
        if (!canRicochet || currentRicochets >= Parameters.PossibleRicochets) return false;

        var angle = Ray.CalculateIncidentAngle(ray, hit.normal);
        if (angle > Parameters.RicochetAngle) return false;

        ray = ray.CalculateRicochetRay(hit.hitPosition, hit.normal, ray.Length);
        PlayRicochetSound(hit.hitPosition);

        if (astronaut != null)
        {
            astronaut.PlayerStatistics.ProjectilesRicocheted++;
        }

        SetLineRenderer();
        currentRicochets++;

        if (currentRicochets == 5)
        {
            PlayExplosionSound(hit.hitPosition);
            return true;
        }

        return true;
    }

    private void HandleVoxelHit(HitInfo hit)
    {
        ProcessHitStats(hit.hitPosition);
        ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.hitPosition + hit.normal.ToVector3() * 0.1f, Parameters.ID);

        if (Parameters.Penetration >= hit.block.Durability)
        {
            ApplyBlockDamage(hit);
        }
        else
        {
            ProjectileHitEffectsManager.Instance.PlayNoPenetrationEffect(hit.hitPosition + hit.normal.ToVector3() * 0.1f);
        }

        Enabled = false;
        OnDespawn?.Invoke(this);
    }

    private void ApplyBlockDamage(HitInfo hit)
    {
        hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage, Parameters.DropBlock);

        if (astronaut != null)
        {
            astronaut.PlayerStatistics.BlockDamageDealt += currentDamage;
            if (currentDamage > hit.block.Durability)
            {
                astronaut.PlayerStatistics.BlocksDestroyed++;
            }
        }

        if (currentDamage >= 50)
        {
            PlayExplosionSound(hit.hitPosition);
        }
    }

    private void ProcessHitStats(Vector3 hitPos)
    {
        if (astronaut != null)
        {
            astronaut.PlayerStatistics.ShotsHit++;
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

        if (astronaut != null)
        {
            astronaut.PlayerStatistics.ExplosionsCaused++;
        }
    }

    public override void Render()
    {
        if (!Enabled) return;
        base.Render();
        lineRenderer.Render();
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
        Scale = Vector3.One;
        ray = new Ray(Vector3.Zero, Vector3.Zero, 0f);
        lineRenderer.ClearPoints();
    }
}