namespace ServerCommon
{
    public enum LogType { Normal, Info, Success, Warning, Error }

    public interface ILogger
    {
        void Log(string message, LogType type);
    }
}
