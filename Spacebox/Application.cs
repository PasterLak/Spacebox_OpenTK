using System.Runtime.InteropServices;

namespace Spacebox
{
    internal class Application
    {
        public const string Version = "0.0.9";
        public const string Author = "PasterLak";
        public const string EngineVersion = "1.3";

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

        public enum PlatformName : byte
        {
            Windows,
            Linux,
            OSX,
            Unknown
        }

    }
}
