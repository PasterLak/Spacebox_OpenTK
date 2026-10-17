namespace Spacebox.Game.Generation;

public static class StreamingBudget
{
    private const int GameplayMeshDispatchPerFrame = 4;
    private const int GameplayMeshUploadPerFrame = 2;
    private const double GameplayDispatcherBudgetMs = 6.0;

    public static bool IsLoading { get; set; }

    public static int MeshDispatchPerFrame => IsLoading ? int.MaxValue : GameplayMeshDispatchPerFrame;
    public static int MeshUploadPerFrame => IsLoading ? int.MaxValue : GameplayMeshUploadPerFrame;
    public static double DispatcherBudgetMs => IsLoading ? double.MaxValue : GameplayDispatcherBudgetMs;
}
