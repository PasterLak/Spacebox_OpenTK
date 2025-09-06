using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Engine.Utils
{
    public static class JsonFixer
    {
        public static string FixJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return json;

            json = NormalizeWhitespace(json);
        
            json = FixUnquotedKeys(json);
            json = FixSingleQuotes(json);
            json = FixMissingCommas(json);
            json = FixTrailingCommas(json);
            json = FixDuplicateCommas(json);

            return json;
        }

           private static string NormalizeWhitespace(string json)
        {
            var lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var result = new StringBuilder();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.AppendLine(trimmed);
                }
                else
                {
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        private static string FixUnquotedKeys(string json)
        {
            return Regex.Replace(json, @"(\{|,|\[)\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*:",
                "$1\"$2\":", RegexOptions.Multiline);
        }

        private static string FixSingleQuotes(string json)
        {
            var result = new StringBuilder();
            bool inDoubleQuotes = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    escaped = false;
                    result.Append(c);
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    result.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inDoubleQuotes = !inDoubleQuotes;
                    result.Append(c);
                }
                else if (c == '\'' && !inDoubleQuotes)
                {
                    result.Append('"');
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        private static string FixMissingCommas(string json)
        {
            var lines = json.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var result = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (i < lines.Length - 1 && !string.IsNullOrEmpty(trimmed))
                {
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        string nextTrimmed = lines[j].Trim();
                        if (string.IsNullOrEmpty(nextTrimmed)) continue;

                        bool needsComma = (trimmed.EndsWith("}") || trimmed.EndsWith("]") ||
                                         trimmed.EndsWith("\"") || Regex.IsMatch(trimmed, @"\d$") ||
                                         trimmed.EndsWith("true") || trimmed.EndsWith("false") ||
                                         trimmed.EndsWith("null")) &&
                                        (nextTrimmed.StartsWith("{") || nextTrimmed.StartsWith("[") ||
                                         nextTrimmed.StartsWith("\"") || nextTrimmed.Contains(":")) &&
                                        !trimmed.EndsWith(",");

                        if (needsComma)
                        {
                            line = line.TrimEnd() + ",";
                        }
                        break;
                    }
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }

        private static string FixTrailingCommas(string json)
        {
            return Regex.Replace(json, @",\s*([}\]])", "$1", RegexOptions.Multiline);
        }

        private static string FixDuplicateCommas(string json)
        {
            return Regex.Replace(json, @",\s*,+", ",", RegexOptions.Multiline);
        }

        public static T LoadJsonSafe<T>(string path) where T : class
        {
            string json = "";
            try
            {
                json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException)
            {
                try
                {
                    Debug.Error("[JsonFixer] JSON syntax errors found and fixed - " + path);
                    string fixedJson = FixJson(json);
                    return JsonSerializer.Deserialize<T>(fixedJson);
                }
                catch (Exception ex)
                {
                    Debug.Error($"[JsonFixer] Failed to fix JSON: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[JsonFixer] Unexpected error: {ex.Message}");
                return null;
            }
        }
    }
}