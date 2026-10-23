using Engine.Multithreading;

namespace Engine.Tests.Core;

public class WorkerPoolManagerTests
{
    [Fact]
    public async Task Enqueue_RunsTasksOfEveryPriority()
    {
        int done = 0;
        var tasks = new List<Task>();
        var priorities = new[] { WorkerPoolManager.Priority.High, WorkerPoolManager.Priority.Medium, WorkerPoolManager.Priority.Low };
        for (int i = 0; i < 300; i++)
            tasks.Add(WorkerPoolManager.Enqueue(_ => Interlocked.Increment(ref done), priorities[i % 3]));

        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(300, done);
    }

    [Fact]
    public async Task Enqueue_ReturnsResult()
    {
        int result = await WorkerPoolManager.Enqueue(_ => 21 * 2, WorkerPoolManager.Priority.Low).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Enqueue_StartsPromptlyWhenIdle()
    {
        await WorkerPoolManager.Enqueue(_ => { }, WorkerPoolManager.Priority.Low).WaitAsync(TimeSpan.FromSeconds(10));
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 20; i++)
            await WorkerPoolManager.Enqueue(_ => { }, WorkerPoolManager.Priority.Medium).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(watch.ElapsedMilliseconds < 500, $"20 sequential tasks took {watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task Enqueue_CancelledTokenCancelsTask()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var task = WorkerPoolManager.Enqueue(_ => { }, WorkerPoolManager.Priority.High, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task Enqueue_FailedTaskIsFaultedAndCounted()
    {
        long failed = WorkerPoolManager.TasksFailed;

        var task = WorkerPoolManager.Enqueue(_ => throw new InvalidOperationException("boom"), WorkerPoolManager.Priority.Medium);

        await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(WorkerPoolManager.TasksFailed > failed);
    }

    [Fact]
    public async Task Enqueue_ContinuationsDoNotRunOnWorkerThreads()
    {
        var task = WorkerPoolManager.Enqueue(_ => Thread.CurrentThread.Name, WorkerPoolManager.Priority.Medium);
        var continuation = task.ContinueWith(_ => Thread.CurrentThread.Name, TaskContinuationOptions.ExecuteSynchronously);

        string worker = await task.WaitAsync(TimeSpan.FromSeconds(10));
        string after = await continuation.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.StartsWith("WorkerPool-", worker);
        Assert.False(after != null && after.StartsWith("WorkerPool-"), $"continuation ran on {after}");
    }

    [Fact]
    public async Task Workers_RunBelowNormalPriority()
    {
        var priority = await WorkerPoolManager.Enqueue(_ => Thread.CurrentThread.Priority, WorkerPoolManager.Priority.High).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ThreadPriority.BelowNormal, priority);
    }
}
