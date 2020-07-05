using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

internal class DllResolver
{
    static unsafe DllResolver()
    {
        NativeLibrary.SetDllImportResolver(Assembly.Load("TerraFX.Interop.Windows"), ResolveNativeDependency);
    }

    private const string NativeDependencyFolder = "nativedependencies";

    private static IntPtr ResolveNativeDependency(string name, Assembly assembly, DllImportSearchPath? path)
    {
        if (Directory.EnumerateFiles(NativeDependencyFolder).Contains(name))
        {
            return NativeLibrary.Load(NativeDependencyFolder + name);
        }

        return IntPtr.Zero;
    }
}
