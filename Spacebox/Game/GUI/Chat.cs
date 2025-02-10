using Engine;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Client;

namespace Spacebox.Game.GUI
{
    struct ChatMessage
    {
        public string Text;
        public Vector4 Color;
        public ChatMessage(string text, Vector4 color)
        {
            Text = text;
            Color = color;
        }
    }

    public static class Chat
    {
        public static bool IsVisible { get; set; }
        static bool _alwaysVisible;
        static bool _placeOnLeft = true;
        static float _hideDelay = 10f;
        static float _hideTimer;
        static string _inputBuffer = "";
        public static bool FocusInput { get; set; }
        static bool _prevFocusInput;
        static readonly List<ChatMessage> _messages = new();

        public static void SetAlwaysVisible(bool value) => _alwaysVisible = value;
        public static void SetPlaceOnLeft(bool value) => _placeOnLeft = value;
        public static void SetHideDelay(float seconds) => _hideDelay = seconds;

        static void AddMessage(string text, Color4 c)
        {
            _messages.Add(new ChatMessage(text, new Vector4(c.R, c.G, c.B, c.A)));
            IsVisible = true;
            _hideTimer = _hideDelay;
        }

        public static void Success(string text) => Write(text, Color4.Green);
        public static void Warning(string text) => Write(text, Color4.Orange);
        public static void Info(string text) => Write(text, Color4.Yellow);
        public static void Error(string text) => Write(text, Color4.Red);
        public static void Write(string text) => Write(text, Color4.White);

        public static void Write(string text, Color4 c)
        {
            AddMessage(text, c);
            FocusInput = false;
        }

        public static void Say(string text, string name)
        {
            if (ClientNetwork.Instance != null)
                ClientNetwork.Instance.SendMessage(text);
            AddMessage(name + text, Color4.White);
        }

        public static void Say(string text)
        {
            Say(text, "");
        }

        private static void StartInput()
        {
            if (_inputBuffer.Length > 0) return;
            

                Input.MoveCursorToCenter();
            ToggleManager.SetState("player", false);
            PanelUI.AllowScroll = false;
            InputManager.Enabled = false;
            if (!IsVisible)
            {
              
                FocusInput = true;
                _hideTimer = _hideDelay;
            }
            else
            {
                if (_inputBuffer.Length == 0)
                {
                    _hideTimer = _hideDelay;
                    FocusInput = true;
                }
            }
            IsVisible = true;
            isStopped = false;
        }
        static bool isStopped = true;
        private static void StopInput()
        {
            //if (!FocusInput) return;
            if (!IsVisible) return;

                _inputBuffer = "";

            ToggleManager.SetState("player", true);
            PanelUI.AllowScroll = true;
            InputManager.Enabled = true;
            _prevFocusInput = false;
            FocusInput = false;
            _hideTimer = _hideDelay;
            isStopped = true;

        }

        private static void UpdateHide()
        {
            if(!IsVisible) return;
            if (_alwaysVisible) return;

            if (FocusInput || _inputBuffer.Length > 0)
            {
                _hideTimer = _hideDelay;
            }
            else
            {
                _hideTimer -= Time.Delta;
                if (_hideTimer <= 0f && !_alwaysVisible)
                {
                    _inputBuffer = "";
                    IsVisible = false;
                }

            }
        }

        public static void Update()
        {
            if (ToggleManager.OpenedWindowsCount > 0 || Debug.IsVisible)
            {
                if (Input.IsKeyDown(Keys.Escape))
                {
                  
                }
                return;
            }
           
           
            if (Input.IsKeyDown(Keys.Escape))
            {
              
                StopInput();
               
            }

            if (Input.IsKeyDown(Keys.T))
            {
              
                StartInput();
            }

            UpdateHide();

        }

        public static uint Vec4ToUintColor(Vector4 color)
        {
            uint r = (uint)MathF.Round(color.X * 255f) & 0xFF;
            uint g = (uint)MathF.Round(color.Y * 255f) & 0xFF;
            uint b = (uint)MathF.Round(color.Z * 255f) & 0xFF;
            uint a = (uint)MathF.Round(color.W * 255f) & 0xFF;
            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        public static void OnGUI()
        {
            if (!IsVisible && !_alwaysVisible) return;

            var displaySize = ImGui.GetIO().DisplaySize;
            float chatHeight = displaySize.Y * 0.5f;
            float chatWidth = displaySize.X / 3f;
            float x = _placeOnLeft
                ? (chatWidth * 0.02f)
                : displaySize.X - chatWidth + (chatWidth * 0.02f);
            float y = 0f;

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(x, y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(chatWidth, chatHeight), ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(2, 2));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new System.Numerics.Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(0, 0, 0, 0));

            ImGui.Begin("ChatWindow",
                ImGuiWindowFlags.NoTitleBar
                | ImGuiWindowFlags.NoResize
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse);

            DrawChatMessages();

            if (!isStopped && FocusInput)
            {
                bool done = DrawInputField();

                if (FocusInput) ImGui.SetKeyboardFocusHere(-1);

                if (done)
                {
                    OnSend();
                }
            }


            ImGui.End();
            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }

        private static void DrawChatMessages()
        {
            float listHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 2f;

            ImGui.BeginChild("ChatMessages",
               new System.Numerics.Vector2(0, listHeight),
               ImGuiChildFlags.None,
               ImGuiWindowFlags.None);

            float currentY = listHeight;
            int startIndex = Math.Max(0, _messages.Count - 100);

            for (int i = _messages.Count - 1; i >= startIndex; i--)
            {
                var msg = _messages[i];
                var textSize = ImGui.CalcTextSize(msg.Text);
                float lineHeight = textSize.Y + ImGui.GetStyle().ItemSpacing.Y;
                currentY -= lineHeight;
                if (currentY < 0) currentY = 0;
                ImGui.SetCursorPos(new System.Numerics.Vector2(0, currentY));
                ImGui.PushStyleColor(ImGuiCol.Text, Vec4ToUintColor(msg.Color));
                ImGui.Text(msg.Text);
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
        }

        private static bool DrawInputField()
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(0, 0, 0, 0));
            bool inputActive = ImGui.InputText("##ChatInput",
                ref _inputBuffer,
                256,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.NoHorizontalScroll);
            ImGui.PopStyleColor();

            return inputActive;
        }

        private static void OnSend()
        {
            if (!string.IsNullOrWhiteSpace(_inputBuffer))
            {
                if (ClientNetwork.Instance != null)
                    ClientNetwork.Instance.SendMessage(_inputBuffer);
                else
                    AddMessage(_inputBuffer, Color4.White);
            }
            _inputBuffer = "";
            FocusInput = false;
            ToggleManager.SetState("player", true);
            PanelUI.AllowScroll = true;
            _hideTimer = _hideDelay;
            InputManager.Enabled = true;
        }


        public static void Clear()
        {
            _messages.Clear();
            _inputBuffer = "";
        }
    }
}
