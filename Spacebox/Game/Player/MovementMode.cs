using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using Spacebox.Scenes;

namespace Spacebox.Game.Player;

public class MovementMode : GameModeBase
{
    private const float _cameraSpeed = 3.5f;
    private const float _shiftSpeed = 7.5f;
    private float speedUpPower = 0;
    private AudioSource flySpeedUpAudio;

    private AudioSource wallhitAudio;
    private AudioSource wallhitAudio2;

    public MovementMode(Astronaut player, InteractionHandler interactionHandler) : base(player, interactionHandler)
    {
        player.CollisionEnabled = true;
        player.EnableCameraSway(true);
        player.InertiaController.EnableInertia(true);

        player.SetCameraSpeed(_cameraSpeed, _shiftSpeed);
        SetInertia(player.InertiaController);
        SetCameraSway(player.CameraSway);

        if (flySpeedUpAudio == null)
            flySpeedUpAudio = new AudioSource(SoundManager.GetClip("flySpeedUp"));
        flySpeedUpAudio.IsLooped = true;

        wallhitAudio = new AudioSource(SoundManager.GetClip("wallhit"));
        wallhitAudio2 = new AudioSource(SoundManager.GetClip("wallHit2"));
    }

    public override GameMode GetGameMode()
    {
        return GameMode.Spectator;
    }

    public override void OnEnable()
    {
 
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
            if (world.IsColliding(player.Position, player.BoundingVolume))
            {
                position.X -= movement.X;
                ApplyVelocityDamage(player.InertiaController.Velocity.Length, player);
                player.InertiaController.Velocity =
                    new Vector3(0, player.InertiaController.Velocity.Y, player.InertiaController.Velocity.Z);
            }


            position.Y += movement.Y;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume))
            {
                position.Y -= movement.Y;
                ApplyVelocityDamage(player.InertiaController.Velocity.Length, player);
                player.InertiaController.Velocity =
                    new Vector3(player.InertiaController.Velocity.X, 0, player.InertiaController.Velocity.Z);
            }

            position.Z += movement.Z;
            UpdateBoundingAt(position, player);
            if (world.IsColliding(player.Position, player.BoundingVolume))
            {
                position.Z -= movement.Z;
                ApplyVelocityDamage(player.InertiaController.Velocity.Length, player);
                player.InertiaController.Velocity =
                    new Vector3(player.InertiaController.Velocity.X, player.InertiaController.Velocity.Y, 0);
            }

            player.Position = position;
        }
        else
        {
            player.Position += movement;
        }
    }

    byte damageMultiplayer = 4;

    private void ApplyVelocityDamage(float speed, Astronaut player)
    {
        if (speed > 4 && speed <= 9)
        {
             if (wallhitAudio.IsPlaying) wallhitAudio.Stop();

             wallhitAudio.Volume = 0;
             wallhitAudio.Volume = MathHelper.Min(wallhitAudio.Volume + speed * 0.05f, 1);
             wallhitAudio.Play();
        }
        else if (speed > 9)
        {
             wallhitAudio2.Volume = MathHelper.Min(0 + speed * 0.05f, 1);
             wallhitAudio2.Play();
        }

        if (speed > 10)
        {
            int damage = (int)(Math.Abs(speed) - 10f);

            if(GetGameMode() == GameMode.Survival)
            player.HealthBar.StatsData.Decrement(damage * damageMultiplayer);
            HealthColorOverlay.SetActive(new System.Numerics.Vector3(1, 0, 0), 0.1f + 1 / (10 - damage));
            player.HitImage.Show();

            if (damage > 5)
            {
                SpaceScene.DeathOn = false;
                flySpeedUpAudio.Stop();
                BlackScreenOverlay.IsEnabled = true;
                ToggleManager.SetState("player", false);
                Settings.ShowInterface = false;

                SpaceScene.Death.Play();
                SpaceScene.DeathOn = true;
            }
        }
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