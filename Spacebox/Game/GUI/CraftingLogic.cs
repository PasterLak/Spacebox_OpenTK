using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class CraftingLogic
{

    public static bool TryGetResources(Storage s1, Storage s2, Blueprint blueprint, Astronaut currentPlayer)
    {
        foreach (var ing in blueprint.Ingredients)
        {
            if (!IsResourceAvailable(ing, s1, s2, currentPlayer))
            {
                return false;
            }
        }

        foreach (var ing in blueprint.Ingredients)
        {
            ConsumeResource(ing, s1, s2, currentPlayer);
        }

        return true;
    }

    public static bool IsResourceAvailable(Ingredient ing, Storage s1, Storage s2, Astronaut player)
    {
        if (IsVirtualResource(ing.Item.Id_string))
        {
            return CheckVirtualResource(ing.Item.Id_string, ing.Quantity, player);
        }

        int totalAvailable = 0;
        if (s1 != null) totalAvailable += s1.GetTotalCountOf(ing.Item);
        if (s2 != null) totalAvailable += s2.GetTotalCountOf(ing.Item);

        return totalAvailable >= ing.Quantity;
    }


    private static void ConsumeResource(Ingredient ing, Storage s1, Storage s2, Astronaut player)
    {
        if (IsVirtualResource(ing.Item.Id_string))
        {
            ConsumeVirtualResource(ing.Item.Id_string, ing.Quantity, player);
            return;
        }

        int required = ing.Quantity;
        int availableS1 = s1.GetTotalCountOf(ing.Item);

        if (availableS1 >= required)
        {
            s1.RemoveItem(ing.Item, (byte)required);
        }
        else
        {
            if (availableS1 > 0)
            {
                s1.RemoveItem(ing.Item, (byte)availableS1);
            }

            required -= availableS1;
            s2.RemoveItem(ing.Item, (byte)required);
        }
    }

    public static bool IsVirtualResource(string itemName)
    {
        return itemName.StartsWith("$");
    }


    public static bool CheckVirtualResource(string itemStringID, int quantity, Astronaut player)
    {
        if (player == null)
        {
            Debug.Error("[Craft] Current player is null!");
            return false;
        }

        switch (itemStringID)
        {
            case "$health":
                return player.HealthBar.StatsData.Value > quantity;

            case "$power":
                return player.PowerBar.StatsData.Value >= quantity;

            default:
                Debug.Error($"[Craft] Unknown virtual resource: {itemStringID}");
                return false;
        }
    }

    private static void ConsumeVirtualResource(string resourceName, int quantity, Astronaut player)
    {
        if (player == null) return;

        switch (resourceName)
        {
            case "$health":
                player.TakeDamage(quantity);
                break;

            case "$power":
                player.PowerBar.StatsData.Decrement(quantity);
                break;
        }
    }


    public static int CalculatePossibleItemCraftCount(Storage s1, Storage s2, Blueprint blueprint, Astronaut currentPlayer)
    {
        if (s1 == null || s2 == null || blueprint == null || blueprint.Ingredients.Length == 0) return 0;

        int minCrafts = int.MaxValue;

        foreach (var ing in blueprint.Ingredients)
        {
            int available = GetAvailableQuantity(ing, s1, s2, currentPlayer);

            if (available < ing.Quantity) return 0;

            int possibleForIngredient = available / ing.Quantity;

            if (possibleForIngredient < minCrafts)
            {
                minCrafts = possibleForIngredient;
            }
        }

        return minCrafts;
    }

    private static int GetAvailableQuantity(Ingredient ing, Storage s1, Storage s2, Astronaut player)
    {
        if (IsVirtualResource(ing.Item.Id_string))
        {
            return GetVirtualResourceValue(ing.Item.Id_string, player);
        }

        return s1.GetTotalCountOf(ing.Item) + s2.GetTotalCountOf(ing.Item);
    }

    private static int GetVirtualResourceValue(string resourceName, Astronaut player)
    {
        if (player == null)
        {
            Debug.Error("[Craft] Current player is null!");
            return 0;
        }

        switch (resourceName)
        {
            case "$health":
                return player.HealthBar.StatsData.Value;

            case "$power":
                return player.PowerBar.StatsData.Value;

            default:
                Debug.Error($"[Craft] Unknown virtual resource: {resourceName}");
                return 0;
        }
    }

}
