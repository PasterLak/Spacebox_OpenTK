using System;
using System.Collections.Generic;
using System.IO;

namespace Spacebox.Common.Audio
{
    public static class AudioPathResolver
    {
        public static string ResolvePath(string inputPath, string baseDirectory, List<string> allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return null;

            // Проверка, является ли inputPath абсолютным путем и существует ли файл
            if (Path.IsPathRooted(inputPath) && File.Exists(inputPath))
                return Path.GetFullPath(inputPath);

            // Если inputPath содержит подкаталоги, рассматриваем его как относительный к baseDirectory
            string combinedPath = Path.Combine(baseDirectory, inputPath);
            if (File.Exists(combinedPath))
                return Path.GetFullPath(combinedPath);

            string filename = Path.GetFileName(inputPath);
            string relativePath = Path.GetDirectoryName(inputPath) ?? string.Empty;

            // Попытка добавить разрешенные расширения
            foreach (var ext in allowedExtensions)
            {
                string searchPath = string.IsNullOrEmpty(relativePath) ? filename + ext : Path.Combine(relativePath, filename + ext);
                string fullPath = Path.Combine(baseDirectory, searchPath);
                if (File.Exists(fullPath))
                    return Path.GetFullPath(fullPath);
            }

            // Если inputPath не имеет расширения, попробуем все разрешенные расширения
            if (Path.GetExtension(inputPath).Length == 0)
            {
                foreach (var ext in allowedExtensions)
                {
                    string searchPath = string.IsNullOrEmpty(relativePath) ? filename + ext : Path.Combine(relativePath, filename + ext);
                    string fullPath = Path.Combine(baseDirectory, searchPath);
                    if (File.Exists(fullPath))
                        return Path.GetFullPath(fullPath);
                }
            }

            // Поиск рекурсивно во всех подкаталогах
            foreach (var ext in allowedExtensions)
            {
                string searchPattern = Path.GetFileNameWithoutExtension(filename) + ext;
                try
                {
                    var files = Directory.EnumerateFiles(baseDirectory, searchPattern, SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        return Path.GetFullPath(file);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            return null;
        }
    }
}
