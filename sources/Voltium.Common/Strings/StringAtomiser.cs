using System;
using System.Diagnostics;
using System.Threading;

namespace Voltium.Common
{

    /// <summary>
    /// The type used for creating and using <see cref="Atom"/>s, which are lightweight high performance handles for
    /// <see cref="string"/>s
    /// </summary>
    public static class StringAtomiser
    {
        // TODO should we do some basic interning here? maybe
        //private static int[] ghcRem1024 = new string[64];
        private static string[] _stringArray = new string[64];
        private static ushort _largestAtom = 0;

        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private const ushort MaxAtomValue = ushort.MaxValue;

        /// <summary>
        /// Retrieve a <see cref="string"/> using its associated <see cref="Atom"/>
        /// </summary>
        /// <param name="atom">The atom, from <see cref="CreateAtom(string)"/>, which represents the string</param>
        /// <returns>The <see cref="string"/> for the <see cref="Atom"/></returns>
        public static string Retrieve(Atom atom)
        {
            Debug.Assert(atom.RawValue < _stringArray.Length);

            string val;

            // because uint16 read is atomic we don't actually need to enter the read lock here 🎉
            {
                val = _stringArray[atom.RawValue];
            }

            return val;
        }

        /// <summary>
        /// Creates a new <see cref="Atom"/> for a given <see cref="string"/>
        /// </summary>
        /// <param name="str">The <see cref="string"/> to create the <see cref="Atom"/> for</param>
        /// <returns>A new <see cref="Atom"/> which can be used to retrieve the <see cref="string"/></returns>
        public static Atom CreateAtom(string str)
        {
            ResizeIfNecessary();

            Atom val;

            _lock.EnterWriteLock();
            {
                val = new Atom(++_largestAtom);
                _stringArray[_largestAtom] = str;
            }
            _lock.ExitWriteLock();

            return val;
        }

        private static void ResizeIfNecessary()
        {
            if (_stringArray.Length - 1 == _largestAtom)
            {
                if (_largestAtom == MaxAtomValue)
                {
                    ThrowHelper.ThrowInsufficientMemoryException($"Only {ushort.MaxValue} atoms are allowed at once");
                }

                _lock.EnterWriteLock();
                {
                    // check if another thread has done the hard work for us while we entered lock
                    if (_stringArray.Length - 1 == _largestAtom)
                    {
                        var newArray = new string[_stringArray.Length * 2];
                        _stringArray.AsSpan().CopyTo(newArray);
                        _stringArray = newArray;
                    }
                }
                _lock.ExitWriteLock();
            }
        }
    }
}
