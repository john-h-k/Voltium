using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal struct SparseList<T>
    {
        private List<int> _indices;
        private List<T> _array;

        public ref T this[int index]
            => ref _array.GetRef(_indices[index]);
    }
}
