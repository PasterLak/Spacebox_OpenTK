using Engine;
using Engine.Commands;

namespace Spacebox.Game.Commands;

internal sealed class SsaoDebugCommand : CommandBase
{
    public override string Name => "ssao_debug";
    public override string Description => "Toggle the raw SSAO view";
    public override bool AvailableInTerminal => false;

    public override void Execute(string[] args)
    {
        var ssao = (EngineWindow.Instance as SpaceboxWindow)?.Ssao;
        if (ssao == null)
        {
            Debug.Warning("SSAO is not available");
            return;
        }

        ssao.DebugView = !ssao.DebugView;
        Debug.Log($"SSAO debug view: {(ssao.DebugView ? "on" : "off")}");
        if (!ssao.Enabled) Debug.Warning("SSAO is off in the graphics settings");
    }
}
