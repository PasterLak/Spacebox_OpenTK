using Engine;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Network;
using System;
using System.Collections.Generic;

namespace Spacebox.Game.GUI
{
    struct ChatMessage
    {
        public string Text;
        public System.Numerics.Vector4 Color;
        public ChatMessage(string text, System.Numerics.Vector4 color)
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

        static readonly List<ChatMessage> _messages = new();

        public static void SetAlwaysVisible(bool value) => _alwaysVisible = value;
        public static void SetPlaceOnLeft(bool value) => _placeOnLeft = value;
        public static bool IsShownOnRight => !_placeOnLeft && (IsVisible || _alwaysVisible);
        public static void SetHideDelay(float seconds) => _hideDelay = seconds;

        private static void AddMessage(string text, Color4 c)
        {
            _messages.Add(new ChatMessage(text, new System.Numerics.Vector4(c.R, c.G, c.B, c.A)));
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

        public static void Say(string sender, string text)
        {
            ChatService.Send(text);
        }

        public static void Say(string text)
        {
            Say("", text);
        }

        private static void StartInput()
        {
            if (_inputBuffer.Length > 0) return;

            PanelUI.AllowScroll = false;
            InputManager0.Enabled = false;

            UIManager.Open("chat");

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
            if (!IsVisible) return;

            _inputBuffer = "";
            ChatCommandInput.Reset();

            PanelUI.AllowScroll = true;
            InputManager0.Enabled = true;

            if (UIManager.IsTop("chat"))
            {
                UIManager.CloseTop();
            }

            FocusInput = false;
            _hideTimer = _hideDelay;
            isStopped = true;
        }

        private static void UpdateHide()
        {
            if (!IsVisible) return;
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
            if (UIManager.IsUIMode && !UIManager.IsOpen("chat") || Debug.IsVisible)
            {
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

            if (Input.IsKeyDown(Keys.Slash) && !UIManager.IsOpen("chat") && _inputBuffer.Length == 0)
            {
                StartInput();
                _inputBuffer = "/";
            }

            UpdateHide();
        }

        public static uint Vec4ToUintColor(System.Numerics.Vector4 color)
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

            float wrapWidth = ImGui.GetContentRegionAvail().X;

            for (int i = _messages.Count - 1; i >= startIndex; i--)
            {
                var msg = _messages[i];
                var textSize = ImGui.CalcTextSize(msg.Text, false, wrapWidth);
                float lineHeight = textSize.Y + ImGui.GetStyle().ItemSpacing.Y;
                currentY -= lineHeight;
                if (currentY < 0) break;
                ImGui.SetCursorPos(new System.Numerics.Vector2(0, currentY));
                ImGui.PushStyleColor(ImGuiCol.Text, Vec4ToUintColor(msg.Color));
                ImGui.PushTextWrapPos(wrapWidth);
                ImGui.TextUnformatted(msg.Text);
                ImGui.PopTextWrapPos();
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
        }

        private static readonly unsafe ImGuiInputTextCallback _inputCallback = OnInputCallback;
        private static readonly System.Numerics.Vector4 SuggestionColor = new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f);
        private static readonly System.Numerics.Vector4 SelectedSuggestionColor = new System.Numerics.Vector4(1f, 0.85f, 0.1f, 1f);
        private static readonly System.Numerics.Vector4 HintColor = new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f);

        private static bool DrawInputField()
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new System.Numerics.Vector4(0, 0, 0, 0));
            bool inputActive = ImGui.InputText("##ChatInput",
                ref _inputBuffer,
                256,
                ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory,
                _inputCallback);
            ImGui.PopStyleColor();

            ChatCommandInput.Update(_inputBuffer);
            DrawSuggestions(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

            return inputActive;
        }

        private static unsafe int OnInputCallback(ImGuiInputTextCallbackData* raw)
        {
            var data = new ImGuiInputTextCallbackDataPtr(raw);
            string current = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(data.Buf, data.BufTextLen) ?? "";

            string replacement = data.EventFlag == ImGuiInputTextFlags.CallbackCompletion
                ? ChatCommandInput.Complete(current)
                : ChatCommandInput.Navigate(current, data.EventKey == ImGuiKey.UpArrow);

            if (replacement != null && replacement != current)
            {
                data.DeleteChars(0, data.BufTextLen);
                data.InsertChars(0, replacement);
            }

            return 0;
        }

        private static void DrawSuggestions(System.Numerics.Vector2 inputMin, System.Numerics.Vector2 inputMax)
        {
            var suggestions = ChatCommandInput.Suggestions;
            string usage = ChatCommandInput.Usage;
            if (suggestions.Count == 0 && usage.Length == 0) return;

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(inputMin.X, inputMax.Y + 2f), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.75f);
            ImGui.Begin("ChatSuggestions",
                ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.AlwaysAutoResize
                | ImGuiWindowFlags.NoFocusOnAppearing
                | ImGuiWindowFlags.NoNav
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoSavedSettings);

            if (usage.Length > 0) ImGui.TextColored(HintColor, usage);

            for (int i = 0; i < suggestions.Count; i++)
            {
                bool selected = i == ChatCommandInput.Selected;
                ImGui.TextColored(selected ? SelectedSuggestionColor : SuggestionColor, (selected ? "> " : "  ") + suggestions[i].Text);
                if (suggestions[i].Hint.Length == 0) continue;

                ImGui.SameLine();
                ImGui.TextColored(HintColor, suggestions[i].Hint);
            }

            if (suggestions.Count > 0) ImGui.TextColored(HintColor, "Tab completes, Up/Down selects");
            ImGui.End();
        }

        private static void OnSend()
        {
            if (!string.IsNullOrWhiteSpace(_inputBuffer))
            {
                ChatCommandInput.Sent(_inputBuffer);
                ChatService.Send(_inputBuffer);
            }
            _inputBuffer = "";
            FocusInput = false;

            if (UIManager.IsTop("chat"))
            {
                UIManager.CloseTop();
            }

            PanelUI.AllowScroll = true;
            _hideTimer = _hideDelay;
            InputManager0.Enabled = true;
        }

        public static void Clear()
        {
            _messages.Clear();
            _inputBuffer = "";
        }
    }
}