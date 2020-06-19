using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.RenderEngine
{
     struct RenderObject
    {
        public readonly Buffer Vertices;
        public readonly Buffer Indices;
    }
}
