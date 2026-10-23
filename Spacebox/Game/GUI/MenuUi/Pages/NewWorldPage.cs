using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class NewWorldPage : MenuPage
{
    private const float Width = 440f;

    private readonly GameMenu _menu;
    private readonly LineEdit _name = new LineEdit { MaxLength = 64 };
    private readonly LineEdit _author = new LineEdit { MaxLength = 64 };
    private readonly LineEdit _seed = new LineEdit { MaxLength = 32, SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly ChoiceBox _gameMode = new ChoiceBox();
    private WorldBadges _badges;
    private readonly ChoiceBox _gameSet = new ChoiceBox();
    private readonly VBoxContainer _extensions = new VBoxContainer();
    private readonly Label _extensionStatus = new Label { Autowrap = true };
    private MenuWindowFrame _frame;
    private Button _random;
    private Button _confirm;
    private bool _hasExtensionErrors;
    private int _shownGameSet = -1;

    public NewWorldPage(GameMenu menu)
    {
        _menu = menu;
    }

    protected override void Build()
    {
        _frame = MenuLayout.Window("Create New World", Width);
        AddChild(_frame.Root);
        var body = _frame.Body;

        body.AddChild(MenuLayout.Field("World Name", _name));
        body.AddChild(MenuLayout.Field("Author", _author));

        var seedRow = new HBoxContainer();
        seedRow.AddChild(_seed);
        _random = MenuLayout.InlineIconButton(_menu, "Resources/Textures/UI/dice.png", "Pick a random seed", () => _seed.Text = new Random().Next().ToString());
        seedRow.AddChild(_random);
        body.AddChild(MenuLayout.Field("Seed", seedRow));

        _badges = new WorldBadges(_menu);
        _gameMode.SetChoices(Enum.GetNames<GameMode>());
        body.AddChild(MenuLayout.Field("Game Mode", _gameMode));
        body.AddChild(MenuLayout.Field("GameSet", _gameSet));

        var extensionBox = new PanelContainer { ThemeTypeVariation = "InsetPanel", CustomMinimumSize = new Vector2(0f, 90f) };
        var extensionColumn = new VBoxContainer();
        extensionColumn.AddChild(_extensions);
        extensionColumn.AddChild(_extensionStatus);
        extensionBox.AddChild(extensionColumn);
        body.AddChild(MenuLayout.Field("Extensions", extensionBox));

        _confirm = MenuLayout.Button(_menu, "Create", Submit);
        _frame.Footer.AddChild(_confirm);
        _frame.Footer.AddChild(MenuLayout.Button(_menu, "Cancel", _menu.SetStateToWorldSelect));

        _name.TextChanged += text => _menu.newWorldName = text;
        _author.TextChanged += text => _menu.newWorldAuthor = text;
        _seed.TextChanged += text => _menu.newWorldSeed = text;
        _name.TextSubmitted += _ => Submit();
        _gameMode.IndexChanged += index => _menu.SelectedGameModeIndex = index;
        _gameSet.IndexChanged += index =>
        {
            if (_menu.SelectedGameSetIndex != index) _menu.NewWorldExtensions.Clear();
            _menu.SelectedGameSetIndex = index;
            RebuildExtensions();
        };
    }

    public override void OnShow()
    {
        bool edit = _menu.IsEditMode;
        _frame.Title.Text = edit ? "Edit World" : "Create New World";
        _confirm.Text = edit ? "Save" : "Create";
        _name.Text = _menu.newWorldName ?? "";
        _author.Text = _menu.newWorldAuthor ?? "";
        _seed.Text = _menu.newWorldSeed ?? "";
        _author.Editable = !edit;
        _seed.Editable = !edit;
        _random.Disabled = edit;
        _gameSet.Disabled = edit;

        var names = new List<string>();
        var icons = new List<Texture2D>();
        foreach (var gameSet in _menu.GameSets)
        {
            names.Add(gameSet.ModName);
            icons.Add(gameSet.Icon);
        }
        _gameSet.SetChoices(names, icons, true);
        _gameSet.SelectedIndex = edit ? Math.Max(0, _menu.GameSets.FindIndex(g => g.ModId == _menu.selectedWorld?.ModId)) : _menu.SelectedGameSetIndex;
        _gameMode.SelectedIndex = _menu.SelectedGameModeIndex;
        _shownGameSet = -1;
        RebuildExtensions();
        _name.GrabFocus();
    }

    private void UpdateModeBadges()
    {
        string modId = _menu.IsEditMode ? _menu.selectedWorld?.ModId : _menu.GameSets.Count > 0 ? _menu.GameSets[Math.Clamp(_menu.SelectedGameSetIndex, 0, _menu.GameSets.Count - 1)].ModId : "default";
        var modes = Enum.GetValues<GameMode>();
        var options = _gameMode.Options;
        for (int i = 0; i < options.Count && i < modes.Length; i++) options[i].Badges = new List<ListBadge> { _badges.ModeBadge(modId, modes[i]) };
    }

    private void RebuildExtensions()
    {
        bool readOnly = _menu.IsEditMode;
        _shownGameSet = _gameSet.SelectedIndex;
        _extensions.ClearChildren();
        UpdateModeBadges();

        var baseConfig = readOnly ? _menu.GetModById(_menu.selectedWorld?.ModId) : _menu.GameSets.Count > 0 ? _menu.GameSets[Math.Clamp(_menu.SelectedGameSetIndex, 0, _menu.GameSets.Count - 1)] : null;
        var available = _menu.ExtensionsFor(baseConfig);
        var selected = readOnly ? WorldGameSetCheck.ExtensionsOf(_menu.selectedWorld).ToList() : _menu.NewWorldExtensions;

        if (available.Count == 0) _extensions.AddChild(new Label { Text = "No extensions installed for this GameSet", ThemeTypeVariation = "MutedLabel" });
        foreach (var extension in available)
        {
            var check = new CheckBox($"{extension.DisplayName}  v{extension.Config.Version}", Contains(selected, extension.Id))
            {
                Disabled = readOnly,
                TooltipText = Tooltip(extension),
                Icon = extension.Config.Icon,
                IconRegion = extension.Config.Icon == null ? null : new Rect2(0f, extension.Config.Icon.Height, extension.Config.Icon.Width, -extension.Config.Icon.Height)
            };
            string id = extension.Id;
            check.Toggled += (_, on) =>
            {
                _menu.Click1?.Play();
                if (on) selected.Add(id);
                else selected.RemoveAll(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
                UpdateExtensionStatus();
            };
            _extensions.AddChild(check);
        }
        UpdateExtensionStatus();
    }

    private void UpdateExtensionStatus()
    {
        _hasExtensionErrors = false;
        if (_menu.IsEditMode)
        {
            var used = WorldGameSetCheck.ExtensionsOf(_menu.selectedWorld);
            SetStatus(used.Length == 0 ? "This world uses no extensions" : "Extensions are chosen when the world is created", "MutedLabel");
            return;
        }

        if (_menu.NewWorldExtensions.Count == 0)
        {
            SetStatus("", "MutedLabel");
            return;
        }

        var resolution = _menu.ResolveNewWorldExtensions();
        _hasExtensionErrors = !resolution.Success;
        if (_hasExtensionErrors) SetStatus(resolution.ErrorText, "ErrorLabel");
        else SetStatus("Load order: " + string.Join(" > ", resolution.Order.Select(e => e.Id)), "MutedLabel");
    }

    private void SetStatus(string text, string variation)
    {
        _extensionStatus.Text = text;
        _extensionStatus.ThemeTypeVariation = variation;
        _extensionStatus.Visible = text.Length > 0;
    }

    private void Submit() => _menu.SubmitWorldForm(_hasExtensionErrors);

    private static string Tooltip(InstalledGameSet extension)
    {
        string text = string.IsNullOrWhiteSpace(extension.Config.Description) ? extension.Id : extension.Config.Description;
        if (extension.Includes.Count > 0) text += "\nIncludes: " + string.Join(", ", extension.Includes);
        return text + "\nSaved with the world, cannot be changed later";
    }

    private static bool Contains(List<string> ids, string id) => ids.Exists(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
}
