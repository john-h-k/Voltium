using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using RaiiSharp.Annotations;

namespace Voltium.Common
{
    /// <summary>
    /// A wrapper struct that encapsulates a pointer to an unmanaged COM object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RaiiOwnershipType]
    public unsafe struct ComPtr<T> : IDisposable, IEquatable<ComPtr<T>> /*, IComType*/ where T : unmanaged
    {
        static ComPtr()
        {
            ComPtr<IUnknown> p;
            Debug.Assert(ComPtr.GetAddressOf(&p) == &p._ptr);

            // *probably* not a valid COM type without a GUID
            Debug.Assert(typeof(T).GetCustomAttribute(typeof(GuidAttribute)) != null);
            CachedGuidHandle = GCHandle.Alloc(CachedGuid, GCHandleType.Pinned);
            PointerToCachedGuid = (Guid*)CachedGuidHandle.AddrOfPinnedObject();
        }

        /// <summary>
        /// Creates a new <see cref="ComPtr{T}"/>, by reference counting <paramref name="ptr"/> so
        /// both the returned <see cref="ComPtr{T}"/> and <paramref name="ptr"/> can be used.
        /// Both must be appropriately disposed of by the caller
        /// </summary>
        /// <param name="ptr">The pointer to create the <see cref="ComPtr{T}"/> from</param>
        /// <returns>A new <see cref="ComPtr{T}"/> with the same underlying value as <paramref name="ptr"/></returns>
        [RaiiCopiesRawPointer]
        public static ComPtr<T> CopyFromPointer(T* ptr) => new ComPtr<T>(ptr).Copy();

        /// <summary>
        /// Whether the current instance is not null
        /// </summary>
        public bool Exists => this != null;

        /// <summary>
        /// Create a new ComPtr from an unmanaged pointer
        /// </summary>
        /// <param name="ptr"></param>
        [RaiiTakesOwnershipOfRawPointer]
        public ComPtr(T* ptr) => _ptr = ptr;

        // DO NOT ADD ANY OTHER MEMBERS!!!
        // both for perf
        // and because ComPtr.GetAddressOf expects this to be first elem
        private T* _ptr;

        /// <summary>
        /// The GUID of the underlying COM type
        /// </summary>
        // https://github.com/dotnet/runtime/issues/36272
        public readonly Guid* Guid => PointerToCachedGuid;

        private static readonly Guid CachedGuid = typeof(T).GUID;

        // ReSharper disable twice StaticMemberInGenericType
        private static readonly Guid* PointerToCachedGuid;
        private static readonly GCHandle CachedGuidHandle;

        /// <summary>
        /// Retrieves the underlying pointer
        /// </summary>
        /// <returns></returns>
        public readonly T* Get() => _ptr;

        /// <summary>
        /// Implicit conversion between a T* and a <see cref="ComPtr{T}"/>
        /// </summary>
        /// <param name="ptr">The COM object pointer</param>
        /// <returns></returns>
        [RaiiTakesOwnershipOfRawPointer]
        public static implicit operator ComPtr<T>(T* ptr) => new ComPtr<T>(ptr);

        /// <summary>
        /// Releases the underlying pointer of the <see cref="ComPtr{T}"/> if necessary
        /// THis function can be recalled safely
        /// </summary>
        public void Dispose()
        {
            var p = (IUnknown*)_ptr;

            if (p != null)
            {
                p->Release();
            }

            _ptr = null;
        }

        private readonly void AddRef()
        {
            var p = (IUnknown*)_ptr;

            if (p != null)
            {
                p->AddRef();
            }
        }

        /// <summary>
        /// Try and cast <typeparamref name="T"/> to <typeparamref name="TInterface"/>
        /// </summary>
        /// <param name="result">A <see cref="ComPtr{T}"/> that encapsulates the casted pointer, if succeeded</param>
        /// <typeparam name="TInterface">The type to cast to</typeparam>
        /// <returns>An HRESULT representing the cast operation</returns>
        [RaiiCopy]
        public readonly int As<TInterface>(out ComPtr<TInterface> result) where TInterface : unmanaged
        {
            var p = (IUnknown*)_ptr;
            TInterface* pResult;

            int hr = p->QueryInterface(Guid, (void**)&pResult);
            result = pResult;
            return hr;
        }

        /// <summary>
        /// Try and cast <typeparamref name="T"/> to <typeparamref name="TInterface"/>
        /// </summary>
        /// <param name="result">A <see cref="ComPtr{T}"/> that encapsulates the casted pointer, if succeeded</param>
        /// <typeparam name="TInterface">The type to cast to</typeparam>
        /// <returns><code>true</code> if the cast succeeded, else <code>false</code></returns>
        [RaiiCopy]
        public readonly bool TryQueryInterface<TInterface>(out ComPtr<TInterface> result) where TInterface : unmanaged
            => Windows.SUCCEEDED(As(out result));

        /// <summary>
        /// Copies the <see cref="ComPtr{T}"/>, and adds a reference
        /// </summary>
        /// <returns>A new <see cref="ComPtr{T}"/></returns>
        [RaiiCopy]
        public readonly ComPtr<T> Copy()
        {
            AddRef();
            return this;
        }

        /// <summary>
        /// Copies the <see cref="ComPtr{T}"/>, and does not add a reference
        /// </summary>
        /// <returns>A new <see cref="ComPtr{T}"/></returns>
        [RaiiMove]
        public ComPtr<T> Move()
        {
            ComPtr<T> copy = this;
            _ptr = null;
            return copy;
        }

        /// <summary>
        /// Compares if 2 <see cref="ComPtr{T}"/> point to the same instance
        /// </summary>
        /// <param name="left">The left pointer to compare</param>
        /// <param name="right">The right pointer to compare</param>
        /// <returns><code>true</code> if they point to the same instance, else <code>false</code></returns>
        public static bool operator ==(ComPtr<T> left, ComPtr<T> right) => left._ptr == right._ptr;

        /// <summary>
        /// Compares if 2 <see cref="ComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="left">The left pointer to compare</param>
        /// <param name="right">The right pointer to compare</param>
        /// <returns><code>true</code> if they point to different instances, else <code>false</code></returns>
        public static bool operator !=(ComPtr<T> left, ComPtr<T> right) => !(left == right);

        /// <summary>
        /// Compares if 2 <see cref="ComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="other">The pointer to compare to</param>
        /// <returns><code>true</code> if they point to the same instances, else <code>false</code></returns>
        public bool Equals(ComPtr<T> other) => _ptr == other._ptr;

        /// <summary>
        /// Compares if 2 <see cref="ComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="obj">The other object to compare</param>
        /// <returns><code>true</code> if they are <see cref="ComPtr{T}"/> which point to different instances, else <code>false</code></returns>
        public override bool Equals(object? obj) => obj is ComPtr<T> other && Equals(other);

        /// <inheritdoc cref="object.GetHashCode"/>
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => (int)_ptr;
    }

    /// <summary>
    /// Defines a set of utility methods on <see cref="ComPtr{T}"/>
    /// </summary>
    public static unsafe class ComPtr
    {
        /// <summary>
        /// Returns the address of the underlying pointer in the <see cref="ComPtr{T}"/>.
        /// </summary>
        /// <param name="comPtr">A pointer to the encapsulated pointer to take the address of</param>
        /// <typeparam name="T">The type of the underlying pointer</typeparam>
        /// <returns>A pointer to the underlying pointer</returns>
        public static T** GetAddressOf<T>(ComPtr<T>* comPtr) where T : unmanaged =>
            (T**)comPtr;

        /// <summary>
        /// Returns the address of the underlying pointer in the <see cref="ComPtr{T}"/>.
        /// This operation is only defined if <paramref name="comPtr"/> is pinned in memory for duration between the
        /// invocation of this method and the final use of the return
        /// </summary>
        /// <param name="comPtr">A pointer to the encapsulated pointer to take the address of</param>
        /// <typeparam name="T">The type of the underlying pointer</typeparam>
        /// <returns>A pointer to the underlying pointer</returns>
        public static void** GetVoidAddressOf<T>(ComPtr<T>* comPtr) where T : unmanaged =>
            (void**)comPtr;

        /// <summary>
        /// Casts a <see cref="ComPtr{T}"/> to a <see cref="ComPtr{TUp}"/> without dynamic type checking
        /// </summary>
        /// <param name="comPtr">The pointer to cast</param>
        /// <typeparam name="T">The original type of the pointer</typeparam>
        /// <typeparam name="TUp">The desired type of the pointer</typeparam>
        /// <returns>The casted pointer</returns>
        [RaiiMove]
        public static ComPtr<TUp> UpCast<T, TUp>(ComPtr<T> comPtr)
            where T : unmanaged
            where TUp : unmanaged
        {
            // if this is hit, your cast is invalid. either use TryCast or have a valid type
#if DEBUG
            Debug.Assert(comPtr.TryQueryInterface(out ComPtr<TUp> assertion));
            assertion.Dispose();
#endif

            return (TUp*)comPtr.Get();
        }
    }
}
