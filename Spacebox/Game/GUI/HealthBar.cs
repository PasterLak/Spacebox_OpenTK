using System.Numerics;
using Spacebox.Common;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.GUI;

namespace Spacebox.GUI
{
    public class HealthBar
    {
        public StatsBarData StatsData { get; private set; }
        private StatsGUI _statsGUI;
        private float timeToDecrement = 4f;
        private float time;
        public HealthBar()
        {
            StatsData = new StatsBarData
            {
                Count = 100,
                MaxCount = 100,
                Name = "Health"
            };

            _statsGUI = new StatsGUI(StatsData)
            {
                FillColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
                TextColor = new Vector4(1f, 1f, 1f, 1f),
                Size = new Vector2(300, 50),
                Position = new Vector2(1140, 220),
                Anchor = Anchor.BottomLeft,
                WindowName = "HealthBar"
                
            }; _statsGUI.ShowText = true;
            _statsGUI.OnResized(Window.Instance.Size);
        }


        public void Update()
        {
          

            if (StatsData.IsMaxReached) return;

            if (time < timeToDecrement)
            {
                time += Time.Delta;
            }

            if (time >= timeToDecrement)
            {
                time = timeToDecrement * 0.5f;
                StatsData.Increment(1);
            }
        }

        public void OnGUI()
        {
            _statsGUI.OnGUI();
        }
    }
}
