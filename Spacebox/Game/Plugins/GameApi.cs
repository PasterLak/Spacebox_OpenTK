using Spacebox.Core;
using Engine; 

namespace Spacebox.Game.Plugins
{
   
    public class GameApiImplementation : IGameApi
    {
        public IGameLogger Debug { get; } = new LoggerBridge();

        private class LoggerBridge : IGameLogger
        {
            public void Log(string message) => Engine.Debug.Log("[MOD] " + message);
            public void Success(string message) => Engine.Debug.Success("[MOD] " + message);
            public void Error(string message) => Engine.Debug.Error("[MOD] " + message);
            public void Warning(string message) => Engine.Debug.Warning("[MOD] " + message);


        }
    }
}