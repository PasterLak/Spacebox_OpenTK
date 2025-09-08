using Engine;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;


namespace Spacebox.Game.Player;

public class MovementMode : GameModeBase
{
    private const float _cameraSpeed = 3.5f;
    private const float _shiftSpeed = 7.5f;
    private float speedUpPower = 0;
    private AudioSource flySpeedUpAudio;

    private AudioSource wallhitAudio;
    private AudioSource wallhitAudio2;

    private static AudioSource blockDestroy;

    public MovementMode(Astronaut player, InteractionHandler interactionHandler) : base(player, interactionHandler)
    {
        player.CollisionEnabled = true;
        player.EnableCameraSway(true);
        player.InertiaController.EnableInertia(true);

        SetInertia(player.InertiaController);
        SetCameraSway(player.CameraSway);

        if (flySpeedUpAudio == null)
        {
            flySpeedUpAudio = new AudioSource(Resources.Load<AudioClip>("flySpeedUp"));
            flySpeedUpAudio.IsLooped = true;
            flySpeedUpAudio.Volume = 0.1f;
        }
           
       

        wallhitAudio = new AudioSource(Resources.Load<AudioClip>("wallhit"));
        wallhitAudio2 = new AudioSource(Resources.Load<AudioClip>("wallHit2"));


    }

    private void PickDestroySound(short blockId)
    {
        var clip = GameAssets.GetBlockAudioClipFromItemID(blockId, BlockInteractionType.Destroy);
        if (clip == null)
        {
            return;
        }

        if (blockDestroy != null && blockDestroy.Clip == clip)
        {
            return;
        }

        if (blockDestroy != null)
        {
            blockDestroy.Stop();
        }

        blockDestroy = new AudioSource(clip);
    }

    public override GameModes.GameMode GetGameMode()
    {
        return GameModes.GameMode.Spectator;
    }

    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(Resources.Load<AudioClip>("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }
    }

    public override void OnDisable()
    {
        InteractionHandler.Interaction.OnDisable();

    }
    public override void UpdateInteraction(Astronaut player)
    {
        InteractionHandler.Update(player);
    }
    public override void Update(Astronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
    }

   

    public override void HandleInput(Astronaut player)
    {

        Vector3 acceleration = Vector3.Zero;
        bool isMoving = false;
        bool isRunning = false;
        Vector3 movement = Vector3.Zero;

        if (player.CanMove)
        {

            if (Input.IsAction("forward"))
            {
                acceleration += player.Front;
                isMoving = true;
            }

            if (Input.IsAction("backward"))
            {
                acceleration -= player.Front;
                isMoving = true;
            }

            if (Input.IsAction("left"))
            {
                acceleration -= player.Right;
                isMoving = true;
            }

            if (Input.IsAction("right"))
            {
                acceleration += player.Right;
                isMoving = true;
            }

            if (Input.IsAction("up"))
            {
                acceleration += player.Up;
                isMoving = true;
            }

            if (Input.IsAction("down"))
            {
                acceleration -= player.Up;
                isMoving = true;
            }

            float roll = 1000f * Time.Delta;

            if (Input.IsAction("rollLeft"))
            {
                player.Roll(-roll);
            }

            if (Input.IsAction("rollRight"))
            {
                player.Roll(roll);
            }



            isRunning = Input.IsAction("sprint");
        }

        if (!isMoving) isRunning = false;
        if (player.PowerBar.StatsData.IsMinReached) isRunning = false;


        if (isRunning)
        {
            if (!flySpeedUpAudio.IsPlaying)
                flySpeedUpAudio.Play();

            const float maxVolume = 0.15f;
            if (speedUpPower < maxVolume)
            {
                speedUpPower += Time.Delta;

                if (speedUpPower > maxVolume)
                {
                    speedUpPower = maxVolume;
                }

                flySpeedUpAudio.Volume = speedUpPower;
            }
        }
        else
        {
            if (speedUpPower > 0)
            {
                speedUpPower -= Time.Delta * 2f;

                if (speedUpPower <= 0)
                {
                    speedUpPower = 0;
                    flySpeedUpAudio.Pause();
                }

                flySpeedUpAudio.Volume = speedUpPower;
            }
        }

        player.InertiaController.SetMode(isRunning);

        if (isRunning && GetGameMode() == GameMode.Survival) player.PowerBar.Use();


        if (player.InertiaController.Enabled)
        {
            if (isMoving)
            {
                player.InertiaController.ApplyInput(acceleration);
            }

            player.InertiaController.Update(isMoving);

            movement = player.InertiaController.Velocity * Time.Delta;

            player.CameraSway.Update(player.InertiaController.Velocity.Length);
        }
        else
        {
            float currentSpeed = isRunning ? _shiftSpeed : _cameraSpeed;

            if (isMoving)
            {
                movement = acceleration.Normalized() * currentSpeed * Time.Delta;
            }

            player.CameraSway.Update(movement.Length / Time.Delta);
        }

        

        var oldPos = player.Position;
        if (movement != Vector3.Zero)
        {
            MoveAndCollide(movement, player);
            player.OnMoved?.Invoke(player);
        }

        if(player.CanMove && player.Position == oldPos)
        {
            var world = World.Instance;

            if (world == null) return;

            if (world.IsColliding(player.Position, player.BoundingVolume, out var col))
            {
                timeInWall += Time.Delta;

                if(timeInWall > 1f)
                {
                    player.TakeDamage(5, new DeathCase("Suffocated in a wall"));
                    timeInWall = 0;
                }
            }
        }
    }

    float timeInWall = 0;

    private struct CollisionState
    {
        public bool hitX;
        public bool hitY;
        public bool hitZ;
        public float totalImpactSpeed;
        public CollideInfo collideInfo;
    }

    private float CalculateEffectiveImpactSpeed(Vector3 velocityBefore, CollisionState collision, int hitCount)
    {
        float speed = velocityBefore.Length;

        if (hitCount == 1)
        {
            if (collision.hitX) return MathF.Abs(velocityBefore.X);
            if (collision.hitY) return MathF.Abs(velocityBefore.Y);
            if (collision.hitZ) return MathF.Abs(velocityBefore.Z);
        }
        else if (hitCount == 2)
        {
            if (collision.hitX && collision.hitY)
                return MathF.Sqrt(velocityBefore.X * velocityBefore.X + velocityBefore.Y * velocityBefore.Y);
            if (collision.hitX && collision.hitZ)
                return MathF.Sqrt(velocityBefore.X * velocityBefore.X + velocityBefore.Z * velocityBefore.Z);
            if (collision.hitY && collision.hitZ)
                return MathF.Sqrt(velocityBefore.Y * velocityBefore.Y + velocityBefore.Z * velocityBefore.Z);
        }

        return speed;
    }

    public void MoveAndCollide(Vector3 movement, Astronaut player)
    {
        Vector3 position = player.Position;

        if (player.CollisionEnabled)
        {
            World world = World.Instance;

            if (world == null) return;

            CollisionState collision = new CollisionState();
            Vector3 velocityBefore = player.InertiaController.Velocity;
            float speedBefore = velocityBefore.Length;

            position.X += movement.X;
            UpdateBoundingAt(position, player);

            if (world.IsColliding(player.Position, player.BoundingVolume, out collision.collideInfo))
            {
                position.X -= movement.X;
                collision.hitX = true;
            }

            position.Y += movement.Y;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume, out var collideInfoY))
            {
                position.Y -= movement.Y;
                collision.hitY = true;
                if (!collision.hitX) collision.collideInfo = collideInfoY;
            }

            position.Z += movement.Z;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume, out var collideInfoZ))
            {
                position.Z -= movement.Z;
                collision.hitZ = true;
                if (!collision.hitX && !collision.hitY) collision.collideInfo = collideInfoZ;
            }

            int hitCount = (collision.hitX ? 1 : 0) + (collision.hitY ? 1 : 0) + (collision.hitZ ? 1 : 0);

            if (hitCount > 0)
            {
                float effectiveSpeed = CalculateEffectiveImpactSpeed(velocityBefore, collision, hitCount);

                if (effectiveSpeed > 4f)
                {
                    var saveSpeed = ApplyVelocityDamage(effectiveSpeed, player, collision.collideInfo);

                    if (saveSpeed)
                    {
                        player.InertiaController.Velocity = velocityBefore * 0.7f;
                    }
                    else
                    {
                        if (collision.hitX) velocityBefore.X = 0;
                        if (collision.hitY) velocityBefore.Y = 0;
                        if (collision.hitZ) velocityBefore.Z = 0;
                        player.InertiaController.Velocity = velocityBefore;
                    }
                }
                else if (speedBefore > 2f)
                {
                    if (collision.hitX) velocityBefore.X *= 0.95f;
                    if (collision.hitY) velocityBefore.Y *= 0.95f;
                    if (collision.hitZ) velocityBefore.Z *= 0.95f;
                    player.InertiaController.Velocity = velocityBefore;
                }
            }

            player.Position = position;
        }
        else
        {
            player.Position += movement;
        }
    }

    private bool ApplyVelocityDamage(float speed, Astronaut player, CollideInfo collideInfo)
    {
        bool saveSpeed = false;

        if (speed > 4 && speed <= 9)
        {
            if (wallhitAudio.IsPlaying) wallhitAudio.Stop();

            wallhitAudio.Volume = MathHelper.Clamp(speed * 0.07f, 0, 1);
            wallhitAudio.Play();
        }
        else if (speed > 9)
        {
            wallhitAudio2.Volume = MathHelper.Clamp(speed * 0.1f, 0, 1);
            wallhitAudio2.Play();

            if (collideInfo.block.IsTransparent)
            {
                Chunk ch = collideInfo.chunk;
                if (ch != null)
                {
                    ch.RemoveBlock(collideInfo.blockPositionIndex, new Vector3SByte(0, 0, 0));
                    if (blockDestroy != null)
                    {
                        PickDestroySound(collideInfo.block.Id);
                        if (!blockDestroy.IsPlaying)
                            blockDestroy.Play();
                    }
                    saveSpeed = true;
                    speed *= 0.7f;
                }
            }

            int dmg = CalculateDamage(speed);

            if (dmg > 0)
            {
                if (dmg < 50)
                {
                    player.TakeDamage(dmg, new DeathCase("Wall checking complete"));
                }
                else
                {
                    player.TakeDamage(dmg, new DeathCase("Met an unfriendly asteroid"));
                }
            }
        }

        return saveSpeed;
    }

    private int CalculateDamage(float impactSpeed)
    {
        if (impactSpeed <= 9f)
            return 0;
        float baseDamage = impactSpeed - 8f;
        int damage = (int)MathF.Max(MathF.Pow(baseDamage, 2.5f) * 0.5f, 1);
        // Speed: 9=0, 10=1, 11=5, 12=11, 13=20, 14=33, 15=50, 16=72, 17=99, 18=132, 19=171, 20=217
        return damage;
    }

    private void UpdateBoundingAt(Vector3 position, Astronaut player)
    {
        if (player.BoundingVolume is BoundingSphere sphere)
        {
            sphere.Center = position + player.Offset;
        }
        else if (player.BoundingVolume is BoundingBox box)
        {
            box.Center = position + player.Offset;
        }
    }

    private void SetInertia(InertiaController inertiaController)
    {
        inertiaController.Enabled = true;
        inertiaController.SetParameters(
            walkTimeToMaxSpeed: 1f,
            walkTimeToStop: 0.5f,
            runTimeToMaxSpeed: 2f,
            runTimeToStop: 0.4f,
            walkMaxSpeed: 8,
            runMaxSpeed: 20
        );

        inertiaController.SetMode(isRunning: false);
        inertiaController.MaxSpeed = inertiaController.WalkMaxSpeed;

        inertiaController.InertiaType = InertiaType.Damping;
    }

    private void SetCameraSway(CameraSway cameraSway)
    {
        cameraSway.InitialIntensity = 0.0015f;
        cameraSway.MaxIntensity = 0.003f;
        cameraSway.InitialFrequency = 15f;
        cameraSway.MaxFrequency = 50f;
        cameraSway.SpeedThreshold = 5.0f;
        cameraSway.Enabled = true;
    }


}