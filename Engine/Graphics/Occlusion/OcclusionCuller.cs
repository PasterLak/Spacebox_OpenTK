using OpenTK.Mathematics;

namespace Engine.Graphics.Occlusion;

public sealed class OcclusionCuller : IDisposable
{
    public const float TeleportDistance = 64f;

    private readonly struct Test
    {
        public readonly OcclusionState State;
        public readonly Matrix4 Model;
        public readonly Vector3 Min;
        public readonly Vector3 Max;

        public Test(OcclusionState state, Matrix4 model, Vector3 min, Vector3 max)
        {
            State = state;
            Model = model;
            Min = min;
            Max = max;
        }
    }

    private readonly List<Test> _tests = new List<Test>();
    private OcclusionBoxRenderer _boxes;
    private Camera _camera;
    private Vector3 _cameraPosition;
    private long _frame;
    private int _epoch;
    private int _drawn;
    private int _occluded;

    public bool Enabled { get; set; } = true;
    public int DrawnLastFrame { get; private set; }
    public int OccludedLastFrame { get; private set; }

    public static bool NeedsReset(bool cameraChanged, Vector3 previousPosition, Vector3 currentPosition)
    {
        return cameraChanged || Vector3.DistanceSquared(previousPosition, currentPosition) > TeleportDistance * TeleportDistance;
    }

    public void BeginFrame(Camera camera)
    {
        _frame++;
        DrawnLastFrame = _drawn;
        OccludedLastFrame = _occluded;
        _drawn = 0;
        _occluded = 0;
        _tests.Clear();

        if (camera == null) return;
        if (NeedsReset(camera != _camera, _cameraPosition, camera.Position)) _epoch++;
        _camera = camera;
        _cameraPosition = camera.Position;
    }

    public bool Prepare(OcclusionState state, bool cameraNear, Matrix4 model, Vector3 min, Vector3 max)
    {
        if (!Enabled || Application.IsHeadless)
        {
            _drawn++;
            return true;
        }

        state.Touch(_frame, _epoch);
        if (state.Pending && state.Query != null && state.Query.TryGetResult(out bool anySamplesPassed))
            state.Apply(anySamplesPassed);

        if (cameraNear)
            state.ForceVisible();
        else if (!state.Pending)
            _tests.Add(new Test(state, model, min, max));

        if (state.Visible) _drawn++;
        else _occluded++;
        return state.Visible;
    }

    public void Flush()
    {
        if (_tests.Count == 0) return;

        _boxes ??= new OcclusionBoxRenderer();
        _boxes.Begin();
        for (int i = 0; i < _tests.Count; i++)
        {
            var test = _tests[i];
            test.State.Query ??= new OcclusionQuery();
            test.State.Query.Begin();
            _boxes.Draw(test.Model, test.Min, test.Max);
            test.State.Query.End();
            test.State.Pending = true;
        }
        _boxes.End();
        _tests.Clear();
    }

    public void Dispose()
    {
        _boxes?.Dispose();
        _boxes = null;
        _tests.Clear();
    }
}
