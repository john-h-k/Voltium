using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ObjLoader.Loader.Loaders;
using UkooLabs.FbxSharpie;
using Voltium.Core.Devices.Shaders;
using Silk.NET;
using Silk.NET.Assimp;
using Voltium.Common;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using SimpScene = Silk.NET.Assimp.Scene;
using SimpMesh = Silk.NET.Assimp.Mesh;
using SimpFace = Silk.NET.Assimp.Face;
using SimpReturn = Silk.NET.Assimp.Return;
using Node = SharpGLTF.Schema2.Node;
using ModelRoot = SharpGLTF.Schema2.ModelRoot;
using System;
using System.Buffers;

namespace Voltium.ModelLoading
{
    [ShaderInput]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct TexturedVertex
    {
        public TexturedVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texC)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
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
            Tangent = new(tangentX, tangentY, tangentZ);
            TexC = new(texU, texV);
        }

        public Vector3 Position;


        public Vector3 Normal;
        public Vector3 Tangent;

        [InputLayout(Name = "TexCoord")]
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

    public struct Material
    {
        public Vector4 DiffuseAlbedo;
        public Vector4 ReflectionFactor;
        public float Shininess;
    }

    public readonly struct Scene
    {

    }

    public struct Mesh<TVertex>
    {
        public readonly TVertex[] Vertices;
        public readonly uint[] Indices;
        public Material Material;
        public Matrix4x4 World;

        public Mesh(TVertex[] vertices, uint[] indices, Material material = default, Matrix4x4 world = default)
        {
            if (world == default)
            {
                world = Matrix4x4.Identity;
            }

            Vertices = vertices;
            Indices = indices;
            Material = material;
            World = world;
        }
    }

    /// <summary>
    /// The type used for loading of 2 and 3D models
    /// </summary>
    public static class ModelLoader
    {
        private static readonly ObjLoaderFactory _factory = new ObjLoaderFactory();
        private static readonly IObjLoader _loader = _factory.Create(new MaterialStreamProvider());
        private static readonly Assimp _assimp = Assimp.GetApi();

        private static MemoryHandle DiffuseKey = StringHelper.MarshalToUnmanagedAscii(Assimp.MaterialColorDiffuseBase);
        private static MemoryHandle ShininessKey = StringHelper.MarshalToUnmanagedAscii(Assimp.MaterialShininessBase);
        private static MemoryHandle ReflectionFactorKey = StringHelper.MarshalToUnmanagedAscii(Assimp.MaterialColorReflectiveBase);
        // TODO improve
        public static unsafe Mesh<TexturedVertex>[] Load(string filename)
        {
            using var ascii = StringHelper.MarshalToUnmanagedAscii(filename);
            SimpScene* pScene = _assimp.ImportFile((byte*)ascii.Pointer, 0);

            var meshes = new ArrayBuilder<Mesh<TexturedVertex>>();

            for (var i = 0; i < pScene->MNumMeshes; i++)
            {
                SimpMesh* pMesh = pScene->MMeshes[i];
                var vertices = new TexturedVertex[pMesh->MNumVertices];

                var material = new Material();
                var simpMaterial = pScene->MMaterials[pMesh->MMaterialIndex];

                ThrowIfFailed(_assimp.GetMaterialColor(simpMaterial, (byte*)DiffuseKey.Pointer, 0, 0, &material.DiffuseAlbedo));
                ThrowIfFailed(_assimp.GetMaterialFloatArray(simpMaterial, (byte*)ShininessKey.Pointer, 0, 0, &material.Shininess, null));
                ThrowIfFailed(_assimp.GetMaterialColor(simpMaterial, (byte*)ReflectionFactorKey.Pointer, 0, 0, &material.ReflectionFactor));

                for (var k = 0; k < pMesh->MNumVertices; k++)
                {
                    var vertex = new TexturedVertex(
                        pMesh->MVertices[k],
                        pMesh->MNormals[k],
                        pMesh->MTangents[k],
                        ToVector2(pMesh->MTextureCoords_0[k])
                    );

                    vertices[k] = vertex;
                }

                var indices = new ArrayBuilder<uint>();
                for (var j = 0; j < pMesh->MNumFaces; j++)
                {
                    SimpFace /* <- insert mug of dylan */ pFace = pMesh->MFaces[j];
                    indices.Add(new Span<uint>(pFace.MIndices, (int)pFace.MNumIndices));
                }

                meshes.Add(new Mesh<TexturedVertex>(vertices, indices.MoveTo(), material));
            }

            return meshes.MoveTo();



            static void ThrowIfFailed(SimpReturn @return)
            {
                if (@return == SimpReturn.ReturnSuccess)
                {
                    return;
                }
                if (@return == SimpReturn.ReturnOutofmemory)
                {
                    ThrowHelper.ThrowInsufficientMemoryException("'ReturnOutofmemory' returned by ASSIMP");
                }
                else
                {
                    ThrowHelper.ThrowExternalException("ASSIMP returned a generic error code");
                }
            }
        }

        private static Vector2 ToVector2(Vector3 vec) => Unsafe.As<Vector3, Vector2>(ref vec);

        public static Mesh<TexturedVertex>[] LoadGl_Old(string filename)
        {
            var m = ModelRoot.Load(filename);

            var texturedObjs = new List<Mesh<TexturedVertex>>(m.LogicalMeshes.Count);

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
                    var glMat = mesh.Material;
                    Material mat = default;
                    float metal = 0;

                    foreach (var channel in glMat.Channels)
                    {
                        //BaseColor
                        //MetallicRoughness
                        //Normal
                        //Occlusion
                        //Emissive

                        switch (channel.Key)
                        {
                            case "BaseColor":
                                mat.DiffuseAlbedo = channel.Parameter;
                                break;
                            case "MetallicRoughness":
                                mat.Shininess = channel.Parameter.Y;
                                metal = channel.Parameter.Z;
                                break;
                        }

                        mat.ReflectionFactor = new Vector4(Vector3.Lerp(new(0.4f), Unsafe.As<Vector4, Vector3>(ref mat.DiffuseAlbedo), metal), 0);
                        mat.ReflectionFactor.W = mat.ReflectionFactor.X;
                    }

                    var positions = mesh.GetVertexAccessor("POSITION")?.AsVector3Array();
                    var normals = mesh.GetVertexAccessor("NORMAL")?.AsVector3Array();
                    var texCoords = mesh.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                    var colors = mesh.GetVertexAccessor("COLOR")?.AsColorArray();
                    var tangents = mesh.GetVertexAccessor("TANGENT")?.AsVector4Array();
                    var triangleIndices = mesh.GetTriangleIndices()?.ToArray();

                    var indices = triangleIndices?.SelectMany(i => new[] { i.A, i.B, i.C }).ToArray();

                    if (texCoords is not null)
                    {
                        var vertices = new TexturedVertex[positions?.Count ?? 0];
                        for (var i = 0; i < vertices.Length; i++)
                        {
                            Vector4 tangent = default; // tangents![i];
                            vertices![i] = new TexturedVertex(positions![i], normals![i], Unsafe.As<Vector4, Vector3>(ref tangent), texCoords![i]);
                        }

                        texturedObjs.Add(new(vertices, Unsafe.As<uint[]>(indices!), mat, world));
                    }
                }
            }

            return texturedObjs.ToArray();

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
