using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.VARIANT;

namespace Voltium.Common
{
    internal static class VariantHelper
    {
        public static unsafe string ToString(in VARIANT variant)
        {
            return (VARENUM)variant.vt switch
            {
                VARENUM.VT_EMPTY => string.Empty,
                VARENUM.VT_NULL => "null",
                VARENUM.VT_I2 => variant.iVal.ToString(),
                VARENUM.VT_I4 => variant.intVal.ToString(),
                VARENUM.VT_R4 => variant.fltVal.ToString(),
                VARENUM.VT_R8 => variant.dblVal.ToString(),
                VARENUM.VT_CY => throw new NotImplementedException(),
                VARENUM.VT_DATE => throw new NotImplementedException(),
                VARENUM.VT_BSTR => throw new NotImplementedException(),
                VARENUM.VT_DISPATCH => throw new NotImplementedException(),
                VARENUM.VT_ERROR => throw new NotImplementedException(),
                VARENUM.VT_BOOL => throw new NotImplementedException(),
                VARENUM.VT_VARIANT => throw new NotImplementedException(),
                VARENUM.VT_UNKNOWN => throw new NotImplementedException(),
                VARENUM.VT_DECIMAL => throw new NotImplementedException(),
                VARENUM.VT_I1 => variant.cVal.ToString(),
                VARENUM.VT_UI1 => variant.bVal.ToString(),
                VARENUM.VT_UI2 => variant.uiVal.ToString(),
                VARENUM.VT_UI4 => variant.uintVal.ToString(),
                VARENUM.VT_I8 => variant.llVal.ToString(),
                VARENUM.VT_UI8 => variant.ullVal.ToString(),
                VARENUM.VT_INT => variant.intVal.ToString(),
                VARENUM.VT_UINT => variant.uintVal.ToString(),
                VARENUM.VT_VOID => ((IntPtr)variant.byref).ToString(),
                VARENUM.VT_HRESULT => throw new NotImplementedException(),
                VARENUM.VT_PTR => ((IntPtr)variant.byref).ToString(),
                VARENUM.VT_SAFEARRAY => throw new NotImplementedException(),
                VARENUM.VT_CARRAY => throw new NotImplementedException(),
                VARENUM.VT_USERDEFINED => throw new NotImplementedException(),
                VARENUM.VT_LPSTR => new string(variant.pcVal),
                VARENUM.VT_LPWSTR => new string((char*)variant.puiVal),
                VARENUM.VT_RECORD => throw new NotImplementedException(),
                VARENUM.VT_INT_PTR => throw new NotImplementedException(),
                VARENUM.VT_UINT_PTR => throw new NotImplementedException(),
                VARENUM.VT_FILETIME => throw new NotImplementedException(),
                VARENUM.VT_BLOB => throw new NotImplementedException(),
                VARENUM.VT_STREAM => throw new NotImplementedException(),
                VARENUM.VT_STORAGE => throw new NotImplementedException(),
                VARENUM.VT_STREAMED_OBJECT => throw new NotImplementedException(),
                VARENUM.VT_STORED_OBJECT => throw new NotImplementedException(),
                VARENUM.VT_BLOB_OBJECT => throw new NotImplementedException(),
                VARENUM.VT_CF => throw new NotImplementedException(),
                VARENUM.VT_CLSID => throw new NotImplementedException(),
                VARENUM.VT_VERSIONED_STREAM => throw new NotImplementedException(),
                VARENUM.VT_BSTR_BLOB => throw new NotImplementedException(),
                VARENUM.VT_VECTOR => throw new NotImplementedException(),
                VARENUM.VT_ARRAY => throw new NotImplementedException(),
                VARENUM.VT_BYREF => throw new NotImplementedException(),
                VARENUM.VT_RESERVED => throw new NotImplementedException(),
                VARENUM.VT_ILLEGAL => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
        }
    }
}
