
using System.IO.Compression;

namespace ServerCommon
{
    public static class ZipManager
    {
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
