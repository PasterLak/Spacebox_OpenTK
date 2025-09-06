using OpenTK.Mathematics;
using Engine.Audio;
using Engine.Physics;
using Engine.Light;
using Spacebox.Game.Generation;
using Engine;
using Spacebox.Game.Effects;


namespace Spacebox.Game.Player
{
    public class Projectile : Node3D
    {

        private float distanceTraveled = 0;


        private byte currentRicochets = 0;
        private byte currentDamage = 0;
        private bool canRicochet = false;

        public ProjectileParameters Parameters { get; set; }
        private Ray ray;
        private bool _isActive = false;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        private LineRenderer lineRenderer;
        private Camera camera;

        public Vector3 SpawnPosition { get; private set; }

        public Action<Projectile> OnSpawn;
        public Action<Projectile> OnHit;
        public Action<Projectile> OnDespawn;

        public static AudioSource ricochetSound;
        public AudioSource hitSound;
        public static AudioSource explosionSound;

        private PointLight light;
        private bool useLight = true;
        public Projectile()
        {
            lineRenderer = new LineRenderer();
            lineRenderer.Color = Color4.Blue;
            lineRenderer.Thickness = 0.2f;
            AddChild(lineRenderer);

        }

        public Projectile Initialize(Ray ray, ProjectileParameters parameters, bool useLight = true)
        {
            this.useLight = useLight;
            this.ray = ray;
            ray.Length = 1f;
            this.Parameters = parameters;
            IsActive = true;
            currentDamage = parameters.DamageBlocks;
            canRicochet = parameters.RicochetAngle > 0;
            SpawnPosition = ray.Origin;
            Position = ray.Origin;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            lineRenderer.Thickness = parameters.Thickness;
            lineRenderer.Color = parameters.Color;
            lineRenderer.ClearPoints();
            lineRenderer.AddPoint(Vector3.Zero);
            lineRenderer.AddPoint(ray.Direction * parameters.Length);

            camera = Camera.Main;

            if (ricochetSound == null)
            {

                ricochetSound = new AudioSource(Resources.Load<AudioClip>("ricochet"));
                ricochetSound.Volume = 1f;

            }
            if (hitSound == null)
            {

                hitSound = new AudioSource(Resources.Load<AudioClip>("hitBlock"));
                hitSound.Volume = 1f;

            }
            if (explosionSound == null)
            {

                explosionSound = new AudioSource(Resources.Load<AudioClip>("arExplosion"));
                explosionSound.Volume = 1f;

            }

            if (useLight)
            {
                light = PointLightsPool.Instance.Take();

                light.Range = 4;
                light.Diffuse = parameters.Color3;
                
                light.Specular = Vector3.Zero;
                light.Enabled = true;
            }


            OnSpawn?.Invoke(this);

            return this;
        }

        public void Update()
        {
            if (!_isActive) return;

            Vector3 movement = ray.Direction * Parameters.Speed * Time.Delta;
            Position += movement;

            ray.Origin += movement;
            distanceTraveled += Parameters.Speed * Time.Delta;

            if (useLight)
                light.Position = Position;

            if (distanceTraveled >= Parameters.MaxTravelDistance)
            {
                IsActive = false;

                if (currentDamage >= 50)
                {
                    explosionSound.SetVolumeByDistance(Vector3.Distance(camera.Position, this.Position), 200);
                    explosionSound.Play();
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
                        ray = ray.CalculateRicochetRay(hit.position, hit.normal, ray.Length);
                        ricochetSound.SetVolumeByDistance(Vector3.Distance(camera.Position, this.Position), 100);

                        
                        var dmg0 = MathF.Min(Parameters.DamageBlocks, maxDamage);
                        ricochetSound.SetPitchByValue(maxDamage - dmg0, 0, maxDamage, 0.5f, 1f);

                        ricochetSound.Play();
                        lineRenderer.ClearPoints();
                        lineRenderer.AddPoint(Vector3.Zero);
                        lineRenderer.AddPoint(ray.Direction * Parameters.Length);
                        currentRicochets++;

                        if (currentRicochets == 5)
                        {
                            explosionSound.SetVolumeByDistance(Vector3.Distance(camera.Position, this.Position), 200);
                            explosionSound.Play();
                        }
                        return;
                    }

                }


                OnHit?.Invoke(this);
               
                hitSound.SetVolumeByDistance(Vector3.Distance(camera.Position, this.Position), 100);

            
                var dmg = MathF.Min(Parameters.DamageBlocks, maxDamage);
                hitSound.SetPitchByValue(maxDamage - dmg, 0, maxDamage, 0.5f, 1f);
              
                hitSound.Play();
                ProjectileHitEffectsManager.Instance.PlayHitEffect(hit.position + hit.normal.ToVector3() * 0.1f, Parameters.ID);

                if (currentDamage > hit.block.Durability)
                {

                    hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage, Parameters.DropBlock);

                    IsActive = false;
                    if (currentDamage >= 50)
                    {
                        explosionSound.SetVolumeByDistance(Vector3.Distance(camera.Position, this.Position), 200);
                        explosionSound.Play();
                    }
                    OnDespawn?.Invoke(this);

                }
                else
                {
                    hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage, Parameters.DropBlock);

                    IsActive = false;
                    OnDespawn?.Invoke(this);
                }
            }
        }
   
        public void Render()
        {
            if (!_isActive) return;
            lineRenderer.Render();
        }

        public void Reset()
        {
            if (useLight)
            {
                if (PointLightsPool.Instance != null)
                    PointLightsPool.Instance.PutBack(light);
            }

            IsActive = false;
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
