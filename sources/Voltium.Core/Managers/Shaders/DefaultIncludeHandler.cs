using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Managers
{
    // LayoutKind.Sequential is ignored when the type contains references
    // This type is safe because we pin it during its time in native code,
    // and the native code never accesses the references
    // but we must make it explicit layout so it works
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct IncludeHandler : IDisposable
    {
        [FieldOffset(0)]
        public IDxcIncludeHandler.Vtbl* Vtbl;
        [FieldOffset(8)]
        public string AppDirContext;
        [FieldOffset(16)]
        public string ShaderDirContext;
        [FieldOffset(24)]
        private ComPtr<IDxcUtils> _utils;


        [FieldOffset(32)]
        private IDxcIncludeHandler._AddRef _addRef;
        [FieldOffset(40)]
        private IDxcIncludeHandler._QueryInterface _queryInterface;
        [FieldOffset(48)]
        private IDxcIncludeHandler._LoadSource _loadSource;
        [FieldOffset(56)]
        private IDxcIncludeHandler._Release _release;

        public void Init(ComPtr<IDxcUtils> utils)
        {
            Vtbl = (IDxcIncludeHandler.Vtbl*)Marshal.AllocHGlobal(sizeof(IDxcIncludeHandler.Vtbl));

            _addRef = _AddRef;
            _queryInterface = _QueryInterface;
            _loadSource = _LoadSource;
            _release = _Release;

            Vtbl->AddRef = Marshal.GetFunctionPointerForDelegate(_addRef);
            Vtbl->QueryInterface = Marshal.GetFunctionPointerForDelegate(_queryInterface);
            Vtbl->Release = Marshal.GetFunctionPointerForDelegate(_release);
            Vtbl->LoadSource = Marshal.GetFunctionPointerForDelegate(_loadSource);

            AppDirContext = Directory.GetCurrentDirectory();
            _utils = utils.Move();
        }

        public void SetShaderDirContext(string dir) => ShaderDirContext = dir;

        private static int _LoadSource(IDxcIncludeHandler* pThis, ushort* pFilename, IDxcBlob** ppIncludeSource)
            => AsThis(pThis).LoadSource(pFilename, ppIncludeSource);

        public int LoadSource(ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            if (pFilename == null || ppIncludeSource == null)
            {
                return Windows.E_POINTER;
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
                    return Windows.ERROR_FILE_NOT_FOUND;
                }

                var includeText = File.ReadAllText(file.FullName);
                IDxcBlob* blob = CreateBlob(includeText).Get();

                *ppIncludeSource = blob;
                return Windows.S_OK;
            }
            catch
            {
                return Windows.E_FAIL;
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

        private static uint _AddRef(IDxcIncludeHandler* pThis)
            => AsThis(pThis).AddRef();

        public uint AddRef()
        {
            return default;
        }

        private static int _QueryInterface(IDxcIncludeHandler* pThis, Guid* riid, void** ppvObject)
            => AsThis(pThis).QueryInterface(riid, ppvObject);

        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            ThrowHelper.ThrowNotSupportedException();
            return default;
        }

        public static uint _Release(IDxcIncludeHandler* pThis)
            => AsThis(pThis).Release();

        public uint Release()
        {
            return default;
        }

        public void Dispose()
        {
            _utils.Dispose();
        }
    }
}
