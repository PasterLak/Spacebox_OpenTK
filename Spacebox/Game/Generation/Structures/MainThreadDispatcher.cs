
using System.Collections.Concurrent;


namespace Spacebox.Game.Generation;

public class MainThreadDispatcher
{
    private static MainThreadDispatcher _instance;
    public static MainThreadDispatcher Instance => _instance ?? (_instance = new MainThreadDispatcher());
    private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
    public void Enqueue(Action action) => _queue.Enqueue(action);
    public void ExecutePending()
    {
        while (_queue.TryDequeue(out var action))
            action();
    }
}

