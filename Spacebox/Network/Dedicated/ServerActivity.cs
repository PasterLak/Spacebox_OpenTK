namespace Spacebox.Network.Dedicated;

public enum ServerActivityState
{
    Active,
    Idle,
    Sleep
}

public sealed class ServerActivity
{
    public const float IdleAfterSeconds = 30f;

    private int _activeRate;
    private int _idleRate;
    private float _sleepAfterSeconds;
    private float _emptySeconds;

    public ServerActivity(int activeRate, int idleRate, float sleepAfterMinutes)
    {
        Configure(activeRate, idleRate, sleepAfterMinutes);
    }

    public void Configure(int activeRate, int idleRate, float sleepAfterMinutes)
    {
        _activeRate = activeRate;
        _idleRate = idleRate;
        _sleepAfterSeconds = sleepAfterMinutes > 0f ? sleepAfterMinutes * 60f : 0f;
    }

    public ServerActivityState State { get; private set; }

    public int TargetRate => State == ServerActivityState.Active || _idleRate <= 0 ? _activeRate : Math.Min(_idleRate, _activeRate);

    public ServerActivityState Update(int connectedPeers, float deltaSeconds)
    {
        if (connectedPeers > 0)
        {
            _emptySeconds = 0f;
            State = ServerActivityState.Active;
            return State;
        }

        _emptySeconds += Math.Max(0f, deltaSeconds);
        if (_sleepAfterSeconds > 0f && _emptySeconds >= _sleepAfterSeconds) State = ServerActivityState.Sleep;
        else if (_emptySeconds >= IdleAfterSeconds) State = ServerActivityState.Idle;
        else State = ServerActivityState.Active;
        return State;
    }
}
