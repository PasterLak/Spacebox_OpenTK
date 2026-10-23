using Spacebox.Game.Player.Interactions;

namespace Spacebox.Game.Player.GameModes;

public static class GameModeInteractions
{
    private static readonly Type[] BreakingTypes = { typeof(InteractionDestroyBlockSurvival), typeof(InteractionDestroyBlockCreative), typeof(InteractionEraser) };
    private static readonly Type[] PlacingTypes = { typeof(InteractionPlaceBlock) };
    private static readonly Type[] WeaponTypes = { typeof(InteractionShoot) };

    public static HashSet<Type> Filter(HashSet<Type> allowed, GameModeRules rules)
    {
        if (!rules.BreakBlocks) allowed.ExceptWith(BreakingTypes);
        if (!rules.PlaceBlocks) allowed.ExceptWith(PlacingTypes);
        if (!rules.UseWeapons) allowed.ExceptWith(WeaponTypes);
        return allowed;
    }

    public static bool IsRuleControlled(Type type) => Array.IndexOf(BreakingTypes, type) >= 0 || Array.IndexOf(PlacingTypes, type) >= 0 || Array.IndexOf(WeaponTypes, type) >= 0;

    public static InteractionMode DefaultFor(HashSet<Type> allowed, Func<InteractionMode> preferred, Type preferredType)
    {
        return allowed.Contains(preferredType) ? preferred() : new InteractionDefault();
    }
}
