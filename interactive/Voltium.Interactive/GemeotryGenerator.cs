using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Interactive
{
    public readonly struct Geometry
    {
        public readonly Vertex[] Vertices;
        public readonly ushort[] Indices;

        public Geometry(Vertex[] vertices, ushort[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }
    }
    public static class GemeotryGenerator
    {
        public static Geometry CreateCube(float radius)
        {
            var cubeVertices = new Vertex[8]
            {
                new Vertex(new Vector3(-radius, -radius, -radius), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(-radius, -radius, radius), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(-radius, radius, -radius), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(-radius, radius, radius), new Vector4(0.0f, 1.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(radius, -radius, -radius), new Vector4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(radius, -radius, radius), new Vector4(1.0f, 0.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(radius, radius, -radius), new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(radius, radius, radius), new Vector4(1.0f, 1.0f, 1.0f, 1.0f))
            };

            return new Geometry(cubeVertices, CubeIndices);
        }

        private static ushort[] CubeIndices = new ushort[36]
        {
            0, 2, 1, 1, 2, 3, /* -x */ 4, 5, 6, 5, 7, 6, /* +x */ 0, 1, 5, 0, 5, 4, /* -y  */
            2, 6, 7, 2, 7, 3, /* +y */ 0, 4, 6, 0, 6, 2, /* -z */ 1, 3, 7, 1, 7, 5 /* +z */
        };
}
}
