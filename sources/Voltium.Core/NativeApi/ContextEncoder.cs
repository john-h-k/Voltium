using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Voltium.Common;
using Voltium.Core.NativeApi;

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

            ref var start = ref MemoryMarshal.GetReference(buffer);
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

            ref var start = ref MemoryMarshal.GetReference(buffer);
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable, (uint)sizeof(TVariable) * variableCount);
            Writer.Advance(commandLength);
        }

        public unsafe void Emit<TCommand>(TCommand* value) where TCommand : unmanaged, ICommand
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref MemoryMarshal.GetReference(buffer);
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Writer.Advance(commandLength);
        }

        public unsafe void EmitEmpty(CommandType command)
        {
            var commandLength = sizeof(CommandType);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref MemoryMarshal.GetReference(buffer);
            Unsafe.As<byte, CommandType>(ref start) = command;
            Writer.Advance(commandLength);
        }
    }
}
