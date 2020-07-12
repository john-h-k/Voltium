using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    // LayoutKind.Sequential is ignored when the type contains references
    // This type is safe because we pin it during its time in native code,
    // and the native code never accesses the references
    // but we must make it explicit layout so it works
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct IncludeHandler : IDisposable
    {
        [FieldOffset(0)]
        private void** _pVtbl;
        [FieldOffset(8)]
        public string AppDirContext;
        [FieldOffset(16)]
        public string ShaderDirContext;
        [FieldOffset(24)]
        private ComPtr<IDxcUtils> _utils;

        private static readonly IntPtr Heap;
        private static void** Vtbl;

        static IncludeHandler()
        {
            Heap = GetProcessHeap();
            Vtbl = (void**)HeapAlloc(Heap, 0, (uint)sizeof(nuint) * 4);

            // these should be stdcall in the future

            // Native vtable layout
            // - QueryInterface
            // - AddRef
            // - Release
            // - LoadSource

            Vtbl[0] = (delegate*<IDxcIncludeHandler*, Guid*, void**, int>)&_QueryInterface;
            Vtbl[1] = (delegate*<IDxcIncludeHandler*, uint>)&_AddRef;
            Vtbl[2] = (delegate*<IDxcIncludeHandler*, uint>)&_Release;
            Vtbl[3] = (delegate*<IDxcIncludeHandler*, ushort*, IDxcBlob**, int>)&_LoadSource;
        }

        public void Init(ComPtr<IDxcUtils> utils)
        {
            _pVtbl = Vtbl;

            AppDirContext = Directory.GetCurrentDirectory();
            _utils = utils.Move();
        }

        public void SetShaderDirContext(string dir) => ShaderDirContext = dir;

        [UnmanagedCallersOnly]
        private static int _LoadSource(IDxcIncludeHandler* pThis, ushort* pFilename, IDxcBlob** ppIncludeSource)
            => AsThis(pThis).LoadSource(pFilename, ppIncludeSource);

        public int LoadSource(ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            if (pFilename == null || ppIncludeSource == null)
            {
                return E_POINTER;
            }

            try
            {
                var filename = new string((char*)pFilename);
                var shaderLocalPath = Path.Combine(ShaderDirContext, filename);
                var appLocalPath = Path.Combine(AppDirContext, filename);

                FileInfo file;
                if (File.Exists(shaderLocalPath))
                {
                    file = new FileInfo(shaderLocalPath);
                }
                else if (File.Exists(appLocalPath))
                {
                    file = new FileInfo(appLocalPath);
                }
                else
                {
                    return E_FAIL;
                }

                var includeText = File.ReadAllText(file.FullName);
                IDxcBlob* blob = CreateBlob(includeText).Get();

                *ppIncludeSource = blob;
                return S_OK;
            }
            catch
            {
                return E_FAIL;
            }
        }

        private ComPtr<IDxcBlob> CreateBlob(ReadOnlySpan<char> data)
        {
            using ComPtr<IDxcBlobEncoding> encoding = default;
            fixed (char* pData = data)
            {
                Guard.ThrowIfFailed(_utils.Get()->CreateBlob(pData, (uint)(data.Length * sizeof(char)), ShaderManager.DXC_CP_UTF16, ComPtr.GetAddressOf(&encoding)));
            }
            return ComPtr.UpCast<IDxcBlobEncoding, IDxcBlob>(encoding.Move());
        }

        private static ref IncludeHandler AsThis(IDxcIncludeHandler* pThis) => ref Unsafe.As<IDxcIncludeHandler, IncludeHandler>(ref *pThis);

        [UnmanagedCallersOnly]
        private static uint _AddRef(IDxcIncludeHandler* pThis)
            => AsThis(pThis).AddRef();

        public uint AddRef()
        {
            return default;
        }

        [UnmanagedCallersOnly]
        private static int _QueryInterface(IDxcIncludeHandler* pThis, Guid* riid, void** ppvObject)
            => AsThis(pThis).QueryInterface(riid, ppvObject);

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        [UnmanagedCallersOnly]
        public static uint _Release(IDxcIncludeHandler* pThis)
            => AsThis(pThis).Release();

        public uint Release()
        {
            return default;
        }

        public void Dispose()
        {
            _utils.Dispose();
            Marshal.FreeHGlobal((IntPtr)Vtbl);
        }
    }

    internal static unsafe class DefaultIncludeHandlerExtensions
    {
        public static ref IDxcIncludeHandler GetPinnableReference(ref this IncludeHandler handler)
            => ref Unsafe.As<IncludeHandler, IDxcIncludeHandler>(ref handler);
    }
}
