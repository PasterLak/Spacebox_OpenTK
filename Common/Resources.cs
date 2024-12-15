namespace Spacebox.Common
{
    public interface IResource : IDisposable
    {

    }

    public class Res : IResource
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public static class Resources
    {

        public static T Load<T>(string path) where T : class, IResource
        {
            return default(T);
        }


        public static void Test()
        {
            var x = Resources.Load<Res>("ttt");
        }
    }
}
