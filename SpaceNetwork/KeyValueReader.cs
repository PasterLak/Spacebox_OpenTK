

namespace SpaceNetwork
{

    public class KeyValueFileReader
    {
        public static Dictionary<string, string> ReadFile(string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            var dict = new Dictionary<string, string>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            foreach (var line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int index = line.IndexOf('=');
                if (index <= 0 || index >= line.Length - 1)
                    continue;
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();
                dict[key] = value;
            }
            return dict;
        }
    }

}
