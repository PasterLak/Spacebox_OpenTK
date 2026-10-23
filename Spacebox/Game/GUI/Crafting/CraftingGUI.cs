using Engine;
using Engine.Audio;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using System;
using System.Linq;

namespace Spacebox.Game.GUI;

public class CraftingGUI
{
    private static bool _wasVisible = false;


    private static CraftingCategory[] category;
    private static Storage Inventory;
    private static Storage Panel;

    private static AudioSource scrollAudio;
    private static AudioSource clickAudio;

    private static AudioSource openSound;
    private static AudioSource closeSound;

    private static int ColumnsOfItems;
    private static int hovered = -1;
    private static LocalAstronaut currentPlayer;

    public static void Toggle(LocalAstronaut player)
    {
        currentPlayer = player;
        Inventory = player.Inventory;
        Panel = player.Panel;

        if (player.GameMode != GameMode.Survival)
        {
            UIManager.Toggle("crafting", "inventory", "creative");
        }
        else
        {
            UIManager.Toggle("crafting", "inventory");
        }
    }

    static int CategoriesCountX = 0;
    const int CategoriesCountY = 2;

    public static void Init()
    {
        category = CraftingCategoryLayout.Arrange(GameAssets.CraftingCategories.Values, CategoriesCountY, out CategoriesCountX);

        var slotSize = CategoriesCountX * 1.5f;

        ColumnsOfItems = (int)Math.Ceiling(slotSize);


        scrollAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/scroll.ogg"), AudioChannel.UI);
        clickAudio = new AudioSource(GameAssets.LoadResource<AudioClip>("Resources/Audio/click1.ogg"), AudioChannel.UI);

        if (openSound == null || openSound.IsDisposed)
        {
            openSound = new AudioSource(Resources.Load<AudioClip>("openBlock1"), AudioChannel.UI);
            openSound.Volume = 1f;
        }

        if (closeSound == null || closeSound.IsDisposed)
        {
            closeSound = new AudioSource(Resources.Load<AudioClip>("openBlock4"), AudioChannel.UI);
            closeSound.Volume = 1f;
        }
    }

    public static bool IsVisible { get; private set; }
    public static CraftingCategory[] Categories => category;
    public static int CategoryColumns => CategoriesCountX;
    public static int CategoryRows => CategoriesCountY;
    public static int ItemColumns => ColumnsOfItems;
    public static LocalAstronaut Player => currentPlayer;

    public static void Update()
    {
        bool isVisible = UIManager.IsOpen("crafting");

        if (isVisible != _wasVisible)
        {
            if (isVisible) openSound?.Play();
            else closeSound?.Play();

            _wasVisible = isVisible;
        }

        IsVisible = isVisible;
    }

    public static void PlayClick() => clickAudio?.Play();

    public static void PlayHover(int id)
    {
        if (SpaceboxWindow.Instance != null && !SpaceboxWindow.Instance.IsFocused) return;
        if (hovered == id || scrollAudio == null) return;

        hovered = id;
        if (scrollAudio.IsPlaying) scrollAudio.Stop();
        scrollAudio.Play();
    }

    public static bool CanCraft(CraftingCategory.Data data)
    {
        if (data?.blueprint == null || Inventory == null) return false;
        foreach (var ingredient in data.blueprint.Ingredients)
        {
            if (!CraftingLogic.IsResourceAvailable(ingredient, Inventory, Panel, currentPlayer)) return false;
        }
        return true;
    }

    public static int PossibleCount(CraftingCategory.Data data)
    {
        return data?.blueprint == null || Inventory == null ? 0 : CraftingLogic.CalculatePossibleItemCraftCount(Inventory, Panel, data.blueprint, currentPlayer);
    }

    public static int AvailableCount(Ingredient ingredient)
    {
        if (ingredient.Item.Id_string == "$health") return currentPlayer?.HealthBar.StatsData.Value ?? 0;
        if (ingredient.Item.Id_string == "$power") return currentPlayer?.PowerBar.StatsData.Value ?? 0;
        return Inventory == null ? 0 : Inventory.GetTotalCountOf(ingredient.Item) + Panel.GetTotalCountOf(ingredient.Item);
    }

    public static void Craft(Blueprint blueprint)
    {
        clickAudio?.Play();
        OnClick(blueprint);
    }

    private static void OnClick(Blueprint blueprint)
    {

        if (blueprint == null)
        {
            Debug.Error("[Craft] Blueprint is equal null!");
            return;
        }

        if (Inventory == null)
        {
            Debug.Error("[Craft] Inventory is equal null!");
            return;
        }
        if (Panel == null)
        {
            Debug.Error("[Craft] Panel is equal null!");
            return;
        }
        if (blueprint.Product == null)
        {
            Debug.Error("[Craft] Blueprint.Product is equal null!");
            return;
        }
        if (blueprint.Product.Item == null)
        {
            Debug.Error("[Craft] Blueprint.Product.Item is equal null!");
            return;
        }

        var bp = blueprint;
        int times = 1;

        if (Input.IsKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift))
        {
            bp = bp.Clone();

            var possibleCount = CraftingLogic.CalculatePossibleItemCraftCount(Inventory, Panel, bp, currentPlayer);

            if (possibleCount >= 5)
            {
                Blueprint.ScaleBlueprint(bp, 5);
                times = 5;
            }
            else
            {

                if (possibleCount > 1)
                {
                    Blueprint.ScaleBlueprint(bp, possibleCount);
                    times = possibleCount;
                }
            }

        }

        if (CraftingLogic.TryGetResources(Inventory, Panel, bp, currentPlayer))
        {
            var item = bp.Product.Item;
            var total = bp.Product.Quantity;
            Spacebox.Network.Items.ItemSync.ClaimCraft(blueprint.Product.Item, times);
            var isGear = item.Is<WeaponItem>() || item.Is<DrillItem>();

            var (primary, secondary) = isGear ? (Panel, Inventory) : (Inventory, Panel);

            primary.TryAddItem(item, total, out var rest);

            if (rest > 0)
            {
                secondary.TryAddItem(item, rest, out rest);
            }

            if (rest > 0)
                Spacebox.Game.Generation.World.DropEffectManager?.DropItem(currentPlayer.Position, currentPlayer.Front, 1f, item, rest);

            currentPlayer.PlayerStatistics.ItemsCrafted += total;
            Spacebox.Game.Player.PlayerActivity.Raise(Spacebox.Game.Player.PlayerActivityKind.ItemCrafted, item.Id, total);
        }
        else
        {

        }
    }



    public static void Dispose()
    {
        if (scrollAudio != null)
        {
            scrollAudio.Dispose();
            scrollAudio = null;
        }
        if (clickAudio != null)
        {
            clickAudio.Dispose();
            clickAudio = null;
        }
        if (openSound != null)
        {
            openSound.Dispose();
            openSound = null;
        }
        if (closeSound != null)
        {
            closeSound.Dispose();
            closeSound = null;
        }
        Inventory = null;
        Panel = null;
        currentPlayer = null;
        category = null;
        hovered = -1;

    }
}