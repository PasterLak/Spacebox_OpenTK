using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Effects.Machines;

public sealed class MachineActivities
{
    private readonly List<IMachineActivity> _activities = new List<IMachineActivity>();

    public static MachineActivities CreateDefault()
    {
        var activities = new MachineActivities();
        activities.Register(new ProcessingMachineActivity());
        activities.Register(new DisassemblerActivity());
        activities.Register(new GeneratorActivity());
        return activities;
    }

    public void Register(IMachineActivity activity)
    {
        if (activity != null) _activities.Add(activity);
    }

    public IMachineActivity Find(Block block)
    {
        for (int i = 0; i < _activities.Count; i++)
        {
            if (_activities[i].Handles(block)) return _activities[i];
        }
        return null;
    }
}
