using ServerCommon;

namespace SpaceServerUI
{
    public class UILogger : ILogger
    {
        private readonly MainWindow window;
        public UILogger(MainWindow window)
        {
            this.window = window;
        }


        public void Log(string message, ServerCommon.LogType type)
        {
            window.LogMessage(message, type);
        }
    }
}
