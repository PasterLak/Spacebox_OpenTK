using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Engine.Multithreading;


namespace Engine.Multithreading
{

    public class WorkerPoolTest
    {

        private static double HeavyCompute(int iterations, CancellationToken ct)
        {
            double acc = 0;
            for (int i = 0; i < iterations; i++)
            {
                ct.ThrowIfCancellationRequested();
                acc += Math.Sin(i * 0.0001);
            }
            return acc;
        }

        public static async Task RunTest()
        {
            const int tasksCount = 8;
            const int iterations = 2_000_000_000; 

            var sw = Stopwatch.StartNew();
            var jobs = new List<Task<double>>(tasksCount);

            for (int i = 0; i < tasksCount; i++)
            {
                jobs.Add(WorkerPoolManager.Enqueue<double>(
                    ct => HeavyCompute(iterations, ct),
                    WorkerPoolManager.Priority.High
                ));
            }

            Console.WriteLine($"Dispatched {tasksCount} jobs in {sw.ElapsedMilliseconds} ms");

            double[] results = await Task.WhenAll(jobs);
            sw.Stop();

            Console.WriteLine($"All done in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"Result [0]: {results[0]:F5}");
            Console.WriteLine($"Tasks queued:   {WorkerPoolManager.TasksQueued}");
            Console.WriteLine($"Tasks completed:{WorkerPoolManager.TasksCompleted}");
            Console.WriteLine($"Tasks failed:  {WorkerPoolManager.TasksFailed}");
        }
    }

}
