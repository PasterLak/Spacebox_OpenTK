using Engine;
using Spacebox.Network.Messages;
using Spacebox.Network.Moderation;

namespace Spacebox.Network;

public static class JoinRequestRules
{
    public static string? Validate(JoinRequestMessage message, string address)
    {
        if (message.GameVersion != Application.Version) return $"Version mismatch: server {Application.Version}, client {message.GameVersion}";
        string nameError = PlayerNameRules.Validate(message.Name);
        if (nameError != null) return nameError;
        if (ServerModeration.IsBanned(message.Name.Trim(), address)) return ServerModeration.BannedReason;
        return null;
    }
}
