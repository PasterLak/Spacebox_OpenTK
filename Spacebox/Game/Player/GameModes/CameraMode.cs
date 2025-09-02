using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine;
using Spacebox.Game.Player.Interactions;
namespace Spacebox.Game.Player.GameModes.GameModes;

public class CameraMode : GameModeBase
{
    private const float _cameraSpeed = 50f;
    private const float _shiftSpeed = 150f;

    public CameraMode(Astronaut player) : base(player, new InteractionHandler(GameMode.Spectator))
    {
        player.CollisionEnabled = false;
        player.EnableCameraSway(false);
        player.InertiaController.EnableInertia(false);

        player.SetCameraSpeed(_cameraSpeed, _shiftSpeed);
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
        //return;
        //InteractionHandler.Update(player);
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