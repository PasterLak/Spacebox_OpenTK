namespace Engine.Graphics.Rhi;

public sealed class GpuStateTracker
{
    public const int MaxTextureUnits = 32;
    public const int MaxColorAttachments = 8;

    private static readonly int TextureTypeCount = Enum.GetValues<TextureType>().Length;

    private readonly int[] _textures = new int[MaxTextureUnits * TextureTypeCount];
    private readonly bool[] _colorWrite = new bool[MaxColorAttachments];
    private int _activeUnit;
    private int _program;
    private bool _depthTest;
    private bool _depthWrite;
    private bool _blend;
    private bool _culling;
    private CullFace _cullFace;
    private BlendFactor _blendSource;
    private BlendFactor _blendDestination;
    private bool _blendFuncKnown;

    public GpuStateTracker()
    {
        Reset();
    }

    public int ActiveUnit => _activeUnit;
    public int Program => _program;
    public bool DepthTest => _depthTest;
    public bool DepthWrite => _depthWrite;
    public bool Blend => _blend;
    public bool Culling => _culling;
    public CullFace CullFace => _cullFace;

    public void Reset()
    {
        Array.Clear(_textures);
        Array.Fill(_colorWrite, true);
        _activeUnit = 0;
        _program = 0;
        _depthTest = true;
        _depthWrite = true;
        _blend = false;
        _culling = false;
        _cullFace = CullFace.Back;
        _blendSource = BlendFactor.One;
        _blendDestination = BlendFactor.Zero;
        _blendFuncKnown = true;
    }

    public void InvalidateTextures()
    {
        Array.Clear(_textures);
        _activeUnit = -1;
    }

    public void InvalidateBlendFunc() => _blendFuncKnown = false;

    public void InvalidateProgram() => _program = -1;

    public bool SetActiveUnit(int unit)
    {
        if (_activeUnit == unit) return false;
        _activeUnit = unit;
        return true;
    }

    public bool SetTexture(int unit, TextureType type, int texture)
    {
        int slot = Slot(unit, type);
        if (_textures[slot] == texture) return false;
        _textures[slot] = texture;
        return true;
    }

    public int GetTexture(int unit, TextureType type) => _textures[Slot(unit, type)];

    public void ForgetTexture(int texture)
    {
        for (int i = 0; i < _textures.Length; i++)
        {
            if (_textures[i] == texture) _textures[i] = 0;
        }
    }

    public bool SetProgram(int program)
    {
        if (_program == program) return false;
        _program = program;
        return true;
    }

    public void ForgetProgram(int program)
    {
        if (_program == program) _program = 0;
    }

    public bool SetDepthTest(bool enabled) => Change(ref _depthTest, enabled);

    public bool SetDepthWrite(bool enabled) => Change(ref _depthWrite, enabled);

    public bool SetBlend(bool enabled) => Change(ref _blend, enabled);

    public bool SetCulling(bool enabled) => Change(ref _culling, enabled);

    public bool SetCullFace(CullFace face)
    {
        if (_cullFace == face) return false;
        _cullFace = face;
        return true;
    }

    public bool SetBlendFunction(BlendFactor source, BlendFactor destination)
    {
        if (_blendFuncKnown && _blendSource == source && _blendDestination == destination) return false;
        _blendSource = source;
        _blendDestination = destination;
        _blendFuncKnown = true;
        return true;
    }

    public bool SetColorWrite(int attachment, bool enabled)
    {
        if ((uint)attachment >= MaxColorAttachments) return true;
        return Change(ref _colorWrite[attachment], enabled);
    }

    private static int Slot(int unit, TextureType type)
    {
        if ((uint)unit >= MaxTextureUnits) throw new ArgumentOutOfRangeException(nameof(unit));
        return (int)type * MaxTextureUnits + unit;
    }

    private static bool Change(ref bool field, bool value)
    {
        if (field == value) return false;
        field = value;
        return true;
    }
}
