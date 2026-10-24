using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class ParticleSystemPanel : PanelContainer
{
    private const float WidthRatio = 0.25f;
    private const float RefreshInterval = 0.2f;

    private static readonly string[] EmitterNames = { "Box", "Cone", "Disk", "Plane", "Sphere", "Point" };
    private static readonly Type[] EmitterTypes = { typeof(BoxEmitter), typeof(ConeEmitter), typeof(DiskEmitter), typeof(PlaneEmitter), typeof(SphereEmitter), typeof(PointEmitter) };
    private static readonly string[] SpaceNames = { "Local", "World" };
    private static readonly string[] FilterNames = { "Nearest", "Linear" };

    private readonly HBoxContainer _tabs = new HBoxContainer();
    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly List<ParticleRow> _bound = new List<ParticleRow>();
    private readonly Label _active = new Label { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
    private readonly ItemList _textures = new ItemList { CustomMinimumSize = new Vector2(0f, 140f), IconSize = 24f, TooltipText = "Click a texture to use it" };
    private readonly IReadOnlyList<(string Name, Texture2D Texture)> _textureBank;
    private ParticleSystem[] _systems = Array.Empty<ParticleSystem>();
    private ParticleSystem _shown;
    private Type _shownEmitter;
    private int _activeIndex;
    private float _refreshTimer;

    public ParticleSystemPanel(IReadOnlyList<(string Name, Texture2D Texture)> textureBank)
    {
        Name = "ParticleSystems";
        _textureBank = textureBank;
        PlaceAtRight(this);

        var scroll = new ScrollContainer();
        AddChild(scroll);
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        column.AddThemeConstantOverride("separation", 6f);
        scroll.AddChild(column);
        column.AddChild(new Label { Text = "PARTICLE SYSTEMS", FontColor = DarkTheme.Accent });
        column.AddChild(_tabs);
        column.AddChild(_rows);

        foreach (var (name, texture) in textureBank) _textures.AddItem(name, texture, texture);
        _textures.ItemSelected += index =>
        {
            if (_shown != null && _textures.Items[index].Metadata is Texture2D texture) _shown.Material = new ParticleMaterial(texture);
        };
    }

    public ParticleSystem Shown => _shown;

    public static void PlaceAtRight(Control panel)
    {
        panel.AnchorLeft = 1f - WidthRatio;
        panel.AnchorRight = 1f;
        panel.AnchorTop = 0f;
        panel.AnchorBottom = 1f;
        panel.GrowHorizontal = GrowDirection.Begin;
    }

    public void SetSystems(ParticleSystem[] systems)
    {
        systems ??= Array.Empty<ParticleSystem>();
        if (!SameSystems(systems))
        {
            _systems = (ParticleSystem[])systems.Clone();
            BuildTabs();
        }

        _activeIndex = _systems.Length == 0 ? 0 : Math.Clamp(_activeIndex, 0, _systems.Length - 1);
        var system = _systems.Length == 0 ? null : _systems[_activeIndex];
        if (system != _shown || system?.Emitter?.GetType() != _shownEmitter) Rebuild(system);
    }

    public override void Process(float deltaTime)
    {
        base.Process(deltaTime);
        _refreshTimer -= deltaTime;
        if (_refreshTimer > 0f) return;
        _refreshTimer = RefreshInterval;
        RefreshRows();
    }

    public void RefreshRows()
    {
        foreach (var row in _bound) row.Refresh();
        if (_shown != null) _active.Text = $"Active particles: {_shown.ParticlesCount}";
    }

    public static string GenerateEmitterCode(EmitterBase e, string varName) => ParticleSystemUI.GenerateEmitterCode(e, varName);

    private bool SameSystems(ParticleSystem[] systems)
    {
        if (systems.Length != _systems.Length) return false;
        for (int i = 0; i < systems.Length; i++)
        {
            if (!ReferenceEquals(systems[i], _systems[i])) return false;
        }
        return true;
    }

    private void BuildTabs()
    {
        _tabs.ClearChildren();
        for (int i = 0; i < _systems.Length; i++)
        {
            int index = i;
            var button = new Button { Text = $"System {i + 1}", ThemeTypeVariation = "SmallButton" };
            button.Pressed += _ => _activeIndex = index;
            _tabs.AddChild(button);
        }
    }

    private void Rebuild(ParticleSystem system)
    {
        _shown = system;
        _shownEmitter = system?.Emitter?.GetType();
        _bound.Clear();
        _rows.ClearChildren();
        if (system == null) return;

        var actions = new HBoxContainer();
        var restart = new Button { Text = "Restart" };
        restart.Pressed += _ => system.Restart();
        actions.AddChild(restart);
        var code = new Button { Text = "Copy emitter code", TooltipText = "Copy C# code that creates this emitter" };
        code.Pressed += _ => Root?.Clipboard?.SetText(GenerateEmitterCode(system.Emitter, $"emitter{_activeIndex + 1}"));
        actions.AddChild(code);
        _rows.AddChild(actions);

        Add(new ParticleCheckRow("Enabled", () => system.Enabled, v => system.Enabled = v));
        Section("Transform");
        Add(new ParticleVectorRow("Position", () => system.Position, v => system.Position = v));
        Add(new ParticleVectorRow("Rotation", () => system.Rotation, v => system.Rotation = v, 1));
        Add(new ParticleVectorRow("Scale", () => system.Scale, v => system.Scale = v));

        Section("System");
        AddTextureRows(system);
        Add(new ParticleNumberRow("Max Particles", () => system.Max, v => system.Max = Math.Max(0, (int)v), 1, true));
        var activeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };
        activeRow.AddChild(_active);
        var clear = new Button { Text = "Clear", ThemeTypeVariation = "SmallButton" };
        clear.Pressed += _ => system.ClearParticles();
        activeRow.AddChild(clear);
        _rows.AddChild(activeRow);
        Add(new ParticleNumberRow("Emission Rate", () => system.Rate, v => system.Rate = v));
        Add(new ParticleNumberRow("Simulation Spd", () => system.SimulationSpeed, v => system.SimulationSpeed = v));
        Add(new ParticleNumberRow("Duration", () => float.IsInfinity(system.Duration) ? 0f : system.Duration, v => system.Duration = v <= 0f ? float.PositiveInfinity : v) { TooltipText = "Seconds the system emits, 0 = endless" });
        Add(new ParticleCheckRow("Loop", () => system.Loop, v => system.Loop = v));
        Add(new ParticleChoiceRow("Space", SpaceNames, () => system.Space == SimulationSpace.Local ? 0 : 1, v => system.Space = v == 0 ? SimulationSpace.Local : SimulationSpace.World));
        Add(new ParticleChoiceRow("Emitter Type", EmitterNames, () => Array.IndexOf(EmitterTypes, system.Emitter.GetType()), v => ChangeEmitter(system, v)));

        Section("Emitter");
        foreach (var row in ParticleEmitterRows.Common(system.Emitter)) Add(row);
        foreach (var row in ParticleEmitterRows.For(system.Emitter)) Add(row);
        RefreshRows();
    }

    private void AddTextureRows(ParticleSystem system)
    {
        Add(new ParticleChoiceRow("Filter", FilterNames, () => system.Material?.MainTexture?.FilterMode == FilterMode.Nearest ? 0 : 1, v =>
        {
            var texture = system.Material?.MainTexture;
            if (texture != null) texture.FilterMode = v == 0 ? FilterMode.Nearest : FilterMode.Linear;
        }));
        var flips = new HBoxContainer();
        var flipX = new Button { Text = "Flip X", ThemeTypeVariation = "SmallButton" };
        flipX.Pressed += _ => system.Material?.MainTexture?.FlipX();
        var flipY = new Button { Text = "Flip Y", ThemeTypeVariation = "SmallButton" };
        flipY.Pressed += _ => system.Material?.MainTexture?.FlipY();
        flips.AddChild(flipX);
        flips.AddChild(flipY);
        _rows.AddChild(flips);
        if (_textureBank.Count > 0) _rows.AddChild(_textures);
    }

    private void ChangeEmitter(ParticleSystem system, int index)
    {
        if (index < 0 || index >= EmitterTypes.Length || system.Emitter.GetType() == EmitterTypes[index]) return;
        var emitter = (EmitterBase)Activator.CreateInstance(EmitterTypes[index])!;
        emitter.CopyFrom(system.Emitter);
        system.SetEmitter(emitter, false);
    }

    private void Add(ParticleRow row)
    {
        _bound.Add(row);
        _rows.AddChild(row);
    }

    private void Section(string title)
    {
        _rows.AddChild(new Label { Text = title, FontColor = DarkTheme.Accent });
    }
}
