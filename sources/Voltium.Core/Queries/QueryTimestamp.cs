using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Core.Devices;

namespace Voltium.Core.Queries
{
    public interface IQueryType
    {
        internal QueryType Type { get; }
    }

    public readonly struct TimestampQuery : IQueryType
    {
        private readonly ulong Value;

        QueryType IQueryType.Type => QueryType.Timestamp;

        internal TimestampQuery(ulong value)
        {
            Value = value;
        }

        public readonly TimeSpan ToTimeSpan(INativeQueue queue)
            => ToTimeSpan(queue.Frequency);

        public readonly TimeSpan ToTimeSpan(ulong frequency)
            => TimeSpan.FromSeconds((double)Value / frequency);

        public static TimeSpan Interval(ulong frequency, TimestampQuery left, TimestampQuery right) => right.ToTimeSpan(frequency) - left.ToTimeSpan(frequency);
    }

    public readonly struct OcclusionQuery : IQueryType
    {
        private readonly ulong Value;

        internal OcclusionQuery(ulong value)
        {
            Value = value;
        }

        public ulong VisiblePixelCount => Value;

        QueryType IQueryType.Type => QueryType.Occlusion;

        public override string ToString() => $"OcclusionQuery: {VisiblePixelCount} pixels passed depth/stencil test";
    }

    public readonly struct BinaryOcclusionQuery : IQueryType
    {
        private readonly ulong Value;

        QueryType IQueryType.Type => QueryType.BinaryOcclusion;

        internal BinaryOcclusionQuery(ulong value)
        {
            Value = value;
        }

        public static bool operator true(BinaryOcclusionQuery query) => query.Value != 0;
        public static bool operator false(BinaryOcclusionQuery query) => query.Value == 0;

        public override string ToString() => this ? "BinaryOcclusionQuery: Some samples passed depth/stencil test" : "BinaryOcclusionQuery: No samples passed depth/stencil test";
    }

    public readonly struct PipelineStatisticsQuery : IQueryType, IFormattable
    {
        public readonly ulong InputAssemblerVertexCount;
        public readonly ulong InputAssemblerPrimitiveCount;
        public readonly ulong VertexShaderInvocationCount;
        public readonly ulong GeometryShaderInvocationCount;
        public readonly ulong GeometryShaderPrimitiveOutputCount;
        public readonly ulong RasterizerPrimitiveInputCount;
        public readonly ulong RasterizerPrimitiveOutputCount;
        public readonly ulong PixelShaderInvocationCount;
        public readonly ulong HullShaderInvocationCount;
        public readonly ulong DomainShaderInvocationCount;
        public readonly ulong ComputeShaderInvocationCount;

        public override string ToString()
            => $"PipelineStatistics: \n" +
                $"\tInputAssemblerVertexCount: {InputAssemblerVertexCount}\n" +
                $"\tInputAssemblerPrimitiveCount: {InputAssemblerPrimitiveCount}\n" +
                $"\tVertexShaderInvocationCount: {VertexShaderInvocationCount}\n" +
                $"\tGeometryShaderInvocationCount: {GeometryShaderInvocationCount}\n" +
                $"\tGeometryShaderPrimitiveOutputCount: {GeometryShaderPrimitiveOutputCount}\n" +
                $"\tRasterizerPrimitiveInputCount: {RasterizerPrimitiveInputCount}\n" +
                $"\tRasterizerPrimitiveOutputCount: {RasterizerPrimitiveOutputCount}\n" +
                $"\tPixelShaderInvocationCount: {PixelShaderInvocationCount}\n" +
                $"\tHullShaderInvocationCount: {HullShaderInvocationCount}\n" +
                $"\tDomainShaderInvocationCount: {DomainShaderInvocationCount}\n" +
                $"\tComputeShaderInvocationCount: {ComputeShaderInvocationCount}\n";

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            if (format is null)
            {
                return ToString();
            }

            var (type, value) = format switch
            {
                "C" or "Compute" => ("Compute", $"ComputeShaderInvocationCount: {ComputeShaderInvocationCount}\n"),

                "IA" or "InputAssembler" => ("InputAssembler",
                        $"InputAssemblerVertexCount: {InputAssemblerVertexCount}\n" +
                        $"\tInputAssemblerPrimitiveCount: {InputAssemblerPrimitiveCount}\n"),

                "R" or "Rasterizer" => ("Rasterizer",
                        $"RasterizerPrimitiveInputCount: {RasterizerPrimitiveInputCount}\n" +
                        $"\tRasterizerPrimitiveOutputCount: {RasterizerPrimitiveOutputCount}\n"),

                "G" or "Graphics" => ("Graphics",
                        $"InputAssemblerVertexCount: {InputAssemblerVertexCount}\n" +
                        $"\tInputAssemblerPrimitiveCount: {InputAssemblerPrimitiveCount}\n" +
                        $"\tVertexShaderInvocationCount: {VertexShaderInvocationCount}\n" +
                        $"\tGeometryShaderInvocationCount: {GeometryShaderInvocationCount}\n" +
                        $"\tGeometryShaderPrimitiveOutputCount: {GeometryShaderPrimitiveOutputCount}\n" +
                        $"\tRasterizerPrimitiveInputCount: {RasterizerPrimitiveInputCount}\n" +
                        $"\tRasterizerPrimitiveOutputCount: {RasterizerPrimitiveOutputCount}\n" +
                        $"\tPixelShaderInvocationCount: {PixelShaderInvocationCount}\n" +
                        $"\tHullShaderInvocationCount: {HullShaderInvocationCount}\n" +
                        $"\tDomainShaderInvocationCount: {DomainShaderInvocationCount}\n"),

                _ => ThrowHelper.ThrowFormatException<(string, string)>($"Invalid format specifier '{format}'")
            };

            return $"{type} PipelineStatistics: \n\t{value}";
        }

        QueryType IQueryType.Type => QueryType.PipelineStatistics;
    }
}
