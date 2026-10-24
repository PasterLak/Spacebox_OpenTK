using System.Numerics;
using OpenTK.Windowing.Common;
using Engine.Commands;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Engine
{
    public static class Debug
    {
        public enum MessageType : byte
        {
            Info,
            Warning,
            Error,
            Success
        }

        private struct ConsoleMessage
        {
            public string Text;
            public int Count = 0;
            public Vector4 Color;
            public MessageType Type = MessageType.Info;

            public ConsoleMessage(string text, Vector4 color)
            {
                Text = text;
                Color = color;
                Count = 0;
                Type = MessageType.Info;
            }
        }

        private static string _inputBuffer = "";
        private static List<ConsoleMessage> _messages = new List<ConsoleMessage>();
        private const int MaxStoredMessages = 5000;
        private const int TrimBatch = 500;
        private const int MaxRenderedMessages = 400;
        private static List<string> _commandHistory = new List<string>();
        private static int _historyPos = -1;
        private static bool _focusInput = false;

        public static Action<bool> OnVisibilityWasChanged;

        private static long messageCount = 0;
        private static long warningCount = 0;
        private static long errorCount = 0;
        private static long successCount = 0;

        private static Vector4 errorColor = new Vector4(1, 0, 0, 1);
        private static Vector4 waringColor = new Vector4(1, 0.45f, 0, 1);
        private static Vector4 infoColor = new Vector4(1, 1, 1, 1);
        private static Vector4 successColor = new Vector4(0, 1, 0, 1);




        private const string DebugFolderPath = "Debug";
        private static readonly string HistoryFilePath = Path.Combine(DebugFolderPath, "command_history.txt");
        private static int _autoCompleteIndex = 0;
        private static List<CommandBase> _autoCompleteMatches = new List<CommandBase>();

        private static readonly object _lock = new object();
        private static int _version;

        public static bool IsVisible => UIManager.IsOpen("debug");

        static Debug()
        {
            LoadHistory();
            RegisterDefaultCommands();
        }

        private static void RegisterDefaultCommands()
        {
            RegisterCommandGlobal(new ClearCommand());
            RegisterCommandGlobal(new CameraRelativeRenderingCommand());
            RegisterCommandGlobal(new VersionCommand());
            RegisterCommandGlobal(new ColorCommand());
            RegisterCommandGlobal(new HelpCommand());
            RegisterCommandGlobal(new ExitCommand());
            RegisterCommandGlobal(new ResourcesCommand());
            RegisterCommandGlobal(new SaveMessagesCommand());
            RegisterCommandGlobal(new UiValidateCommand());
        }

        private static void RegisterCommandGlobal(CommandBase command)
        {
            CommandManager.RegisterGlobalCommand(command);
        }

        public static void RegisterCommand(CommandBase command)
        {
            CommandManager.RegisterSceneCommand(command);
        }

        public static void ToggleVisibility()
        {
            UIManager.Toggle("debug");

            if (!IsVisible)
            {
                SaveHistory();
            }
            else
            {
                _focusInput = true;
            }

            OnVisibilityWasChanged?.Invoke(IsVisible);
        }

        public static bool MirrorToStandardOutput { get; set; }

        public static event Action<string, MessageType> MessageLogged;

        public static void ExecuteCommand(string command)
        {
            ProcessCommand(command);
        }

        private static void AddMessage(string message, Vector4? color = null)
        {
            var msg = new ConsoleMessage(message, color ?? infoColor);

            switch (color)
            {
                case var c when c == errorColor:
                    msg.Type = MessageType.Error;
                    break;
                case var c when c == waringColor:
                    msg.Type = MessageType.Warning;
                    break;
                case var c when c == successColor:
                    msg.Type = MessageType.Success;
                    break;
                default:
                    break;
            }
            MirrorMessage(message, msg.Type);
            Store(msg);
            MessageLogged?.Invoke(message, msg.Type);
        }

        private static void Store(ConsoleMessage msg)
        {
            lock (_lock)
            {
                _messages.Add(msg);
                messageCount++;
                _version++;
                if (_messages.Count > MaxStoredMessages + TrimBatch) _messages.RemoveRange(0, _messages.Count - MaxStoredMessages);
            }
        }

        private static void AddMessage(string message, OpenTK.Mathematics.Color4 color)
        {
            var msg = new ConsoleMessage(message, new Vector4(color.R, color.G, color.B, color.A));

            switch (color)
            {
                case var c when c == errorColor.ToOpenTKColor4():
                    msg.Type = MessageType.Error;
                    break;
                case var c when c == waringColor.ToOpenTKColor4():
                    msg.Type = MessageType.Warning;
                    break;
                case var c when c == successColor.ToOpenTKColor4():
                    msg.Type = MessageType.Success;
                    break;
                default:
                    break;
            }
            MirrorMessage(message, msg.Type);
            Store(msg);
            MessageLogged?.Invoke(message, msg.Type);
        }

        private static readonly object _mirrorLock = new object();

        private static void MirrorMessage(string message, MessageType type)
        {
            if (!MirrorToStandardOutput) return;

            lock (_mirrorLock)
            {
                var previous = System.Console.ForegroundColor;
                System.Console.ForegroundColor = type switch
                {
                    MessageType.Error => System.ConsoleColor.Red,
                    MessageType.Warning => System.ConsoleColor.Yellow,
                    MessageType.Success => System.ConsoleColor.Green,
                    _ => previous
                };
                System.Console.Out.WriteLine($"[{System.DateTime.Now:HH:mm:ss}] {message}");
                System.Console.ForegroundColor = previous;
            }
        }

        public static void WriteLine(string message)
        {
            AddMessage($"{message}");
        }

        public static void Write(string message)
        {
            AddMessage($"{message}");
        }

        public static void Log(string message, OpenTK.Mathematics.Color4 color)
        {
            AddMessage($"{message}", color);
        }

        public static void Log(string sender, string message, OpenTK.Mathematics.Color4 color)
        {
            AddMessage($"[{sender}] {message}", color);
        }

        public static void Log(object sender, string message, OpenTK.Mathematics.Color4 color)
        {
            AddMessage($"[{sender.GetType().Name}] {message}", color);
        }

        public static void Log(string sender, string message)
        {
            AddMessage($"[{sender}] {message}", Color.White);
        }

        public static void Log(object sender, string message)
        {
            AddMessage($"[{sender.GetType().Name}] {message}", Color.White);
        }

        public static void Success(object sender, string message)
        {
            AddMessage($"[Success][{sender.GetType().Name}] {message}", successColor);
            successCount++;
        }

        public static void Success(string message)
        {
            AddMessage($"[Success] {message}", successColor);
            successCount++;
        }

        public static void Warning(string message)
        {
            AddMessage($"[Warning] {message}", waringColor);
            warningCount++;
        }

        public static void Log(string message, Vector4 color)
        {
            AddMessage($"{message}", color);
        }

        public static void Log(object message)
        {
            AddMessage($"{message.ToString()}", infoColor);
        }

        public static void Log(string message)
        {
            AddMessage($"{message}", infoColor);
        }

        public static void Error(string message)
        {
            AddMessage($"[ERROR] {message}", errorColor);
            errorCount++;
        }

        public static void ClearMessages()
        {
            lock (_lock)
            {
                _messages.Clear();
                _version++;
                messageCount = 0;
                warningCount = 0;
                errorCount = 0;
                successCount = 0;
            }
        }

        internal readonly struct ConsoleLine
        {
            public ConsoleLine(string text, Vector4 color, MessageType type)
            {
                Text = text;
                Color = color;
                Type = type;
            }

            public string Text { get; }
            public Vector4 Color { get; }
            public MessageType Type { get; }
        }

        internal static int MessageVersion
        {
            get
            {
                lock (_lock) return _version;
            }
        }

        internal static long CountOf(MessageType type)
        {
            lock (_lock)
            {
                return type switch
                {
                    MessageType.Success => successCount,
                    MessageType.Warning => warningCount,
                    MessageType.Error => errorCount,
                    _ => messageCount
                };
            }
        }

        internal static void CopyLines(List<ConsoleLine> target, MessageType filter, int max)
        {
            target.Clear();
            lock (_lock)
            {
                int start = _messages.Count - 1;
                for (int i = start; i >= 0 && target.Count < max; i--)
                {
                    if (filter == MessageType.Info || _messages[i].Type == filter) target.Add(new ConsoleLine(_messages[i].Text, _messages[i].Color, _messages[i].Type));
                }
            }
            target.Reverse();
        }

        internal const int MaxShownLines = MaxRenderedMessages;

        internal static string ConsoleTextFor(MessageType filter) => BuildConsoleText(filter);

        internal static void Submit(string command) => ProcessCommand(command);

        internal static string StepHistory(string current, int direction)
        {
            _inputBuffer = current ?? "";
            NavigateHistory(direction);
            return _inputBuffer;
        }

        internal static string Complete(string current)
        {
            _inputBuffer = current ?? "";
            AutoComplete();
            return _inputBuffer;
        }

        internal static bool TakeFocusRequest()
        {
            bool requested = _focusInput;
            _focusInput = false;
            return requested;
        }

        private static string BuildConsoleText(MessageType filter)
        {
            var lines = new List<(string, MessageType)>();
            lock (_lock)
            {
                foreach (var message in _messages) lines.Add((message.Text, message.Type));
            }
            return ConsoleText.Build(lines, filter);
        }

        private static void AddToHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            _commandHistory.Add(command);

            if (_commandHistory.Count > 20)
            {
                _commandHistory.RemoveAt(0);
            }
            _historyPos = -1;
        }

        private static void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0)
            {
                return;
            }

            _historyPos += direction;

            if (_historyPos < 0)
            {
                _historyPos = 0;
            }
            else if (_historyPos >= _commandHistory.Count)
            {
                _historyPos = _commandHistory.Count;
                _inputBuffer = "";
                return;
            }

            _inputBuffer = _commandHistory[_historyPos];
            _focusInput = true;
        }

        private static void AutoComplete()
        {
            string[] tokens = _inputBuffer.Split(' ');
            if (tokens.Length == 0)
                return;

            string lastToken = tokens[^1];
            List<CommandBase> matches = CommandManager.FindCommandsStartingWith(lastToken).ToList();

            if (matches.Count == 1)
            {
                tokens[^1] = matches[0].Name;
                _inputBuffer = string.Join(" ", tokens) + " ";
                _focusInput = true;
            }
            else if (matches.Count > 1)
            {
                foreach (var cmd in matches)
                {
                    AddMessage($"- {cmd.Name}: {cmd.Description}", new Vector4(0.5f, 0.5f, 1f, 1f));
                }
                _focusInput = true;
            }
        }

        private static void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            AddToHistory(command);

            var parts = SplitCommand(command);
            if (parts.Length == 0)
                return;

            var cmdName = parts[0].ToLower();
            var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

            var commandObj = CommandManager.GetCommand(cmdName);
            if (commandObj != null)
            {
                try
                {
                    commandObj.Execute(args);
                }
                catch (Exception ex)
                {
                    Error($"Error executing command '{cmdName}': {ex.Message}");
                    Error($"Error executing command '{cmdName}': {ex}");
                }
            }
            else
            {
                Error($"Unknown command: {cmdName}");
            }
        }

        private static string[] SplitCommand(string input)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (var c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                parts.Add(current.ToString());

            return parts.ToArray();
        }

        private static void LoadHistory()
        {
            if (File.Exists(HistoryFilePath))
            {
                _commandHistory = File.ReadAllLines(HistoryFilePath).ToList();
                if (_commandHistory.Count > 20)
                    _commandHistory = _commandHistory.TakeLast(20).ToList();
            }
        }

        private static void SaveHistory()
        {
            Directory.CreateDirectory(DebugFolderPath);
            File.WriteAllLines(HistoryFilePath, _commandHistory);
        }

        public static void SaveMessagesToFile(bool useDefaultFileName = false)
        {
            Write("Date: " + GetDateTimeNow());

            if (!Directory.Exists(DebugFolderPath))
            {
                Directory.CreateDirectory(DebugFolderPath);
            }

            string filename = useDefaultFileName ? "last_console_log.txt" : $"console_log_{GetDateTimeNow()}.txt";
            string filepath = Path.Combine(DebugFolderPath, filename);

            try
            {
                lock (_lock)
                {
                    File.WriteAllLines(filepath, _messages.Select(m => m.Text));
                }
                Log($"Console messages saved to '{filepath}'.");
            }
            catch (Exception ex)
            {
                Error($"Failed to save console messages to file '{filepath}': {ex.Message}");
            }
        }

        private static string GetDateTimeNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
    }
}