using System;
using System.IO;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Annotations;
using Voltium.Common;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    [NativeComType(implements: typeof(IDxcIncludeHandler))]
    internal unsafe partial struct DxcIncludeHandler : IDisposable
    {
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
                *ppIncludeSource = CreateBlob(text).Ptr;
            }

            return hr;
        }


        private ComPtr<IDxcBlob> CreateBlob(ReadOnlySpan<char> data)
        {
            using ComPtr<IDxcBlobEncoding> encoding = default;
            fixed (char* pData = data)
            {
                Guard.ThrowIfFailed(_utils.Ptr->CreateBlob(pData, (uint)(data.Length * sizeof(char)), ShaderManager.DXC_CP_UTF16, ComPtr.GetAddressOf(&encoding)));
            }
            return ComPtr.UpCast<IDxcBlobEncoding, IDxcBlob>(encoding.Move());
        }

        private static ref DxcIncludeHandler AsThis(IDxcIncludeHandler* pThis) => ref Unsafe.As<IDxcIncludeHandler, DxcIncludeHandler>(ref *pThis);


        public void Dispose()
        {
            _utils.Dispose();
        }
    }

    //internal unsafe static class IncludeHandlerExtensions
    //{
    //    public static ref IDxcIncludeHandler GetPinnableReference(ref this DxcIncludeHandler handler)
    //        => ref Unsafe.As<nuint, IDxcIncludeHandler>(ref Unsafe.AsRef(handler.Vtbl));


    //    public static ref ID3DInclude GetPinnableReference(ref this LegacyFxcIncludeHandler handler)
    //        => ref Unsafe.As<nuint, ID3DInclude>(ref Unsafe.AsRef(handler.Vtbl));
    //}
}
