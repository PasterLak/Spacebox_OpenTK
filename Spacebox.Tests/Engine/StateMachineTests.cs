using Engine.AI;

namespace Spacebox.Tests.Engine;

public class StateMachineTests
{
    private sealed class Blackboard
    {
        public bool Alarm;
        public readonly List<string> Log = new List<string>();
    }

    private sealed class LoggingState : State<Blackboard>
    {
        private readonly string _name;

        public LoggingState(string name)
        {
            _name = name;
        }

        protected override void OnEnter() => Context.Log.Add("enter " + _name);
        protected override void OnExit() => Context.Log.Add("exit " + _name);
    }

    [Fact]
    public void Transition_ChangesStateWhenConditionHolds()
    {
        var blackboard = new Blackboard();
        var idle = new LoggingState("idle");
        var run = new LoggingState("run");
        var machine = new StateMachine<Blackboard>(blackboard);
        machine.AddTransition(idle, run, b => b.Alarm);

        machine.Update(0.1f);
        Assert.Same(idle, machine.Current);

        blackboard.Alarm = true;
        machine.Update(0.1f);
        Assert.Same(run, machine.Current);
        Assert.Equal(new[] { "enter idle", "exit idle", "enter run" }, blackboard.Log);
    }

    [Fact]
    public void NestedMachine_ExitsChildWhenParentLeaves()
    {
        var blackboard = new Blackboard();
        var inner = new LoggingState("inner");
        var flee = new LoggingState("flee");
        var nested = new StateMachine<Blackboard>();
        nested.SetInitial(inner);

        var machine = new StateMachine<Blackboard>(blackboard);
        machine.SetInitial(nested);
        machine.AddAnyTransition(flee, b => b.Alarm);

        machine.Update(0.1f);
        Assert.True(machine.IsIn(inner));

        blackboard.Alarm = true;
        machine.Update(0.1f);

        Assert.Same(flee, machine.Current);
        Assert.False(inner.IsActive);
        Assert.Contains("exit inner", blackboard.Log);
    }

    [Fact]
    public void TimeInState_AccumulatesAndResetsOnEnter()
    {
        var blackboard = new Blackboard();
        var a = new LoggingState("a");
        var b = new LoggingState("b");
        var machine = new StateMachine<Blackboard>(blackboard);
        machine.AddTransition(a, b, _ => a.TimeInState >= 0.25f);

        machine.Update(0.1f);
        machine.Update(0.1f);
        Assert.Same(a, machine.Current);

        machine.Update(0.1f);
        machine.Update(0.1f);
        Assert.Same(b, machine.Current);
        Assert.Equal(0.1f, b.TimeInState, 3);
    }
}
