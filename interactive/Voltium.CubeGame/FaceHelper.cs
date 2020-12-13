using System;
using System.Numerics;

namespace Voltium.CubeGame
{
    internal static class FaceHelper
    {
        public const float Radius = 0.5f;

        public static readonly uint[] FaceIndices = new uint[6]
        {
            0, 1, 2,
            0, 2, 3,
        };

        public static readonly BlockVertex[] Face = new BlockVertex[]
        {
        };

        public static unsafe int FaceVerticesSize => sizeof(BlockVertex) * 4;
        public static unsafe int FaceIndicesSize => sizeof(uint) * 6;

        private static BlockVertex[] Transform(BlockVertex[] blocks, in Matrix4x4 transform)
        {
            var transformed = new BlockVertex[blocks.Length];

            for (int i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i];
                block.Position = Vector3.Transform(block.Position, transform);
                transformed[i] = block;
            }

            return transformed;
        }

        public static readonly BlockVertex[] FrontFace = new BlockVertex[]
        {
            // Fill in the front face vertex data.
	        new BlockVertex(-Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            new BlockVertex(-Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
            new BlockVertex(+Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
            new BlockVertex(+Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
        };

        public static readonly BlockVertex[] BackFace = new BlockVertex[]
        {
            // Fill in the back face vertex data.
            new BlockVertex(-Radius, -Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
            new BlockVertex(+Radius, -Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            new BlockVertex(+Radius, +Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
            new BlockVertex(-Radius, +Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
        };

        public static readonly BlockVertex[] LeftFace = new BlockVertex[]
        {
            // Fill in the left face vertex data.
            new BlockVertex(-Radius, -Radius, +Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f),
            new BlockVertex(-Radius, +Radius, +Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f),
            new BlockVertex(-Radius, +Radius, -Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f),
            new BlockVertex(-Radius, -Radius, -Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f),
        };

        public static readonly BlockVertex[] RightFace = new BlockVertex[]
        {
            // Fill in the right face vertex data.
            new BlockVertex(+Radius, -Radius, -Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f),
            new BlockVertex(+Radius, +Radius, -Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f),
            new BlockVertex(+Radius, +Radius, +Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f),
            new BlockVertex(+Radius, -Radius, +Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f)
        };

        public static readonly BlockVertex[] TopFace = new BlockVertex[]
        {
            // Fill in the top face vertex data.
            new BlockVertex(-Radius, +Radius, -Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            new BlockVertex(-Radius, +Radius, +Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
            new BlockVertex(+Radius, +Radius, +Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
            new BlockVertex(+Radius, +Radius, -Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
        };

        public static readonly BlockVertex[] BottomFace = new BlockVertex[]
        {
            // Fill in the bottom face vertex data.
            new BlockVertex(-Radius, -Radius, -Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
            new BlockVertex(+Radius, -Radius, -Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
            new BlockVertex(+Radius, -Radius, +Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
            new BlockVertex(-Radius, -Radius, +Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
        };
    }
}
