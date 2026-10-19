using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
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
    private LineRenderer _reservedAreaRenderer;
    private readonly List<Vector3> _reservedAreaPoints = new List<Vector3>(16);
    private static readonly int[] ReservedAreaPath = { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
    private readonly Vector3[] _reservedCorners = new Vector3[8];
    private Matrix4 _currentTransform = Matrix4.Identity;
    private Matrix4 _magnetTransform = Matrix4.Identity;
    private Matrix4 _entityRotation = Matrix4.Identity;
    private Vector3 _highlightSize = Vector3.One;
    private Direction _blockDirection = Block.DefaultDirection;
    private Rotation _blockRotation = Block.DefaultRotation;
    private bool _shapeSupported;
    private int _shapePoseKey = -1;

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
            Scale = new Vector3(1.05f),
            DisposesTexture = false
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

        _reservedAreaRenderer = new LineRenderer
        {
            Color = Color4.Cyan,
            Thickness = MagnetLineThickness,
            Enabled = false
        };
        AddChild(_reservedAreaRenderer);

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
        _currentTransform = Matrix4.Identity;
        _magnetTransform = Matrix4.Identity;
        _highlightSize = Vector3.One;
        _shapePoseKey = -1;
        HideReservedArea();
        ApplyBlockTransform();
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

        if (CurrentBlockData != null && CurrentBlockData.AllSidesAreSame() && !CurrentBlockData.IsMultiblock)
        {
            _onSurface = false;
        }

        SetupBlockMesh();
        UpdateBlockRotation();
    }

    private void SetupBlockMesh()
    {
        if (CurrentBlockData == null || CurrentBlockData.Shape != null) return;

        if (CurrentBlockData.IsMultiblock)
        {
            var (vertices, indices) = MultiblockPreviewMesh.Build(CurrentBlockData, BlockState.Inactive);
            SimpleBlock.SetCustomMesh(vertices, indices);
            return;
        }

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

        if (CurrentBlockData.Shape != null)
        {
            _currentTransform = Matrix4.Identity;
            _magnetTransform = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, _blockDirection, _blockRotation);
            UpdateShapeMesh(CurrentBlockData.Shape);
            ApplyBlockTransform();
            _transformDirty = true;
            return;
        }
        else if (CurrentBlockData.AllSidesAreSame() && !CurrentBlockData.IsMultiblock)
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

        _magnetTransform = _currentTransform;
        ApplyBlockTransform();
        _transformDirty = true;
    }

    private void UpdateShapeMesh(BlockShape shape)
    {
        int key = ((int)_blockDirection * 4 + (int)_blockRotation) * 2 + (_shapeSupported ? 1 : 0);
        if (key == _shapePoseKey) return;

        _shapePoseKey = key;
        var pose = shape.GetPose(_blockDirection, _blockRotation, _shapeSupported);
        var (vertices, indices) = BlockShapePreviewMesh.Build(CurrentBlockData, shape, pose, BlockState.Inactive);
        SimpleBlock.SetCustomMesh(vertices, indices);
    }

    private void ApplyBlockTransform()
    {
        SimpleBlock.SetBlockTransform(Matrix4.CreateScale(_highlightSize) * _currentTransform * _entityRotation);
    }

    public void SetPlaceable(bool placeable)
    {
        SimpleBlock.Enabled = placeable;
    }

    public void ShowReservedArea(Vector3 minPosition, Vector3 size, SpaceEntity entity, bool placeable = true)
    {
        _reservedAreaRenderer.Color = placeable ? Color4.Cyan : Color4.Red;
        _reservedCorners[0] = minPosition;
        _reservedCorners[1] = minPosition + new Vector3(size.X, 0, 0);
        _reservedCorners[2] = minPosition + new Vector3(size.X, 0, size.Z);
        _reservedCorners[3] = minPosition + new Vector3(0, 0, size.Z);
        _reservedCorners[4] = minPosition + new Vector3(0, size.Y, 0);
        _reservedCorners[5] = minPosition + new Vector3(size.X, size.Y, 0);
        _reservedCorners[6] = minPosition + size;
        _reservedCorners[7] = minPosition + new Vector3(0, size.Y, size.Z);

        _reservedAreaPoints.Clear();
        for (int i = 0; i < ReservedAreaPath.Length; i++)
        {
            var corner = _reservedCorners[ReservedAreaPath[i]];
            _reservedAreaPoints.Add(entity != null ? entity.ToWorldSpace(corner) : corner);
        }

        _reservedAreaRenderer.SetPoints(_reservedAreaPoints);
        _reservedAreaRenderer.Enabled = true;
    }

    public void HideReservedArea()
    {
        if (_reservedAreaRenderer != null)
            _reservedAreaRenderer.Enabled = false;
    }

    public void SetHighlightSize(Vector3 size)
    {
        if (CurrentBlockData != null) size = Vector3.One;
        if (_highlightSize == size) return;

        _highlightSize = size;
        ApplyBlockTransform();
    }

    public void UpdatePosition(Vector3 position, Direction direction, Rotation rotation, bool onSurface, SpaceEntity entity = null)
    {
        Vector3 center = position + Vector3.One * 0.5f;
        Vector3 newPosition = entity != null ? entity.ToWorldSpace(center) : center;
        Matrix4 entityRotation = entity != null && entity.HasRotation ? entity.RotationMatrix : Matrix4.Identity;
        bool shapeSupported = onSurface && IsShapeSupported(position, direction, rotation, entity);

        if (SimpleBlock.Position == newPosition &&
            _shapeSupported == shapeSupported &&
            _blockDirection == direction &&
            _blockRotation == rotation &&
            _onSurface == onSurface &&
            _entityRotation == entityRotation)
        {
            return;
        }

        if (_entityRotation != entityRotation)
        {
            _entityRotation = entityRotation;
            ApplyBlockTransform();
        }

        LineRenderer.Enabled = !onSurface;
        SimpleBlock.Position = newPosition;

        _blockDirection = direction;
        _blockRotation = rotation;
        _shapeSupported = shapeSupported;

        if (CurrentBlockData != null)
        {
            if (_onSurface != onSurface)
            {
                Rotation = Block.DefaultRotation;
            }

            _onSurface = onSurface;

            if (CurrentBlockData.AllSidesAreSame() && !CurrentBlockData.IsMultiblock)
            {
                _onSurface = false;
            }

            UpdateBlockRotation();
        }

        _transformDirty = true;
    }

    private bool IsShapeSupported(Vector3 position, Direction direction, Rotation rotation, SpaceEntity entity)
    {
        var shape = CurrentBlockData?.Shape;
        if (shape == null || entity == null) return false;

        var local = entity.WorldPositionToLocal(position);
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        return BlockShapes.IsSupported(entity, cell, shape, direction, rotation);
    }

    public override void Update()
    {
        if (!Settings.ShowInterface) return;
        if (UIManager.IsUIMode) return;

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

        if (Input.IsActionDown("magnet"))
        {
            EnableMagnet = !EnableMagnet;
        }
    }

    public override void Render()
    {
        if (!Settings.ShowInterface) return;

        bool showMagnet = EnableMagnet && _onSurface && CurrentBlockData != null && !UIManager.IsUIMode;

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
        base.Render();
    }

    public override void OnGUI()
    {
        if (!Settings.ShowInterface) return;
        if (Debug.IsVisible) return;
        base.OnGUI();

        if (_magnetTextTimer > 0)
        {
            CenteredText.Show();
            _magnetTextTimer -= Time.Delta;
            CenteredText.SetText($"Magnet: {(EnableMagnet? "on" : "off")}");
        }
        
    }

    private void UpdateMagnetVisuals()
    {
        _cachedMagnetPoints.Clear();

        Vector3 pos = SimpleBlock.Position;

        for (int i = 0; i < _baseCorners.Length; i++)
        {
            Vector4 transformed = _baseCorners[i] * (_magnetTransform * _entityRotation);
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
        _reservedAreaRenderer?.Dispose();
        LineRenderer?.Dispose();
        Instance = null;
    }
}