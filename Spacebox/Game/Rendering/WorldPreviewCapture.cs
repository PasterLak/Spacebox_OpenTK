namespace Spacebox.Game.Rendering;

public static class WorldPreviewCapture
{
    public static bool IsActive { get; private set; }

    public static void Run(Action capture)
    {
        if (IsActive) return;

        var selector = BlockSelector.Instance;
        bool selectorEnabled = selector != null && selector.Enabled;
        if (selectorEnabled) selector.Enabled = false;
        IsActive = true;
        try
        {
            capture();
        }
        finally
        {
            IsActive = false;
            if (selectorEnabled) selector.Enabled = true;
        }
    }
}
