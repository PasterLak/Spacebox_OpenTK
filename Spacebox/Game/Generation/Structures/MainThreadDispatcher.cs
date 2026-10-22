
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
    public void Clear() => _queue.Clear();

    public void ExecutePending()
    {
        while (_queue.TryDequeue(out var action))
            Run(action);
    }

    private static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Engine.Debug.Error($"[MainThreadDispatcher] {ex}");
        }
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
            Run(action);
            if (Stopwatch.GetTimestamp() >= deadline) return;
        }
    }
}

