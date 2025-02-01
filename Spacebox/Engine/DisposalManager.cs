using System.Collections.Concurrent;

namespace Spacebox.Engine
{
   
    public static class DisposalManager
    {
      
        private static readonly ConcurrentQueue<IDisposable> _disposalQueue = new ConcurrentQueue<IDisposable>();

      
        public static void EnqueueForDispose(IDisposable disposable)
        {
            if (disposable == null)
                return;

            _disposalQueue.Enqueue(disposable);
        }

        public static void ProcessDisposals()
        {
            if(_disposalQueue.Count < 1) return;

            while (_disposalQueue.TryDequeue(out IDisposable disposable))
            {
                try
                {
                    disposable.Dispose();

                }
                catch (Exception ex)
                {
                   
                    Debug.Error($"[DisposalManager] error dispose: {ex.Message}");
                 
                }
                finally
                {
                    Debug.Success("Was disposed by DisposalManager!");
                }
            }
        }
    }
}
