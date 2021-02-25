using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates a type internally contains a D3D12 object. This interface is for internal consumption and debugging tools only
    /// </summary>
    public interface IInternalGraphicsObject<T>
    {
        internal unsafe TypedHandle<T> GetPointer();
    }

    internal static class IInternalGraphicsObjectExtensions
    {
        public static TypedHandle<T> GetPointer<T>(this T val) where T : IInternalGraphicsObject<T> => val.GetPointer();
    }


    [GenerateEquality]
    internal unsafe readonly struct TypedHandle<T>
    {
        internal readonly ulong Handle;
        public TypedHandle(ulong handle) => Handle = handle;

        public static implicit operator TypedHandle<T>(void* p) => (ulong)p;
        public static implicit operator TypedHandle<T>(ulong p) =>  new (p);
    }
}
