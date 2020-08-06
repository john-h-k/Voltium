using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.Core.MetaCommands
{
    /// <summary>
    /// Describes a meta command
    /// </summary>
    public struct MetaCommandDesc
    {
        /// <summary>
        /// The <see cref="Guid"/> for the metacommand
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// The name of the metacommand
        /// </summary>
        public readonly string Name;

        internal MetaCommandDesc(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            using var builder = StringHelper.RentStringBuilder();
            builder.Append("MetaCommand: ").Append(Name).Append(" - ").Append(Id);
            return builder.ToString();
        }
    }
}
