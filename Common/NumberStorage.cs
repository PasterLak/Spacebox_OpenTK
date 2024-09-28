using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spacebox.Common
{
    public static class NumberStorage
    {
        /// <summary>
        /// Сохраняет два числа в указанный файл.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="a">Первое число.</param>
        /// <param name="b">Второе число.</param>
        public static void SaveNumbers(string path, int a, int b)
        {
            // Форматируем числа как строку с разделителем пробел
            string content = $"{a} {b}";

            // Пишем строку в файл, создавая или перезаписывая его
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Загружает два числа из указанного файла.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <returns>Кортеж с двумя числами.</returns>
        public static (int, int) LoadNumbers(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл не найден: {path}");

            // Читаем содержимое файла
            string content = File.ReadAllText(path).Trim();

            // Разбиваем строку по пробелу
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
