

namespace Spacebox.Engine.GUI
{
    public abstract class OverlayElement
    {
        public Action BeforeDraw { get; set; }
        public Action AfterDraw { get; set; }

        public OverlayElement ElementBefore { get; set; }
        public OverlayElement ElementAfter { get; set; }

        public abstract void OnGUIText();
    }
}
