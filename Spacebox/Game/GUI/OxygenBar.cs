using System.Numerics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.GUI;

public class OxygenBar
{
    public static readonly Vector4 Color = new Vector4(0f, 1f, 1f, 1f);
    private const string AirText = "Air";

    public StatsData StatsData { get; }
    public StatsGUI StatsGUI { get; set; }

    public float ConsumeInterval { get; set; } = 10f;
    public int ConsumeAmount { get; set; } = 20;
    public int RefillPerSecond { get; set; } = 10;
    public bool IsInAir { get; private set; }

    private float _time;
    private float _refillTime;

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
        SetInAir(isInAir);

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

    private void SetInAir(bool isInAir)
    {
        if (IsInAir == isInAir) return;

        IsInAir = isInAir;
        StatsGUI.TextOverride = isInAir ? AirText : null;
        _refillTime = 0f;
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
