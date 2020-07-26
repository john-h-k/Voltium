using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Annotations;
using Voltium.Common;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    [NativeComType]
    internal unsafe partial struct LegacyFxcIncludeHandler : IDisposable
    {
        private void** Vtbl;

        [NativeComMethod]
        public int Close(void* data)
        {
            Helpers.Free(data);

            return S_OK;
        }

        [NativeComMethod]
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

        public void Dispose()
        {

        }
    }

    [NativeComType]
    internal unsafe partial struct DxcIncludeHandler : IDisposable
    {
        private string _time;
        public nuint Vtbl;

        private ComPtr<IDxcUtils> _utils;
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

        public void Init(ComPtr<IDxcUtils> utils)
        {
            Init();

            AppDirectory = Directory.GetCurrentDirectory();
            _utils = utils.Move();
        }

        [NativeComMethod]
        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            return E_NOINTERFACE;
        }

        [NativeComMethod]
        public uint AddRef()
        {
            return default;
        }

        [NativeComMethod]
        public uint Release()
        {
            return default;
        }

        [NativeComMethod]
        private int LoadSource(ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            var filename = new string((char*)pFilename);
            var hr = _handler.LoadSource(filename, out string text);

            if (SUCCEEDED(hr))
            {
                *ppIncludeSource = CreateBlob(text).Get();
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

    internal unsafe static class IncludeHandlerExtensions
    {
        public static ref IDxcIncludeHandler GetPinnableReference(ref this DxcIncludeHandler handler)
            => ref Unsafe.As<nuint, IDxcIncludeHandler>(ref handler.Vtbl);


        public static ref ID3DInclude GetPinnableReference(ref this LegacyFxcIncludeHandler handler)
            => ref Unsafe.As<LegacyFxcIncludeHandler, ID3DInclude>(ref handler);
    }
}
