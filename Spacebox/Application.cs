using System.Runtime.InteropServices;

namespace Spacebox
{
    internal class Application
    {
        public const string Version = "0.1.0";
        public const string Author = "PasterLak";
        public const string EngineVersion = "1.4";

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
