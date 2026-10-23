using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation;
using Spacebox.Game.Player.Interactions;
using System;
using System.Collections.Generic;
using Engine;

namespace Spacebox.Game.Player.GameModes;

public class CreativeMode : MovementMode
{
    private const float _cameraSpeed = 3.5f;
    private const float _shiftSpeed = 7.5f;

    private static InteractionHandler CreateInteractionHandler(GameModeRules rules)
    {
        var allowed = GameModeInteractions.Filter(new HashSet<Type>()
        {
            typeof(InteractionDestroyBlockCreative),
            typeof(InteractionPlaceBlock),
            typeof(InteractionConsumeItem),
            typeof(InteractionSpawnMob),
            typeof(InteractionEquipItem),
            typeof(InteractionShoot),
            typeof(InteractionDefault),
            typeof(InteractionEraser),
            typeof(InteractionRepair),
            typeof(InteractionCameraPoint)
        }, rules);

        return new InteractionHandler(GameModeInteractions.DefaultFor(allowed, () => new InteractionPlaceBlock(), typeof(InteractionPlaceBlock)), allowed, GameMode.Creative,
            rules.BreakBlocks ? new IPassiveInteraction[] { new CreativeBlockDestroying() } : Array.Empty<IPassiveInteraction>());
    }

    public CreativeMode(LocalAstronaut player, GameModeRules rules) : base(player, CreateInteractionHandler(rules))
    {
        player.CollisionEnabled = true;
        player.EnableCameraSway(true);
        player.InertiaController.EnableInertia(true);
    }

    public override GameMode GetGameMode()
    {
        return GameMode.Creative;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (UIManager.IsOpen("creative"))
        {
            UIManager.CloseAll();
        }
    }

    public override void Update(LocalAstronaut player)
    {
        player.PowerBar.Update();
        player.HealthBar.Update();
        player.OxygenBar.UpdateInfinite(World.Instance != null && World.Instance.IsBreathable(player.Position));
    }

    public override void HandleInput(LocalAstronaut player)
    {
        base.HandleInput(player);
    }

    public void MoveAndCollide(Vector3 movement, LocalAstronaut player)
    {
        base.MoveAndCollide(movement, player);
    }
}