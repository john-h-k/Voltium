//using System;
//using System.Numerics;

//namespace Voltium.CubeGame
//{
//    internal static class FaceHelper
//    {
//        public const float Radius = 0.5f;

//        public static readonly uint[] FaceIndices = new uint[6]
//        {
//            0, 1, 2,
//            0, 2, 3,
//        };

//        public static readonly BlockVertex[] Face = new BlockVertex[]
//        {
//            new BlockVertex(-Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
//            new BlockVertex(-Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
//            new BlockVertex(+Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
//            new BlockVertex(+Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
//        };

//        private static BlockVertex[] Transform(BlockVertex[] blocks, Matrix4x4 transform)
//        {
//            var transformed = new BlockVertex[blocks.Length];

//            for (int i = 0; i < blocks.Length; i++)
//            {
//                var block = blocks[i];
//                block.Position = Vector3.Transform(block.Position, transform);
//                transformed[i] = block;
//            }

//            return transformed;
//        }

//        static FaceHelper()
//        {
//            FrontFace = Transform(Face, Matrix4x4.Identity);
//            BackFace = Transform(FrontFace, Matrix4x4.CreateRotationX(MathF.PI));

//            LeftFace = Transform(FrontFace, Matrix4x4.CreateRotationX(MathF.PI / 2));
//            RightFace = Transform(LeftFace, Matrix4x4.CreateRotationX(MathF.PI));


//            TopFace = Transform(FrontFace, Matrix4x4.CreateRotationZ(MathF.PI / 2));
//            BottomFace = Transform(TopFace, Matrix4x4.CreateRotationX(MathF.PI));
//        }

//        public static readonly BlockVertex[] FrontFace;
//        public static readonly BlockVertex[] BackFace;
//        public static readonly BlockVertex[] LeftFace;
//        public static readonly BlockVertex[] RightFace;
//        public static readonly BlockVertex[] TopFace;
//        public static readonly BlockVertex[] BottomFace;
//    }
//}
