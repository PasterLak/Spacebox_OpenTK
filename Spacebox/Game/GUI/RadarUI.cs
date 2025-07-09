using ImGuiNET;

using Engine.Audio;

using Spacebox.Game.Player;
using System.Numerics;
using Engine;
using Spacebox.Game.GUI.Menu;
namespace Spacebox.Game.GUI
{
    public class RadarUI
    {
        public static RadarUI Instance;

        private bool _isVisible = false;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                if (_isVisible)
                {
                    openSound?.Play();
                }
                else
                {
                    closeSound?.Play();
                }
            }
        }

        public bool ShowCursor => throw new NotImplementedException();

        public event Action OnOpen;
        public event Action OnClose;

        private Texture2D _texture;
        private Texture2D _grid;
        private Texture2D _line;
        private Texture2D _scanning;
        private Texture2D _mask;

        private AudioSource _scanningAudio;
        private AudioSource _foundAudio;

        private AudioSource openSound;
        private AudioSource closeSound;

        private Vector2 uvMin = Vector2.Zero;
        private Vector2 uvMax = Vector2.Zero;
        private Vector2 uvSize = new Vector2(0.15f, 0.15f);
        private Vector2 uvDir = new Vector2(0.7f, 0.5f);

        private float _rotationAngleGrid = 0f;
        private float rotationSpeed = MathF.PI / 2 * 0.8f;
        private float scanningCircleSize = 0;
        private bool showCircle = false;

        bool pointVisible = false;
        float time = 2;
        public RadarUI(Texture2D skyboxTexture)
        {
            _texture = skyboxTexture;
            Instance = this;
            uvMax = uvSize;
            _grid = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarGrid.png");
            _line = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarLine.png");
            _scanning = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/scanning.png");
            _mask = Resources.Load<Texture2D>("Resources/Textures/UI/Radar/radarMask.png");


            _grid.FilterMode = FilterMode.Nearest;
            _line.FilterMode = FilterMode.Nearest;
            _scanning.FilterMode = FilterMode.Nearest;
            _mask.FilterMode = FilterMode.Nearest;

            _scanningAudio = new AudioSource(Resources.Load<AudioClip>("radarScanning"));
            _foundAudio = new AudioSource(Resources.Load<AudioClip>("radarFound"));


            var inventory = ToggleManager.Register("radar");
            inventory.IsUI = true;
            inventory.OnStateChanged += s =>
            {
                IsVisible = s;
            };

            if (openSound == null)
            {
                openSound = new AudioSource(Resources.Load<AudioClip>("openBlock1"));
                openSound.Volume = 1f;

            }

            if (closeSound == null)
            {
                closeSound = new AudioSource(Resources.Load<AudioClip>("openBlock4"));
                closeSound.Volume = 1f;

            }
        }

        public void OnGUI()
        {
            if (!IsVisible) return;

            Vector2 displaySize = ImGui.GetIO().DisplaySize;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1f, 0.75f, 0f, 0f));
            float windowWidth = displaySize.Y * 0.4f;
            float windowHeight = displaySize.Y * 0.4f;
            var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

            ImGui.Begin("Main Menu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoScrollbar);

            float spacing = windowHeight * 0.03f;

            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);

            var drawList = ImGui.GetWindowDrawList();

            UpdateUV(Time.Delta * 0.005f);


            drawList.AddImage(_texture.Handle, windowPos + new Vector2(spacing, spacing),
                windowPos + new Vector2(windowWidth, windowHeight) - new Vector2(spacing, spacing),
                uvMin, uvMax);

            drawList.AddRectFilled(
               windowPos + new Vector2(spacing, spacing),
               windowPos + new Vector2(windowWidth, windowHeight) - new Vector2(spacing, spacing),
               ImGui.GetColorU32(new Vector4(62 / 255f, 150 / 255f, 67 / 255f, 1)));

            drawList.AddImage(_grid.Handle, windowPos + new Vector2(spacing, spacing),
                windowPos + new Vector2(windowWidth, windowHeight) - new Vector2(spacing, spacing)
            );

            _rotationAngleGrid += rotationSpeed * Time.Delta;
            _rotationAngleGrid %= MathF.PI * 2;

            if (_rotationAngleGrid >= 3.1f && _rotationAngleGrid < 3.2f)
            {
                if (!_scanningAudio.IsPlaying)
                {
                    _scanningAudio.Play();
                    scanningCircleSize = 0;
                    showCircle = true;
                }
            }

            if (_rotationAngleGrid >= 4.99f && _rotationAngleGrid < 5.1f)
            {
                if (!_foundAudio.IsPlaying)
                {
                    _foundAudio.Play();
                    pointVisible = true;
                }
            }

            Vector2 center = new Vector2(windowWidth / 2, windowHeight / 2) + windowPos;
            Vector2 halfSize = new Vector2(windowWidth, windowHeight) * 0.5f;

            if (pointVisible)
            {
                var direction = GetDirectionFromAngle(5f);

                time -= Time.Delta;

                if (time <= 0)
                {
                    time = 0;

                }
                float distance = windowHeight * 0.5f * 0.7f; // 0.1 min  0.85 max
                Vector2 pointPos = center + direction * distance;

                float pointRadius = windowHeight * 0.015f;

                float alpha = time / 2f;
                alpha = Math.Clamp(alpha, 0f, 1f);

                //drawList.AddCircleFilled(pointPos, pointRadius,
                //     ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));

                drawList.AddRectFilled(pointPos - new Vector2(pointRadius, pointRadius),
                    pointPos + new Vector2(pointRadius, pointRadius),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));

                if (time <= 0)
                {
                    time = 2;
                    pointVisible = false;
                }
            }


            if (showCircle)
            {
                scanningCircleSize += 2f * Time.Delta;
                if (scanningCircleSize > 0.99f)
                {
                    showCircle = false;
                    scanningCircleSize = 0;
                }

                float alpha = 1.0f - scanningCircleSize;
                alpha = Math.Clamp(alpha, 0f, 1f);

                drawList.AddImage(_scanning.Handle,
                    center - halfSize * scanningCircleSize,
                    center + halfSize * scanningCircleSize,
                     new Vector2(0, 0), new Vector2(1, 1),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, alpha))
            );
            }





            Vector2[] corners = new Vector2[]
            {
                new Vector2(-halfSize.X, -halfSize.Y),
                new Vector2(-halfSize.X, halfSize.Y),
                new Vector2(halfSize.X, halfSize.Y),
                new Vector2(halfSize.X, -halfSize.Y)
            };

            float cos = MathF.Cos(_rotationAngleGrid);
            float sin = MathF.Sin(_rotationAngleGrid);

            for (int i = 0; i < corners.Length; i++)
            {
                float x = corners[i].X * cos - corners[i].Y * sin;
                float y = corners[i].X * sin + corners[i].Y * cos;
                corners[i] = new Vector2(x, y) + center;
            }

            Vector2 uv0 = new Vector2(0, 0);
            Vector2 uv1 = new Vector2(0, 1);
            Vector2 uv2 = new Vector2(1, 1);
            Vector2 uv3 = new Vector2(1, 0);

            drawList.AddImageQuad(
                _line.Handle,
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                uv0, uv1, uv2, uv3,
                ImGui.GetColorU32(new Vector4(1, 1, 1, 1))
            );


            drawList.AddImage(_mask.Handle, windowPos + new Vector2(spacing, spacing),
                windowPos + new Vector2(windowWidth, windowHeight) - new Vector2(spacing, spacing)
            );

            ImGui.PopStyleColor(1);
            ImGui.End();
        }



        private Vector2 GetDirectionFromAngle(float radians)
        {
            float adjustedRadians = radians - MathF.PI;
            float x = MathF.Cos(adjustedRadians);
            float y = MathF.Sin(adjustedRadians);
            return new Vector2(x, y);
        }



        private void UpdateUV(float deltaTime)
        {
            Vector2 movement = uvDir * deltaTime;

            Vector2 newUvMin = uvMin + movement;
            Vector2 newUvMax = uvMin + uvSize + movement;

            if (newUvMin.X < 0)
            {
                uvDir.X *= -1;
                newUvMin.X = 0;
                newUvMax.X = uvSize.X;
            }
            if (newUvMin.Y < 0)
            {
                uvDir.Y *= -1;
                newUvMin.Y = 0;
                newUvMax.Y = uvSize.Y;
            }
            if (newUvMax.X > 1)
            {
                uvDir.X *= -1;
                newUvMax.X = 1;
                newUvMin.X = 1 - uvSize.X;
            }
            if (newUvMax.Y > 1)
            {
                uvDir.Y *= -1;
                newUvMax.Y = 1;
                newUvMin.Y = 1 - uvSize.Y;
            }

            uvMin = newUvMin;
            uvMax = newUvMax;
        }

        public static void CenterButtonWithBackground(string label, float width, float height, Action onClick)
        {
            float windowWidth = ImGui.GetWindowWidth();
            float cursorX = (windowWidth - width) * 0.5f;
            ImGui.SetCursorPosX(cursorX);

            Vector2 buttonPos = ImGui.GetCursorScreenPos();
            float offsetValue = height * 0.1f;
            Vector2 offset = new Vector2(offsetValue, offsetValue);

            uint borderColor = ImGui.GetColorU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
            uint lightColor = ImGui.GetColorU32(new Vector4(0.5f, 0.5f, 0.5f, 1f));

            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(buttonPos - offset, buttonPos + new Vector2(width, height) + offset, borderColor);
            drawList.AddRectFilled(buttonPos, buttonPos + new Vector2(width, height), lightColor);

            if (ImGui.Button(label, new Vector2(width, height)))
            {
                onClick?.Invoke();
            }
        }

        public void Open()
        {
            IsVisible = true;
            OnOpen?.Invoke();
            ToggleManager.SetState("player", !IsVisible);
        }

        public void Close()
        {
            IsVisible = false;
            OnClose?.Invoke();
            ToggleManager.SetState("player", !IsVisible);
        }

        public void Toggle(Astronaut player)
        {
            var v = !IsVisible;

            ToggleManager.DisableAllWindows();



            if (v)
            {
                OnOpen?.Invoke();
                ToggleManager.SetState("mouse", true);
                ToggleManager.SetState("player", false);
                ToggleManager.SetState("radar", v);
                ToggleManager.SetState("panel", false);
            }

            else
            {
                OnClose?.Invoke();
                ToggleManager.SetState("mouse", false);
                ToggleManager.SetState("player", true);
                ToggleManager.SetState("panel", true);
            }

        }

        public void Render(Vector2 windowSize)
        {
        }
    }
}
