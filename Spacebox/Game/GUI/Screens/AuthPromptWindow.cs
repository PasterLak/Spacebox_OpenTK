using System.Numerics;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network;
using Spacebox.Network.Auth;

namespace Spacebox.Game.GUI;

public static class AuthPromptWindow
{
    private static readonly Vector4 TitleColor = new Vector4(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 ErrorColor = new Vector4(0.85f, 0.15f, 0.1f, 1f);

    private static string _password = "";
    private static string _confirmation = "";
    private static bool _savePassword;
    private static string _localError;
    private static bool _isOpen;
    private static bool _focusPassword;

    public static bool IsVisible => ClientAuth.IsAwaitingPassword;

    public static void OnGUI()
    {
        if (!ClientAuth.IsAwaitingPassword)
        {
            if (_isOpen) Close();
            return;
        }

        if (!_isOpen) Open();

        bool register = !ClientAuth.IsRegistered;
        var display = ImGui.GetIO().DisplaySize;
        float padding = MathF.Max(12f, display.Y * 0.02f);
        float frame = ImGui.GetFrameHeight();
        float line = ImGui.GetTextLineHeightWithSpacing();
        float buttonHeight = MathF.Max(28f, display.Y * 0.045f);
        float width = MathF.Min(MathF.Max(display.X * 0.32f, 420f), display.X * 0.95f);
        int fieldRows = register ? 2 : 1;
        float height = padding * 4.5f + line * 4f + (frame + line) * fieldRows + frame + buttonHeight;
        var position = new Vector2((display.X - width) * 0.5f, display.Y * 0.5f);

        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.SetNextWindowFocus();
        ImGui.Begin("##authPrompt", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

        float contentWidth = width - padding * 2f;
        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, register ? "Create an account" : "Log in");
        ImGui.SetCursorPosX(padding);
        ImGui.PushTextWrapPos(padding + contentWidth);
        ImGui.TextWrapped(register
            ? $"\"{ClientAuth.PlayerName}\" is not registered on this server yet. Choose a password for this name."
            : $"Enter the password of \"{ClientAuth.PlayerName}\".");
        ImGui.PopTextWrapPos();

        bool submit = DrawPasswordField(padding, contentWidth, "Password", "##authPassword", ref _password, _focusPassword);
        _focusPassword = false;
        if (register) submit |= DrawPasswordField(padding, contentWidth, "Repeat password", "##authConfirm", ref _confirmation, false);

        ImGui.SetCursorPos(new Vector2(padding, ImGui.GetCursorPosY() + padding * 0.5f));
        ImGui.Checkbox("Save password and join automatically next time", ref _savePassword);

        string error = _localError ?? ClientAuth.Error;
        ImGui.SetCursorPosX(padding);
        if (!string.IsNullOrEmpty(error)) ImGui.TextColored(ErrorColor, error);
        else ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));

        float buttonWidth = (contentWidth - padding) * 0.5f;
        float buttonY = height - padding - buttonHeight;
        GameMenu.ButtonWithBackground2(register ? "Create account" : "Join", new Vector2(buttonWidth, buttonHeight), new Vector2(padding, buttonY), () => submit = true);
        GameMenu.ButtonWithBackground2("Cancel", new Vector2(buttonWidth, buttonHeight), new Vector2(padding * 2f + buttonWidth, buttonY), Cancel);

        ImGui.End();

        if (submit) Submit();
    }

    private static bool DrawPasswordField(float padding, float width, string label, string id, ref string value, bool focus)
    {
        ImGui.SetCursorPos(new Vector2(padding, ImGui.GetCursorPosY() + padding * 0.5f));
        ImGui.Text(label);
        ImGui.SetCursorPosX(padding);
        ImGui.SetNextItemWidth(width);
        if (focus) ImGui.SetKeyboardFocusHere();
        return ImGui.InputText(id, ref value, PasswordCrypto.MaxPasswordLength, ImGuiInputTextFlags.Password | ImGuiInputTextFlags.EnterReturnsTrue);
    }

    private static void Submit()
    {
        if (ClientAuth.Submit(_password, _confirmation, _savePassword, out var error))
        {
            Close();
            return;
        }

        _localError = error;
    }

    public static void Cancel()
    {
        Close();
        ClientConnector.Cancel();
        ConnectionScreen.Hide();
    }

    private static void Open()
    {
        _isOpen = true;
        _focusPassword = true;
        _localError = null;
        _password = "";
        _confirmation = "";
    }

    private static void Close()
    {
        _isOpen = false;
        _password = "";
        _confirmation = "";
        _localError = null;
    }
}
