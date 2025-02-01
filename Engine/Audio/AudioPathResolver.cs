using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.Audio
{
    public static class AudioPathResolver
    {
        public static string ResolvePath(string inputPath, string baseDirectory, List<string> allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                return null;

            
            if (Path.IsPathRooted(inputPath) && File.Exists(inputPath))
                return Path.GetFullPath(inputPath);

         
            string combinedPath = Path.Combine(baseDirectory, inputPath);
            if (File.Exists(combinedPath))
                return Path.GetFullPath(combinedPath);

            string filename = Path.GetFileName(inputPath);
            string relativePath = Path.GetDirectoryName(inputPath) ?? string.Empty;

          
            foreach (var ext in allowedExtensions)
            {
                string searchPath = string.IsNullOrEmpty(relativePath) ? filename + ext : Path.Combine(relativePath, filename + ext);
                string fullPath = Path.Combine(baseDirectory, searchPath);
                if (File.Exists(fullPath))
                    return Path.GetFullPath(fullPath);
            }

          
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
