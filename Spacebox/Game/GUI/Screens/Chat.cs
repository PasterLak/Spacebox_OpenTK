using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Network;

namespace Spacebox.Game.GUI
{
    public static class Chat
    {
        public const string WindowId = "chat";
        public const int MaxInputLength = 256;

        public static bool IsVisible { get; set; }
        static bool _alwaysVisible;
        static bool _placeOnLeft = true;
        static float _hideDelay = 10f;
        static string _inputBuffer = "";
        static bool isStopped = true;
        public static bool FocusInput { get; set; }

        public static ChatLog Log { get; } = new ChatLog();
        public static string InputText => _inputBuffer;
        public static int InputRevision { get; private set; }
        public static bool IsTyping => FocusInput && !isStopped;
        public static bool AlwaysVisible => _alwaysVisible;
        public static bool PlaceOnLeft => _placeOnLeft;
        public static float HideDelay => _hideDelay;

        public static void SetAlwaysVisible(bool value) => _alwaysVisible = value;
        public static void SetPlaceOnLeft(bool value) => _placeOnLeft = value;
        public static bool IsShownOnRight => !_placeOnLeft && (IsVisible || _alwaysVisible);
        public static void SetHideDelay(float seconds) => _hideDelay = seconds;

        private static void AddMessage(string text, Color4 c)
        {
            Log.Add(text, c, Time.Total);
            IsVisible = true;
        }

        public static void Success(string text) => Write(text, Color4.Green);
        public static void Warning(string text) => Write(text, Color4.Orange);
        public static void Info(string text) => Write(text, Color4.Yellow);
        public static void Error(string text) => Write(text, Color4.Red);
        public static void Write(string text) => Write(text, Color4.White);

        public static void Write(string text, Color4 c)
        {
            AddMessage(text, c);
            if (UIManager.IsOpen(WindowId)) return;
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

        private static void StartInput(string text)
        {
            if (IsTyping) return;

            PanelUI.AllowScroll = false;
            InputManager0.Enabled = false;

            UIManager.Open(WindowId);

            FocusInput = true;
            IsVisible = true;
            isStopped = false;
            ReplaceInput(text);
        }

        public static void CancelInput()
        {
            if (FocusInput || UIManager.IsOpen(WindowId)) StopInput();
        }

        private static void StopInput()
        {
            _inputBuffer = "";
            ChatCommandInput.Reset();

            PanelUI.AllowScroll = true;
            InputManager0.Enabled = true;

            if (UIManager.IsTop(WindowId))
            {
                UIManager.CloseTop();
            }

            FocusInput = false;
            isStopped = true;
            InputRevision++;
        }

        public static void SetInput(string text)
        {
            _inputBuffer = text ?? "";
            ChatCommandInput.Update(_inputBuffer);
        }

        public static void Complete()
        {
            ApplyReplacement(ChatCommandInput.Complete(_inputBuffer));
        }

        public static void Navigate(bool up)
        {
            ApplyReplacement(ChatCommandInput.Navigate(_inputBuffer, up));
        }

        private static void ApplyReplacement(string replacement)
        {
            if (replacement == null || replacement == _inputBuffer) return;
            ReplaceInput(replacement);
        }

        private static void ReplaceInput(string text)
        {
            SetInput(text);
            InputRevision++;
        }

        private static void UpdateVisibility()
        {
            IsVisible = _alwaysVisible || FocusInput || UIManager.IsOpen(WindowId) || Log.HasVisible(Time.Total, _hideDelay);
        }

        public static void Update()
        {
            UpdateVisibility();

            if (UIManager.IsUIMode && !UIManager.IsOpen(WindowId) || Debug.IsVisible)
            {
                return;
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                CancelInput();
                return;
            }

            if (Input.IsKeyDown(Keys.T))
            {
                StartInput("");
            }

            if (Input.IsKeyDown(Keys.Slash) && !UIManager.IsOpen(WindowId))
            {
                StartInput("/");
            }
        }

        public static void Submit(string text)
        {
            SetInput(text);
            if (!string.IsNullOrWhiteSpace(_inputBuffer))
            {
                ChatCommandInput.Sent(_inputBuffer);
                ChatService.Send(_inputBuffer);
            }

            StopInput();
        }

        public static void Clear()
        {
            Log.Clear();
            _inputBuffer = "";
            InputRevision++;
        }
    }
}
