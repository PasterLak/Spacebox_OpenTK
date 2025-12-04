using Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class CraftingLogic
{

    public static bool TryGetResources(Storage s1, Storage s2, Blueprint blueprint, Astronaut currentPlayer)
    {
        foreach (var ing in blueprint.Ingredients)
        {
            int totalAvailable = 0;
            if (ing.Item.Name == "$health")
            {
                if (currentPlayer != null)
                {
                    if (currentPlayer.HealthBar.StatsData.Value > ing.Quantity)
                    {
                        continue;
                    }
                    else return false;
                }
                else
                {
                    Debug.Error("[Craft] Current player is null!");
                    return false;
                }
            }
            else
            {
                totalAvailable = s1.GetTotalCountOf(ing.Item) + s2.GetTotalCountOf(ing.Item);
            }
            if (totalAvailable < ing.Quantity)
                return false;
        }
        foreach (var ing in blueprint.Ingredients)
        {
            int required = ing.Quantity;
            int availableS1 = 0;

            if (ing.Item.Name == "$health")
            {
                if (currentPlayer != null)
                {
                    var hp = currentPlayer.HealthBar.StatsData.Value;
                    if (hp > required)
                    {

                        currentPlayer.TakeDamage(required);
                        continue;
                    }
                    else return false;
                }
                else
                {
                    Debug.Error("[Craft] Current player is null!");
                    return false;
                }
            }

            availableS1 = s1.GetTotalCountOf(ing.Item);


            if (availableS1 >= required)
            {

                s1.RemoveItem(ing.Item, (byte)required);
                continue;
            }
            else
            {
                if (availableS1 > 0)
                    s1.RemoveItem(ing.Item, (byte)availableS1);
                required -= availableS1;
                int availableS2 = s2.GetTotalCountOf(ing.Item);
                if (availableS2 >= required)
                    s2.RemoveItem(ing.Item, (byte)required);
                else
                    s2.RemoveItem(ing.Item, (byte)availableS2);
            }
        }
        return true;
    }


    public static int CalculatePossibleItemCraftCount(Storage Inventory, Storage Panel, Blueprint blueprint, Astronaut currentPlayer)
    {
        if (Inventory == null) return 0;
        if (Panel == null) return 0;
        if (blueprint == null) return 0;
        if (blueprint.Ingredients.Length == 0) return 0;

        int[] r = new int[blueprint.Ingredients.Length];

        var min = int.MaxValue;

        for (int i = 0; i < r.Length; i++)
        {
            r[i] = 0;

            if (blueprint.Ingredients[i].Item.Name == "$health")
            {
                if (currentPlayer != null)
                    r[i] = currentPlayer.HealthBar.StatsData.Value;
                else
                {
                    Debug.Error("[Craft] Current player is null!");
                    r[i] = 0;
                }

            }
            else
            {
                r[i] = Inventory.GetTotalCountOf(blueprint.Ingredients[i].Item) + Panel.GetTotalCountOf(blueprint.Ingredients[i].Item);
            }
            r[i] = r[i] / blueprint.Ingredients[i].Quantity;

            if (r[i] < min) min = r[i];
        }

        return min;
    }

}
