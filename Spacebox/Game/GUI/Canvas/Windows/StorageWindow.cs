using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.GUI.MenuUi;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class StorageWindow : GameWindow
{
    private const int MaxNameLength = 31;

    private readonly StorageGridControl _grid = new StorageGridControl();
    private readonly IconButton _rename = new IconButton(HeaderButtonSize) { TooltipText = "Rename" };
    private readonly LineEdit _nameEdit = new LineEdit { ThemeTypeVariation = "HeaderField", SizeFlagsVertical = SizeFlags.ShrinkCenter, MaxLength = MaxNameLength, CustomMinimumSize = new Vector2(140f, 0f), Visible = false };
    private readonly Button _save;
    private readonly Button _combine;
    private readonly Button _sort;
    private Storage _shown;

    private readonly StorageUI _storage;

    public StorageWindow(StorageUI storage) : base("Storage")
    {
        _storage = storage;
        Name = "Storage";
        Header.AddChild(_rename);
        Header.AddChild(_nameEdit);
        _save = AddHeaderButton("Save", "Save the name", SaveName);
        _save.Visible = false;
        _combine = AddHeaderButton(StorageButtons.CombineText, StorageButtons.CombineTooltip, () => StorageButtons.Combine(_storage.Storage));
        _sort = AddHeaderButton(StorageButtons.SortText, StorageButtons.SortTooltip, () => StorageButtons.Sort(_storage.Storage));
        _rename.Clicked += StartRename;
        _nameEdit.TextSubmitted += _ => SaveName();
        _grid.SlotClicked += _storage.OnSlotClicked;
        Body.AddChild(_grid);
    }

    public override bool IsOpen => UIManager.IsOpen("storage") && _storage.Storage != null;

    public override Sounds.UiSoundSpec OpenSound => new Sounds.UiSoundSpec("openStorage", 1f, 0.08f);
    public override Sounds.UiSoundSpec CloseSound => new Sounds.UiSoundSpec("closeStorage", 1f, 0.08f);

    protected override void Refresh()
    {
        var storage = _storage.Storage;
        if (!ReferenceEquals(storage, _shown))
        {
            _shown = storage;
            EndRename();
        }

        bool editing = _nameEdit.Visible;
        string title = _storage.StorageBlock?.Name ?? storage.Name;
        if (!editing && TitleLabel.Text != title) TitleLabel.Text = title;
        _rename.SetSprite(UIAtlas.Get("pencil"));
        _rename.Visible = !editing && _storage.StorageBlock != null && storage.SizeX >= 2;
        _combine.Visible = storage.SizeX >= 3;
        _sort.Visible = storage.SizeX >= 3;
        _grid.Show(storage);
    }

    private void StartRename()
    {
        if (_storage.StorageBlock == null) return;
        _nameEdit.Text = _storage.StorageBlock.Name;
        _nameEdit.Visible = true;
        _save.Visible = true;
        TitleLabel.Visible = false;
        _nameEdit.GrabFocus();
    }

    private void SaveName()
    {
        _storage.Rename(_nameEdit.Text);
        EndRename();
    }

    private void EndRename()
    {
        _nameEdit.Visible = false;
        _save.Visible = false;
        TitleLabel.Visible = true;
    }
}
