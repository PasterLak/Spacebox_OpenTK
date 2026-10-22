using Spacebox.Game.GUI;

namespace Spacebox.Game.ShipViewer;

public enum SavedWorldStatus
{
    Ready,
    NeedsUpgrade,
    Unsupported,
    Corrupt
}

public sealed class SavedWorld
{
    public SavedWorld(string folderPath, WorldInfo info, SavedWorldStatus status, string message)
    {
        FolderPath = folderPath;
        Info = info;
        Status = status;
        Message = message ?? "";
    }

    public string FolderPath { get; }
    public WorldInfo Info { get; }
    public SavedWorldStatus Status { get; }
    public string Message { get; }

    public string Name => string.IsNullOrWhiteSpace(Info?.Name) ? Path.GetFileName(FolderPath) : Info.Name;
    public string ModId => string.IsNullOrWhiteSpace(Info?.ModId) ? "default" : Info.ModId;
    public string GameVersion => Info?.GameVersion ?? "";
    public bool CanBeViewed => Status == SavedWorldStatus.Ready || Status == SavedWorldStatus.NeedsUpgrade;
}
