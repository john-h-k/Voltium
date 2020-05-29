# ComPtr<T>

`ComPtr<T>` is a smart-pointer type, used alongside `RaiiSharp` analyzer and `RaiiSharp.Annotations`, to get explicit RAII and resource management semantics in C#. During the design of `ComPtr<T>`, there were a few key goals:

* Be low-overhead
* Provide a similar API to its native counterpart, `Microsoft::WRL::ComPtr`. ATL also has a `CComPtr`, and winrt has `com_ptr`, but they are less used
* Have the same layout in memory as a raw pointer
* Provide a simple, yet very strict, API to enforce resource management rules
* Minimise COM-related bugs

In the end, a mixed approach alongside an analyzer was considered best.

Basic understanding of how COM types work is essential to understanding `ComPtr<T>`. They work similar to C# objecst in some ways, in that they all share a base interface, which is called
`IUnknown`, and defines the following methods (C#-ified for simplicity). COM objects work in terms of interfaces, rather than base classes

```cs
void AddRef();
void Release();
int QueryInterface(Guid* iid, void** ppObject)
```

Internally, all COM objects have a reference counter, which begins at 1 when the object is created, and when it reaches 0, the object is destroyed. `Release` functions to decrement this value,
indicating the caller is no longer using its COM object. Not appropriately releasing a COM object will result in a resource leak. `AddRef` functions to increment this value, indicating the caller has made a copy, e.g to pass to a method which takes ownership of the pointer.

`QueryInterface` is used to query a type for its available interfaces, somewhat like `is` or `as` in C#, except not only for child types. It returns an `int`, where `0` (`S_OK`) means success,
`-2147467262/0x80004002` (`E_NOINTERFACE`) means it could not provide the requested interface, and `-2147467261/0x80004003` (`E_POINTER`) indicates `ppObject` was `null`. If it succeded, it will write the pointer to the requested interface to `ppObject`. Types in COM are represented by GUIDs, which are 128 bit unique identifies, anotherwise known as UUIDs. C++ provides `__uuidof()` to find the GUID of a type, and C# has `typeof(T).GUID`, although this is a slow property access so caching is essential. `ppObject` is a pointer to the pointer which will receive the value of the requested interface, and so the pointer pointed to by `ppObject` should be of the type that the GUID represents.

Here is an example of using `QueryInterface` (without smart pointers) to see if an `IUnknown` is an `ID3D12Object`, in C++, then C#

```cpp
IUnknown* unknown = ...;

ID3D12Object* pObject;

if (!unknown->QueryInterface(&__uuidof(*pObject), (void**)&pObject)) // if 0 returned
{
    pObject->SetName("pObject"); // for example
}
// or, using the macro in combaseapi.h, to handle the __uuidof and the void casting. Note this
// is a slightly strange macro in that it provides 2 params
if (!unknown->QueryInterface(IID_PPV_ARGS(&pObject)))
{
    assert(pObject);
    pObject->SetName("pObject"); // for example
}
```

```cs
IUnknown* unknown = ...;

ID3D12Object* pObject;

// Address of rvalues is only an MSVC extension in C++, and not allowed in C#, so we get a local
Guid iid = typeof(ID3D12Object).GUID;
// alternatively, you can use a library which contains all the GUID values, which is quicker
Guid iid = D3D12.IID_ID3D12Object;

if (unknown->QueryInterface(&iid, (void**)&pObject) == 0) // if 0 returned
{
    Debug.Assert(pObject != null);
    pObject->SetName("pObject"); // for example
}
// no macros in C# :(
```

Note: `QueryInterface` writes out a value which has a ref count incremented. You don't need to call `AddRef` on the new object

The declaration of `ComPtr<T>` is this:

```cs
[RaiiOwnershipType]
public unsafe struct ComPtr<T> : IDisposable, IEquatable<ComPtr<T>> where T : unmanaged
```

We make it a struct for performance purposes, and to keep layout identical to the underlying pointer. `IDisposable` is necessary for use in `using` statement, although
we would prefer if structural typing for these, as boxing a `ComPtr<T>` to `IDisposable` is almost always a semantic error. However, it does nicely allow for generic constraints/

Having lists or dictionaries of `ComPtr<T>`s is reasonably common, so providing `IEquatable<T>` helps accelerate performance here. ***IMPORTANT:*** `ComPtr<T>` is an immutable type generally,
except when `Dispose`, `Release`, or `Move` is called. These release ownership of the type, and so set the internal value to `null`. Once these are called, you no longer have access to the underlying pointer, so can't compare it or check if it is present in a dictionary (as the hashcode will always be `0`).

The `unmanaged` constraint is necessary to allow `T*`s.

For a list of the `RaiiSharp` annotations used in `ComPtr<T>`, refer to the [RaiiSharp](RaiiSharp.md) doc page. A basic understanding of these is expected for the rest of this document.

The standard constructor, which accepts a `T*`, and the implicit conversion from `T*` to `ComPtr<T>`, are `[RaiiTakesOwnershipOfRawPointer]` methods. These are used less often than you'd expect, as most DX APIs provide COM objects as out values rather than returns.

`Copy` is the standard `[RaiiCopy]` method. Its implementation is quite simple:

```cs
AddRef();
return this;
```

Where `AddRef` simply adds a reference if the underlying pointer is not null.

`Move` is the standard `[RaiiMove]` method. Again, it has a simple implementation:

```cs
var copy = this;
_ptr = null;
return copy;
```

It simply copies the value, sets the current instance to null, and returns it. No reference incrementing/decrementing occurs, as we
lose our reference, the caller gains a reference, so the net change is 0.

`As<TInterface>` and `TryQueryInterface<TInterface>`, the managed versions of `QueryInterface`, are also `[RaiiCopy]` methods.
`As` provides the closest mapping to `QueryInterface,` and shares naming with the native `ComPtr`, whereas `TryQueryInterface` provides a normal
C# style bool-try-out API. As `QueryInterface` returns a new interface with an incremented ref value, they copy.

TODO