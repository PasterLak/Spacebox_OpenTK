namespace Engine.Commands
{
    public interface ICommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(string[] args);

        public bool IsInt(string param)
        {
            return int.TryParse(param, out _);
        }

        public bool IsFloat(string param)
        {
            return float.TryParse(param, out _);
        }

        public bool IsByte(string param)
        {
            return byte.TryParse(param, out _);
        }


    }
}
