using Engine.CanvasUI;

namespace Engine.Commands;

public sealed class UiValidateCommand : CommandBase
{
    public override string Name => "ui_validate";
    public override string Description => "Validates the layout of every CanvasUI layer; 'ui_validate overlay' toggles the on-screen issue overlay";

    public override void Execute(string[] args)
    {
        bool toggleOverlay = args.Length > 0 && args[0] == "overlay";
        var layers = CanvasLayer.Active;
        if (layers.Count == 0)
        {
            Debug.Warning("No CanvasUI layers are active");
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (toggleOverlay) layer.ShowLayoutIssues = !layer.ShowLayoutIssues;
            var report = layer.Validate();
            string text = $"{layer.Name}: {report.Format()}";
            if (report.IsClean) Debug.Success(text);
            else Debug.Warning(text);
        }
    }
}
