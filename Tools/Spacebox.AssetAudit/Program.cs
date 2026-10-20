namespace Spacebox.AssetAudit;

public static class Program
{
    private const string Usage = "Usage: Spacebox.AssetAudit audit <repoRoot> [--out <file>] [--list]";

    public static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "audit")
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string repoRoot = args[1];
        if (!Directory.Exists(Path.Combine(repoRoot, AssetAuditor.GameFolder)))
        {
            Console.Error.WriteLine($"'{repoRoot}' is not the repository root (no {AssetAuditor.GameFolder} folder).");
            return 2;
        }

        string outFile = ReadOption(args, "--out");
        bool list = Array.IndexOf(args, "--list") >= 0;

        var result = new AssetAuditor(repoRoot).Analyze();
        AuditReport.Print(result, Console.Out, list || outFile == null);

        if (outFile != null)
            File.WriteAllLines(outFile, result.Unused.Select(file => file.RelativePath));

        return 0;
    }

    private static string ReadOption(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

public static class AuditReport
{
    private const int GroupDepth = 3;

    public static void Print(AssetAuditResult result, TextWriter output, bool listFiles)
    {
        output.WriteLine($"Assets: {result.Used.Count + result.Unused.Count}, used: {result.Used.Count}, unused: {result.Unused.Count} ({FormatSize(result.UnusedBytes)})");

        foreach (var group in result.Unused.GroupBy(file => GroupOf(file.RelativePath)).OrderByDescending(group => group.Sum(file => file.Size)))
        {
            output.WriteLine($"  {group.Key,-50} {group.Count(),4} files  {FormatSize(group.Sum(file => file.Size)),10}");
            if (!listFiles) continue;
            foreach (var file in group) output.WriteLine($"      {file.RelativePath}");
        }

        foreach (var missing in result.MissingReferences) output.WriteLine($"Missing reference in code: {missing}");
        foreach (var warning in result.Warnings) output.WriteLine($"Warning: {warning}");
    }

    public static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / (1024f * 1024f):0.0} MB";
        if (bytes >= 1024) return $"{bytes / 1024f:0.0} KB";
        return $"{bytes} B";
    }

    private static string GroupOf(string relativePath)
    {
        var segments = relativePath.Split('/');
        return string.Join('/', segments.Take(Math.Min(GroupDepth, segments.Length - 1)));
    }
}
