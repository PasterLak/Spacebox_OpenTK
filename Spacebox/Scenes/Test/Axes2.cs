using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Engine;

namespace Spacebox.Scenes.Test
{
    // Axes class that displays an object's coordinate axes using LineRenderer.
    // Inherits from SceneNode to use its transformation (Position, Rotation, Scale).
    public class Axes2 : SceneNode, IDisposable
    {
        private LineRenderer2 _xAxis;
        private LineRenderer2 _yAxis;
        private LineRenderer2 _zAxis;

        // Length of each axis.
        public float AxisLength { get; set; }

        // Thickness for all axes.
        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                UpdateThickness();
            }
        }
        private float _thickness;

        /// <summary>
        /// Creates axes with the specified center, axis length and thickness.
        /// </summary>
        /// <param name="center">Center position of the axes.</param>
        /// <param name="axisLength">Length of each axis.</param>
        /// <param name="thickness">Line thickness.</param>
        public Axes2(Vector3 center, float axisLength = 1.0f, float thickness = 0.1f)
        {
            // Set the position of this SceneNode.
            Position = center;
            AxisLength = axisLength;
            _thickness = thickness;

            // Create X-axis (red).
            _xAxis = new LineRenderer2();
            _xAxis.Color = Color4.Red;
            _xAxis.Thickness = thickness;
            _xAxis.SetPoints(new List<Vector3>
            {
                Vector3.Zero,
                new Vector3(axisLength, 0, 0)
            });
            _xAxis.Parent = this;

            // Create Y-axis (green).
            _yAxis = new LineRenderer2();
            _yAxis.Color = Color4.Green;
            _yAxis.Thickness = thickness;
            _yAxis.SetPoints(new List<Vector3>
            {
                Vector3.Zero,
                new Vector3(0, axisLength, 0)
            });
            _yAxis.Parent = this;

            // Create Z-axis (blue).
            _zAxis = new LineRenderer2();
            _zAxis.Color = Color4.Blue;
            _zAxis.Thickness = thickness;
            _zAxis.SetPoints(new List<Vector3>
            {
                Vector3.Zero,
                new Vector3(0, 0, axisLength)
            });
            _zAxis.Parent = this;
        }

        /// <summary>
        /// Updates the thickness of all axes.
        /// </summary>
        private void UpdateThickness()
        {
            _xAxis.Thickness = _thickness;
            _yAxis.Thickness = _thickness;
            _zAxis.Thickness = _thickness;
        }

        /// <summary>
        /// Renders the axes.
        /// </summary>
        /// <param name="camera">The current camera to obtain view and projection matrices.</param>
        public void RenderAxes(Camera camera)
        {
            UpdateAxes();
            // Make sure the camera is available.
            if (camera == null)
                return;

            // Apply parent's transform via SceneNode if needed.
            // Render each axis.
            _xAxis.Render();
            _yAxis.Render();
            _zAxis.Render();
        }
        /// <summary>
        /// Updates the axes. Call this method in the scene's update cycle if needed.
        /// </summary>
        private void UpdateAxes()
        {
            _xAxis.Update();
            _yAxis.Update();
            _zAxis.Update();
        }


        public void Dispose()
        {
            _xAxis.Dispose();
            _yAxis.Dispose();
            _zAxis.Dispose();
        }
    }
}
