using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Engine;
using Spacebox.Game.Resource;

namespace Spacebox.Game.GUI
{
    public static class Theme
    {
        public static class Colors
        {
            public static Color4Byte Background { get; set; } = new Color4Byte(191, 191, 191, 255);
            public static Color4Byte Deep { get; set; } = new Color4Byte(166, 166, 166, 255);
            public static Color4Byte Deep2 { get; set; } = new Color4Byte(115, 115, 115, 255);
            public static Color4Byte BorderLight { get; set; } = new Color4Byte(230, 230, 230, 255);
            public static Color4Byte BorderDark { get; set; } = new Color4Byte(128, 128, 128, 255);
        }

        public static void LoadThemeColors(string modFolderPath)
        {
            try
            {
                string modPath = Path.Combine(modFolderPath, "Resources", "theme.json");
                string defaultPath = Path.Combine("Resources", "theme.json");
                string finalPath = File.Exists(modPath) ? modPath : defaultPath;

                if (File.Exists(finalPath))
                {
                    string json = File.ReadAllText(finalPath);
                    var rawData = JsonSerializer.Deserialize<Dictionary<string, Color4Byte>>(json);

                    if (rawData != null)
                    {
                        foreach (var kvp in rawData)
                        {
                            if (kvp.Key.Equals("Background", StringComparison.OrdinalIgnoreCase)) Colors.Background = kvp.Value;
                            else if (kvp.Key.Equals("Deep", StringComparison.OrdinalIgnoreCase)) Colors.Deep = kvp.Value;
                            else if (kvp.Key.Equals("Deep2", StringComparison.OrdinalIgnoreCase)) Colors.Deep2 = kvp.Value;
                            else if (kvp.Key.Equals("BorderLight", StringComparison.OrdinalIgnoreCase)) Colors.BorderLight = kvp.Value;
                            else if (kvp.Key.Equals("BorderDark", StringComparison.OrdinalIgnoreCase)) Colors.BorderDark = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

        }
    }
}