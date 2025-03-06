using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;


namespace Spacebox
{
    internal class Application
    {
        public const string Version = "0.1.2";
        public const string Author = "PasterLak";
        public const string EngineVersion = "1.5";

        public static PlatformName Platform => CheckPlatform();
        private static PlatformName CheckPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return PlatformName.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return PlatformName.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return PlatformName.OSX;
            }
            else
            {
                return PlatformName.Unknown;
            }
        }

        public static string GetSystemLanguage()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        public static string GetSystemLanguageDisplayName()
        {
            return CultureInfo.CurrentCulture.DisplayName;
        }

        public static bool IsSystemLanguage(string languageCode)
        {
            return string.Equals(CultureInfo.CurrentCulture.TwoLetterISOLanguageName, languageCode, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSystemLanguageOneOf(string[] languageCodes)
        {
            foreach (string languageCode in languageCodes)
            {
                if (IsSystemLanguage(languageCode)) return true;
            }
            return false;
        }

        public static void OpenLink(string url)
        {
            if (url == string.Empty) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Engine.Debug.Error($"Failed to open URL: {ex.Message}");
            }
        }

        public enum PlatformName : byte
        {
            Windows,
            Linux,
            OSX,
            Unknown
        }

    }
}
