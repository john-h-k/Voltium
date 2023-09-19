global using System.Runtime.InteropServices;

using System;
using System.Text;

namespace Interop
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class NativeTypeNameAttribute : Attribute
    {
        public NativeTypeNameAttribute(string name)
        {

        }
    }

    // namespace ObjC
    // {
    //     public struct objc_selector {}
    //     public struct id {}

    //     public struct __IOSurface {}

    //     public struct OS_dispatch_queue {}
    //     public struct OS_dispatch_data {}

    //     public static unsafe partial class Runtime
    //     {
    //         public static objc_selector* sel_registerName(string str)
    //         {
    //             fixed (byte* p = Encoding.UTF8.GetBytes(str))
    //             {
    //                 return sel_registerName((sbyte*)p);
    //             }
    //         }
    //     }
    // }
}