
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Queries
{
    public unsafe struct QuerySet
    {
        internal QuerySetHandle Handle;
        public readonly uint Length;
        public readonly QuerySetType Type;

        private Disposal<QuerySetHandle> _dispose;

        internal QuerySet(QuerySetHandle handle, uint length, QuerySetType type, Disposal<QuerySetHandle> dispose)
        {
            Handle = handle;
            Length = length;
            Type = type;
            _dispose = dispose;
        }

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
