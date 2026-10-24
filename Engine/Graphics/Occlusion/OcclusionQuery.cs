using Engine.Graphics.Rhi;

namespace Engine.Graphics.Occlusion;

public sealed class OcclusionQuery : IDisposable
{
    private QueryHandle _handle;

    public OcclusionQuery()
    {
        if (!Application.IsHeadless) _handle = Gpu.Device.CreateOcclusionQuery();
    }

    public void Begin()
    {
        if (_handle.IsValid) Gpu.Device.BeginOcclusionQuery(_handle);
    }

    public void End()
    {
        if (_handle.IsValid) Gpu.Device.EndOcclusionQuery();
    }

    public bool TryGetResult(out bool anySamplesPassed)
    {
        anySamplesPassed = true;
        if (!_handle.IsValid) return true;

        return Gpu.Device.TryGetOcclusionQueryResult(_handle, out anySamplesPassed);
    }

    public void Dispose()
    {
        if (!_handle.IsValid) return;

        Gpu.Device.DeleteQuery(_handle);
        _handle = QueryHandle.None;
    }
}
