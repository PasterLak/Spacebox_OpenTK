using System.Security.Cryptography;
using System.Text;

public static class HashHelper
{
    public static string CalculateFolderHash(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return string.Empty;

        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                             .OrderBy(p => p).ToArray();

        using (var md5 = MD5.Create())
        {
            foreach (var file in files)
            {

                string relativePath = Path.GetRelativePath(folderPath, file);

                relativePath = relativePath.Replace("\\", "/").ToLowerInvariant();

                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath);
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                byte[] contentBytes = File.ReadAllBytes(file);
                md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            md5.TransformFinalBlock(new byte[0], 0, 0);

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
        }
    }

    public static bool VerifyFolderHash(string folderPath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return false;

        string currentHash = CalculateFolderHash(folderPath);

        return string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}