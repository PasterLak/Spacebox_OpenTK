using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Spacebox.Common.Physics;

namespace Spacebox.Common
{
    public class BoundingBoxRenderer : LineRenderer, IDisposable
    {
        private BoundingBox _boundingBox;

        public BoundingBox BoundingBox
        {
            get => _boundingBox;
            set
            {
                _boundingBox = value;
                UpdateLines();
            }
        }

        public BoundingBoxRenderer(BoundingBox boundingBox, Color4 color = default, float thickness = 0.1f)
        {
            _boundingBox = boundingBox;
            this.Color = color == default ? Color4.Red : color;
            this.Thickness = thickness;
            UpdateLines();
        }

        private void UpdateLines()
        {
            if (_boundingBox == null)
                return;

            var lines = new List<Vector3>();

            Vector3 min = _boundingBox.Min;
            Vector3 max = _boundingBox.Max;

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(min.X, min.Y, min.Z),
                new Vector3(max.X, min.Y, min.Z),
                new Vector3(max.X, max.Y, min.Z),
                new Vector3(min.X, max.Y, min.Z),
                new Vector3(min.X, min.Y, max.Z),
                new Vector3(max.X, min.Y, max.Z),
                new Vector3(max.X, max.Y, max.Z),
                new Vector3(min.X, max.Y, max.Z)
            };

            int[,] edges = new int[,]
            {
                {0,1}, {1,2}, {2,3}, {3,0},
                {4,5}, {5,6}, {6,7}, {7,4},
                {0,4}, {1,5}, {2,6}, {3,7}
            };

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                lines.Add(corners[edges[i, 0]]);
                lines.Add(corners[edges[i, 1]]);
            }

            this.SetPoints(lines);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}
