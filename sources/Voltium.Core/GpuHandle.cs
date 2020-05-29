﻿using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// Represents an opaque GPU handle to a D3D12 descriptor
    /// </summary>
    public readonly struct GpuHandle
    {
        /// <summary>
        /// The value of the handle
        /// </summary>
        public readonly D3D12_GPU_DESCRIPTOR_HANDLE Value;

        /// <summary>
        /// Adds an <see cref="int"/> to the underlying value of the handle
        /// </summary>
        /// <param name="left">The handle to add to</param>
        /// <param name="right">The amount to add to it</param>
        /// <returns>An offsetted handle</returns>
        public static GpuHandle operator +(GpuHandle left, int right)
        {
            var copy = left.Value;
            return new GpuHandle(copy.Offset(right));
        }

        /// <summary>
        /// Creates a new instance of <see cref="GpuHandle"/>
        /// </summary>
        /// <param name="value">The underlying handle value</param>
        public GpuHandle(D3D12_GPU_DESCRIPTOR_HANDLE value)
        {
            Value = value;
        }

        /// <summary>
        /// Defines the implicit conversion between a <see cref="D3D12_GPU_DESCRIPTOR_HANDLE"/>
        /// and a <see cref="GpuHandle"/>
        /// </summary>
        /// <param name="handle">The handle to convert</param>
        /// <returns>A new <see cref="GpuHandle"/></returns>
        public static implicit operator GpuHandle(D3D12_GPU_DESCRIPTOR_HANDLE handle) => new GpuHandle(handle);
    }
}
