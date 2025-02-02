
namespace SpaceNetwork
{
    public class KeyValueFileWriter
    {
        public static void WriteFile(string fileName, Dictionary<string, string> data)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var kvp in data)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
        }
    }
}
