using System.Numerics;
using ImGuiNET;
using OpenTK.Windowing.Common;
using Engine.Commands;
using System.Drawing;

namespace Engine
{
    public static class Debug
    {
        
        private struct ConsoleMessage
        {
            public string Text;
            public Vector4 Color;

            public ConsoleMessage(string text, Vector4 color)
            {
                Text = text;
                Color = color;
            }
        }
        
        private static bool _isVisible = false;
        private static CursorState _previousCursorState;
        private static string _inputBuffer = "";
        private static List<ConsoleMessage> _messages = new List<ConsoleMessage>();
        private static List<string> _commandHistory = new List<string>();
        private static int _historyPos = -1;
        private static bool _focusInput = false;

        public static Action<bool> OnVisibilityWasChanged;


        private static readonly string HistoryFilePath = "command_history.txt";
        private static int _autoCompleteIndex = 0;
        private static List<ICommand> _autoCompleteMatches = new List<ICommand>();

        static Debug()
        {
            LoadHistory();
            RegisterCommand(new ClearCommand());
            RegisterCommand(new VersionCommand());
            RegisterCommand(new ColorCommand());
            RegisterCommand(new HelpCommand());
            RegisterCommand(new ExitCommand());
            RegisterCommand(new ResourcesCommand());
            RegisterCommand(new SaveMessagesCommand());

        }

        public static void RegisterCommand(ICommand command)
        {
            CommandManager.RegisterCommand(command);
        }

        public static void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            
            if (_isVisible)
            {
                _previousCursorState = Input.GetCursorState();
                Input.ShowCursor();
                _focusInput = true;

            }
            else
            {
                Input.SetCursorState(_previousCursorState);
                SaveHistory();

            }

            OnVisibilityWasChanged?.Invoke(_isVisible);
        }

        public static bool IsVisible => _isVisible;

        public static void AddMessage(string message, Vector4? color = null)
        {
            _messages.Add(new ConsoleMessage(message, color ?? new Vector4(1f, 1f, 1f, 1f)));
        }

        public static void AddMessage(string message, OpenTK.Mathematics.Color4 color)
        {
            _messages.Add(new ConsoleMessage(message, new Vector4(color.R, color.G, color.B, color.A)));
        }

        public static void WriteLine(string message)
        {
            AddMessage($"{message}");
            Console.WriteLine($"{message}");
        }

        public static void Write(string message)
        {
            AddMessage($"{message}");
            Console.WriteLine($"{message}");
        }

        public static void Log(string message, OpenTK.Mathematics.Color4 color)
        {
            AddMessage($"{message}", color);
            Console.WriteLine($"{message}");
        }

        public static void Log(object sender, string message, OpenTK.Mathematics.Color4 color)
        {
            AddMessage($"[{sender.GetType().Name}] {message}", color);
            Console.WriteLine($"[{sender.GetType().Name}] {message}");
        }
        public static void Log(object sender, string message)
        {
            AddMessage($"[{sender.GetType().Name}] {message}", Color.White);
            Console.WriteLine($"[{sender.GetType().Name}] {message}");
        }

        public static void Success(object sender, string message)
        {
            AddMessage($"[Success][{sender.GetType().Name}] {message}", new Vector4(0, 1, 0, 1));
            Console.WriteLine($"[Success][{sender.GetType().Name}] {message}");
        }

        public static void Success(string message)
        {
            AddMessage($"[Success] {message}", new Vector4(0, 1, 0, 1));
            Console.WriteLine($"[Success] {message}");
        }

        public static void Warning(string message)
        {
            AddMessage($"[Warning] {message}", new Vector4(1, 0.45f, 0, 1));
            Console.WriteLine($"[Warning] {message}");
        }

        public static void Log(string message, Vector4 color)
        {
            AddMessage($"{message}", color);
            Console.WriteLine($"{message}");
        }

        public static void Log(string message)
        {
            //AddMessage($"[DEBUG] {message}", new Vector4(0.2f, 0.7f, 1f, 1f));
            AddMessage($"{message}", new Vector4(1, 1, 1f, 1f));
            Console.WriteLine($"{message}");
        }

        public static void Error(string message)
        {
            AddMessage($"[ERROR] {message}", new Vector4(1f, 0f, 0f, 1f));
            Console.WriteLine($"[ERROR] {message}");
        }

        public static void ClearMessages()
        {
            _messages.Clear();
            //Log("Console messages cleared.");
        }

        public static void Render(Vector2 windowSize)
        {
            if (!_isVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowSize.X * 0.4f, windowSize.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 200), new Vector2(float.MaxValue, float.MaxValue));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0f, 0f, 0f, 0.8f));
            ImGui.Begin("Console", ref _isVisible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);

            ImGui.Text("Console");
            ImGui.Separator();

            float childHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - 40f;
            if (childHeight < 100f)
                childHeight = 100f;

            ImGui.BeginChild("ScrollingRegion", new Vector2(0, childHeight), ImGuiChildFlags.AlwaysAutoResize);
            foreach (var msg in _messages)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, msg.Color);
                ImGui.TextWrapped(msg.Text);
                ImGui.PopStyleColor();
            }
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);
            ImGui.EndChild();

            ImGui.Separator();

            float buttonWidth = 120f;
            float inputWidth = ImGui.GetContentRegionAvail().X - buttonWidth - ImGui.GetStyle().ItemSpacing.X;

            if (ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                {
                    NavigateHistory(-1);
                    _focusInput = true;
                }
                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                {

                    NavigateHistory(1);
                    _focusInput = true;
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Tab))
                {

                    AutoComplete();
                    _focusInput = true;
                }
            }


            if (ImGui.Button("Send", new Vector2(buttonWidth, 0)))
            {

                ProcessCommand(_inputBuffer);
                _inputBuffer = "";
                _focusInput = true;
            }

            ImGui.SameLine();


            ImGui.SetNextItemWidth(inputWidth);
            bool inputActivated = ImGui.InputText("##Input", ref _inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);

            if (_focusInput)
            {
                ImGui.SetKeyboardFocusHere(-1);

                _focusInput = false;
            }

            if (inputActivated)
            {

                ProcessCommand(_inputBuffer);
                _inputBuffer = "";
                _focusInput = true;
            }

            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
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
            List<ICommand> matches = CommandManager.FindCommandsStartingWith(lastToken).ToList();


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
                    AddMessage($"Error executing command '{cmdName}': {ex.Message}", new Vector4(1f, 0f, 0f, 1f));
                    Error($"Error executing command '{cmdName}': {ex}");
                }
            }
            else
            {
                AddMessage($"Unknown command: {cmdName}", new Vector4(1f, 0f, 0f, 1f));

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
            else
            {

            }
        }

        private static void SaveHistory()
        {
            File.WriteAllLines(HistoryFilePath, _commandHistory);
        }
      
        public static void SaveMessagesToFile(bool useDefaultFileName = false)
        {
            const string debugFolderPath = "Debug";

            Write("Date: " + GetDateTimeNow());

            if (!Directory.Exists(debugFolderPath))
            {
                Directory.CreateDirectory(debugFolderPath);
            }

            string filename = useDefaultFileName ? "last_console_log.txt": $"console_log_{GetDateTimeNow()}.txt";
            string filepath = Path.Combine(debugFolderPath, filename);

            try
            {
                File.WriteAllLines(filepath, _messages.Select(m => m.Text));
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
