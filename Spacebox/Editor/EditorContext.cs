using Engine.Light;
using Spacebox.Editor.Data;
using Spacebox.Editor.Preview;

namespace Spacebox.Editor;

public sealed class EditorContext : IDisposable
{
    private const float StatusDuration = 4f;
    private static readonly OpenTK.Mathematics.Vector3 PreviewAmbient = new OpenTK.Mathematics.Vector3(1f, 1f, 1f);

    private readonly List<PreviewViewport> _viewports = new List<PreviewViewport>();
    private float _statusTime;

    public GameSetProject Project { get; }
    public EditorAssets Assets { get; } = new EditorAssets();
    public string StatusText { get; private set; } = "";
    public bool StatusIsError { get; private set; }
    public bool ReloadRequested { get; private set; }
    public bool ExitRequested { get; private set; }

    public EditorContext(GameSetProject project)
    {
        Project = project;
    }

    public PreviewViewport CreateViewport()
    {
        var viewport = new PreviewViewport();
        _viewports.Add(viewport);
        return viewport;
    }

    public void RequestReload()
    {
        ReloadRequested = true;
    }

    public void ClearReloadRequest()
    {
        ReloadRequested = false;
    }

    public void RequestExit()
    {
        ExitRequested = true;
    }

    public void SetStatus(string text, bool isError = false)
    {
        StatusText = text;
        StatusIsError = isError;
        _statusTime = StatusDuration;
    }

    public void Update(float deltaTime)
    {
        if (_statusTime > 0f)
        {
            _statusTime -= deltaTime;
            if (_statusTime <= 0f) StatusText = "";
        }

        for (int i = 0; i < _viewports.Count; i++)
            _viewports[i].Update(deltaTime);
    }

    public void InvalidatePreviews()
    {
        for (int i = 0; i < _viewports.Count; i++)
            _viewports[i].Clear();
    }

    public void RenderPreviews()
    {
        var ambient = Lighting.AmbientColor;
        int fog = Lighting.FogDensity;
        Lighting.AmbientColor = PreviewAmbient;
        Lighting.FogDensity = 0;

        try
        {
            for (int i = 0; i < _viewports.Count; i++)
                _viewports[i].Render();
        }
        finally
        {
            Lighting.AmbientColor = ambient;
            Lighting.FogDensity = fog;
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < _viewports.Count; i++)
            _viewports[i].Dispose();
        _viewports.Clear();
    }
}
