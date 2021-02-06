namespace Voltium.Core.ShaderLang
{
    public abstract partial class Shader
    {
        public static class Math
        {
            public const double E = 2.7182818284590451;
            public const double PI = 3.1415926535897931;
            public const double Tau = 6.2831853071795862;

            [Intrinsic] public static double Lerp(double x) => throw null!;
            [Intrinsic] public static double Abs(double value) => throw null!;
            [Intrinsic] public static short Abs(short value) => throw null!;
            [Intrinsic] public static int Abs(int value) => throw null!;
            [Intrinsic] public static long Abs(long value) => throw null!;
            [Intrinsic] public static sbyte Abs(sbyte value) => throw null!;
            [Intrinsic] public static float Abs(float value) => throw null!;
            [Intrinsic] public static double Acos(double d) => throw null!;
            //public static double Acosh(double d) => throw null!;
            [Intrinsic] public static double Asin(double d) => throw null!;
            //public static double Asinh(double d) => throw null!;
            [Intrinsic] public static double Atan(double d) => throw null!;
            [Intrinsic] public static double Atan2(double y, double x) => throw null!;
            //public static double Atanh(double d) => throw null!;
            //public static long BigMul(int a, int b) => throw null!;
            //public static long BigMul(long a, long b, out long low) => throw null!;
            //public static ulong BigMul(ulong a, ulong b, out ulong low) => throw null!;
            //public static double BitDecrement(double x) => throw null!;
            //public static double BitIncrement(double x) => throw null!;
            //public static double Cbrt(double d) => throw null!;
            [Intrinsic("ceil")] public static double Ceiling(double a) => throw null!;
            [Intrinsic] public static ulong Clamp(ulong value, ulong min, ulong max) => throw null!;
            [Intrinsic] public static byte Clamp(byte value, byte min, byte max) => throw null!;
            [Intrinsic] public static double Clamp(double value, double min, double max) => throw null!;
            [Intrinsic] public static short Clamp(short value, short min, short max) => throw null!;
            [Intrinsic] public static int Clamp(int value, int min, int max) => throw null!;
            [Intrinsic] public static long Clamp(long value, long min, long max) => throw null!;
            [Intrinsic] public static sbyte Clamp(sbyte value, sbyte min, sbyte max) => throw null!;
            [Intrinsic] public static float Clamp(float value, float min, float max) => throw null!;
            [Intrinsic] public static ushort Clamp(ushort value, ushort min, ushort max) => throw null!;
            [Intrinsic] public static uint Clamp(uint value, uint min, uint max) => throw null!;
            //public static double CopySign(double x, double y) => throw null!;
            [Intrinsic] public static double Cos(double d) => throw null!;
            [Intrinsic] public static double Cosh(double value) => throw null!;
            //public static int DivRem(int a, int b, out int result) => throw null!;
            //public static long DivRem(long a, long b, out long result) => throw null!;
            [Intrinsic] public static double Exp(double d) => throw null!;
            [Intrinsic] public static double Floor(double d) => throw null!;
            [Intrinsic("fma")] public static double FusedMultiplyAdd(double x, double y, double z) => throw null!;
            //public static double IEEERemainder(double x, double y) => throw null!;
            //public static int ILogB(double x) => throw null!;
            [Intrinsic] public static double Log(double d) => throw null!;
            //public static double Log(double a, double newBase) => throw null!;
            [Intrinsic] public static double Log10(double d) => throw null!;
            [Intrinsic] public static double Log2(double x) => throw null!;
            [Intrinsic] public static uint Max(uint val1, uint val2) => throw null!;
            [Intrinsic] public static ushort Max(ushort val1, ushort val2) => throw null!;
            [Intrinsic] public static float Max(float val1, float val2) => throw null!;
            [Intrinsic] public static sbyte Max(sbyte val1, sbyte val2) => throw null!;
            [Intrinsic] public static long Max(long val1, long val2) => throw null!;
            [Intrinsic] public static ulong Max(ulong val1, ulong val2) => throw null!;
            [Intrinsic] public static short Max(short val1, short val2) => throw null!;
            [Intrinsic] public static double Max(double val1, double val2) => throw null!;
            [Intrinsic] public static byte Max(byte val1, byte val2) => throw null!;
            [Intrinsic] public static int Max(int val1, int val2) => throw null!;
            //public static double MaxMagnitude(double x, double y) => throw null!;
            [Intrinsic] public static ushort Min(ushort val1, ushort val2) => throw null!;
            [Intrinsic] public static float Min(float val1, float val2) => throw null!;
            [Intrinsic] public static sbyte Min(sbyte val1, sbyte val2) => throw null!;
            [Intrinsic] public static long Min(long val1, long val2) => throw null!;
            [Intrinsic] public static double Min(double val1, double val2) => throw null!;
            [Intrinsic] public static short Min(short val1, short val2) => throw null!;
            [Intrinsic] public static byte Min(byte val1, byte val2) => throw null!;
            [Intrinsic] public static uint Min(uint val1, uint val2) => throw null!;
            [Intrinsic] public static int Min(int val1, int val2) => throw null!;
            [Intrinsic] public static ulong Min(ulong val1, ulong val2) => throw null!;
            //public static double MinMagnitude(double x, double y) => throw null!;
            [Intrinsic] public static double Pow(double x, double y) => throw null!;
            [Intrinsic] public static double Round(double a) => throw null!;
            //public static double Round(double value, int digits) => throw null!;
            //public static double Round(double value, int digits, MidpointRounding mode) => throw null!;
            //public static double Round(double value, MidpointRounding mode) => throw null!;
            [Intrinsic("ldexp")] public static double ScaleB(double x, int n) => throw null!;
            [Intrinsic] public static int Sign(float value) => throw null!;
            [Intrinsic] public static int Sign(sbyte value) => throw null!;
            [Intrinsic] public static int Sign(long value) => throw null!;
            [Intrinsic] public static int Sign(double value) => throw null!;
            [Intrinsic] public static int Sign(short value) => throw null!;
            [Intrinsic] public static int Sign(int value) => throw null!;
            [Intrinsic] public static double Sin(double a) => throw null!;
            [Intrinsic] public static double Sinh(double value) => throw null!;
            [Intrinsic] public static double Sqrt(double d) => throw null!;
            [Intrinsic] public static double Tan(double a) => throw null!;
            [Intrinsic] public static double Tanh(double value) => throw null!;
            [Intrinsic("trunc")] public static double Truncate(double d) => throw null!;
        }
    }
}
