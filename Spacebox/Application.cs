

namespace Spacebox
{
    internal class Application
    {
        public const string Version = "0.0.9";
        public const string Author = "PasterLak";
        public const string EngineVersion = "1.3";


        public static PlatformName Platform => CheckPlatform2();
       /* private static PlatformName CheckPlatform()
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
        }*/

      
            private static PlatformName CheckPlatform2()
            {
#if WINDOWS
            return PlatformName.Windows;
#elif LINUX
       return PlatformName.Linux;
#elif OSX
       return PlatformName.OSX;
#else
        return PlatformName.Unknown;
#endif
        }




        public enum PlatformName
        {
            Windows,
            Linux,
            OSX,
            Unknown
        }

    }
}
