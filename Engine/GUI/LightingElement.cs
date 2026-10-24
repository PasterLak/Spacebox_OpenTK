using Engine.Light;

namespace Engine.GUI
{
    public class LightingElement : OverlayElement
    {

        public override void Write(OverlayText text)
        {
            BeforeDraw?.Invoke();

            var registeredLights =  LightSystem.GetRegisteredLightsCount;
            int maxLights = LightSystem.MAX_DIR + LightSystem.MAX_POINT + LightSystem.MAX_SPOT;
            var activeTotal = LightSystem.ActiveDirectionalLights + LightSystem.ActivePointLights + LightSystem.ActiveSpotLights;


            text.Section("[LIGHTING]");
            text.Line($"Registered: {registeredLights}/{maxLights} | ActiveTotal: {activeTotal}/{maxLights}");
            text.Line($"Active: Dir {LightSystem.ActiveDirectionalLights}/{LightSystem.MAX_DIR} | Point {LightSystem.ActivePointLights}/{LightSystem.MAX_POINT} " +
                $"| Spot {LightSystem.ActiveSpotLights}/{LightSystem.MAX_SPOT}");


            AfterDraw?.Invoke();
        }
    }
}
