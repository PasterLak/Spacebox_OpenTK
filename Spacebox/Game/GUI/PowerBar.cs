using Spacebox.Game.GUI;
using System.Numerics;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Player;
using Engine;

namespace Spacebox.GUI
{
    public class PowerBar
    {
        public StatsBarData StatsData { get; private set; }
        public StatsGUI StatsGUI { get; set; }

        private float timeToDecrement = 1f;
        private float time;
        public PowerBar()
        {
            StatsData = new StatsBarData
            {
                Count = 100,
                MaxCount = 100,
                Name = "Power"
            };

            StatsGUI = new StatsGUI(StatsData)
            {
                FillColor = new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f),
                TextColor = new Vector4(1f, 1f, 1f, 1f),
                Size = new Vector2(300, 50),
                Position = new Vector2(425, 225), // new Vector2(1140, 220),
                Anchor = Anchor.Bottom,
                WindowName = "PowerBar"
            };
            StatsGUI.ShowText = true;
            time = timeToDecrement * 0.5f;
            StatsGUI.OnResized(Window.Instance.Size);
        }

        bool isRunning = false;


        public void Update()
        {
            if(Input.IsActionDown("sprint")) isRunning = true;
            if (Input.IsActionUp("sprint")) isRunning = false;

            if (isRunning) return;
            if (StatsData.IsMaxReached) return;

            if(time < timeToDecrement)
            {
                time += Time.Delta * 2f;
            }

            if(time >= timeToDecrement)
            {
                time = timeToDecrement * 0.5f;
                StatsData.Increment(1);
            }
        }

        public void Use()
        {
            if (StatsData.IsMinReached) return;

            if (time > 0)
            {
                time -= Time.Delta * 4f;
            }

            if (time <= 0)
            {
                time = timeToDecrement * 0.5f;
                StatsData.Decrement(1);
            }
        }

        public void OnGUI()
        {
            StatsGUI.OnGUI();
        }
    }

}
