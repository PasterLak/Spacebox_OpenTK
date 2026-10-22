namespace Spacebox.Game.Generation.Blocks;

public static class PipeRegistry
{
    private static readonly List<PipeBlock> _pipes = new List<PipeBlock>();

    public static void Add(PipeBlock pipe)
    {
        if (!_pipes.Contains(pipe)) _pipes.Add(pipe);
    }

    public static void Remove(PipeBlock pipe)
    {
        _pipes.Remove(pipe);
    }

    public static void Update(float deltaTime)
    {
        for (int i = _pipes.Count - 1; i >= 0; i--)
        {
            if (i < _pipes.Count) _pipes[i].UpdateTransfer(deltaTime);
        }
    }

    public static void UnregisterEntity(SpaceEntity entity)
    {
        _pipes.RemoveAll(pipe => pipe.Entity == entity);
    }

    public static void Clear()
    {
        _pipes.Clear();
    }
}
