using System.IO.Compression;

namespace SpaceNetwork.Utilities
{
    public static class ZipHelper
    {
        public static byte[] ReadZipFromFile(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }
        public static void UnzipData(byte[] zipData, string outputDirectory)
        {
            using (var ms = new MemoryStream(zipData))
            using (var archive = new ZipArchive(ms))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullPath = Path.Combine(outputDirectory, entry.FullName);
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    if (!string.IsNullOrEmpty(entry.Name))
                        entry.ExtractToFile(fullPath, true);
                }
            }
        }

        public static byte[] CreateZipFromFolder(string folderPath)
        {

            string tempZip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            ZipFile.CreateFromDirectory(folderPath, tempZip, CompressionLevel.Optimal, false);
            byte[] data = File.ReadAllBytes(tempZip);
            File.Delete(tempZip);
            return data;
        }
    }
}
