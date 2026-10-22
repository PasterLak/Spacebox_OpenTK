using System.Runtime.InteropServices;
using Engine;

namespace Spacebox.Network.Dedicated;

public sealed class ServerShutdownSignals : IDisposable
{
    public static readonly TimeSpan SaveWait = TimeSpan.FromSeconds(4.5);

    private static readonly PosixSignal[] Signals = { PosixSignal.SIGINT, PosixSignal.SIGTERM, PosixSignal.SIGQUIT };

    private readonly Action<Action> _runOnMainThread;
    private readonly Action _stop;
    private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(false);
    private readonly List<PosixSignalRegistration> _registrations = new List<PosixSignalRegistration>();
    private int _requested;

    public ServerShutdownSignals(Action<Action> runOnMainThread, Action stop)
    {
        _runOnMainThread = runOnMainThread;
        _stop = stop;
    }

    public bool IsStopped => _stopped.IsSet;

    public void Register()
    {
        foreach (var signal in Signals)
        {
            try
            {
                _registrations.Add(PosixSignalRegistration.Create(signal, OnSignal));
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException || ex is IOException)
            {
            }
        }
    }

    public bool Request()
    {
        if (Interlocked.Exchange(ref _requested, 1) != 0) return false;

        _runOnMainThread(() =>
        {
            try
            {
                _stop();
            }
            finally
            {
                _stopped.Set();
            }
        });
        return true;
    }

    public bool WaitUntilStopped(TimeSpan timeout) => _stopped.Wait(timeout);

    public void Dispose()
    {
        foreach (var registration in _registrations) registration.Dispose();
        _registrations.Clear();
    }

    private void OnSignal(PosixSignalContext context)
    {
        var signal = context.Signal;
        _runOnMainThread(() => Debug.Warning($"[Server] {signal} received: saving the world and stopping"));
        if (!Request()) return;

        context.Cancel = true;
        WaitUntilStopped(SaveWait);
    }
}
