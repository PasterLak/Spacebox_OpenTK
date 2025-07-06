using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.Audio;
using Engine.Physics;
using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Physics;
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

        player.SetCameraSpeed(_cameraSpeed, _shiftSpeed);
        SetInertia(player.InertiaController);
        SetCameraSway(player.CameraSway);

        if (flySpeedUpAudio == null)
        {
            flySpeedUpAudio = new AudioSource(SoundManager.GetClip("flySpeedUp"));
            flySpeedUpAudio.IsLooped = true;
            flySpeedUpAudio.Volume = 0.1f;
        }
           
       

        wallhitAudio = new AudioSource(SoundManager.GetClip("wallhit"));
        wallhitAudio2 = new AudioSource(SoundManager.GetClip("wallHit2"));


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

    public override GameMode GetGameMode()
    {
        return GameMode.Spectator;
    }

    public override void OnEnable()
    {
        if (blockDestroy == null)
        {
            blockDestroy = new AudioSource(SoundManager.GetClip("blockDestroyDefault"));
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

            if (Input.IsKey(Keys.W))
            {
                acceleration += player.Front;
                isMoving = true;
            }

            if (Input.IsKey(Keys.S))
            {
                acceleration -= player.Front;
                isMoving = true;
            }

            if (Input.IsKey(Keys.A))
            {
                acceleration -= player.Right;
                isMoving = true;
            }

            if (Input.IsKey(Keys.D))
            {
                acceleration += player.Right;
                isMoving = true;
            }

            if (Input.IsKey(Keys.Space))
            {
                acceleration += player.Up;
                isMoving = true;
            }

            if (Input.IsKey(Keys.LeftControl))
            {
                acceleration -= player.Up;
                isMoving = true;
            }

            float roll = 1000f * Time.Delta;

            if (Input.IsKey(Keys.Q))
            {
                player.Roll(-roll);
            }

            if (Input.IsKey(Keys.E))
            {
                player.Roll(roll);
            }



            isRunning = Input.IsKey(Keys.LeftShift);
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

        if (movement != Vector3.Zero)
        {
            MoveAndCollide(movement, player);
            player.OnMoved?.Invoke(player);
        }
    }

    public void MoveAndCollide(Vector3 movement, Astronaut player)
    {
        Vector3 position = player.Position;

        if (player.CollisionEnabled)
        {
            World world = World.Instance;

            if (world == null) return;

            position.X += movement.X;
            UpdateBoundingAt(position, player);

            CollideInfo collideInfo;


            if (world.IsColliding(player.Position, player.BoundingVolume, out collideInfo))
            {
                position.X -= movement.X;


                var save = ApplyVelocityDamage(player.InertiaController.Velocity.Length, player, collideInfo);
                player.InertiaController.Velocity =
                    new Vector3(save ? player.InertiaController.Velocity.X * 0.8f : 0, player.InertiaController.Velocity.Y, player.InertiaController.Velocity.Z);
            }


            position.Y += movement.Y;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume, out collideInfo))
            {
                position.Y -= movement.Y;
                var save = ApplyVelocityDamage(player.InertiaController.Velocity.Length, player, collideInfo);
                player.InertiaController.Velocity =
                    new Vector3(player.InertiaController.Velocity.X, save ? player.InertiaController.Velocity.Y * 0.8f : 0, player.InertiaController.Velocity.Z);
            }

            position.Z += movement.Z;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume, out collideInfo))
            {
                position.Z -= movement.Z;
                var save = ApplyVelocityDamage(player.InertiaController.Velocity.Length, player, collideInfo);
                player.InertiaController.Velocity =
                    new Vector3(player.InertiaController.Velocity.X, player.InertiaController.Velocity.Y, save ? player.InertiaController.Velocity.Z * 0.8f : 0);
            }

            player.Position = position;
        }
        else
        {
            player.Position += movement;
        }
    }

    const byte damageMultiplayer = 5;

    private bool ApplyVelocityDamage(float speed, Astronaut player, CollideInfo collideInfo)
    {
        /*if (collideInfo.normal != Vector3SByte.Zero)
        {
            Vector3 normal = (Vector3)collideInfo.normal;
            Vector3 velocityDir = player.InertiaController.Velocity;
            float vLen = speed;
            if (vLen > 0.0001f)
            {
                velocityDir.Normalize();
                float dot = MathF.Abs(Vector3.Dot(velocityDir, normal));
                const float SLIDE_THRESHOLD = 0.1f;
                if (dot < SLIDE_THRESHOLD)
                {
                    Debug.Log($"N:{collideInfo.normal} V:{velocityDir} T: { SLIDE_THRESHOLD} ");
                    return true;
                }
            }
        }*/

        bool saveSpeed = false;
        if (speed > 4 && speed <= 9)
        {
            if (wallhitAudio.IsPlaying) wallhitAudio.Stop();

            wallhitAudio.Volume = 0;
            wallhitAudio.Volume = MathHelper.Min(wallhitAudio.Volume + speed * 0.07f, 1);
            wallhitAudio.Play();
        }
        else if (speed > 9)
        {
            wallhitAudio2.Volume = MathHelper.Min(0 + speed * 0.1f, 1);
            wallhitAudio2.Play();

            int damage = (int)(Math.Abs(speed) - 10f);
            if (damage == 0) { damage = 1; }
            //if(GetGameMode() != GameMode.Spectator)


            if (damage >= 3)
            {
                Chunk ch = collideInfo.chunk;

                if (ch != null)
                {
                    if (collideInfo.block.IsTransparent)
                        ch.RemoveBlock(collideInfo.blockPositionIndex, new Vector3SByte(0, 0, 0));
                    if (blockDestroy != null)
                    {
                        PickDestroySound(collideInfo.block.BlockId);
                        if (!blockDestroy.IsPlaying)
                            blockDestroy.Play();
                    }
                    saveSpeed = true;
                    damage--;
                }
            }

            player.HealthBar.StatsData.Decrement(damage * damageMultiplayer);
            HealthColorOverlay.SetActive(new System.Numerics.Vector3(1, 0, 0), 0.1f + 1 / (10 - damage));
            player.HitImage.Show();

            if (damage > 5)
            {
              /*  BaseSpaceScene.DeathOn = false;
                flySpeedUpAudio.Stop();
                BlackScreenOverlay.IsEnabled = true;
                ToggleManager.SetState("player", false);
                Settings.ShowInterface = false;

                OldSpaceScene.Death.Play();
                OldSpaceScene.DeathOn = true;*/
            }


        }

        return saveSpeed;
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