using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;

namespace Spacebox.Game.Player;

public class SpectatorMode : GameModeBase
{
    private const float _cameraSpeed = 8f;
    private const float _shiftSpeed = 16f;

    private static InteractionHandler CreateInteractionHandler()
    {
        return new InteractionHandler();
    }

    public SpectatorMode(Astronaut player) : base(player, CreateInteractionHandler())
    {
        player.CollisionEnabled = false;
        player.EnableCameraSway(false);
        player.InertiaController.EnableInertia(false);

        player.SetCameraSpeed(_cameraSpeed, _shiftSpeed);
    }

    public override void Update(Astronaut player)
    {
    }

    public override void HandleInput(Astronaut player)
    {
        Vector3 acceleration = Vector3.Zero;
        bool isMoving = false;

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

        float deltaTime = (float)Time.Delta;

        Vector3 movement = Vector3.Zero;

        bool isRunning = Input.IsKey(Keys.LeftShift);

        if (!isMoving) isRunning = false;

        float currentSpeed = isRunning ? _shiftSpeed : _cameraSpeed;

        if (isMoving)
        {
            movement = acceleration.Normalized() * currentSpeed * deltaTime;
        }

        if (movement != Vector3.Zero)
        {
            MoveAndCollide(movement, player);
            player.OnMoved?.Invoke(player);
        }
    }

    public void MoveAndCollide(Vector3 movement, Astronaut player)
    {
        player.Position += movement;
    }
}