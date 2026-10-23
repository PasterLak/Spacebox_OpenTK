using Engine;
using Engine.Audio;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Physics.Gravity;
using Spacebox.Network;


namespace Spacebox.Game.Player;

public class MovementMode : GameModeBase
{
    private const float _cameraSpeed = 3.5f;
    private const float _shiftSpeed = 7.5f;
    private const float BodyPushFactor = 1.5f;
    private float speedUpPower = 0;
    private AudioSource flySpeedUpAudio;

    private AudioSource wallhitAudio;
    private AudioSource wallhitAudio2;

    private static AudioSource blockDestroy;

    public MovementMode(LocalAstronaut player, InteractionHandler interactionHandler) : base(player, interactionHandler)
    {
        player.CollisionEnabled = true;
        player.EnableCameraSway(true);
        player.InertiaController.EnableInertia(true);

        SetInertia(player.InertiaController);
        SetCameraSway(player.CameraSway);

        if (flySpeedUpAudio == null || flySpeedUpAudio.IsDisposed)
        {
            flySpeedUpAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/flySpeedUp.ogg"));
            flySpeedUpAudio.IsLooped = true;
            flySpeedUpAudio.Volume = 0.1f;
        }
           
       

        wallhitAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/wallhit.ogg"));
        wallhitAudio2 = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/wallHit2.ogg"));


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
            blockDestroy.Dispose();
        }

        blockDestroy = new AudioSource(clip);
    }

    public override GameModes.GameMode GetGameMode()
    {
        return GameModes.GameMode.Spectator;
    }

    public override void OnEnable()
    {
        if (blockDestroy == null || blockDestroy.IsDisposed)
        {
            blockDestroy = new AudioSource(Resources.Load<AudioClip>("blockDestroyDefault"));
            blockDestroy.Volume = 1f;
        }
    }

    public override void OnDisable()
    {
        InteractionHandler.Interaction.OnDisable();

    }
    public override void UpdateInteraction(LocalAstronaut player)
    {
        
        InteractionHandler.Update(player);
    }
    public override void Update(LocalAstronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
    }

   

    private const float GravityThreshold = 0.3f;

    private bool TryHandleGravity(LocalAstronaut player)
    {
        var gravity = player.CollisionEnabled && player.IsAlive && player.GameModeRules.UseGravity ? GravityField.Sample(player.Position) : Vector3.Zero;

        if (gravity.LengthSquared > GravityThreshold * GravityThreshold)
        {
            StopSpeedUp(player);
            if (!player.Gravity.IsActive) player.Gravity.Enter(player, gravity);
            player.Gravity.Update(player, gravity, player.CanMove);
            player.Footsteps.Update(Time.Delta);
            return true;
        }

        if (player.Gravity.IsActive) player.Gravity.Exit(player);
        return false;
    }

    private void StopSpeedUp(LocalAstronaut player)
    {
        player.InertiaController.SetMode(false);
        player.IsBoosting = false;
        if (speedUpPower <= 0f && !flySpeedUpAudio.IsPlaying) return;

        speedUpPower = 0f;
        flySpeedUpAudio.Volume = 0f;
        flySpeedUpAudio.Pause();
    }

    public override void HandleInput(LocalAstronaut player)
    {
        if (TryHandleGravity(player)) return;


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



            isRunning = MovementAssists.WantsSprint();
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
        player.IsBoosting = isRunning;

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
                    if (GetGameMode() == GameMode.Survival) player.TakeDamage(5, new DeathCase("Suffocated in a wall"));
                    timeInWall = 0;
                }
            }
            else
            {
                timeInWall = 0;
            }
        }
    }

    float timeInWall = 0;

    private const float CollisionSkin = 0.002f;
    private const int MaxResolveIterations = 6;
    private const int MaxSubsteps = 16;

    private struct MoveContext
    {
        public Vector3 Step;
        public Vector3 Velocity;
        public float ImpactSpeed;
        public CollideInfo ImpactInfo;
        public SphereBody HitBody;
    }

    public void MoveAndCollide(Vector3 movement, LocalAstronaut player)
    {
        if (!player.CollisionEnabled)
        {
            player.Position += movement;
            return;
        }

        World world = World.Instance;
        if (world == null) return;

        var sector = World.CurrentSector;
        if (sector == null || player.BoundingVolume is not BoundingSphere sphere)
        {
            player.Position += movement;
            return;
        }

        float radius = sphere.Radius;
        Vector3 velocityBefore = player.InertiaController.Velocity;
        Vector3 position = player.Position;
        int steps = Math.Clamp((int)MathF.Ceiling(movement.Length / (radius * 0.5f)), 1, MaxSubsteps);

        var context = new MoveContext
        {
            Step = movement / steps,
            Velocity = velocityBefore
        };

        for (int i = 0; i < steps; i++)
        {
            Vector3 start = position;
            position += context.Step;

            if (!Resolve(world, sector, player, radius, ref position, ref context))
            {
                position = start;
                break;
            }
        }

        UpdateBoundingAt(position, player);

        if (context.HitBody != null) PushBody(context.HitBody, player, velocityBefore);

        if (context.ImpactSpeed > 4f && context.ImpactInfo.block != null &&
            ApplyVelocityDamage(context.ImpactSpeed, player, context.ImpactInfo))
        {
            context.Velocity = velocityBefore * 0.7f;
        }

        if (!player.IsAlive) context.Velocity = Vector3.Zero;
        player.InertiaController.Velocity = context.Velocity;
        player.Position = position;
    }

    private static bool Resolve(World world, Sector sector, LocalAstronaut player, float radius, ref Vector3 position, ref MoveContext context)
    {
        for (int i = 0; i < MaxResolveIterations; i++)
        {
            if (!TryFindContact(world, sector, player, position, radius, out var normal, out float depth, out var info, ref context.HitBody)) return true;

            position += normal * (depth + CollisionSkin);
            context.Step = SphereBoxMath.RemoveInward(context.Step, normal, out _);
            context.Velocity = SphereBoxMath.RemoveInward(context.Velocity, normal, out float inwardSpeed);

            if (inwardSpeed > context.ImpactSpeed)
            {
                context.ImpactSpeed = inwardSpeed;
                context.ImpactInfo = info;
            }
        }

        SphereBody ignored = null;
        return !TryFindContact(world, sector, player, position, radius, out _, out float remaining, out _, ref ignored) || remaining <= CollisionSkin;
    }

    private static bool TryFindContact(World world, Sector sector, LocalAstronaut player, Vector3 position, float radius, out Vector3 normal, out float depth, out CollideInfo info, ref SphereBody hitBody)
    {
        Vector3 center = position + player.Offset;
        bool found = VoxelSphereContacts.TryFindDeepest(sector, center, radius, out var contact);
        normal = contact.Normal;
        depth = contact.Depth;
        info = contact.CollideInfo;

        var body = player.Body;
        if (body == null || !world.Physics.OverlapsSolidBody(center, body.Radius, body, out var other)) return found;

        Vector3 offset = center - other.Center;
        float distance = offset.Length;
        float bodyDepth = body.Radius + other.Radius - distance;
        if (bodyDepth <= depth) return found;

        hitBody = other;
        normal = distance > 0.000001f ? offset / distance : player.Front * -1f;
        depth = bodyDepth;
        info = default;
        return true;
    }

    private static void PushBody(SphereBody body, LocalAstronaut player, Vector3 playerVelocity)
    {
        if (body.IsKinematic && body.Owner is not IPushable) return;

        Vector3 normal = body.Center - player.Position;
        if (normal.LengthSquared < 0.000001f) return;
        normal.Normalize();

        float playerSpeed = Vector3.Dot(playerVelocity, normal);
        if (playerSpeed <= 0f) return;

        float targetSpeed = playerSpeed * BodyPushFactor;
        if (body.Owner is IPushable pushable) pushable.Push(normal, targetSpeed);
        else body.Push(normal, targetSpeed);
    }

    private bool ApplyVelocityDamage(float speed, LocalAstronaut player, CollideInfo collideInfo)
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

            if (collideInfo.block != null && collideInfo.block.IsTransparent && !Spacebox.Game.Generation.Blocks.UnbreakableBlocks.Is(collideInfo.block))
            {
                Chunk ch = collideInfo.chunk;
                if (ch != null)
                {
                    WorldEditNetwork.NotifyDamaged(ch, collideInfo.blockPositionIndex, collideInfo.block.Id, collideInfo.block.Durability);
                    ch.RemoveBlock(collideInfo.blockPositionIndex, new Vector3SByte(0, 0, 0));
                    WorldEditNetwork.NotifyRemoved(ch, collideInfo.blockPositionIndex);
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

            int dmg = GetGameMode() == GameMode.Survival ? CalculateDamage(speed) : 0;

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