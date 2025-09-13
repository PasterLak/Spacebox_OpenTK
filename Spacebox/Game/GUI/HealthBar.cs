using System.Numerics;

using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Engine;
namespace Spacebox.GUI
{
    public class HealthBar
    {
        public StatsData StatsData { get; private set; }
        public StatsGUI StatsGUI { get; set; }
        private float timeToDecrement = 20f;
        private float _time;
        public HealthBar()
        {
            StatsData = new StatsData
            {
               
                MaxValue = 100,
                Name = "Health"
            };
            StatsData.Fill();

            StatsGUI = new StatsGUI(StatsData)
            {
                FillColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
                TextColor = new Vector4(1f, 1f, 1f, 1f),
                Size = new Vector2(300, 50),
                Position = new Vector2(-425, 225), // new Vector2(1140, 220),
                Anchor = Anchor.Bottom,
                WindowName = "HealthBar"
                
            }; StatsGUI.ShowText = true;
            StatsGUI.OnResized(Window.Instance.Size);
        }


        public void Update()
        {
            if (StatsData.IsMaxReached) return;

            if (_time < timeToDecrement)
            {
                _time += Time.Delta;
            }
            if (_time >= timeToDecrement)
            {
                _time = timeToDecrement * 0.5f;
                StatsData.Increment(1);
            }
        }

        public void OnGUI()
        {
            StatsGUI.OnGUI();
        }
    }
}
