using Engine;
using Engine.Commands;

namespace Spacebox.Tests.Network;

internal static class CommandCapture
{
    public static List<(string Text, Debug.MessageType Type)> Run(CommandBase command, params string[] args)
    {
        var lines = new List<(string Text, Debug.MessageType Type)>();
        int thread = Environment.CurrentManagedThreadId;
        Action<string, Debug.MessageType> capture = (text, type) =>
        {
            if (Environment.CurrentManagedThreadId != thread) return;
            lock (lines) lines.Add((text, type));
        };

        Debug.MessageLogged += capture;
        try
        {
            command.Execute(args);
        }
        finally
        {
            Debug.MessageLogged -= capture;
        }
        return lines;
    }
}
