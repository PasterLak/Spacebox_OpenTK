namespace Spacebox.Game.GUI.Menu;

public sealed class AudioOutputList
{
    public const string SystemDefault = "System default";
    public const string MissingSuffix = " (not connected)";

    private readonly string[] _devices;

    private AudioOutputList(string[] labels, string[] devices)
    {
        Labels = labels;
        _devices = devices;
    }

    public string[] Labels { get; }

    public static AudioOutputList Build(IReadOnlyList<string> available, string chosen)
    {
        var labels = new List<string> { SystemDefault };
        var devices = new List<string> { "" };
        if (available != null)
        {
            for (int i = 0; i < available.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(available[i]) || devices.Contains(available[i])) continue;
                labels.Add(available[i]);
                devices.Add(available[i]);
            }
        }

        if (!string.IsNullOrWhiteSpace(chosen) && !devices.Contains(chosen))
        {
            labels.Add(chosen + MissingSuffix);
            devices.Add(chosen);
        }

        return new AudioOutputList(labels.ToArray(), devices.ToArray());
    }

    public int IndexOf(string device)
    {
        if (string.IsNullOrWhiteSpace(device)) return 0;
        int index = Array.IndexOf(_devices, device);
        return index < 0 ? 0 : index;
    }

    public string DeviceAt(int index) => index > 0 && index < _devices.Length ? _devices[index] : "";
}
