using OpenTK.Mathematics;
using Spacebox.Common;
using System.Collections.Generic;

namespace Spacebox.Game
{
    public class Sector
    {
        public Vector3 Position { get; private set; }
        public Vector3i Index { get; private set; }
        public List<Chunk> Chunks { get; private set; }

        public Sector(Vector3 position, Vector3i index)
        {
            Position = position;
            Index = index;
            Chunks = new List<Chunk>();

            InitializeChunks();
        }

        private void InitializeChunks()
        {
            // Position chunks relative to the sector's position
            Chunk chunk = new Chunk(Position);
            Chunks.Add(chunk);
        }

        public void Update()
        {
            // Update contents of the sector if necessary
        }

        public void Render(Shader shader)
        {
            foreach (var chunk in Chunks)
            {
                chunk.Draw(shader);
            }
        }

        public void Shift(Vector3 shiftAmount)
        {
            Position -= shiftAmount;

            // Shift all chunks within the sector
            foreach (var chunk in Chunks)
            {
                chunk.Shift(shiftAmount);
            }
        }
    }
}
