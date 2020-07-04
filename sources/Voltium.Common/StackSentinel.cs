using System;
using System.Runtime.CompilerServices;

namespace Voltium.Common
{
    internal static unsafe class StackSentinel
    {
        public const int MaxStackallocBytes = 512;

        public static bool SafeToStackalloc<T>(int count) => SafeToStackalloc<T>((nuint)count);
        public static bool SafeToStackalloc<T>(nuint count)
            => (nuint)Unsafe.SizeOf<T>() * count <= MaxStackallocBytes;

        public static bool SafeToStackallocPointers(int count)
            => sizeof(void*) * count <= MaxStackallocBytes;

        public static bool SafeToStackallocPointers<T>(int count)
            => sizeof(void*) * count <= MaxStackallocBytes;

        public static int CanarySize<T>() => sizeof(uint) / ((sizeof(uint) - 1) + Unsafe.SizeOf<T>());

        public static void WriteCanary(uint* p) => *p = 0xDEADBEEF;

        public static void VerifyCanary(uint* p)
        {
            if (*p == 0xDEADBEEF)
            {
                return;
            }

            Environment.FailFast(StackFucked);
        }
        private const string StackFucked = "stack fucked, possible buffer overrun detected. execution immediately ended";

        public static void StackAssert(bool cond)
        {
            if (!cond)
            {
                Environment.FailFast(StackFucked);
            }
        }
    }
}
