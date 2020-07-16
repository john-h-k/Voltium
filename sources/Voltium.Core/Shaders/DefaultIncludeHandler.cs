using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct LegacyFxcIncludeHandler : IDisposable
    {
        [FieldOffset(0)]
        private void** _pVtbl;

        private static readonly IntPtr Heap;
        private static void** Vtbl;
        private static IncludeHandler _handler;

        public string AppDirectory
        {
            get => _handler.AppDirectory;
            set => _handler.AppDirectory = value;
        }

        public string ShaderDirectory
        {
            get => _handler.ShaderDirectory;
            set => _handler.ShaderDirectory = value;
        }

        static LegacyFxcIncludeHandler()
        {
            Vtbl = (void**)Helpers.Alloc((uint)sizeof(nuint) * 2);

            // these should be stdcall in the future

            // Native vtable layout
            // - Close
            // - Open

            Vtbl[0] = (delegate*<ID3DInclude*, D3D_INCLUDE_TYPE, sbyte*, void*, void**, uint*, int>)&_Open;
            Vtbl[1] = (delegate*<ID3DInclude*, void*, int>)&_Close;
        }

        public void Init()
        {
            _pVtbl = Vtbl;

            AppDirectory = Directory.GetCurrentDirectory();
        }

        [UnmanagedCallersOnly]
        private static int _Close(ID3DInclude* pThis, void* data)
            => AsThis(pThis).Close(data);


        [UnmanagedCallersOnly]
        private static int _Open(
                ID3DInclude* pThis,
                D3D_INCLUDE_TYPE IncludeType,
                sbyte* pFileName,
                void* pParentData,
                void** ppData,
                uint* pBytes
            )
            => AsThis(pThis).Open(IncludeType, pFileName, pParentData, ppData, pBytes);

        private static ref LegacyFxcIncludeHandler AsThis(ID3DInclude* ptr) => ref Unsafe.As<ID3DInclude, LegacyFxcIncludeHandler>(ref *ptr);

        public int Close(void* data)
        {
            Helpers.Free(data);

            return S_OK;
        }

        public int Open(
                D3D_INCLUDE_TYPE includeType,
                sbyte* pFileName,
                void* pParentData,
                void** ppData,
                uint* pBytes
            )
        {
            int hr = _handler.LoadSource(new string(pFileName), out byte[] text);

            if (SUCCEEDED(hr))
            {
                void* block = Helpers.Alloc(text.Length);
                text.AsSpan().CopyTo(new Span<byte>(block, text.Length));

                *ppData = block;
                *pBytes = (uint)text.Length;
            }

            return hr;
        }

        public void Dispose()
        {

        }
    }

    // LayoutKind.Sequential is ignored when the type contains references
    // This type is safe because we pin it during its time in native code,
    // and the native code never accesses the references
    // but we must make it explicit layout so it works
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct DxcIncludeHandler : IDisposable
    {
        [FieldOffset(0)]
        private void** _pVtbl;
        [FieldOffset(16)]
        private ComPtr<IDxcUtils> _utils;
        [FieldOffset(24)]
        private IncludeHandler _handler;

        public string AppDirectory
        {
            get => _handler.AppDirectory;
            set => _handler.AppDirectory = value;
        }

        public string ShaderDirectory
        {
            get => _handler.ShaderDirectory;
            set => _handler.ShaderDirectory = value;
        }

        private static readonly IntPtr Heap;
        private static void** Vtbl;

        static DxcIncludeHandler()
        {
            Heap = GetProcessHeap();
            Vtbl = (void**)Helpers.Alloc((uint)sizeof(nuint) * 4);

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

            AppDirectory = Directory.GetCurrentDirectory();
            _utils = utils.Move();
        }


        [UnmanagedCallersOnly]
        private static int _QueryInterface(IDxcIncludeHandler* pThis, Guid* riid, void** ppvObject)
            => AsThis(pThis).QueryInterface(riid, ppvObject);

        [UnmanagedCallersOnly]
        private static uint _AddRef(IDxcIncludeHandler* pThis)
            => AsThis(pThis).AddRef();

        [UnmanagedCallersOnly]
        public static uint _Release(IDxcIncludeHandler* pThis)
            => AsThis(pThis).Release();

        [UnmanagedCallersOnly]
        private static int _LoadSource(IDxcIncludeHandler* pThis, ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            var filename = new string((char*)pFilename);
            var hr = AsThis(pThis)._handler.LoadSource(filename, out string text);

            if (SUCCEEDED(hr))
            {
                *ppIncludeSource = AsThis(pThis).CreateBlob(text).Get();
            }

            return hr;
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

        private static ref DxcIncludeHandler AsThis(IDxcIncludeHandler* pThis) => ref Unsafe.As<IDxcIncludeHandler, DxcIncludeHandler>(ref *pThis);

        public uint AddRef()
        {
            return default;
        }

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return E_NOINTERFACE;
        }

        public uint Release()
        {
            return default;
        }

        public void Dispose()
        {
            _utils.Dispose();
        }
    }

    internal unsafe struct IncludeHandler
    {
        public string ShaderDirectory { get; set; }
        public string AppDirectory { get; set; }

        private string? _lastDirectory;
        
        public int LoadSource(string filename, out string text)
        {
            text = null!;

            if (filename == null || Helpers.IsNullOut(out text))
            {
                return E_POINTER;
            }

            try
            {
                if (!TryGetFile(filename, out var file))
                {
                    return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                }

                text = File.ReadAllText(file.FullName);
                
                return S_OK;
            }
            catch (Exception e)
            {
                return e is FileNotFoundException ? HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) : E_FAIL;
            }
        }

        public int LoadSource(string filename, out byte[] text)
        {
            text = null!;

            if (filename == null || Helpers.IsNullOut(out text))
            {
                return E_POINTER;
            }

            try
            {
                if (!TryGetFile(filename, out var file))
                {
                    return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                }

                text = File.ReadAllBytes(file.FullName);

                return S_OK;
            }
            catch (Exception e)
            {
                return e is FileNotFoundException ? HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) : E_FAIL;
            }
        }


        private bool TryGetFile(string filename, [NotNullWhen(true)] out FileInfo? file)
        {
            var shaderLocalPath = Path.Combine(ShaderDirectory, filename);
            var appLocalPath = Path.Combine(AppDirectory, filename);

            if (File.Exists(shaderLocalPath))
            {
                file = new FileInfo(shaderLocalPath);
            }
            else if (File.Exists(appLocalPath))
            {
                file = new FileInfo(appLocalPath);
            }
            else if (_lastDirectory is not null && Path.Combine(_lastDirectory, filename) is var lastPath && File.Exists(lastPath))
            {
                file = new FileInfo(lastPath);
            }
            else
            {
                file = default;
                return false;
            }

            _lastDirectory = file.DirectoryName;

            return true;
        }

        internal int LoadSource(ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            throw new NotImplementedException();
        }
    }

    internal static unsafe class IncludeHandlerExtensions
    {
        public static ref IDxcIncludeHandler GetPinnableReference(ref this DxcIncludeHandler handler)
            => ref Unsafe.As<DxcIncludeHandler, IDxcIncludeHandler>(ref handler);


        public static ref ID3DInclude GetPinnableReference(ref this LegacyFxcIncludeHandler handler)
            => ref Unsafe.As<LegacyFxcIncludeHandler, ID3DInclude>(ref handler);
    }
}
