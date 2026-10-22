using Spacebox.Game.Player.GameModes;

namespace Spacebox.Agent;

public sealed class AgentOptions
{
    public const int DefaultPort = 47800;
    public const string DefaultWorld = "AgentWorld";

    public bool Enabled { get; private set; }
    public int Port { get; private set; } = DefaultPort;
    public string WorldName { get; private set; }
    public GameMode GameMode { get; private set; } = GameMode.Creative;
    public string Seed { get; private set; }
    public bool FreshWorld { get; private set; }
    public string[] Extensions { get; private set; } = Array.Empty<string>();

    public bool HasWorld => !string.IsNullOrWhiteSpace(WorldName);

    public static AgentOptions Parse(string[] args)
    {
        var options = new AgentOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();
            string next = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[i + 1] : null;

            switch (arg)
            {
                case "--agent":
                    options.Enabled = true;
                    if (next != null && int.TryParse(next, out int port))
                    {
                        options.Port = port;
                        i++;
                    }
                    break;
                case "--agent-world":
                    options.Enabled = true;
                    options.WorldName = next ?? DefaultWorld;
                    if (next != null) i++;
                    break;
                case "--agent-mode" when next != null:
                    if (Enum.TryParse(next, true, out GameMode mode)) options.GameMode = mode;
                    i++;
                    break;
                case "--agent-seed" when next != null:
                    options.Seed = next;
                    i++;
                    break;
                case "--agent-extensions" when next != null:
                    options.Extensions = Spacebox.Game.Resource.Content.WorldExtensionList.Parse(next);
                    i++;
                    break;
                case "--agent-fresh":
                    options.FreshWorld = true;
                    break;
            }
        }

        return options;
    }
}
