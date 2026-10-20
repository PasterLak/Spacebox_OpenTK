namespace Engine.Tests.Core;

public class ConsoleTextTests
{
    private static readonly (string, Debug.MessageType)[] Messages =
    {
        ("started", Debug.MessageType.Info),
        ("careful", Debug.MessageType.Warning),
        ("broken", Debug.MessageType.Error),
        ("loaded", Debug.MessageType.Success)
    };

    [Fact]
    public void Build_WithoutFilter_JoinsAllMessagesInOrder()
    {
        Assert.Equal("started\ncareful\nbroken\nloaded", ConsoleText.Build(Messages, Debug.MessageType.Info));
    }

    [Fact]
    public void Build_WithFilter_KeepsOnlyThatType()
    {
        Assert.Equal("broken", ConsoleText.Build(Messages, Debug.MessageType.Error));
    }

    [Fact]
    public void Build_WithoutMessages_IsEmpty()
    {
        Assert.Equal(string.Empty, ConsoleText.Build(Array.Empty<(string, Debug.MessageType)>(), Debug.MessageType.Info));
    }
}
