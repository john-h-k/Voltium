using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using ObjLoader.Loader.Data;
using ObjLoader.Loader.Data.VertexData;
using ObjLoader.Loader.Loaders;
using Voltium.ModelLoading;
using Material = Voltium.ModelLoading.Material;
using ObjMaterial = ObjLoader.Loader.Data.Material;
using ObjVertex = ObjLoader.Loader.Data.VertexData.Vertex;
using Vertex = Voltium.ModelLoading.TexturedVertex;

namespace Voltium.Interactive
{
    public static class GeometryGenerator
    {
        public static Mesh<Vertex> CreateCube(float radius)
        {
            var cubeVertices = new Vertex[24]
            {
                // Fill in the front face vertex data.
	            new Vertex(-radius, -radius, -radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new Vertex(-radius, +radius, -radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new Vertex(+radius, +radius, -radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
                new Vertex(+radius, -radius, -radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),

                // Fill in the back face vertex data.
                new Vertex(-radius, -radius, +radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
                new Vertex(+radius, -radius, +radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new Vertex(+radius, +radius, +radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new Vertex(-radius, +radius, +radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),

                // Fill in the top face vertex data.
                new Vertex(-radius, +radius, -radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new Vertex(-radius, +radius, +radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new Vertex(+radius, +radius, +radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
                new Vertex(+radius, +radius, -radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),

                // Fill in the bottom face vertex data.
                new Vertex(-radius, -radius, -radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
                new Vertex(+radius, -radius, -radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new Vertex(+radius, -radius, +radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new Vertex(-radius, -radius, +radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),

                // Fill in the left face vertex data.
                new Vertex(-radius, -radius, +radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f),
                new Vertex(-radius, +radius, +radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f),
                new Vertex(-radius, +radius, -radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f),
                new Vertex(-radius, -radius, -radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f),

                // Fill in the right face vertex data.
                new Vertex(+radius, -radius, -radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f),
                new Vertex(+radius, +radius, -radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f),
                new Vertex(+radius, +radius, +radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f),
                new Vertex(+radius, -radius, +radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f)
            };

            return new Mesh<Vertex>(cubeVertices, CubeIndices);
        }

        private const string AssetsFolder = "Assets/";

        private sealed class AssetsProvider : IMaterialStreamProvider
        {
            public Stream Open(string materialFilePath) => File.OpenRead(AssetsFolder + materialFilePath);
        }

        private static readonly ObjLoaderFactory _factory = new ObjLoaderFactory();
        private static readonly IMaterialStreamProvider _assetsProvider = new AssetsProvider();
        private static ThreadLocal<IObjLoader> _loader = new ThreadLocal<IObjLoader>(() => { lock (_factory) { return _factory.Create(_assetsProvider); } });

        public static Mesh<Vertex> LoadSingleModel(string filename, in Material material = default)
        {
            var model = _loader.Value!.Load(File.OpenRead(AssetsFolder + filename));

            var indexCount = model.Groups
                .Aggregate(0, (val, group) => val += group.Faces
                    .Aggregate(0, (val, face) => val + face.Count));

            var indices = new uint[indexCount];
            var vertices = new Vertex[indexCount];


            int c = 0;
            for (int i = 0; i < model.Groups.Count; i++)
            {
                var group = model.Groups[i];
                for (var j = 0; j < group.Faces.Count; j++)
                {
                    var face = group.Faces[j];
                    for (var k = 0; k < face.Count; k++)
                    {
                        Debug.Assert(face.Count == 3);
                        var vertex = face[k];

                        var position = ToVector3(model.Vertices[vertex.VertexIndex - 1]);
                        var normal = ToVector3(model.Normals[vertex.NormalIndex - 1]);
                        var tex = ToVector2(model.Textures[vertex.TextureIndex - 1]);

                        //vertices[c] = new Vertex(position, normal, tex);
                        indices[c] = (uint)c;

                        c++;
                    }
                }
            }

            return new Mesh<Vertex>(vertices, indices, material);
        }

        private static Vector3 ToVector3(ObjVertex vertex) => new Vector3(vertex.X, vertex.Y, vertex.Z);
        private static Vector3 ToVector3(Normal normal) => new Vector3(normal.X, normal.Y, normal.Z);
        private static Vector3 ToVector3(Vec3 vec) => new Vector3(vec.X, vec.Y, vec.Z);
        private static Vector2 ToVector2(Texture tex) => new Vector2(tex.X, tex.Y);

        private static Material ToMaterial(ObjMaterial material) => new Material
        {
            DiffuseAlbedo = new Vector4(ToVector3(material.DiffuseColor), 1),
            ReflectionFactor = new Vector4(ToVector3(material.SpecularColor), 1),
            Shininess = material.SpecularCoefficient
        };

        private static uint[] CubeIndices = new uint[36]
        {
            // Fill in the front face index data
	        0, 1, 2,
            0, 2, 3,

	        // Fill in the back face index data
	        4, 5, 6,
            4, 6, 7,

	        // Fill in the top face index data
	        8, 9, 10,
            8, 10, 11,

	        // Fill in the bottom face index data
	        12, 13, 14,
            12, 14, 15,

	        // Fill in the left face index data
	        16, 17, 18,
            16, 18, 19,

	        // Fill in the right face index data
	        20, 21, 22,
            20, 22, 23
        };
    }
}
