namespace Engine.Multithreading;

internal interface IWorkItem
{
    void Execute();
    void Cancel();
}

internal abstract class WorkItem<T> : TaskCompletionSource<T>, IWorkItem
{
    private readonly CancellationToken _token;

    protected WorkItem(CancellationToken token) : base(TaskCreationOptions.RunContinuationsAsynchronously)
    {
        _token = token;
    }

    public void Execute()
    {
        if (_token.IsCancellationRequested)
        {
            TrySetCanceled(_token);
            return;
        }

        T result;
        try
        {
            result = Run(_token);
        }
        catch (OperationCanceledException)
        {
            TrySetCanceled();
            return;
        }
        catch (Exception ex)
        {
            WorkerPoolManager.ReportFailed(ex);
            TrySetException(ex);
            return;
        }

        WorkerPoolManager.ReportCompleted();
        TrySetResult(result);
    }

    public void Cancel() => TrySetCanceled();

    protected abstract T Run(CancellationToken token);
}

internal sealed class ActionWorkItem : WorkItem<bool>
{
    private readonly Action<CancellationToken> _action;

    public ActionWorkItem(Action<CancellationToken> action, CancellationToken token) : base(token)
    {
        _action = action;
    }

    protected override bool Run(CancellationToken token)
    {
        _action(token);
        return true;
    }
}

internal sealed class FuncWorkItem<T> : WorkItem<T>
{
    private readonly Func<CancellationToken, T> _func;

    public FuncWorkItem(Func<CancellationToken, T> func, CancellationToken token) : base(token)
    {
        _func = func;
    }

    protected override T Run(CancellationToken token) => _func(token);
}
