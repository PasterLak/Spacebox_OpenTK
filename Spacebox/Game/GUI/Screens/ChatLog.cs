using OpenTK.Mathematics;

namespace Spacebox.Game.GUI;

public readonly record struct ChatMessage(string Text, Color4 Color, float Time);

public sealed class ChatLog
{
    public const int DefaultCapacity = 100;
    public const float FadeDuration = 1f;

    private readonly List<ChatMessage> _messages = new List<ChatMessage>();
    private readonly int _capacity;

    public ChatLog(int capacity = DefaultCapacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public int Count => _messages.Count;
    public int Version { get; private set; }

    public void Add(string text, Color4 color, float time)
    {
        _messages.Add(new ChatMessage(text ?? "", color, time));
        if (_messages.Count > _capacity) _messages.RemoveAt(0);
        Version++;
    }

    public void Clear()
    {
        if (_messages.Count == 0) return;
        _messages.Clear();
        Version++;
    }

    public bool HasVisible(float now, float hideDelay) => _messages.Count > 0 && Opacity(now - _messages[^1].Time, hideDelay) > 0f;

    public static float Opacity(float age, float hideDelay)
    {
        if (age < 0f) return 1f;
        if (age >= hideDelay) return 0f;
        float fade = MathF.Min(FadeDuration, hideDelay);
        float left = hideDelay - age;
        return left >= fade ? 1f : left / fade;
    }
}
