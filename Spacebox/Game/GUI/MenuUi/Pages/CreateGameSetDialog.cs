using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class CreateGameSetDialog
{
    private const float Width = 520f;

    private readonly GameMenu _menu;
    private readonly MenuWindowFrame _frame = MenuLayout.Window("Create", Width);
    private readonly MenuModal _modal;
    private readonly SelectableRow _gameSetChoice = KindRow("New GameSet", "A complete GameSet of your own. It starts from Empty (no blocks, items or asteroids), so it works right away and you add everything yourself. Shown in the GameSet list when you create a world.");
    private readonly SelectableRow _extensionChoice = KindRow("Extension", "Adds or changes content of an existing GameSet, for example Default, without copying it. Players turn it on per world in the Extensions list.");
    private readonly LineEdit _name = new LineEdit { MaxLength = GameSetCreator.MaxNameLength, PlaceholderText = "My GameSet" };
    private readonly LineEdit _author = new LineEdit { MaxLength = 32 };
    private readonly ChoiceBox _base = new ChoiceBox();
    private readonly VBoxContainer _baseField;
    private readonly Label _baseHint = new Label { ThemeTypeVariation = "MutedLabel", Autowrap = true };
    private readonly Label _error = new Label { ThemeTypeVariation = "ErrorLabel", Autowrap = true };
    private readonly List<InstalledGameSet> _bases = new List<InstalledGameSet>();
    private NewGameSetKind _kind = NewGameSetKind.GameSet;

    public CreateGameSetDialog(GameMenu menu)
    {
        _menu = menu;
        var kinds = new VBoxContainer();
        kinds.AddThemeConstantOverride("separation", 0f);
        kinds.AddChild(_gameSetChoice);
        kinds.AddChild(_extensionChoice);
        var kindBox = new PanelContainer { ThemeTypeVariation = "InsetPanel" };
        kindBox.AddChild(kinds);
        _frame.Body.AddChild(kindBox);
        _gameSetChoice.Selected += _ => SetKind(NewGameSetKind.GameSet);
        _extensionChoice.Selected += _ => SetKind(NewGameSetKind.Extension);

        _frame.Body.AddChild(MenuLayout.Field("Name", _name));
        _frame.Body.AddChild(MenuLayout.Field("Author", _author));
        _baseField = MenuLayout.Field("Extends", _base);
        _frame.Body.AddChild(_baseField);
        _baseField.AddChild(_baseHint);
        _base.IndexChanged += _ => UpdateBaseHint();
        _frame.Body.AddChild(_error);
        _name.TextSubmitted += _ => Create();

        _frame.Footer.AddChild(MenuLayout.Button(menu, "Create", Create));
        _frame.Footer.AddChild(MenuLayout.Button(menu, "Cancel", Close));
        _modal = new MenuModal(_frame.Root);
    }

    public void Open()
    {
        _bases.Clear();
        _bases.AddRange(GameSetCreator.ExtendableBases(_menu.InstalledGameSets));
        var names = new List<string> { AnyGameSet };
        var icons = new List<Texture2D> { null };
        foreach (var baseSet in _bases)
        {
            names.Add(baseSet.DisplayName);
            icons.Add(_menu.GetModById(baseSet.Id)?.Icon);
        }
        _base.SetChoices(names, icons, true);
        _base.SelectedIndex = _bases.FindIndex(b => string.Equals(b.Id, "default", StringComparison.OrdinalIgnoreCase)) + 1;
        UpdateBaseHint();

        _name.Text = "";
        _author.Text = ClientConfigStore.Load()?.PlayerNickname ?? "";
        SetError(null);
        SetKind(NewGameSetKind.GameSet);
        _modal.Open(MenuCanvas.Current?.Root);
        _name.GrabFocus();
    }

    public void Close() => _modal.Close();

    private static SelectableRow KindRow(string title, string description)
    {
        var row = new SelectableRow(0f);
        row.AddLine(title);
        row.AddLine(description, "MutedLabel", true);
        return row;
    }

    private void SetKind(NewGameSetKind kind)
    {
        _kind = kind;
        _gameSetChoice.IsSelected = kind == NewGameSetKind.GameSet;
        _extensionChoice.IsSelected = kind == NewGameSetKind.Extension;
        _baseField.Visible = kind == NewGameSetKind.Extension;
        _name.PlaceholderText = kind == NewGameSetKind.GameSet ? "My GameSet" : "My Extension";
    }

    private void Create()
    {
        var request = new NewGameSetRequest
        {
            Kind = _kind,
            Name = _name.Text,
            Author = _author.Text,
            BaseId = _kind == NewGameSetKind.Extension ? SelectedBaseId() : ""
        };

        string error = GameSetCreator.Validate(request, _menu.InstalledGameSets);
        if (error != null)
        {
            SetError(error);
            return;
        }

        Close();
        error = _menu.CreateGameSet(request);
        if (error != null) _menu.ShowMessage("Create", error);
    }

    private const string AnyGameSet = "Any GameSet (general pack)";

    private string SelectedBaseId()
    {
        int index = _base.SelectedIndex;
        if (index == 0) return ExtensionTargets.Any;
        return index > 0 && index <= _bases.Count ? _bases[index - 1].Id : "";
    }

    private bool SelectedIsExtension()
    {
        int index = _base.SelectedIndex;
        return index > 0 && index <= _bases.Count && _bases[index - 1].IsExtension;
    }

    private void UpdateBaseHint()
    {
        _baseHint.Text = _base.SelectedIndex == 0
            ? "Works with every GameSet. Bring your own items, projectiles and sounds, start your ids with the pack id, and put recipes that need items of one GameSet into Bases/<its id>/."
            : SelectedIsExtension()
                ? "Adds to this extension. Worlds that pick yours get it automatically, on every GameSet it works with."
                : "Adds to this GameSet only and needs its current version or newer.";
    }

    private void SetError(string text)
    {
        _error.Text = text ?? "";
        _error.Visible = !string.IsNullOrEmpty(text);
    }
}
