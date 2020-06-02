using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Data.VertexData;
using ObjLoader.Loader.Loaders;
using Voltium.Core;
using ObjVertex = ObjLoader.Loader.Data.VertexData.Vertex;

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

        private static ObjLoaderFactory _factory = new();
        private static ThreadLocal<IObjLoader> _loader = new (() => { lock (_factory) { return _factory.Create(); } });

        public static Geometry LoadSingleModel(string filename, RgbaColor color)
        {
            var model = _loader.Value!.Load(File.OpenRead(filename));

            var indexCount = model.Groups
                .Aggregate(0, (val, group) => val += group.Faces
                    .Aggregate(0, (val, face) => val + face.Count));

            var indices = new ushort[indexCount];
            var vertices = new Vertex[indexCount];

            for (int i = 0, c = 0; i < model.Groups.Count; i++)
            {
                var group = model.Groups[i];
                for (var j = 0; j < group.Faces.Count; j++)
                {
                    var face = group.Faces[j];
                    for (var k = 0; k < face.Count; k++)
                    {
                        Debug.Assert(face.Count == 3);
                        var vertex = face[k];
                        vertices[c] = new Vertex(ToVector3(model.Vertices[vertex.VertexIndex - 1]), Unsafe.As<RgbaColor, Vector4>(ref color));
                        indices[c] = (ushort)c;

                        c++;
                    }
                }
            }

            return new Geometry(vertices, indices);
        }

        private static Vector3 ToVector3(ObjVertex vertex) => new Vector3(vertex.X, vertex.Y, vertex.Z);

        private static ushort[] CubeIndices = new ushort[36]
        {
            0, 2, 1, 1, 2, 3, /* -x */ 4, 5, 6, 5, 7, 6, /* +x */ 0, 1, 5, 0, 5, 4, /* -y  */
            2, 6, 7, 2, 7, 3, /* +y */ 0, 4, 6, 0, 6, 2, /* -z */ 1, 3, 7, 1, 7, 5 /* +z */
        };
    }
}
