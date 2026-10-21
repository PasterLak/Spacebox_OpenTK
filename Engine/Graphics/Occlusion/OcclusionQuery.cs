using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics.Occlusion;

public sealed class OcclusionQuery : IDisposable
{
    private int _handle;

    public OcclusionQuery()
    {
        if (!Application.IsHeadless) _handle = GL.GenQuery();
    }

    public void Begin()
    {
        if (_handle != 0) GL.BeginQuery(QueryTarget.AnySamplesPassed, _handle);
    }

    public void End()
    {
        if (_handle != 0) GL.EndQuery(QueryTarget.AnySamplesPassed);
    }

    public bool TryGetResult(out bool anySamplesPassed)
    {
        anySamplesPassed = true;
        if (_handle == 0) return true;

        GL.GetQueryObject(_handle, GetQueryObjectParam.QueryResultAvailable, out int available);
        if (available == 0) return false;

        GL.GetQueryObject(_handle, GetQueryObjectParam.QueryResult, out int result);
        anySamplesPassed = result != 0;
        return true;
    }

    public void Dispose()
    {
        if (_handle == 0) return;

        GL.DeleteQuery(_handle);
        _handle = 0;
    }
}
