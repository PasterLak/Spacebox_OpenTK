namespace Engine
{
    public static class NumberStorage
    {
        public static void SaveNumbers(string path, int a, int b)
        {
        
            string content = $"{a} {b}";

            File.WriteAllText(path, content);
        }

        public static (int, int) LoadNumbers(string path)
        {
          
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            string content = File.ReadAllText(path).Trim();

            string[] parts = content.Split(' ');
    
            if (parts.Length < 2)
                throw new FormatException("The file must contain at least two numbers separated by a space.");

          
            if (!int.TryParse(parts[0], out int a))
                throw new FormatException($"Failed to parse the first number: {parts[0]}");

          
            if (!int.TryParse(parts[1], out int b))
                throw new FormatException($"Failed to parse the second number: {parts[1]}");

           
            return (a, b);
        }
    }
}
