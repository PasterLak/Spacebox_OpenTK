using Engine;
using Engine.Commands;

namespace Engine.Tests;

public class ColorCommandTests
{
    [Fact]
    public void Message_KeepsEveryWord()
    {
        var lines = new List<string>();
        int thread = Environment.CurrentManagedThreadId;
        Action<string, Debug.MessageType> capture = (text, _) =>
        {
            if (Environment.CurrentManagedThreadId == thread) lines.Add(text);
        };

        Debug.MessageLogged += capture;
        try
        {
            new ColorCommand().Execute(new[] { "red", "hello", "big", "world" });
        }
        finally
        {
            Debug.MessageLogged -= capture;
        }

        Assert.Contains("hello big world", lines);
    }
}
