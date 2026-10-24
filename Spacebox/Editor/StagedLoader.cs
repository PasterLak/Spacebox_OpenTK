namespace Spacebox.Editor;

public sealed class StagedLoader
{
    private readonly List<(string Name, Action Run)> _steps = new List<(string, Action)>();
    private int _index;
    private bool _started;

    public StagedLoader Add(string name, Action run)
    {
        _steps.Add((name, run));
        return this;
    }

    public bool IsDone => _index >= _steps.Count;
    public string CurrentStep => IsDone ? "" : _steps[_index].Name;
    public float Progress => _steps.Count == 0 ? 1f : (float)_index / _steps.Count;

    public void Tick()
    {
        if (IsDone) return;
        if (!_started)
        {
            _started = true;
            return;
        }

        var step = _steps[_index];
        _index++;
        step.Run();
    }
}
