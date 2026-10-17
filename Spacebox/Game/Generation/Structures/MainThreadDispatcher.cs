
using System.Collections.Concurrent;
using System.Diagnostics;


namespace Spacebox.Game.Generation;

public class MainThreadDispatcher
{
    private static MainThreadDispatcher _instance;
    public static MainThreadDispatcher Instance => _instance ?? (_instance = new MainThreadDispatcher());
    private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
    public void Enqueue(Action action) => _queue.Enqueue(action);
    public int PendingCount => _queue.Count;

    public void ExecutePending()
    {
        while (_queue.TryDequeue(out var action))
            action();
    }

    public void ExecutePending(double budgetMs)
    {
        if (budgetMs >= double.MaxValue)
        {
            ExecutePending();
            return;
        }

        long deadline = Stopwatch.GetTimestamp() + (long)(budgetMs * Stopwatch.Frequency / 1000.0);
        while (_queue.TryDequeue(out var action))
        {
            action();
            if (Stopwatch.GetTimestamp() >= deadline) return;
        }
    }
}

