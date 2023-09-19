using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Core.Devices;

namespace Voltium.Core.Queries
{
    /// <summary>
    /// Used as a marker interface for query types
    /// </summary>
    public interface IQueryType
    {
        internal QueryType Type { get; }
    }

    /// <summary>
    /// A query of an opaque GPU timestamp tick count
    /// </summary>
    public readonly struct TimestampQuery : IQueryType
    {
        private readonly ulong Value;

        QueryType IQueryType.Type => QueryType.Timestamp;

        internal TimestampQuery(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// Converts the current timestamp to a <see cref="TimeSpan"/> using a given frequency
        /// </summary>
        /// <param name="frequency">The frequency of the queue this timestamp was taken from</param>
        /// <returns>A new <see cref="TimeSpan"/> representing this query</returns>
        public readonly TimeSpan ToTimeSpan(ulong frequency)
            => TimeSpan.FromSeconds((double)Value / frequency);

        /// <summary>
        /// Calcalates the interval <see cref="TimeSpan"/> between 2 queries
        /// </summary>
        /// <param name="frequency">The frequency of the queue these timestamps were taken from</param>
        /// <param name="left">The first (earlier) timestamp</param>
        /// <param name="right">The second (later) timestamp</param>
        /// <returns>A new <see cref="TimeSpan"/> representing this <see cref="TimeSpan"/> between <paramref name="left"/> and <paramref name="right"/></returns>
        public static TimeSpan Interval(ulong frequency, TimestampQuery left, TimestampQuery right) => right.ToTimeSpan(frequency) - left.ToTimeSpan(frequency);
    }

    /// <summary>
    /// A query of how many pixels in a draw were not occluded (passed depth-stencil testing)
    /// </summary>
    public readonly struct OcclusionQuery : IQueryType
    {
        private readonly ulong Value;

        internal OcclusionQuery(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// The number of pixels which were visible in the draw
        /// </summary>
        public ulong VisiblePixelCount => Value;

        QueryType IQueryType.Type => QueryType.Occlusion;

        /// <inheritdoc/>
        public override string ToString() => $"OcclusionQuery: {VisiblePixelCount} pixels passed depth/stencil test";
    }

    /// <summary>
    /// A query of whether any pixels draw were not occluded (passed depth-stencil testing)
    /// </summary>
    public readonly struct BinaryOcclusionQuery : IQueryType
    {
        private readonly ulong Value;

        QueryType IQueryType.Type => QueryType.BinaryOcclusion;

        internal BinaryOcclusionQuery(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// Whether any pixels were visible during the draw
        /// </summary>
        public bool PixelsWereVisible => Value != 0;

        /// <inheritdoc/>
        public override string ToString() => PixelsWereVisible ? "BinaryOcclusionQuery: Some samples passed depth/stencil test" : "BinaryOcclusionQuery: No samples passed depth/stencil test";
    }

    /// <summary>
    /// A query of various attributes of the GPU pipeline
    /// </summary>
    public readonly struct PipelineStatisticsQuery : IQueryType, IFormattable
    {
        /// <summary>
        /// How many vertices were passed to the input assembler (IA)
        /// </summary>
        public readonly ulong InputAssemblerVertexCount;

        /// <summary>
        /// How many primitives were processed by the input assembler (IA)
        /// This is dependent on <see cref="InputAssemblerVertexCount"/>, and the <see cref="Topology"/> used to draw
        /// </summary>
        public readonly ulong InputAssemblerPrimitiveCount;

        /// <summary>
        /// The number of times the vertex shader was invoked
        /// </summary>
        public readonly ulong VertexShaderInvocationCount;

        /// <summary>
        /// The number of times the geometry shader was invoked
        /// </summary>
        public readonly ulong GeometryShaderInvocationCount;

        /// <summary>
        /// The number of times the vertex shader was invoked
        /// </summary>
        public readonly ulong GeometryShaderPrimitiveOutputCount;

        /// <summary>
        /// How many primitives were inputted to the rasterizer
        /// </summary>
        public readonly ulong RasterizerPrimitiveInputCount;

        /// <summary>
        /// The number of primitives outputted by the rasterizer. This may change from <see cref="RasterizerPrimitiveInputCount"/> due to culling and splitting
        /// </summary>
        public readonly ulong RasterizerPrimitiveOutputCount;

        /// <summary>
        /// The number of times the pixel shader was invoked
        /// </summary>
        public readonly ulong PixelShaderInvocationCount;

        /// <summary>
        /// The number of times the hall shader was invoked
        /// </summary>
        public readonly ulong HullShaderInvocationCount;

        /// <summary>
        /// The number of times the domain shader was invoked
        /// </summary>
        public readonly ulong DomainShaderInvocationCount;

        /// <summary>
        /// The number of times the compute shader was invoked
        /// </summary>
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
