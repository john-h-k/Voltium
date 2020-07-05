namespace Voltium.Common
{
    /// <summary>
    /// An opaque handle type used alongside <see cref="StringAtomiser"/> to represent
    /// a <see cref="string"/>
    /// </summary>
    public readonly struct Atom
    {
        // its literally just an array index but we make it all fancy and named and opaque
        // gives it an added mystery value yknow
        internal readonly ushort RawValue;

        internal Atom(ushort rawValue)
        {
            RawValue = rawValue;
        }
    }
}
