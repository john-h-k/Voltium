using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using MathSharp;
using Microsoft.Collections.Extensions;
using SixLabors.ImageSharp;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Pix;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Vector = MathSharp.Vector;

namespace Voltium.Core.Contexts
{
    internal static class ContextEncoder
    {
        public static ContextEncoder<TBufferWriter> Create<TBufferWriter>(TBufferWriter writer) where TBufferWriter : IBufferWriter<byte> => new(writer);
    }
    internal struct ContextEncoder<TBufferWriter> where TBufferWriter : IBufferWriter<byte>
    {
        public TBufferWriter Writer;

        public ContextEncoder(TBufferWriter writer) => Writer = writer;

        public unsafe void EmitVariable<TCommand, TVariable1, TVariable2>(TCommand* value, TVariable1* pVariable1, TVariable2* pVariable2, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable1 : unmanaged
            where TVariable2 : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable1) * (int)variableCount) + (sizeof(TVariable2) * (int)variableCount), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable1, (uint)sizeof(TVariable1) * variableCount);
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable2, (uint)sizeof(TVariable2) * variableCount);

            Writer.Advance(commandLength);
        }

        public unsafe void EmitVariable<TCommand, TVariable>(TCommand* value, TVariable* pVariable, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable) * (int)variableCount), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable, (uint)sizeof(TVariable) * variableCount);
            Writer.Advance(commandLength);
        }

        public unsafe void Emit<TCommand>(TCommand* value) where TCommand : unmanaged, ICommand
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Writer.Advance(commandLength);
        }

        public unsafe void EmitEmpty(CommandType command)
        {
            var commandLength = sizeof(CommandType);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = command;
            Writer.Advance(commandLength);
        }
    }
}
