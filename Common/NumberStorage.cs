
namespace Spacebox.Common
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
                throw new FileNotFoundException($"Файл не найден: {path}");

         
            string content = File.ReadAllText(path).Trim();

            string[] parts = content.Split(' ');

            if (parts.Length < 2)
                throw new FormatException("Файл должен содержать как минимум два числа, разделенных пробелом.");

            // Парсим числа
            if (!int.TryParse(parts[0], out int a))
                throw new FormatException($"Не удалось распарсить первое число: {parts[0]}");

            if (!int.TryParse(parts[1], out int b))
                throw new FormatException($"Не удалось распарсить второе число: {parts[1]}");

            return (a, b);
        }
    }
}
