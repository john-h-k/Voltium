//namespace Voltium.Core.ShaderLang
//{
//    public abstract partial class Shader
//    {
//        [NameRemap("vector<{0}, 2>")] protected struct Vector2<T>
//        {
//            public float X { get => throw null!; set => throw null!; }
//            public float Y { get => throw null!; set => throw null!; }

//            public static Vector2<T> operator /(Vector2<T> left, Vector2<T> right) => throw null!;

//            public Vector2(T x, T y) => throw null!;
//            public Vector2(T xy) => throw null!;
//        }
//        [NameRemap("vector<{0}, 3>")] protected struct Vector3<T>
//        {
//            public float X { get => throw null!; set => throw null!; }
//            public float Y { get => throw null!; set => throw null!; }
//            public float Z { get => throw null!; set => throw null!; }

//            public Vector2<T> XY => throw null!;

//            public Vector3(T x, T y, T z) => throw null!;
//            public Vector3(T xyz) => throw null!;
//            public Vector3(Vector2<T> xy, T z) => throw null!;

//            public static explicit operator Vector3<float>(Vector3<T> vec) => throw null!;
//            public static explicit operator Vector2<float>(Vector3<T> vec) => throw null!;
//        }
//        [NameRemap("vector<{0}, 4>")] protected struct Vector4<T>
//        {
//            public float X { get => throw null!; set => throw null!; }
//            public float Y { get => throw null!; set => throw null!; }
//            public float Z { get => throw null!; set => throw null!; }
//            public float W { get => throw null!; set => throw null!; }

//            public Vector2<float> XY => throw null!;


//            public Vector4(T x, T y, T z, T w) => throw null!;
//            public Vector4(T xyzw) => throw null!;
//            public Vector4(Vector2<T> xy, T z, T w) => throw null!;
//            public Vector4(Vector2<T> xy, Vector2<T> zw) => throw null!;
//            public Vector4(T x, T y, Vector2<T> zw) => throw null!;
//            public Vector4(Vector3<T> xyz, T w) => throw null!;
//        }

//        [NameRemap("matrix<{0}, 4, 4>")] protected struct Matrix4x4<T> { }
//        [NameRemap("matrix<{0}, 4, 3>")] protected struct Matrix4x3<T> { }
//        [NameRemap("matrix<{0}, 3, 4>")] protected struct Matrix3x4<T> { }
//        [NameRemap("matrix<{0}, 3, 3>")] protected struct Matrix3x3<T> { }
//        [NameRemap("matrix<{0}, 3, 2>")] protected struct Matrix3x2<T> { }

//        public static class MathF
//        {
//            public const float E = 2.71828175F;
//            public const float PI = 3.14159274F;
//            public const float Tau = 6.28318548F;

//            [NameRemap] public static float Lerp(float x, float y, float z) => throw null!;

//            [NameRemap] public static float Abs(float x) => throw null!;
//            [NameRemap] public static float Acos(float x) => throw null!;
//            //public static float Acosh(float x) => throw null!;
//            [NameRemap] public static float Asin(float x) => throw null!;
//            //public static float Asinh(float x) => throw null!;    
//            [NameRemap] public static float Atan(float x) => throw null!;
//            [NameRemap] public static float Atan2(float y, float x) => throw null!;
//            //public static float Atanh(float x) => throw null!;
//            //public static float BitDecrement(float x) => throw null!;
//            //public static float BitIncrement(float x) => throw null!;
//            //public static float Cbrt(float x) => throw null!;
//            [NameRemap("ceil")] public static float Ceiling(float x) => throw null!;
//            //public static float CopySign(float x, float y) => throw null!;
//            [NameRemap] public static float Cos(float x) => throw null!;
//            [NameRemap] public static float Cosh(float x) => throw null!;
//            [NameRemap] public static float Exp(float x) => throw null!;
//            [NameRemap] public static float Floor(float x) => throw null!;
//            [NameRemap("fma")] public static float FusedMultiplyAdd(float x, float y, float z) => throw null!;
//            //public static float IEEERemainder(float x, float y) => throw null!;
//            //public static int ILogB(float x) => throw null!;
//            [NameRemap] public static float Log(float x) => throw null!;
//            //public static float Log(float x, float y) => throw null!;
//            [NameRemap] public static float Log10(float x) => throw null!;
//            [NameRemap] public static float Log2(float x) => throw null!;
//            [NameRemap] public static float Max(float x, float y) => throw null!;
//            //public static float MaxMagnitude(float x, float y) => throw null!;
//            [NameRemap] public static float Min(float x, float y) => throw null!;
//            //public static float MinMagnitude(float x, float y) => throw null!;
//            [NameRemap] public static float Pow(float x, float y) => throw null!;
//            //public static float Round(float x, MidpointRounding mode) => throw null!;
//            //public static float Round(float x, int digits, MidpointRounding mode) => throw null!;
//            [NameRemap] public static float Round(float x) => throw null!;
//            //public static float Round(float x, int digits) => throw null!;
//            [NameRemap("ldexp")] public static float ScaleB(float x, int n) => throw null!;
//            [NameRemap] public static int Sign(float x) => throw null!;
//            [NameRemap] public static float Sin(float x) => throw null!;
//            [NameRemap] public static float Sinh(float x) => throw null!;
//            [NameRemap] public static float Sqrt(float x) => throw null!;
//            [NameRemap] public static float Tan(float x) => throw null!;
//            [NameRemap] public static float Tanh(float x) => throw null!;
//            [NameRemap("trunc")] public static float Truncate(float x) => throw null!;
//        }
//    }
//}
