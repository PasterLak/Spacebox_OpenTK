namespace Engine.Graphics.Occlusion;

public sealed class OcclusionState
{
    public const int HideAfterOccludedResults = 2;

    public bool Visible { get; private set; } = true;
    public int OccludedStreak { get; private set; }
    public long LastSeenFrame { get; private set; } = long.MinValue;
    public int Epoch { get; private set; } = -1;
    public bool Pending { get; set; }
    internal OcclusionQuery Query { get; set; }

    public void Touch(long frame, int epoch)
    {
        if (epoch != Epoch || LastSeenFrame < frame - 1) Reset(epoch);
        LastSeenFrame = frame;
    }

    public void Apply(bool anySamplesPassed)
    {
        Pending = false;
        if (anySamplesPassed)
        {
            Visible = true;
            OccludedStreak = 0;
            return;
        }

        OccludedStreak++;
        if (OccludedStreak >= HideAfterOccludedResults) Visible = false;
    }

    public void ForceVisible()
    {
        Visible = true;
        OccludedStreak = 0;
    }

    public void Release()
    {
        if (Query != null) DisposalManager.EnqueueForDispose(Query);
        Query = null;
        Pending = false;
    }

    private void Reset(int epoch)
    {
        Epoch = epoch;
        Visible = true;
        OccludedStreak = 0;
        Pending = false;
    }
}
