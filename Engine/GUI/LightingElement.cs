using Engine.Light;
using ImGuiNET;

namespace Engine.GUI
{
    public class LightingElement : OverlayElement
    {

        public override void OnGUIText()
        {
            BeforeDraw?.Invoke();

            var registeredLights =  LightSystem.GetRegisteredLightsCount;
            int maxLights = LightSystem.MAX_DIR + LightSystem.MAX_POINT + LightSystem.MAX_SPOT;
            var activeTotal = LightSystem.ActiveDirectionalLights + LightSystem.ActivePointLights + LightSystem.ActiveSpotLights;


            ImGui.SeparatorText("[LIGHTING]");
            ImGui.Text($"Registered: {registeredLights}/{maxLights} | ActiveTotal: {activeTotal}/{maxLights}");
            ImGui.Text($"Active: Dir {LightSystem.ActiveDirectionalLights}/{LightSystem.MAX_DIR} | Point {LightSystem.ActivePointLights}/{LightSystem.MAX_POINT} " +
                $"| Spot {LightSystem.ActiveSpotLights}/{LightSystem.MAX_SPOT}");
            

            AfterDraw?.Invoke();
        }
    }
}
