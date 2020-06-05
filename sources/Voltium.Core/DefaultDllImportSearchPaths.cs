using System.Runtime.InteropServices;
using TerraFX.Interop;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]

internal class DllResolver
{
    static unsafe DllResolver()
    {
        fixed (char* pDir = "nativedependencies")
        {
            _ = Windows.AddDllDirectory((ushort*)pDir);
        }
    }
}
