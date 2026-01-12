using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;
using Spacebox.GUI;

namespace Spacebox.Game;

public class BlockSelector : Node3D
{
    public static BlockSelector Instance;

    public static bool IsVisible
    {
        get => Instance != null && Instance.Enabled;
        set { if (Instance != null) Instance.Enabled = value; }
    }

    private const float MagnetTextDuration = 1f;
    private const float MagnetLineThickness = 0.05f;
    private const float NormalLineThickness = 0.1f;
    private const float MagnetBoxSize = 0.51f;
    private const string SelectorTexturePath = "Resources/Textures/selector.png";
    private const string ShaderPath = "Resources/Shaders/blockPreview";

    public SimpleBlock SimpleBlock { get; private set; }
    public LineRenderer LineRenderer { get; private set; }
    public BlockData CurrentBlockData { get; private set; }

    public bool EnableMagnet
    {
        get => _enableMagnet;
        private set
        {
            if (_enableMagnet == value) return;
            _enableMagnet = value;
            Rotation = Block.DefaultRotation;
            _magnetTextTimer = MagnetTextDuration;
            _transformDirty = true;
        }
    }
    public Rotation Rotation { get; private set; } = Block.DefaultRotation;

    private Texture2D _selectorTexture;
    private LineRenderer _magnetLineRenderer;
    private Matrix4 _currentTransform = Matrix4.Identity;
    private Direction _blockDirection = Block.DefaultDirection;
    private Rotation _blockRotation = Block.DefaultRotation;

    private bool _enableMagnet = true;
    private bool _onSurface;
    private float _magnetTextTimer;
    private bool _transformDirty;

    private readonly List<Vector3> _cachedMagnetPoints = new List<Vector3>(5);
    private readonly Vector4[] _baseCorners;

    public BlockSelector()
    {
        Instance = this;

        _selectorTexture = GameAssets.LoadResource<Texture2D>(SelectorTexturePath);
        _selectorTexture.FilterMode = FilterMode.Nearest;

        var shader = Resources.Load<Shader>(ShaderPath);
        var material = new TextureMaterial(_selectorTexture, shader)
        {
            Color = Color4.White
        };

        SimpleBlock = new SimpleBlock(material, Vector3.Zero)
        {
            Scale = new Vector3(1.05f)
        };
        AddChild(SimpleBlock);

        _magnetLineRenderer = new LineRenderer
        {
            Color = Color4.Yellow,
            Thickness = MagnetLineThickness,
            Enabled = false
        };
        AddChild(_magnetLineRenderer);

        LineRenderer = new LineRenderer
        {
            Thickness = NormalLineThickness,
            Color = Color4.Red,
            Enabled = false
        };
        LineRenderer.AddPoint(Vector3.Zero);
        LineRenderer.AddPoint(Vector3.One);
        AddChild(LineRenderer);

        PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;

        _baseCorners = new Vector4[]
        {
            new Vector4(-MagnetBoxSize, -MagnetBoxSize, -MagnetBoxSize, 1f),
            new Vector4(MagnetBoxSize, -MagnetBoxSize, -MagnetBoxSize, 1f),
            new Vector4(MagnetBoxSize, -MagnetBoxSize, MagnetBoxSize, 1f),
            new Vector4(-MagnetBoxSize, -MagnetBoxSize, MagnetBoxSize, 1f)
        };

        OnSelectedSlotWasChanged(PanelUI.SelectedSlotId);
    }

    public void OnSelectedSlotWasChanged(short slot)
    {
        SimpleBlock.ResetBlockTransform();
        _transformDirty = true;

        if (PanelUI.IsHolding<DrillItem>())
        {
            ConfigureForDrill();
        }
        else if (PanelUI.IsHolding<BlockItem>())
        {
            ConfigureForBlock();
        }
        else
        {
            Rotation = Rotation.None;
            CurrentBlockData = null;
        }
    }

    private void ConfigureForDrill()
    {
        SimpleBlock.Material.MainTexture = _selectorTexture;
        SimpleBlock.Scale = new Vector3(1.05f);
        SimpleBlock.ResetUV();
        CurrentBlockData = null;
    }

    private void ConfigureForBlock()
    {
        var blockItem = PanelUI.CurrentSlot().Item as BlockItem;
        if (blockItem == null) return;

        CurrentBlockData = GameAssets.GetBlockDataById(blockItem.Id);
        SimpleBlock.Material.MainTexture = GameAssets.BlocksTexture;
        SimpleBlock.Scale = Vector3.One;

        if (CurrentBlockData != null && CurrentBlockData.AllSidesAreSame())
        {
            _onSurface = false;
        }

        SetupBlockMesh();
        UpdateBlockRotation();
    }

    private void SetupBlockMesh()
    {
        if (CurrentBlockData == null) return;

        for (byte i = 0; i < 6; i++)
        {
            Face face = (Face)i;
            Vector2[] uv = CurrentBlockData.GetFaceUV(face, BlockState.Inactive);
            SimpleBlock.ChangeUV(uv, face, false);
        }

        SimpleBlock.RegenerateMesh();
    }

    private void UpdateBlockRotation()
    {
        if (CurrentBlockData == null) return;

        if (CurrentBlockData.AllSidesAreSame())
        {
            _currentTransform = Matrix4.Identity;
        }
        else
        {
            _currentTransform = BlockRotationHelper.CalculateTransformMatrix(
                CurrentBlockData.BaseFrontDirection,
                _blockDirection,
                _blockRotation
            );
        }

        SimpleBlock.SetBlockTransform(_currentTransform);
        _transformDirty = true;
    }

    public void UpdatePosition(Vector3 position, Direction direction, Rotation rotation, bool onSurface)
    {
        Vector3 newPosition = position + Vector3.One * 0.5f;

        if (SimpleBlock.Position == newPosition &&
            _blockDirection == direction &&
            _blockRotation == rotation &&
            _onSurface == onSurface)
        {
            return;
        }

        LineRenderer.Enabled = !onSurface;
        SimpleBlock.Position = newPosition;

        _blockDirection = direction;
        _blockRotation = rotation;

        if (CurrentBlockData != null)
        {
            if (_onSurface != onSurface)
            {
                Rotation = Block.DefaultRotation;
            }

            _onSurface = onSurface;

            if (CurrentBlockData.AllSidesAreSame())
            {
                _onSurface = false;
            }

            UpdateBlockRotation();
        }

        _transformDirty = true;
    }

    public override void Update()
    {
        if (!Settings.ShowInterface) return;
        base.Update();

        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.IsActionDown("block_rotate"))
        {
            Rotation = (Rotation)(((byte)Rotation + 1) % 4);
            if (CurrentBlockData != null)
            {
                UpdateBlockRotation();
            }
        }

        if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.M))
        {
            EnableMagnet = !EnableMagnet;
        }
    }

    public override void Render()
    {
        if (!Settings.ShowInterface) return;
        base.Render();

        bool showMagnet = EnableMagnet && _onSurface && CurrentBlockData != null;

        if (showMagnet)
        {
            if (_transformDirty)
            {
                UpdateMagnetVisuals();
                _transformDirty = false;
            }
            _magnetLineRenderer.Enabled = true;
        }
        else
        {
            _magnetLineRenderer.Enabled = false;
        }
    }

    public override void OnGUI()
    {
        if (!Settings.ShowInterface) return;
        base.OnGUI();

        if (_magnetTextTimer > 0)
        {
            CenteredText.Show();
            _magnetTextTimer -= Time.Delta;
            CenteredText.SetText($"Magnet: {EnableMagnet}");
        }
        else
        {
            CenteredText.Hide();
        }
    }

    private void UpdateMagnetVisuals()
    {
        _cachedMagnetPoints.Clear();

        Vector3 pos = SimpleBlock.Position;

        for (int i = 0; i < _baseCorners.Length; i++)
        {
            Vector4 transformed = _baseCorners[i] * _currentTransform;
            _cachedMagnetPoints.Add(transformed.Xyz + pos);
        }
        _cachedMagnetPoints.Add(_cachedMagnetPoints[0]);

        _magnetLineRenderer.SetPoints(_cachedMagnetPoints);
    }

    public Direction GetDirection() => _blockDirection;
    public Rotation GetRotation() => Rotation;

    public override void Destroy()
    {
        base.Destroy();
        PanelUI.OnSlotChanged -= OnSelectedSlotWasChanged;
        SimpleBlock?.Destroy();
        _selectorTexture?.Dispose();
        _magnetLineRenderer?.Dispose();
        LineRenderer?.Dispose();
        Instance = null;
    }
}