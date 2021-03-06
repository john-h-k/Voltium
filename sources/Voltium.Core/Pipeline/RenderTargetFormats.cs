using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices.Shaders;

namespace Voltium.Core.Pipeline
{
    public struct InputLayout
    {
        internal ReadOnlyMemory<ShaderInput> ShaderInputs;

        public static InputLayout Empty => default;

        public unsafe InputLayout(ReadOnlyMemory<ShaderInput> inputs)
        {
            ShaderInputs = inputs;
        }

        public static InputLayout FromType<TShaderInput>() where TShaderInput : struct, IBindableShaderType
            => new InputLayout(default(TShaderInput).GetShaderInputs());

    }


    public partial struct RenderTargetFormats
    {
        public uint Count;

        private FormatBuffer8 _formats;

        /// <summary>
        /// Creates a new <see cref="RenderTargetFormats"/>
        /// </summary>
        public RenderTargetFormats(
            uint count,
            DataFormat e0 = DataFormat.Unknown,
            DataFormat e1 = DataFormat.Unknown,
            DataFormat e2 = DataFormat.Unknown,
            DataFormat e3 = DataFormat.Unknown,
            DataFormat e4 = DataFormat.Unknown,
            DataFormat e5 = DataFormat.Unknown,
            DataFormat e6 = DataFormat.Unknown,
            DataFormat e7 = DataFormat.Unknown
        )
        {
            Unsafe.SkipInit(out this);
            Count = count;
            this[0] = e0;
            this[1] = e1;
            this[2] = e2;
            this[3] = e3;
            this[4] = e4;
            this[5] = e5;
            this[6] = e6;
            this[7] = e7;
        }

        /// <summary>
        /// Creates a new <see cref="RenderTargetFormats"/>
        /// </summary>
        // convenience overload
        public RenderTargetFormats(
            uint count,
            BackBufferFormat e0, // prevent ambigous ctor
            BackBufferFormat e1 = 0,
            BackBufferFormat e2 = 0,
            BackBufferFormat e3 = 0,
            BackBufferFormat e4 = 0,
            BackBufferFormat e5 = 0,
            BackBufferFormat e6 = 0,
            BackBufferFormat e7 = 0
        )
        {
            Unsafe.SkipInit(out this);
            Count = count;
            this[0] = (DataFormat)e0;
            this[1] = (DataFormat)e1;
            this[2] = (DataFormat)e2;
            this[3] = (DataFormat)e3;
            this[4] = (DataFormat)e4;
            this[5] = (DataFormat)e5;
            this[6] = (DataFormat)e6;
            this[7] = (DataFormat)e7;
        }


        /// <inheritdoc cref="this[int]"/>
        public ref DataFormat this[uint index]
            => ref _formats[index];

        /// <summary>
        /// Returns a reference to the <see cref="DataFormat"/> at position <paramref name="index"/>
        /// </summary>
        public ref DataFormat this[int index]
            => ref _formats[index];

        /// <inheritdoc cref="FormatBuffer8.GetPinnableReference"/>
        public ref DataFormat GetPinnableReference()
            => ref _formats.GetPinnableReference();

        /// <summary>
        /// Implicit conversion
        /// </summary>
        public static implicit operator RenderTargetFormats(BackBufferFormat format)
            => new RenderTargetFormats(1, format);

        /// <summary>
        /// Implicit conversion
        /// </summary>
        public static implicit operator RenderTargetFormats(DataFormat format)
            => new RenderTargetFormats(1, format);

        /// <summary>
        /// A buffer of 8 <see cref="DataFormat"/>s
        /// </summary>
        [FixedBufferType(typeof(DataFormat), 8)]
        internal unsafe partial struct FormatBuffer8
        {
            /// <summary>
            /// Creates a new <see cref="FormatBuffer8"/>
            /// </summary>
            public FormatBuffer8(
                DataFormat e0 = DataFormat.Unknown,
                DataFormat e1 = DataFormat.Unknown,
                DataFormat e2 = DataFormat.Unknown,
                DataFormat e3 = DataFormat.Unknown,
                DataFormat e4 = DataFormat.Unknown,
                DataFormat e5 = DataFormat.Unknown,
                DataFormat e6 = DataFormat.Unknown,
                DataFormat e7 = DataFormat.Unknown
            )
            {
                Unsafe.SkipInit(out this);
                this[0] = e0;
                this[1] = e1;
                this[2] = e2;
                this[3] = e3;
                this[4] = e4;
                this[5] = e5;
                this[6] = e6;
                this[7] = e7;
            }

            /// <summary>
            /// Creates a new <see cref="FormatBuffer8"/>
            /// </summary>
            // convenience overload
            public FormatBuffer8(
                BackBufferFormat e0, // prevent ambigous ctor
                BackBufferFormat e1 = 0,
                BackBufferFormat e2 = 0,
                BackBufferFormat e3 = 0,
                BackBufferFormat e4 = 0,
                BackBufferFormat e5 = 0,
                BackBufferFormat e6 = 0,
                BackBufferFormat e7 = 0
            )
            {
                Unsafe.SkipInit(out this);
                this[0] = (DataFormat)e0;
                this[1] = (DataFormat)e1;
                this[2] = (DataFormat)e2;
                this[3] = (DataFormat)e3;
                this[4] = (DataFormat)e4;
                this[5] = (DataFormat)e5;
                this[6] = (DataFormat)e6;
                this[7] = (DataFormat)e7;
            }

            /// <summary>
            /// Implicit conversion
            /// </summary>
            public static implicit operator FormatBuffer8(BackBufferFormat format)
                => new FormatBuffer8(format);

            /// <summary>
            /// Implicit conversion
            /// </summary>
            public static implicit operator FormatBuffer8(DataFormat format)
                => new FormatBuffer8(format);
        }
    }
}
