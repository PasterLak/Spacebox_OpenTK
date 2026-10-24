using Engine.Graphics.Rhi;
using OpenTK.Mathematics;
using Engine;
namespace Spacebox.GUI
{

    public static class BlackScreenOverlay
    {
        private static bool _isEnabled = false;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static void Initialize()
        {

        }

        public static void OnGUI()
        {
            if (_isEnabled)
            {

                if (!Engine.Application.IsHeadless)
                {
                    Gpu.Device.SetClearColor(Color4.Black);
                    Gpu.Device.Clear(ClearTargets.Color | ClearTargets.Depth);
                }


            }
        }

        public static void Shutdown()
        {

        }
    }

}

