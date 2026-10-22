namespace Engine.Diagnostics;

public sealed class SystemReport
{
    public string OperatingSystem { get; init; } = "";
    public string Architecture { get; init; } = "";
    public string Runtime { get; init; } = "";
    public string Cpu { get; init; } = "";
    public int LogicalCores { get; init; }
    public long MemoryBytes { get; init; }
    public string GpuRenderer { get; init; } = "";
    public string GpuVendor { get; init; } = "";
    public string OpenGlVersion { get; init; } = "";
    public string GlslVersion { get; init; } = "";
    public string Monitor { get; init; } = "";
    public string GameVersion { get; init; } = "";

    public IReadOnlyList<string> Lines()
    {
        var lines = new List<string>
        {
            $"[System] Game: {Value(GameVersion)}",
            $"[System] OS: {Value(OperatingSystem)} ({Value(Architecture)}), {Value(Runtime)}",
            $"[System] CPU: {Value(Cpu)}, {LogicalCores} logical cores",
            $"[System] RAM: {FormatMemory(MemoryBytes)}"
        };
        if (!string.IsNullOrWhiteSpace(GpuRenderer) || !string.IsNullOrWhiteSpace(GpuVendor))
        {
            lines.Add($"[System] GPU: {Value(GpuRenderer)} ({Value(GpuVendor)})");
            lines.Add($"[System] OpenGL: {Value(OpenGlVersion)}, GLSL {Value(GlslVersion)}");
        }
        if (!string.IsNullOrWhiteSpace(Monitor)) lines.Add($"[System] Monitor: {Monitor}");
        return lines;
    }

    public static string FormatMemory(long bytes)
    {
        if (bytes <= 0) return "unknown";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.0} GB";
    }

    private static string Value(string text) => string.IsNullOrWhiteSpace(text) ? "unknown" : text.Trim();
}
