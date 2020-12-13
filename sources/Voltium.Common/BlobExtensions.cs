using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static unsafe class BlobExtensions
    {
        public static ID3DBlob* AsD3DBlob(this ref IDxcBlob blob) => (ID3DBlob*)Unsafe.AsPointer(ref blob);
        public static IDxcBlob* AsDxcBlob(this ref ID3DBlob blob) => (IDxcBlob*)Unsafe.AsPointer(ref blob);

        public static ReadOnlySpan<byte> AsSpan(this ref IDxcBlob blob) => new ReadOnlySpan<byte>(blob.GetBufferPointer(), checked((int)blob.GetBufferSize()));
        public static ReadOnlySpan<T> AsSpan<T>(this ref IDxcBlob blob) where T : unmanaged => new ReadOnlySpan<T>(blob.GetBufferPointer(), checked((int)blob.GetBufferSize() / sizeof(T)));


        public static string GetString(this ref IDxcBlob blob, Encoding? encoding) => encoding is null ? blob.AsSpan<char>().ToString() : encoding.GetString(blob.AsSpan());
        public static string GetString(this ref IDxcBlob blob)
        {
            var pBlob = new UniqueComPtr<IDxcBlob>((IDxcBlob*)Unsafe.AsPointer(ref blob));
            if (pBlob.TryQueryInterface<IDxcBlobUtf8>(out var utf8))
            {
                using (utf8)
                {
                    return utf8.Ptr->GetString();
                }
            }
            else if (pBlob.TryQueryInterface<IDxcBlobUtf16>(out var utf16))
            {
                using (utf16)
                {
                    return utf16.Ptr->GetString();
                }
            }

            return blob.GetString(null);
        }

        public static string GetString(this ref IDxcBlobUtf8 blob) => new string(blob.GetStringPointer(), 0, checked((int)blob.GetStringLength()));
        public static string GetString(this ref IDxcBlobUtf16 blob) => new string((char*)blob.GetStringPointer(), 0, checked((int)blob.GetStringLength()));
    }
}
