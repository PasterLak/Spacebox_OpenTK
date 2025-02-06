using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;

using Spacebox.GUI;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player
{
    public class InteractionCameraPoint : InteractionMode
    {
        private LineRenderer lineRenderer;
        private CubeRenderer cubeRenderer;
        private bool IsEditing = true;
        private bool isFlying = false;
        public float FlightSpeed = 2f;
        private float flightDistance = 0f;
        private float totalPathLength = 0f;
        public bool LookAlongPath = false;

        private ItemModel _model;
        public InteractionCameraPoint(ItemModel model)
        {
            _model = model;
            _model.EnableRender = true;
        }

        private readonly List<Sample> pathSamples = new List<Sample>();

        private struct Sample
        {
            public float Dist;
            public Vector3 Pos;
        }

        private Astronaut player;

        public override void OnEnable()
        {
            lineRenderer = new LineRenderer();
            lineRenderer.Color = Color4.Yellow;
            lineRenderer.Thickness = 0.01f;
            lineRenderer.Enabled = true;

            cubeRenderer = new CubeRenderer(Vector3.Zero);
            cubeRenderer.Enabled = true;
            cubeRenderer.Color = Color4.Yellow;
            cubeRenderer.Scale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        public override void OnDisable()
        {
            lineRenderer.ClearPoints();
            _model.EnableRender = true;
            if (player != null)
            {
                player.CollisionEnabled = true;
                player.CameraActive = true;
            }
        }

        public override void Update(Astronaut player)
        {
            if (this.player == null)
                this.player = player;

            if (!player.CanMove) return;

            if (isFlying)
                player.CollisionEnabled = false;
            else
                player.CollisionEnabled = true;

            if (isFlying && LookAlongPath)
                player.CameraActive = false;
            else
                player.CameraActive = true;

            if (!isFlying)
            {
                HandleEditing(player);
                HandleSpeedChange();
            }
            else
            {
                HandleSpeedChange();
                flightDistance += FlightSpeed * Time.Delta;
                if (flightDistance >= totalPathLength)
                {
                    isFlying = false;
                    IsEditing = true;
                }
                else
                {
                    player.Position = GetPositionByDistance(flightDistance);
                    if (LookAlongPath)
                    {
                        float lookAhead = 0.1f;
                        float nextDist = flightDistance + lookAhead;
                        if (nextDist > totalPathLength) nextDist = totalPathLength;
                        var nextPos = GetPositionByDistance(nextDist);
                        var dir = nextPos - player.Position;
                        if (dir.LengthSquared > 1e-6f)
                        {
                            dir.Normalize();
                            player.SetRotation(CreateLookRotation(dir, Vector3.UnitY));
                        }
                    }
                }
            }
        }

        public override void Render(Astronaut player)
        {
            if (!isFlying) lineRenderer.Render();

            if (!isFlying)
            {
                _model.EnableRender = true;
                if (lineRenderer.Points.Count > 0)
                {
                    cubeRenderer.Position = lineRenderer.Points[0];
                    cubeRenderer.Render();
                }
            }
            else
            {
                _model.EnableRender = false;
            }
        }

        private void HandleEditing(Astronaut player)
        {
            if (IsEditing)
            {
                if (Input.IsKey(Keys.X) && Input.IsMouseButtonDown(MouseButton.Left))
                {
                    lineRenderer.ClearPoints();
                }
                else
                {
                    if (Input.IsMouseButtonDown(MouseButton.Right))
                        lineRenderer.AddPoint(player.Position);

                    if (Input.IsMouseButtonDown(MouseButton.Left))
                        lineRenderer.Pop();
                }
            }

            if (Input.IsMouseButtonDown(MouseButton.Middle))
            {
                if (lineRenderer.Points.Count > 1)
                {
                    IsEditing = false;
                    RebuildPath();
                    if (totalPathLength > 0f)
                    {
                        isFlying = true;
                        flightDistance = 0f;
                        CenteredText.Hide();
                    }
                }
            }
        }

        private void HandleSpeedChange()
        {
            if (Input.IsKey(Keys.LeftAlt))
            {
                PanelUI.AllowScroll = false;
                float scrollDelta = Input.MouseScrollDelta.Y;
                if (MathF.Abs(scrollDelta) > 0.0001f)
                {
                    FlightSpeed = MathF.Max(0.1f, FlightSpeed + scrollDelta * 0.2f);
                }

                if(!isFlying)
                {
                    CenteredText.SetText($"Speed: {FlightSpeed:F1}");
                    CenteredText.Show();
                }
               
            }
            else
            {
                PanelUI.AllowScroll = true;
                CenteredText.Hide();
            }
        }

        private void RebuildPath()
        {
            pathSamples.Clear();
            totalPathLength = 0f;
            if (lineRenderer.Points.Count < 2) return;

            int segCount = lineRenderer.Points.Count - 1;
            int stepsPerSegment = 40;
            Vector3 prevPos = lineRenderer.Points[0];
            float accumDist = 0f;
            pathSamples.Add(new Sample { Dist = 0f, Pos = prevPos });

            for (int i = 0; i < segCount; i++)
            {
                Vector3 p0 = lineRenderer.Points[Math.Max(i - 1, 0)];
                Vector3 p1 = lineRenderer.Points[i];
                Vector3 p2 = lineRenderer.Points[i + 1];
                Vector3 p3 = lineRenderer.Points[Math.Min(i + 2, lineRenderer.Points.Count - 1)];

                for (int s = 1; s <= stepsPerSegment; s++)
                {
                    float t = s / (float)stepsPerSegment;
                    Vector3 pos = CatmullRom(p0, p1, p2, p3, t);
                    float dist = (pos - prevPos).Length;
                    accumDist += dist;
                    pathSamples.Add(new Sample { Dist = accumDist, Pos = pos });
                    prevPos = pos;
                }
            }
            totalPathLength = accumDist;
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private Vector3 GetPositionByDistance(float dist)
        {
            if (pathSamples.Count == 0) return Vector3.Zero;
            if (dist <= 0f) return pathSamples[0].Pos;
            if (dist >= totalPathLength) return pathSamples[^1].Pos;

            int left = 0;
            int right = pathSamples.Count - 1;
            while (right - left > 1)
            {
                int mid = (left + right) / 2;
                if (pathSamples[mid].Dist < dist) left = mid; else right = mid;
            }

            float segmentDist = pathSamples[right].Dist - pathSamples[left].Dist;
            if (segmentDist < 1e-6f) return pathSamples[left].Pos;

            float alpha = (dist - pathSamples[left].Dist) / segmentDist;
            return Vector3.Lerp(pathSamples[left].Pos, pathSamples[right].Pos, alpha);
        }

        private Quaternion CreateLookRotation(Vector3 forward, Vector3 up)
        {
            forward.Normalize();
            var right = Vector3.Cross(up, forward).Normalized();
            var newUp = Vector3.Cross(forward, right).Normalized();
            var m = new Matrix4(
                new Vector4(right.X, right.Y, right.Z, 0f),
                new Vector4(newUp.X, newUp.Y, newUp.Z, 0f),
                new Vector4(forward.X, forward.Y, forward.Z, 0f),
                new Vector4(0f, 0f, 0f, 1f)
            );
            return m.ExtractRotation();
        }
    }
}
