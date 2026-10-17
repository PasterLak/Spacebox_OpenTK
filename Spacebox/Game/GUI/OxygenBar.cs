using System.Numerics;
using Engine;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.GUI;

public class OxygenBar
{
    public static readonly Vector4 Color = new Vector4(0f, 1f, 1f, 1f);

    public StatsData StatsData { get; }
    public StatsGUI StatsGUI { get; set; }

    public float ConsumeInterval { get; set; } = 10f;
    public int ConsumeAmount { get; set; } = 1;

    private float _time;

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

    public void Update()
    {
        if (ConsumeInterval <= 0f || StatsData.IsMinReached) return;

        _time += Time.Delta;
        if (_time < ConsumeInterval) return;

        _time -= ConsumeInterval;
        StatsData.Decrement(ConsumeAmount);
    }

    public void OnGUI()
    {
        StatsGUI.OnGUI();
    }
}
