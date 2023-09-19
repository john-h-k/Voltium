using System;
using TerraFX.Interop;
using Voltium.Annotations;
using Voltium.Common;
using TerraFX.Interop.DirectX;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace Voltium.Core.Devices
{
    [NativeComType(implements: typeof(ID3DInclude))]
    internal unsafe partial struct LegacyFxcIncludeHandler
    {
        private IncludeHandler _handler;

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
    }

    //internal unsafe static class IncludeHandlerExtensions
    //{
    //    public static ref IDxcIncludeHandler GetPinnableReference(ref this DxcIncludeHandler handler)
    //        => ref Unsafe.As<nuint, IDxcIncludeHandler>(ref Unsafe.AsRef(handler.Vtbl));


    //    public static ref ID3DInclude GetPinnableReference(ref this LegacyFxcIncludeHandler handler)
    //        => ref Unsafe.As<nuint, ID3DInclude>(ref Unsafe.AsRef(handler.Vtbl));
    //}
}
