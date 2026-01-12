using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;
using Spacebox.Game.Generation;

namespace Spacebox.Game;

public class BlockSelector : IDisposable
{

    public static BlockSelector Instance;
    public static bool IsVisible = false;

    private Texture2D selectorTexture;
    public SimpleBlock SimpleBlock { get; private set; }

    private LineRenderer _magnetLineRenderer;
    private Matrix4 _currentTransform = Matrix4.Identity;

    private Direction blockDirection = Block.DefaultDirection;
    private Rotation blockRotation = Block.DefaultRotation;

    private bool _enableMagnet = true;
    public bool EnableMagnet
    {
        get => _enableMagnet;
        private set
        {
            _enableMagnet = value;
            Rotation = Block.DefaultRotation;
        }
    }
    public Rotation Rotation = Block.DefaultRotation;
    private BlockData currentBlockData;
    public BlockData CurrentBlockData => currentBlockData;

    public BlockSelector()
    {
        Instance = this;

        selectorTexture = GameAssets.LoadResource<Texture2D>("Resources/Textures/selector.png");
        selectorTexture.FilterMode = FilterMode.Nearest;

        var material = new TextureMaterial(selectorTexture, Resources.Load<Shader>("Resources/Shaders/blockPreview"));
        material.Color = new Color4(1f, 1f, 1f, 1f);

        SimpleBlock = new SimpleBlock(material, Vector3.Zero);
        SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);

        _magnetLineRenderer = new LineRenderer();
        _magnetLineRenderer.Color = Color4.Yellow;
        _magnetLineRenderer.Thickness = 0.05f;

        PanelUI.OnSlotChanged += OnSelectedSlotWasChanged;
        OnSelectedSlotWasChanged(PanelUI.SelectedSlotId);
    }

    public void OnSelectedSlotWasChanged(short slot)
    {
        SimpleBlock.ResetBlockTransform();
       
        if (PanelUI.IsHolding<DrillItem>())
        {
            SimpleBlock.Material.MainTexture = selectorTexture;
            SimpleBlock.Scale = new Vector3(1.05f, 1.05f, 1.05f);
            SimpleBlock.ResetUV();
            currentBlockData = null;
        }
        else if (PanelUI.IsHolding<BlockItem>())
        {
            var blockItem = PanelUI.CurrentSlot().Item as BlockItem;
            if (blockItem != null)
            {
                currentBlockData = GameAssets.GetBlockDataById(blockItem.Id);
                SimpleBlock.Material.MainTexture = GameAssets.BlocksTexture;
                SimpleBlock.Scale = new Vector3(1f, 1f, 1f);
                if (currentBlockData != null)
                {
                    if (currentBlockData.AllSidesAreSame())
                        _onSurface = false;
                }
                SetupBlockMesh();
                UpdateBlockRotation();
            }
        }
        else
        {
            Rotation = Rotation.None;
        }
    }

    private void SetupBlockMesh()
    {
        if (currentBlockData == null) return;

        for (byte i = 0; i < 6; i++)
        {
            Face face = (Face)i;
            Vector2[] uv = currentBlockData.GetFaceUV(face, BlockState.Inactive);
            SimpleBlock.ChangeUV(uv, face, false);
        }

        SimpleBlock.RegenerateMesh();
    }

    private void UpdateBlockRotation()
    {
        if (currentBlockData == null) return;
        if (currentBlockData.AllSidesAreSame())
        {
            _currentTransform = Matrix4.Identity;
            return;
        }

        _currentTransform = BlockRotationHelper.CalculateTransformMatrix(
            currentBlockData.BaseFrontDirection,
            blockDirection,
            blockRotation
        );

        SimpleBlock.SetBlockTransform(_currentTransform);
    }
    private bool _onSurface;
    public void UpdatePosition(Vector3 position, Direction direction, Rotation rotation, bool onSurface)
    {
        Vector3 newPosition = position + Vector3.One * 0.5f;


        if (SimpleBlock.Position == newPosition && blockDirection == direction && blockRotation == rotation)
            return;

        World.Instance.LineRenderer.Enabled = !onSurface;

        SimpleBlock.Position = newPosition;

        blockDirection = direction;
        blockRotation = rotation;
        if (currentBlockData != null)
        {
            _onSurface = onSurface;

            if (currentBlockData.AllSidesAreSame())
                _onSurface = false;

            UpdateBlockRotation();
        }
    }

    public void Render()
    {
        if (!IsVisible || !Settings.ShowInterface) return;

        if (Input.IsActionDown("block_rotate"))
        {
            Rotation = (Rotation)(((byte)Rotation + 1) % 4);
            if (currentBlockData != null)
            {
                UpdateBlockRotation();
            }
        }

        if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.M))
        {
            EnableMagnet = !EnableMagnet;

        }
        SimpleBlock.Render();

        if (EnableMagnet && _onSurface && currentBlockData != null)
        {
            UpdateMagnetVisuals();
            _magnetLineRenderer.Render();
        }
    }

    private void UpdateMagnetVisuals()
    {
        float size = 0.51f;
        Vector4[] bottomCorners = new Vector4[]
        {
            new Vector4(-size, -size, -size, 1f),
            new Vector4(size, -size, -size, 1f),
            new Vector4(size, -size, size, 1f),
            new Vector4(-size, -size, size, 1f)
        };

        var points = new List<Vector3>();
        for (int i = 0; i < bottomCorners.Length; i++)
        {
            Vector4 transformed = bottomCorners[i] * _currentTransform;
            points.Add(transformed.Xyz + SimpleBlock.Position);
        }
        points.Add(points[0]);

        _magnetLineRenderer.SetPoints(points);
    }

    public Direction GetDirection() => blockDirection;
    public Rotation GetRotation() => Rotation;

    public void Dispose()
    {
        SimpleBlock?.Destroy();
        selectorTexture?.Dispose();
        _magnetLineRenderer?.Dispose();
        Instance = null;
    }
}