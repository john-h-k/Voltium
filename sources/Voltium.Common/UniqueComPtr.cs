using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RaiiSharp.Annotations;
using TerraFX.Interop;

namespace Voltium.Common
{
    /// <summary>
    /// A wrapper struct that encapsulates a pointer to an unmanaged COM object
    /// </summary>
    /// <typeparam name="T">The type of the underlying COM object</typeparam>
    [RaiiOwnershipType]
    public unsafe struct UniqueComPtr<T> : IDisposable, IEquatable<UniqueComPtr<T>> /*, IComType*/ where T : unmanaged
    {
        private static Guid* Initialize()
        {
            UniqueComPtr<IUnknown> p = default;
            Debug.Assert(ComPtr.GetAddressOf(&p) == &p._ptr);

            // *probably* not a valid COM type without a GUID
#if REFLECTION
            Debug.Assert(typeof(T).GetCustomAttribute(typeof(GuidAttribute)) != null);
#endif

            var ptr = (Guid*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(T), sizeof(Guid));
            *ptr = typeof(T).GUID;
            return ptr;
        }


        /// <summary>
        /// Whether the current instance is not null
        /// </summary>
        public bool Exists => _ptr != null;

        /// <summary>
        /// Create a new ComPtr from an unmanaged pointer
        /// </summary>
        /// <param name="ptr"></param>
        [RaiiTakesOwnershipOfRawPointer]
        public UniqueComPtr(T* ptr) => _ptr = ptr;

        // DO NOT ADD ANY OTHER MEMBERS!!!
        // both for perf
        // and because ComPtr.GetAddressOf/GetVoidAddressOf expects this to be first elem
        // i believe some other code in the engine relies on it being blittable to void* too
        private T* _ptr;

        /// <summary>
        /// The IID (Interface ID) of the underlying COM type
        /// </summary>
        // https://github.com/dotnet/runtime/issues/36272
        public readonly Guid* Iid => StaticIid;

        /// <summary>
        /// The IID (Interface ID) of the underlying COM type
        /// </summary>
        public static Guid* StaticIid { get; } = Initialize();

        /// <summary>
        /// Retrieves the underlying pointer
        /// </summary>
        public readonly T* Ptr => _ptr;

        /// <summary>
        /// Explicit conversion between a T* and a <see cref="UniqueComPtr{T}"/>
        /// </summary>
        /// <param name="ptr">The COM object pointer</param>
        /// <returns></returns>
        [RaiiTakesOwnershipOfRawPointer]
        public static explicit operator UniqueComPtr<T>(T* ptr) => new UniqueComPtr<T>(ptr);

        /// <summary>
        /// Releases the underlying pointer of the <see cref="UniqueComPtr{T}"/> if necessary.
        /// This function can be recalled safely
        /// </summary>
        public void Dispose()
        {
            var p = (IUnknown*)_ptr;

            if (p != null)
            {
                _ = p->Release();
            }

            _ptr = null;
        }

        internal uint? RefCount
        {
            get
            {
                var p = (IUnknown*)_ptr;

                if (p != null)
                {
                    _ = p->AddRef();
                    uint count = p->Release();
                    return count;
                }
                return null;
            }
        }

        internal bool IsSingleRef => RefCount == 1;

        private readonly void AddRef()
        {
            var p = (IUnknown*)_ptr;

            if (p != null)
            {
                _ = p->AddRef();
            }
        }

        /// <summary>
        /// Try and cast <typeparamref name="T"/> to <typeparamref name="TInterface"/>
        /// </summary>
        /// <param name="result">A <see cref="UniqueComPtr{T}"/> that encapsulates the casted pointer, if succeeded</param>
        /// <typeparam name="TInterface">The type to cast to</typeparam>
        /// <returns>An HRESULT representing the cast operation</returns>
        [RaiiCopy]
        public readonly int As<TInterface>(out UniqueComPtr<TInterface> result) where TInterface : unmanaged
        {
            var p = (IUnknown*)_ptr;
            TInterface* pResult;

            if (p is null)
            {
                result = default;
                return Windows.E_POINTER;
            }

            Guid* iid = UniqueComPtr<TInterface>.StaticIid;
            int hr = p->QueryInterface(iid, (void**)&pResult);
            result = new UniqueComPtr<TInterface>(pResult);
            return hr;
        }

        /// <summary>
        /// Try and cast <typeparamref name="T"/> to <typeparamref name="TInterface"/>
        /// </summary>
        /// <param name="result">A <see cref="UniqueComPtr{T}"/> that encapsulates the casted pointer, if succeeded</param>
        /// <typeparam name="TInterface">The type to cast to</typeparam>
        /// <returns><code>true</code> if the cast succeeded, else <code>false</code></returns>
        [RaiiCopy]
        public readonly bool TryQueryInterface<TInterface>(out UniqueComPtr<TInterface> result) where TInterface : unmanaged
            => Windows.SUCCEEDED(As(out result));

        /// <summary>
        /// Determine if the current pointer supports a given interface, so that <see cref="TryQueryInterface{T}(out UniqueComPtr{T})"/>
        /// will succeed
        /// </summary>
        /// <typeparam name="TInterface">The type to check for</typeparam>
        /// <returns><see langword="true"/> if the type supports <typeparamref name="TInterface"/>, else <see langword="false" /> </returns>
        public readonly bool HasInterface<TInterface>() where TInterface : unmanaged
        {
            var result = TryQueryInterface<TInterface>(out var ptr);
            ptr.Dispose();
            return result;
        }

        /// <summary>
        /// Copies the <see cref="UniqueComPtr{T}"/>, and adds a reference
        /// </summary>
        /// <returns>A new <see cref="UniqueComPtr{T}"/></returns>
        [RaiiCopy]
        public readonly UniqueComPtr<T> Copy()
        {
            AddRef();
            return this;
        }

        /// <summary>
        /// Copies the <see cref="UniqueComPtr{T}"/>, and does not add a reference
        /// </summary>
        /// <returns>A new <see cref="UniqueComPtr{T}"/></returns>
        [RaiiMove]
        public UniqueComPtr<T> Move()
        {
            UniqueComPtr<T> copy = this;
            _ptr = null;
            return copy;
        }

        /// <summary>
        /// Compares if 2 <see cref="UniqueComPtr{T}"/> point to the same instance
        /// </summary>
        /// <param name="left">The left pointer to compare</param>
        /// <param name="right">The right pointer to compare</param>
        /// <returns><code>true</code> if they point to the same instance, else <code>false</code></returns>
        public static bool operator ==(UniqueComPtr<T> left, UniqueComPtr<T> right) => left._ptr == right._ptr;

        /// <summary>
        /// Compares if 2 <see cref="UniqueComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="left">The left pointer to compare</param>
        /// <param name="right">The right pointer to compare</param>
        /// <returns><code>true</code> if they point to different instances, else <code>false</code></returns>
        public static bool operator !=(UniqueComPtr<T> left, UniqueComPtr<T> right) => !(left == right);

        /// <summary>
        /// Compares if 2 <see cref="UniqueComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="other">The pointer to compare to</param>
        /// <returns><code>true</code> if they point to the same instances, else <code>false</code></returns>
        public bool Equals(UniqueComPtr<T> other) => _ptr == other._ptr;

        /// <summary>
        /// Compares if 2 <see cref="UniqueComPtr{T}"/> point to different instances
        /// </summary>
        /// <param name="obj">The other object to compare</param>
        /// <returns><code>true</code> if they are <see cref="UniqueComPtr{T}"/> which point to different instances, else <code>false</code></returns>
        public override bool Equals(object? obj) => obj is UniqueComPtr<T> other && Equals(other);

        /// <summary>
        /// Casts a <see cref="UniqueComPtr{T}"/> to a <see cref="UniqueComPtr{T}"/> without dynamic type checking
        /// </summary>
        /// <typeparam name="TBase">The desired type of the pointer</typeparam>
        /// <returns>The casted pointer</returns>
        public UniqueComPtr<TBase> AsBase<TBase>() where TBase : unmanaged
            => ComPtr.UpCast<T, TBase>(this);

        /// <summary>
        /// Casts a <see cref="UniqueComPtr{T}"/> to a <see cref="UniqueComPtr{T}"/>
        /// </summary>
        /// <returns>The casted pointer</returns>
        public UniqueComPtr<IUnknown> AsIUnknown()
            => AsBase<IUnknown>();

        /// <inheritdoc cref="object.GetHashCode"/>
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        public override int GetHashCode() => ((ulong)_ptr).GetHashCode();
    }

    /// <summary>
    /// Defines a set of utility methods on <see cref="UniqueComPtr{T}"/>
    /// </summary>
    public unsafe static class ComPtr
    {
        /// <summary>
        /// Returns the address of the underlying pointer in the <see cref="UniqueComPtr{T}"/>.
        /// </summary>
        /// <param name="comPtr">A pointer to the encapsulated pointer to take the address of</param>
        /// <typeparam name="T">The type of the underlying pointer</typeparam>
        /// <returns>A pointer to the underlying pointer</returns>
        public static T** GetAddressOf<T>(UniqueComPtr<T>* comPtr) where T : unmanaged
        {
            comPtr->Dispose();
            return (T**)comPtr;
        }

        /// <summary>
        /// Try and cast <typeparamref name="T"/> to <typeparamref name="TInterface"/>
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="result">A <see cref="UniqueComPtr{T}"/> that encapsulates the casted pointer, if succeeded</param>
        /// <typeparam name="T">The type of the underlying COM object</typeparam>
        /// <typeparam name="TInterface">The type to cast to</typeparam>
        /// <returns><code>true</code> if the cast succeeded, else <code>false</code></returns>
        public static bool TryQueryInterface<T, TInterface>(T* ptr, out TInterface* result)
            where T : unmanaged
            where TInterface : unmanaged
        {
            var success = new UniqueComPtr<T>(ptr).TryQueryInterface<TInterface>(out var comPtr);
            result = comPtr.Ptr;
            return success;
        }

        /// <summary>
        /// Returns the address of the underlying pointer in the <see cref="UniqueComPtr{T}"/>.
        /// </summary>
        /// <param name="comPtr">A pointer to the encapsulated pointer to take the address of</param>
        /// <typeparam name="T">The type of the underlying pointer</typeparam>
        /// <returns>A pointer to the underlying pointer</returns>
        public static void** GetVoidAddressOf<T>(UniqueComPtr<T>* comPtr) where T : unmanaged
        {
            comPtr->Dispose();
            return (void**)comPtr;
        }

        /// <summary>
        /// Casts a <see cref="UniqueComPtr{T}"/> to a <see cref="UniqueComPtr{T}"/> without dynamic type checking
        /// </summary>
        /// <param name="comPtr">The pointer to cast</param>
        /// <typeparam name="T">The original type of the pointer</typeparam>
        /// <typeparam name="TUp">The desired type of the pointer</typeparam>
        /// <returns>The casted pointer</returns>
        [RaiiMove]
        public static UniqueComPtr<TUp> UpCast<T, TUp>(UniqueComPtr<T> comPtr)
            where T : unmanaged
            where TUp : unmanaged
        {
            // if this is hit, your cast is invalid. either use TryQueryInterface or, preferrably, have a valid type
#if DEBUG
            Debug.Assert(comPtr.TryQueryInterface(out UniqueComPtr<TUp> assertion));
            assertion.Dispose();
#endif

            return new UniqueComPtr<TUp>((TUp*)comPtr.Ptr);
        }


        /// <summary>
        /// Creates a new <see cref="UniqueComPtr{T}"/>, by reference counting <paramref name="ptr"/> so
        /// both the returned <see cref="UniqueComPtr{T}"/> and <paramref name="ptr"/> can be used.
        /// Both must be appropriately disposed of by the caller
        /// </summary>
        /// <param name="ptr">The pointer to create the <see cref="UniqueComPtr{T}"/> from</param>
        /// <returns>A new <see cref="UniqueComPtr{T}"/> with the same underlying value as <paramref name="ptr"/></returns>
        [RaiiCopiesRawPointer]
        public static UniqueComPtr<T> CopyFromPointer<T>(T* ptr) where T : unmanaged
            => new UniqueComPtr<T>(ptr).Copy();
    }
}
