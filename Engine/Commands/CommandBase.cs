

namespace Engine.Commands
{
    public abstract class CommandBase
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void Execute(string[] args);

        protected bool TryParse<T>(string param, out T result) where T : IParsable<T>
        {
            return T.TryParse(param, null, out result);
        }

        protected bool Is<T>(string param) where T : IParsable<T>
        {
            return T.TryParse(param, null, out _);
        }

        protected bool ValidateArgs(string[] args, int expectedCount)
        {
            if (args.Length != expectedCount)
            {
                Debug.Error($"Error: Expected {expectedCount} arguments, but got {args.Length}.");
                Debug.Error($"Usage: {Name} - {Description}");
                return false;
            }
            return true;
        }
    }
}