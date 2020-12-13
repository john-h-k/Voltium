using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal static unsafe class AssemblyExtensions
    {
        public static Span<byte> GetBinaryResource(this Assembly assembly, string name)
        {
            var stream = (UnmanagedMemoryStream)(assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException("Invalid resource"));

            return new Span<byte>(stream.PositionPointer, (int)(stream.Length - stream.Position));
        }
    }
}
