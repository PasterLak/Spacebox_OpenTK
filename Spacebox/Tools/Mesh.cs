using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spacebox.Tools
{
    class Mesh
    {
        
        public float[] vertices;
        public int vertexCount;

        public Mesh()
        {
        }
        public Mesh(float[] _vertices, int vCount)
        {
            vertices = _vertices;
            vertexCount = vCount;
        }
    }
}