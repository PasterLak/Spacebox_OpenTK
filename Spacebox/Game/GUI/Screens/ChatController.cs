using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Network;

namespace Spacebox.Game.GUI
{
    public sealed class ChatController : IGameUiController
    {
        public const string WindowId = "chat";
        public const int MaxInputLength = 256;

        private readonly PanelController _panel;

        public bool IsVisible { get; set; }
        private bool _alwaysVisible;
        private bool _placeOnLeft = true;
        private float _hideDelay = 10f;
        private string _inputBuffer = "";
        private bool isStopped = true;
        public bool FocusInput { get; set; }

        public ChatLog Log { get; } = new ChatLog();
        public ChatCommandInput Commands { get; } = new ChatCommandInput();
        public string InputText => _inputBuffer;
        public int InputRevision { get; private set; }
        public bool IsTyping => FocusInput && !isStopped;
        public bool AlwaysVisible => _alwaysVisible;
        public bool PlaceOnLeft => _placeOnLeft;
        public float HideDelay => _hideDelay;

        public ChatController(PanelController panel)
        {
            _panel = panel;
        }

        public void SetAlwaysVisible(bool value) => _alwaysVisible = value;
        public void SetPlaceOnLeft(bool value) => _placeOnLeft = value;
        public bool IsShownOnRight => !_placeOnLeft && (IsVisible || _alwaysVisible);
        public void SetHideDelay(float seconds) => _hideDelay = seconds;

        private void AddMessage(string text, Color4 c)
        {
            Log.Add(text, c, Time.Total);
            IsVisible = true;
        }

        public void Success(string text) => Write(text, Color4.Green);
        public void Warning(string text) => Write(text, Color4.Orange);
        public void Info(string text) => Write(text, Color4.Yellow);
        public void Error(string text) => Write(text, Color4.Red);
        public void Write(string text) => Write(text, Color4.White);

        public void Write(string text, Color4 c)
        {
            AddMessage(text, c);
            if (UIManager.IsOpen(WindowId)) return;
            FocusInput = false;
        }

        public void Say(string sender, string text)
        {
            ChatService.Send(text);
        }

        public void Say(string text)
        {
            Say("", text);
        }

        private void StartInput(string text)
        {
            if (IsTyping) return;

            _panel.AllowScroll = false;
            InputManager0.Enabled = false;

            UIManager.Open(WindowId);

            FocusInput = true;
            IsVisible = true;
            isStopped = false;
            ReplaceInput(text);
        }

        public void CancelInput()
        {
            if (FocusInput || UIManager.IsOpen(WindowId)) StopInput();
        }

        private void StopInput()
        {
            _inputBuffer = "";
            Commands.Reset();

            _panel.AllowScroll = true;
            InputManager0.Enabled = true;

            if (UIManager.IsTop(WindowId))
            {
                UIManager.CloseTop();
            }

            FocusInput = false;
            isStopped = true;
            InputRevision++;
        }

        public void SetInput(string text)
        {
            _inputBuffer = text ?? "";
            Commands.Update(_inputBuffer);
        }

        public void Complete()
        {
            ApplyReplacement(Commands.Complete(_inputBuffer));
        }

        public void Navigate(bool up)
        {
            ApplyReplacement(Commands.Navigate(_inputBuffer, up));
        }

        private void ApplyReplacement(string replacement)
        {
            if (replacement == null || replacement == _inputBuffer) return;
            ReplaceInput(replacement);
        }

        private void ReplaceInput(string text)
        {
            SetInput(text);
            InputRevision++;
        }

        private void UpdateVisibility()
        {
            IsVisible = _alwaysVisible || FocusInput || UIManager.IsOpen(WindowId) || Log.HasVisible(Time.Total, _hideDelay);
        }

        public void Update()
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

        public void Submit(string text)
        {
            SetInput(text);
            if (!string.IsNullOrWhiteSpace(_inputBuffer))
            {
                Commands.Sent(_inputBuffer);
                ChatService.Send(_inputBuffer);
            }

            StopInput();
        }

        public void Reset() => Clear();

        public void Clear()
        {
            Log.Clear();
            _inputBuffer = "";
            InputRevision++;
        }
    }
}
