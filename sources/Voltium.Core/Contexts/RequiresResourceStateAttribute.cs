using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Contexts
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class RequiresResourceStateAttribute : Attribute
    {
        public RequiresResourceStateAttribute(ResourceState state)
        {

        }
    }
}
