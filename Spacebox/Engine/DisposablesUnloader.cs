namespace Spacebox.Engine
{
    internal class DisposablesUnloader
    {
        private static List<IDisposable> disposables = new List<IDisposable>();

        public static void Add(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        public static void Dispose()
        {
            if (disposables.Count == 0) return;

            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
