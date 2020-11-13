using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Collections.Extensions;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static unsafe class ComIdentity
    {
        private static readonly Dictionary<nuint, object> _identity = new();

        public static void RegisterComObject<TCom, TManaged>(TCom* com, TManaged managed) where TCom : unmanaged where TManaged : class
        {
            lock (_identity)
            {
                _identity.Add((nuint)com, managed);
            }
        }

        public static bool TryGetManagedObject<TCom, TManaged>(TCom* com, [NotNullWhen(true)] out TManaged managed) where TCom : unmanaged where TManaged : class?
        {
            object? val;
            lock (_identity)
            {
                val = _identity.TryGetValue((nuint)com, out var obj) ? obj : null;
            }
            Debug.Assert(val is null or TManaged);
            managed = (TManaged)val!;
            return managed is not null;
        }

        public static TManaged GetManagedObject<TCom, TManaged>(TCom* com) where TCom : unmanaged where TManaged : class
        {
            lock (_identity)
            {
                return (TManaged)_identity[(nuint)com];
            }
        }
    }
}
