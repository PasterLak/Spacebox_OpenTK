using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Windows;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class CraftTooltip : VBoxContainer
{
    private const float IconSize = 20f;
    private const float TextWidth = 240f;
    private const float PictureHeight = 110f;

    private readonly List<(Label Label, Ingredient Ingredient)> _ingredients = new List<(Label, Ingredient)>();
    private readonly Item _item;
    private bool _pictureAdded;

    public CraftTooltip(CraftingCategory.Data data)
    {
        Name = "CraftTooltip";
        _item = data?.item;
        MouseFilter = MouseFilter.Ignore;
        AddThemeConstantOverride("separation", 3f);

        string description = data?.item?.Description ?? "";
        if (description.Length > 0)
            AddChild(new Label { Text = description, ThemeTypeVariation = "MutedLabel", Autowrap = true, CustomMinimumSize = new Vector2(TextWidth, 0f), MouseFilter = MouseFilter.Ignore });

        var ingredients = data?.blueprint?.Ingredients;
        if (ingredients == null)
        {
            AddChild(StatusText.Create("Not craftable", StatusKind.Bad));
            return;
        }

        AddChild(new Label { Text = "Requires:", ThemeTypeVariation = "TooltipLabel", MouseFilter = MouseFilter.Ignore });
        foreach (var ingredient in ingredients)
        {
            var row = AddChild(new HBoxContainer { MouseFilter = MouseFilter.Ignore });
            row.AddThemeConstantOverride("separation", 4f);
            var icon = row.AddChild(new ItemIconControl(IconSize) { MouseFilter = MouseFilter.Ignore });
            icon.Set(ingredient.Item.Icon, 0, "");
            var label = row.AddChild(StatusText.Create(ingredient.ToString(), KindOf(ingredient)));
            label.MouseFilter = MouseFilter.Ignore;
            label.VerticalAlignment = VerticalAlignment.Center;
            _ingredients.Add((label, ingredient));
        }
    }

    private void AddPicture(Item item)
    {
        if (item is not IItemTooltipImage picture || picture.TooltipImage == null) return;
        float aspect = Math.Clamp(picture.TooltipImageAspect, ItemTooltipPicture.MinAspect, ItemTooltipPicture.MaxAspect);
        AddChild(new TextureRect
        {
            Texture = picture.TooltipImage,
            FlipV = true,
            StretchMode = TextureStretchMode.KeepAspectCentered,
            CustomMinimumSize = new Vector2(MathF.Round(PictureHeight * aspect), PictureHeight),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            MouseFilter = MouseFilter.Ignore
        });
    }

    public void Refresh()
    {
        if (!_pictureAdded)
        {
            _pictureAdded = true;
            AddPicture(_item);
        }
        foreach (var (label, ingredient) in _ingredients) StatusText.SetKind(label, KindOf(ingredient));
    }

    private static StatusKind KindOf(Ingredient ingredient) => CraftingGUI.AvailableCount(ingredient) >= ingredient.Quantity ? StatusKind.Good : StatusKind.Bad;
}
