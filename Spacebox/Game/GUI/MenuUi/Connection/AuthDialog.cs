using Engine.CanvasUI;
using Spacebox.Network.Auth;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class AuthDialog
{
    private readonly MenuWindowFrame _frame = MenuLayout.Window("Log in", 440f);
    private readonly MenuModal _modal;
    private readonly Label _message = new Label { Autowrap = true };
    private readonly LineEdit _password = new LineEdit { Secret = true, MaxLength = PasswordCrypto.MaxPasswordLength };
    private readonly LineEdit _confirmation = new LineEdit { Secret = true, MaxLength = PasswordCrypto.MaxPasswordLength };
    private readonly VBoxContainer _confirmationField;
    private readonly CheckBox _save = new CheckBox("Save password and join automatically next time", false);
    private readonly Label _error = new Label { ThemeTypeVariation = "ErrorLabel", Autowrap = true };
    private readonly Button _submit;
    private string _localError;
    private bool _closing;

    public AuthDialog()
    {
        _frame.Body.AddChild(_message);
        _frame.Body.AddChild(MenuLayout.Field("Password", _password));
        _confirmationField = MenuLayout.Field("Repeat password", _confirmation);
        _frame.Body.AddChild(_confirmationField);
        _frame.Body.AddChild(_save);
        _frame.Body.AddChild(_error);
        _password.TextSubmitted += _ => Submit();
        _confirmation.TextSubmitted += _ => Submit();
        _submit = MenuLayout.Button(null, "Join", Submit);
        _frame.Footer.AddChild(_submit);
        _frame.Footer.AddChild(MenuLayout.Button(null, "Cancel", Cancel));
        _modal = new MenuModal(_frame.Root);
        _modal.Closed += () =>
        {
            if (!_closing && ClientAuth.IsAwaitingPassword) AuthPromptWindow.Cancel();
        };
    }

    public void Sync(CanvasRoot root)
    {
        if (!ClientAuth.IsAwaitingPassword)
        {
            if (_modal.IsOpen) CloseQuietly();
            return;
        }

        bool register = !ClientAuth.IsRegistered;
        if (!_modal.IsOpen)
        {
            _password.Text = "";
            _confirmation.Text = "";
            _localError = null;
            _modal.Open(root);
            _password.GrabFocus();
        }

        _frame.Title.Text = register ? "Create an account" : "Log in";
        _message.Text = register
            ? $"\"{ClientAuth.PlayerName}\" is not registered on this server yet. Choose a password for this name."
            : $"Enter the password of \"{ClientAuth.PlayerName}\".";
        _confirmationField.Visible = register;
        _submit.Text = register ? "Create account" : "Join";
        string error = _localError ?? ClientAuth.Error;
        _error.Text = error ?? "";
        _error.Visible = !string.IsNullOrEmpty(error);
    }

    private void Submit()
    {
        if (ClientAuth.Submit(_password.Text, _confirmation.Text, _save.ButtonPressed, out var error))
        {
            CloseQuietly();
            return;
        }

        _localError = error;
    }

    private void Cancel()
    {
        CloseQuietly();
        AuthPromptWindow.Cancel();
    }

    private void CloseQuietly()
    {
        _closing = true;
        _modal.Close();
        _closing = false;
        _password.Text = "";
        _confirmation.Text = "";
        _localError = null;
    }
}
