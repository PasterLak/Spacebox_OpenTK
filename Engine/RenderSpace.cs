using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace Engine
{
    public static class RenderSpace
    {
        public static Vector3 Origin { get; private set; } = Vector3.Zero;

        static int _lastFrame = -1;

        private static bool _switchSpace = false;

        public static void SwitchSpace()
        {
            _switchSpace = true;
        }

        public static void BeginFrame()
        {
            //if (_lastFrame == frameId) return;         
           // _lastFrame = frameId;

            var cam = Camera.Main;

            if (cam == null) return;

            if(_switchSpace)
            {
                _switchSpace = false;
                cam.SetRenderSpace(!cam.CameraRelativeRender);
            }

            UpdateOrigin();
           
        }

        public static void UpdateOrigin()
        {
            var cam = Camera.Main;
            if (cam == null) return;

            if (cam.CameraRelativeRender)
                Origin = cam.Position;
            else
                Origin = Vector3.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToRender(Vector3 world) => world - Origin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToWorld(Vector3 render) => render + Origin;
    }
}
