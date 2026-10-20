using Engine;

namespace Spacebox.Game.Events
{
    public struct GraphicsSettingsChangedEvent : IEvent
    {
        public GraphicsSettings NewSettings;
    }

}