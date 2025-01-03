using OpenTK.Mathematics;
using Spacebox.UI;

namespace Spacebox.Game.Player;

public class CreativeMode : MovementMode
{
    private const float _cameraSpeed = 3.5f;
    private const float _shiftSpeed = 7.5f;

    private static InteractionHandler CreateInteractionHandler()
    {
        return new InteractionHandler(new InteractionPlaceBlock(), new HashSet<Type>()
        {
            typeof(InteractionDestroyBlock),
            typeof(InteractionPlaceBlock),
            typeof(InteractionConsumeItem),
            typeof(InteractionDefault)
        }, GameMode.Creative);
    }

    public CreativeMode(Astronaut player) : base(player, CreateInteractionHandler())
    {
        player.CollisionEnabled = true;
        player.EnableCameraSway(true);
        player.InertiaController.EnableInertia(true);

        player.SetCameraSpeed(_cameraSpeed, _shiftSpeed);
       
    }

    public override GameMode GetGameMode()
    {
        return GameMode.Creative;
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        CreativeWindowUI.Enabled = true;
    }

    public override void OnDisable()
    {
       base.OnDisable();
        CreativeWindowUI.IsVisible = false;
        CreativeWindowUI.Enabled = false;
    }

    public override void Update(Astronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
    }

    public override void HandleInput(Astronaut player)
    {
       base.HandleInput(player);
    }

    public void MoveAndCollide(Vector3 movement, Astronaut player)
    {
       base.MoveAndCollide(movement, player);
    }

   

}