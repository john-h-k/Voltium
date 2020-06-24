using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ObjLoader;
using ObjLoader.Loader.Loaders;
using TerraFX.Interop;
using UkooLabs.FbxSharpie;
using SharpGLTF;
using Voltium.Common;
using Voltium.TextureLoading;
using SharpGLTF.Schema2;
using SharpGLTF.Geometry;
using Voltium.Core.Managers.Shaders;
using System.Runtime.InteropServices;
using SharpGLTF.Memory;
using System.Collections.Generic;
using Voltium.Core;
using System;
using System.Diagnostics;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Voltium.ModelLoading
{
    [ShaderInput]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct TexturedVertex
    {
        public TexturedVertex(Vector3 position, Vector3 normal, Vector2 texC)
        {
            Position = position;
            Normal = normal;
            TexC = texC;
        }

        public TexturedVertex(
            float vertexX, float vertexY, float vertexZ,
            float normalX, float normalY, float normalZ,
            float tangentX, float tangentY, float tangentZ,
            float texU, float texV
        )
        {
            Position = new(vertexX, vertexY, vertexZ);
            Normal = new(normalX, normalY, normalZ);
            TexC = new(texU, texV);
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexC;
    }

    [ShaderInput]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ColorVertex
    {
        public ColorVertex(Vector3 position, Vector3 normal, Vector4 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        public ColorVertex(
            float vertexX, float vertexY, float vertexZ,
            float normalX, float normalY, float normalZ,
            float tangentX, float tangentY, float tangentZ,
            float colorR, float colorG, float colorB, float colorA
        )
        {
            Position = new(vertexX, vertexY, vertexZ);
            Normal = new(normalX, normalY, normalZ);
            Color = new(colorR, colorG, colorB, colorA);
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Color;
    }

    /// <summary>
    /// The type used for loading of 2 and 3D models
    /// </summary>
    public static class ModelLoader
    {
        // TODO improve
        public static void Load(string filename, out (TexturedVertex[] Vertices, ushort[] Indices, Matrix4x4 World)[] texturedVertices, out (ColorVertex[] Vertices, ushort[] Indices)[] coloredVertices)
        {
            var m = ModelRoot.Load(filename);
            
            var texturedObjs = new List<(TexturedVertex[] Vertices, ushort[] Indices, Matrix4x4 World)>(m.LogicalMeshes.Count);
            var coloredObjs = new List<(ColorVertex[] Vertices, ushort[] Indices)>(m.LogicalMeshes.Count);

            //Hemi.001
            //Hemi
            //Point.001
            //Point
            //Cube.002
            //Gun
            //hulle
            //bullet
            //bullet_light  

            var scene = m.DefaultScene;
            var nodes = scene.VisualChildren;

            IEnumerable<Node> Flatten(IEnumerable<Node> e) => e.SelectMany(c => Flatten(c.VisualChildren)).Concat(e);

            var flat = Flatten(nodes).Where(n => !n.Name.Contains("Plane") && n.Mesh is not null);

            foreach (var node in flat)
            {
                var world = node.WorldMatrix;
                foreach (var mesh in node.Mesh.Primitives)
                {
                    var positions = mesh.GetVertexAccessor("POSITION")?.AsVector3Array();
                    var normals = mesh.GetVertexAccessor("NORMAL")?.AsVector3Array();
                    var texCoords = mesh.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                    var colors = mesh.GetVertexAccessor("COLOR")?.AsColorArray();
                    var triangleIndices = mesh.GetTriangleIndices()?.ToArray();

                    var indices = triangleIndices?.SelectMany(i => new[] { (ushort)i.A, (ushort)i.B, (ushort)i.C }).ToArray();

                    if (texCoords is not null)
                    {
                        var vertices = new TexturedVertex[positions?.Count ?? 0];
                        for (var i = 0; i < vertices.Length; i++)
                        {
                            vertices![i] = new TexturedVertex(positions![i], normals![i], texCoords![i]);
                        }

                        texturedObjs.Add((vertices, indices!, world));
                    }
                }
            }

            texturedVertices = texturedObjs.ToArray();
            coloredVertices = coloredObjs.ToArray();

            //foreach (var texture in m.LogicalTextures)
            //{
            //    var image = texture.PrimaryImage;
            //    if (!image.Content.IsDds)
            //    {
            //        ThrowHelper.ThrowNotSupportedException("DDS only supported as of now");
            //    }

            //    var dds = TextureLoader.CreateTexture(image.Content.Content.ToArray() /* have to copy to get a ROM<byte> */, TexType.Dds);


            //}
        }
    }
}
