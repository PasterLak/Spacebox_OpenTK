using Engine;
using Engine.Commands;

namespace Spacebox.Game.Commands;

internal sealed class OcclusionCommand : CommandBase
{
    public override string Name => "occlusion";
    public override string Description => "Chunk occlusion culling. Usage: occlusion [on|off]";
    public override bool AvailableInTerminal => false;

    public override void Execute(string[] args)
    {
        var culler = EngineWindow.Instance?.Occlusion;
        if (culler == null) return;

        if (args.Length > 0)
        {
            if (args[0].Equals("on", StringComparison.OrdinalIgnoreCase)) culler.Enabled = true;
            else if (args[0].Equals("off", StringComparison.OrdinalIgnoreCase)) culler.Enabled = false;
        }

        Debug.Log($"Occlusion culling: {(culler.Enabled ? "on" : "off")}, drawn {culler.DrawnLastFrame}, occluded {culler.OccludedLastFrame} (chunks and players)");
    }
}
