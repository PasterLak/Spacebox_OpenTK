using System.Numerics;
using Engine;
using Engine.Components;
using Engine.Graphics;
using Engine.Light;
using ImGuiNET;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Armor;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Spacebox.Game.GUI;

public sealed class PlayerPreview : IDisposable
{
    private static readonly Vector3 Origin = new Vector3(0f, 5000f, 0f);
    private static readonly Vector3 FigureCenter = new Vector3(0f, -0.17f, 0f);
    private const float FigureRadius = 0.75f;
    private const float StartYaw = 205f;
    private const float CameraPitch = -6f;
    public const float MaxPitch = 75f;
    private const float RotateSpeed = 0.5f;
    private const string BodyModel = "Resources/Models/Player/Astronaut_Body_Fly.obj";
    private const string HelmetModel = "Resources/Models/Player/Astronaut_Helmet_Closed.obj";
    private const string TankModel = "Resources/Models/Player/Astronaut_Tank_Fly.obj";

    private static readonly HeldItemPose HeldPose = new HeldItemPose(new Vector3(0.4f, -0.55f, -0.3f), 0.4f, new Vector3(0.4f, -0.33f, -0.45f), 0.16f);
    private static readonly EquipmentSlotType[] ArmorSlots = Enum.GetValues<EquipmentSlotType>();

    private RenderTexture _target;
    private OrbitViewCamera _camera;
    private Node3D _tilt;
    private Node3D _figure;
    private TextureMaterial _skin;
    private ArmorRenderer _armor;
    private HeldItemModels _held;
    private Skybox _skybox;
    private string _skinColor;
    private Item _heldItem;
    private float _yaw = StartYaw;
    private float _pitch;
    private bool _visible;

    public static float NormalizeYaw(float yaw)
    {
        yaw %= 360f;
        return yaw < 0f ? yaw + 360f : yaw;
    }

    public static float ClampPitch(float pitch) => Math.Clamp(pitch, -MaxPitch, MaxPitch);

    public bool Show(LocalAstronaut player, PlayerEquipment equipment, Item heldItem, Vector2 size)
    {
        if (Application.IsHeadless || player == null || size.X < 8f || size.Y < 8f) return false;

        Ensure();
        Sync(player, equipment, heldItem);
        _target.Resize((int)size.X, (int)size.Y);

        var start = ImGui.GetCursorScreenPos();
        ImGui.Image(_target.ColorTexture, size, new Vector2(0f, 1f), new Vector2(1f, 0f));
        ImGui.SetCursorScreenPos(start);
        ImGui.InvisibleButton("##playerPreview", size);

        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 0f))
        {
            var delta = ImGui.GetIO().MouseDelta;
            _yaw = NormalizeYaw(_yaw + delta.X * RotateSpeed);
            _pitch = ClampPitch(_pitch + delta.Y * RotateSpeed);
        }
        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _yaw = StartYaw;
            _pitch = 0f;
        }

        _visible = true;
        return true;
    }

    public void Render()
    {
        if (!_visible || _target == null) return;
        _visible = false;

        _figure.Rotation = new Vector3(0f, _yaw, 0f);
        _tilt.Rotation = new Vector3(_pitch, 0f, 0f);

        int fog = Lighting.FogDensity;
        Lighting.FogDensity = 0;

        try
        {
            _target.Render(_camera, DrawScene);
        }
        finally
        {
            Lighting.FogDensity = fog;
        }
    }

    public void Dispose()
    {
        _target?.Dispose();
        _target = null;
        _skybox?.Destroy();
        _skybox = null;
        _figure = null;
        _tilt = null;
        _held = null;
        _armor = null;
        _skin = null;
        _heldItem = null;
        _skinColor = null;
    }

    private void DrawScene()
    {
        _skybox?.Render();
        _figure.Render();
        _held.ItemModel.Render();
        _held.BlockModel.Render();
    }

    private void Ensure()
    {
        if (_target != null) return;

        _target = new RenderTexture(64, 64) { ClearColor = new OpenTK.Mathematics.Color4(0.03f, 0.04f, 0.06f, 1f) };
        _camera = new OrbitViewCamera();
        _camera.SetAngles(0f, CameraPitch);
        _camera.Frame(Origin + FigureCenter, FigureRadius);
        _camera.DepthFar = 500f;

        _skin = new TextureMaterial(Astronaut.GetAstronautTexture(GameSessionSkin()));
        _tilt = new Node3D { Name = "PlayerPreviewTilt", Position = Origin + FigureCenter };
        _figure = new Node3D { Name = "PlayerPreview", Position = -FigureCenter };
        _tilt.AddChild(_figure);
        _figure.AttachComponent(new ModelRendererComponent(new Model(Resources.Load<Mesh>(BodyModel), _skin)));
        _figure.AttachComponent(new ModelRendererComponent(new Model(Resources.Load<Mesh>(HelmetModel), _skin)));
        _figure.AttachComponent(new ModelRendererComponent(new Model(Resources.Load<Mesh>(TankModel), _skin)));
        _armor = _figure.AttachComponent(new ArmorRenderer());
        _held = new HeldItemModels(_figure, HeldPose);

        if (Lighting.Skybox is Skybox { Material: TextureMaterial sky } && sky.MainTexture != null)
            _skybox = new Skybox(sky.MainTexture);
    }

    private void Sync(LocalAstronaut player, PlayerEquipment equipment, Item heldItem)
    {
        string skin = GameSessionSkin();
        if (skin != _skinColor)
        {
            _skinColor = skin;
            _skin.MainTexture = Astronaut.GetAstronautTexture(skin);
        }

        foreach (var slot in ArmorSlots)
            _armor.Set(slot, equipment.GetSlot(slot) is { HasItem: true, Item: EquipmentItem item } ? item : null);

        if (!ReferenceEquals(heldItem, _heldItem))
        {
            _heldItem = heldItem;
            _held.SetItem(heldItem);
            _held.SetVisible(true);
        }
    }

    private static string GameSessionSkin()
    {
        string skin = Spacebox.Network.GameSession.SkinColor;
        return string.IsNullOrWhiteSpace(skin) ? "Yellow" : skin;
    }
}
