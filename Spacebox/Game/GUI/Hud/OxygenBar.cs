using System.Numerics;
using Engine;
using Engine.UI;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.GUI;

public class OxygenBar
{
    public static readonly Vector4 Color = new Vector4(0f, 1f, 1f, 1f);
    private const string AirText = "Air";
    private const string InfiniteText = "Infinite";

    public StatsData StatsData { get; }
    public StatsGUI StatsGUI { get; set; }

    public float ConsumeInterval { get; set; } = 10f;
    public int ConsumeAmount { get; set; } = 1;
    public int RefillPerSecond { get; set; } = 10;
    public bool IsInAir { get; private set; }
    public bool IsInfinite { get; private set; }
    public event Action<bool> AirChanged;

    private float _time;
    private float _refillTime;
    private bool _stateApplied;

    public OxygenBar()
    {
        StatsData = new StatsData
        {
            MaxValue = 100,
            Name = "Oxygen"
        };
        StatsData.Fill();

        StatsGUI = new StatsGUI(StatsData)
        {
            FillColor = Color,
            BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
            TextColor = new Vector4(1f, 1f, 1f, 1f),
            Size = new Vector2(300, 50),
            Position = new Vector2(0, 225),
            Anchor = Anchor.Bottom,
            WindowName = "OxygenBar"
        };
        StatsGUI.ShowText = true;
        StatsGUI.OnResized(SpaceboxWindow.Instance.Size);
    }

    public void Update(bool isInAir)
    {
        SetState(isInAir, false);

        if (isInAir)
        {
            Refill();
            return;
        }

        if (ConsumeInterval <= 0f || StatsData.IsMinReached) return;

        _time += Time.Delta;
        if (_time < ConsumeInterval) return;

        _time -= ConsumeInterval;
        StatsData.Decrement(ConsumeAmount);
    }

    public void UpdateInfinite(bool isInAir)
    {
        SetState(isInAir, true);
    }

    public static string GetTextOverride(bool isInAir, bool isInfinite)
    {
        if (isInAir) return AirText;
        return isInfinite ? InfiniteText : null;
    }

    private void SetState(bool isInAir, bool isInfinite)
    {
        if (IsInAir == isInAir && IsInfinite == isInfinite && _stateApplied) return;

        bool airChanged = _stateApplied && IsInAir != isInAir;
        _stateApplied = true;
        IsInAir = isInAir;
        IsInfinite = isInfinite;
        StatsGUI.TextOverride = GetTextOverride(isInAir, isInfinite);
        _refillTime = 0f;
        if (airChanged) AirChanged?.Invoke(isInAir);
    }

    private void Refill()
    {
        if (StatsData.IsMaxReached) return;

        _refillTime += Time.Delta;
        if (_refillTime < 1f) return;

        _refillTime -= 1f;
        StatsData.Increment(RefillPerSecond);
    }

    public void OnGUI()
    {
        StatsGUI.OnGUI();
    }
}
