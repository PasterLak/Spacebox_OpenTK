using Engine;

namespace Spacebox.Game.Events
{
    public struct GraphicsSettingsChangedEvent : IEvent
    {
        public GraphicsSettings NewSettings;
    }

    public struct AudioSettingsChangedEvent : IEvent
    {
        public AudioSettings NewSettings;
    }
}