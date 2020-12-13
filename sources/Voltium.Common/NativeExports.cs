using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Collections.Extensions;

namespace Voltium.Common
{
    internal sealed class NativeExports : IDisposable
    {
        private struct Export : IEquatable<Export>
        {
            public IntPtr Library;
            public string Name;

            public bool Equals([AllowNull] Export other) => Name == other.Name && Library == other.Library;
            public override int GetHashCode() => HashCode.Combine(Library, Name);
        }

        private static readonly DictionarySlim<Export, IntPtr> _cache = new();

        private IntPtr _library;
        private int _dispose;

        public NativeExports(IntPtr library, bool dispose = true)
        {
            _library = library;
            _dispose = dispose ? 1 : 0;
        }


        public NativeExports(string libraryName) : this(NativeLibrary.Load(libraryName), true)
        {
        }

        public IntPtr this[string name]
        {
            get
            {
                var export = new Export { Library = _library, Name = name };
                if (_cache.TryGetValue(export, out var pointer))
                {
                    return pointer;
                }
                pointer = NativeLibrary.GetExport(_library, name);
                _cache.GetOrAddValueRef(export) = pointer;
                return pointer;
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _dispose, 0, 1) == 0)
            {
                return;
            }

            NativeLibrary.Free(_library);
        }
    }
}
