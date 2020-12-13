using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Extensions;

namespace Voltium.Core.Devices.Shaders
{
    /// <summary>
    /// Represents an input element for a shader
    /// </summary>
    // !!! WARNING !!!
    // changing this type breaks IAInputDescGenerator in Voltium.Analyzers. If you change, make sure to fix generator too
    public struct ShaderInput
    {
        private D3D12_INPUT_ELEMENT_DESC Desc;

        /// <summary>
        /// The name of the element
        /// </summary>
        public unsafe string Name
        {
            get
            {
                if (Desc.SemanticName != null)
                {
                    StringHelpers.FreeUnmanagedAscii(Desc.SemanticName);
                }
                return new string(Desc.SemanticName);
            }

            set => Desc.SemanticName = StringHelpers.MarshalToUnmanagedAscii(value);
        }

        /// <summary>
        /// Allows access to the underlying ASCII of <see cref="Name"/>, without marshalling it
        /// </summary>
        public unsafe ReadOnlySpan<byte> RawName
        {
            get => StringHelpers.ToSpan(Desc.SemanticName);
            set => Desc.SemanticName = (sbyte*)value.ToUnmanaged();
        }

        /// <summary>
        /// The name-index of the element, which is appended to <see cref="Name"/>
        /// </summary>
        public uint NameIndex { get => Desc.SemanticIndex; set => Desc.SemanticIndex = value; }

        /// <summary>
        /// The offset, in bytes, of this element from the start of the current vertex channel
        /// </summary>
        public uint Offset { get => Desc.AlignedByteOffset; set => Desc.AlignedByteOffset = value; }

        /// <summary>
        /// The type of the element
        /// </summary>
        public DataFormat Type { get => (DataFormat)Desc.Format; set => Desc.Format = (DXGI_FORMAT)value; }

        /// <summary>
        /// The channel of the element
        /// </summary>
        public uint Channel { get => Desc.InputSlot; set => Desc.InputSlot = value; }

        /// <summary>
        /// The <see cref="InputClass"/> of this element, either per-vertex or per-intsance
        /// </summary>
        public InputClass InputClass { get => (InputClass)Desc.InputSlotClass; set => Desc.InputSlotClass = (D3D12_INPUT_CLASSIFICATION)value; }

        /// <summary>
        /// Creates a new instance of <see cref="ShaderInput"/>
        /// </summary>
        public ShaderInput(string name, DataFormat type, uint offset = 0xFFFFFFFF, uint nameIndex = 0, uint channel = 0, InputClass inputClass = InputClass.PerVertex)
        {
            // offset = 0xFFFFFFFF causes D3D12 to append as next element
            this = default;
            Name = name;
            Offset = offset;
            Type = type;
            NameIndex = nameIndex;
            Channel = channel;
            InputClass = inputClass;
        }
    }
}
