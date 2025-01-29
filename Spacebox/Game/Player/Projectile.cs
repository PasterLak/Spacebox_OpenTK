using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;


namespace Spacebox.Game.Player
{
    public class Projectile : Node3D, IPoolable<Projectile>
    {
       
        private float distanceTraveled = 0;
     
   
        private byte currentRicochets = 0;
        private byte currentDamage = 0;
        private bool canRicochet = false;

        private ProjectileParameters parameters;
        private Ray ray;
        private bool _isActive = false;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        private LineRenderer lineRenderer;
        private Camera camera;

        public Action<Projectile> OnSpawn;
        public Action<Projectile> OnHit;
        public Action<Projectile> OnDespawn;

        public static AudioSource ricochetSound;
        public static AudioSource hitSound;
        public static AudioSource explosionSound;


        public Projectile()
        {
            lineRenderer = new LineRenderer();
            lineRenderer.Color = Color4.Blue;
            lineRenderer.Thickness = 0.2f;
            AddChild(lineRenderer);
        }

        public Projectile Initialize(Ray ray, ProjectileParameters parameters)
        {
            this.ray = ray;
            ray.Length = 1f;
            this.parameters = parameters;
            IsActive = true;
            currentDamage = parameters.Damage;
            canRicochet = parameters.RicochetAngle > 0;

            Position = ray.Origin;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            lineRenderer.Thickness = parameters.Thickness;
            lineRenderer.Color = parameters.Color;
            lineRenderer.ClearPoints();
            lineRenderer.AddPoint(Vector3.Zero);
            lineRenderer.AddPoint(ray.Direction * parameters.Length);
            
            camera = Camera.Main;

            if(ricochetSound == null)
            {

                ricochetSound = new AudioSource(SoundManager.GetClip("ricochet"));
                ricochetSound.Volume = 1f;
                
            }
            if (hitSound == null)
            {

                hitSound = new AudioSource(SoundManager.GetClip("hitBlock"));
                hitSound.Volume = 1f;

            }
            if (explosionSound == null)
            {

                explosionSound = new AudioSource(SoundManager.GetClip("arExplosion"));
                explosionSound.Volume = 1f;

            }

            OnSpawn?.Invoke(this);

            return this;
        }

        public void Update()
        {
            if (!_isActive) return;

            Vector3 movement = ray.Direction * parameters.Speed * Time.Delta;
            Position += movement;

            ray.Origin += movement;
            distanceTraveled += parameters.Speed * Time.Delta;

            if (distanceTraveled >= parameters.MaxTravelDistance)
            {
                IsActive = false;

                if(currentDamage >= 50)
                {
                    explosionSound.Play();
                }
                OnDespawn?.Invoke(this);
                return;
            }

            if (World.CurrentSector.Raycast(ray, out var hit))
            {

                if (canRicochet && currentRicochets < parameters.PossibleRicochets)
                {

                    var angle = Ray.CalculateIncidentAngle(ray, hit);

                    if (angle <= parameters.RicochetAngle)
                    {
                        ray = ray.CalculateRicochetRay(hit, ray.Length);
                        ricochetSound.Play();
                        lineRenderer.ClearPoints();
                        lineRenderer.AddPoint(Vector3.Zero);
                        lineRenderer.AddPoint(ray.Direction * parameters.Length);
                        currentRicochets++;

                        if(currentRicochets == 5)
                        {
                            explosionSound.Play();
                        }
                        return;
                    }

                }

                
                OnHit?.Invoke(this);
                hitSound.Play();

                if(currentDamage > hit.block.Durability)
                {
                    
                    hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage);
                    var d = currentDamage - hit.block.Durability ;
                  
                }
                else
                {
                    hit.chunk.DamageBlock(hit.blockPositionIndex, hit.normal, currentDamage);

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

        public Projectile CreateFromPool()
        {
            return new Projectile();
        }

        public void Reset()
        {
            IsActive = false;
            Position = Vector3.Zero;
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
