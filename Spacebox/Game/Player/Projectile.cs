using Engine;
using Engine.Audio;
using Engine.Light;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player
{
    public class Projectile : Node3D
    {

        public ProjectileParameters Parameters { get; private set; }

        public Vector3 SpawnPosition { get; private set; }

        public Action<Projectile> OnSpawn;
        public Action<Projectile> OnHit;
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
        private Astronaut? astronaut;


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

        public Projectile Initialize(Ray ray, ref ProjectileParameters parameters, Astronaut? owner, bool useLight = true)
        {
            this.useLight = useLight;
            this.ray = ray;
            ray.Length = 1f;
            this.Parameters = parameters;

            Enabled = true;
            currentDamage = parameters.DamageBlocks;
            canRicochet = parameters.RicochetAngle > 0;
            SpawnPosition = ray.Origin;
            Position = ray.Origin;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
            astronaut = owner;

            SetLineRenderer(lineRenderer, ray, ref parameters);
            SetSounds();

            if (useLight)
            {
                light = SetLight(PointLightsPool.Take(), ref parameters);
            }


            OnSpawn?.Invoke(this);

            return this;
        }
        private static void SetLineRenderer(LineRenderer lineRenderer, Ray ray, ref ProjectileParameters parameters)
        {
            lineRenderer.Thickness = parameters.Thickness;
            lineRenderer.Color = parameters.Color;
            lineRenderer.ClearPoints();
            lineRenderer.AddPoint(Vector3.Zero);
            lineRenderer.AddPoint(ray.Direction * parameters.Length);
        }

        private void SetSounds()
        {
            if (ricochetSound == null)
            {

                ricochetSound = new AudioSource(Resources.Load<AudioClip>("ricochet"));
                ricochetSound.Volume = 1f;

                ricochetSound.Setup3D(5.0f, 200.0f, 2.0f);
                ricochetSound.Position = Position;

            }
            if (hitSound == null)
            {

                hitSound = new AudioSource(Resources.Load<AudioClip>("hitBlock"));
                hitSound.Volume = 1f;

                hitSound.Setup3D(10.0f, 400.0f, 1.5f);
                hitSound.Position = Position;

            }
            if (explosionSound == null)
            {

                explosionSound = new AudioSource(Resources.Load<AudioClip>("arExplosion"));
                explosionSound.Volume = 1f;
                explosionSound.Setup3D(20.0f, 800.0f, 0.5f);
                explosionSound.Position = Position;
            }
        }

        private static PointLight SetLight(PointLight light, ref ProjectileParameters parameters)
        {

            light.Range = 4;
            light.Diffuse = parameters.Color3;
            light.Specular = Vector3.Zero;
            light.Enabled = true;

            return light;
        }

        public override void Update()
        {
            if (!Enabled) return;

            base.Update();

            Vector3 movement = ray.Direction * Parameters.Speed * Time.Delta;
            Position += movement;

            ray.Origin += movement;
            distanceTraveled += Parameters.Speed * Time.Delta;

            if (useLight)
                light.Position = Position;

            if (distanceTraveled >= Parameters.MaxTravelDistance)
            {
                Enabled = false;

                if (currentDamage >= 50)
                {
                    explosionSound.Position = Position;

                    explosionSound.SetPitchByValue(Parameters.MaxTravelDistance + 5 - distanceTraveled, 0, Parameters.MaxTravelDistance, 0.8f, 1f);

                    explosionSound.Play();

                    if (astronaut != null)
                    {
                        astronaut.PlayerStatistics.ExplosionsCaused++;
                    }
                }
                OnDespawn?.Invoke(this);
                return;
            }

            if (World.CurrentSector.Raycast(ray, out var hit))
            {
                const int maxDamage = 100;
                if (canRicochet && currentRicochets < Parameters.PossibleRicochets)
                {

                    var angle = Ray.CalculateIncidentAngle(ray, hit.normal);

                    if (angle <= Parameters.RicochetAngle)
                    {
                        ray = ray.CalculateRicochetRay(hit.hitPosition, hit.normal, ray.Length);
                        ricochetSound.Position = hit.hitPosition;


                        var dmg0 = MathF.Min(Parameters.DamageBlocks, maxDamage);
                        ricochetSound.SetPitchByValue(maxDamage - dmg0, 0, maxDamage, 0.5f, 1f);

                        ricochetSound.Position = hit.hitPosition;
                        ricochetSound.Play();
                        if (astronaut != null)
                        {
                            astronaut.PlayerStatistics.ProjectilesRicocheted++;
                        }

                        lineRenderer.ClearPoints();
                        lineRenderer.AddPoint(Vector3.Zero);
                        lineRenderer.AddPoint(ray.Direction * Parameters.Length);
                        currentRicochets++;

                        if (currentRicochets == 5)
                        {

                            explosionSound.Position = hit.hitPosition;
                            explosionSound.Play();
                            if (astronaut != null)
                            {
                                astronaut.PlayerStatistics.ExplosionsCaused++;
                            }
                        }
                        return;
                    }

                }


                OnHit?.Invoke(this);


                var dmg = MathF.Min(Parameters.DamageBlocks, maxDamage);
                hitSound.Position = hit.hitPosition;
                hitSound.SetPitchByValue(maxDamage - dmg, 0, maxDamage, 0.5f, 1f);

                hitSound.Position = hit.hitPosition;

                hitSound.Play();
                if (astronaut != null)
                {
                    astronaut.PlayerStatistics.ShotsHit++;
                }
                ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.hitPosition + hit.normal.ToVector3() * 0.1f, Parameters.ID);


                if (Parameters.Penetration >= hit.block.Durability)
                {
                    if (currentDamage > hit.block.Durability)
                    {

                        hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage, Parameters.DropBlock);
                        if (astronaut != null)
                        {
                            astronaut.PlayerStatistics.BlockDamageDealt += currentDamage;
                            astronaut.PlayerStatistics.BlocksDestroyed++;
                        }
                        Enabled = false;
                        if (currentDamage >= 50)
                        {

                            explosionSound.Position = hit.hitPosition;
                            explosionSound.Play();
                            if (astronaut != null)
                            {
                                astronaut.PlayerStatistics.ExplosionsCaused++;
                            }
                        }
                        OnDespawn?.Invoke(this);

                    }
                    else
                    {
                        hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage, Parameters.DropBlock);
                        if (astronaut != null)
                        {
                            astronaut.PlayerStatistics.BlockDamageDealt += currentDamage;

                        }

                        Enabled = false;
                        OnDespawn?.Invoke(this);
                    }
                }
                else
                {
                    ProjectileHitEffectsManager.Instance.PlayNoPenetrationEffect(hit.hitPosition + hit.normal.ToVector3() * 0.1f);
                    Enabled = false;
                    OnDespawn?.Invoke(this);
                }

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
            if (useLight)
            {
                if (PointLightsPool != null)
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
}
