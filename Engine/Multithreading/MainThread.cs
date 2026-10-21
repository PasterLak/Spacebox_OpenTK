using System.Collections.Concurrent;

namespace Engine;

public static class MainThread
{
    private static readonly ConcurrentQueue<Action> _afterRenderActions = new ConcurrentQueue<Action>();

    public static Task<T> Run<T>(Func<T> action)
    {
        return Enqueue(EngineWindow._mainThreadActions, action);
    }

    public static Task Run(Action action)
    {
        return Run(() =>
        {
            action();
            return true;
        });
    }

    public static Task<T> RunAfterRender<T>(Func<T> action)
    {
        return Enqueue(_afterRenderActions, action);
    }

    internal static void RunPendingAfterRender()
    {
        while (_afterRenderActions.TryDequeue(out var action)) action();
    }

    private static Task<T> Enqueue<T>(ConcurrentQueue<Action> queue, Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Enqueue(() =>
        {
            try
            {
                completion.SetResult(action());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }
}
