//using System;
//using System.Runtime.InteropServices;
//using Voltium.Core.Devices;

//namespace Voltium.CubeGame
//{
//    internal class ChunkSystem
//    {
//        private GraphicsDevice _device;

//        private Memory<Chunk> _chunks;
//        public ReadOnlyMemory<Chunk> Chunks => _chunks;
//        public const int Width = 16, Height = 16, Depth = 16;

//        public ChunkSystem(GraphicsDevice device)
//        {
//            _device = device;
//        }

//        public void BuildMesh()
//        {
//            var vertexSpan = MemoryMarshal.Cast<byte, BlockVertex>(Vertices.Data);
//            var indexSpan = MemoryMarshal.Cast<byte, uint>(Indices.Data);
//            for (var i = 0; i < Width; i++)
//            {
//                for (var j = 0; j < Height; j++)
//                {
//                    for (var k = 0; k < Depth; k++)
//                    {
//                        BuildVisibleFaces(ref vertexSpan, ref indexSpan, i, j, k);
//                    }
//                }
//            }
//        }

//        private void BuildVisibleFaces(ref Span<BlockVertex> vertices, ref Span<uint> indices, int x, int y, int z)
//        {
//            var blocks = BlockSpan;

//            var maybeBlock = blocks[x, y, z];

//            if (maybeBlock is null)
//            {
//                return;
//            }

//            // Check for faces
//            var hasLeftFace = x == 0 || blocks[x - 1, y, z] is null;
//            var hasRightFace = x == Width - 1 || blocks[x + 1, y, z] is null;

//            var hasBottomFace = y == 0 || blocks[x, y - 1, z] is null;
//            var hasTopFace = y == Height - 1 || blocks[x, y + 1, z] is null;

//            var hasFrontFace = z == 0 || blocks[x, y, z - 1] is null;
//            var hasBackFace = z == Depth - 1 || blocks[x, y, z + 1] is null;

//            if (hasLeftFace)
//            {
//                AddFace(FaceHelper.LeftFace, ref vertices, ref indices);
//            }
//            if (hasRightFace)
//            {
//                AddFace(FaceHelper.RightFace, ref vertices, ref indices);
//            }
//            if (hasTopFace)
//            {
//                AddFace(FaceHelper.TopFace, ref vertices, ref indices);
//            }
//            if (hasBottomFace)
//            {
//                AddFace(FaceHelper.BottomFace, ref vertices, ref indices);
//            }
//            if (hasFrontFace)
//            {
//                AddFace(FaceHelper.FrontFace, ref vertices, ref indices);
//            }
//            if (hasBackFace)
//            {
//                AddFace(FaceHelper.BackFace, ref vertices, ref indices);
//            }

//            static void AddFace(BlockVertex[] blockVertices, ref Span<BlockVertex> vertices, ref Span<uint> indices)
//            {
//                blockVertices.CopyTo(vertices);
//                vertices = vertices.Slice(4);
//                FaceHelper.FaceIndices.CopyTo(indices);
//                indices = indices.Slice(6);
//            }
//        }
//    }
//}
