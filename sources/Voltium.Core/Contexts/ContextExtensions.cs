using System.Runtime.CompilerServices;
using Voltium.Core.Contexts;

namespace Voltium.Core
{
    /// <summary>
    /// Extensions for <see cref="GraphicsContext"/>, <see cref="ComputeContext"/>, and <see cref="CopyContext"/>
    /// </summary>
    public static class ContextExtensions
    {
        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ref CopyContext AsMutable(this in CopyContext context) => ref Unsafe.AsRef(in context);

        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ref ComputeContext AsMutable(this in ComputeContext context) => ref Unsafe.AsRef(in context);

        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ref GraphicsContext AsMutable(this in GraphicsContext context) => ref Unsafe.AsRef(in context);


        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ref UploadContext AsMutable(this in UploadContext context) => ref Unsafe.AsRef(in context);


        /// <summary>
        /// Returns the <see cref="GpuContext"/> for a given <see cref="CopyContext"/>
        /// </summary>
        /// <param name="context">The <see cref="CopyContext"/> to convert</param>
        /// <returns>A <see cref="GpuContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref GpuContext AsGpuContext(this ref CopyContext context) => ref Unsafe.As<CopyContext, GpuContext>(ref context);

        /// <summary>
        /// Returns the <see cref="GpuContext"/> for a given <see cref="GraphicsContext"/>
        /// </summary>
        /// <param name="context">The <see cref="GraphicsContext"/> to convert</param>
        /// <returns>A <see cref="GpuContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref GpuContext AsGpuContext(this ref GraphicsContext context) => ref Unsafe.As<GraphicsContext, GpuContext>(ref context);

        /// <summary>
        /// Returns the <see cref="GpuContext"/> for a given <see cref="ComputeContext"/>
        /// </summary>
        /// <param name="context">The <see cref="ComputeContext"/> to convert</param>
        /// <returns>A <see cref="GpuContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref GpuContext AsGpuContext(this ref ComputeContext context) => ref Unsafe.As<ComputeContext, GpuContext>(ref context);

        /// <summary>
        /// Returns the <see cref="CopyContext"/> for a given <see cref="GraphicsContext"/>
        /// </summary>
        /// <param name="context">The <see cref="GraphicsContext"/> to convert</param>
        /// <returns>A <see cref="CopyContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref CopyContext AsCopyContext(this ref GraphicsContext context) => ref Unsafe.As<GraphicsContext, CopyContext>(ref context);

        /// <summary>
        /// Returns the <see cref="CopyContext"/> for a given <see cref="ComputeContext"/>
        /// </summary>
        /// <param name="context">The <see cref="ComputeContext"/> to convert</param>
        /// <returns>A <see cref="CopyContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref CopyContext AsCopyContext(this ref ComputeContext context) => ref Unsafe.As<ComputeContext, CopyContext>(ref context);


        /// <summary>
        /// Returns the <see cref="CopyContext"/> for a given <see cref="UploadContext"/>
        /// </summary>
        /// <param name="context">The <see cref="UploadContext"/> to convert</param>
        /// <returns>A <see cref="CopyContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref CopyContext AsCopyContext(this ref UploadContext context) => ref Unsafe.As<UploadContext, CopyContext>(ref context);

        /// <summary>
        /// Returns the <see cref="ComputeContext"/> for a given <see cref="GraphicsContext"/>
        /// </summary>
        /// <param name="context">The <see cref="GraphicsContext"/> to convert</param>
        /// <returns>A <see cref="ComputeContext"/> recording to the same list as <paramref name="context"/></returns>
        public static ref ComputeContext AsComputeContext(this ref GraphicsContext context) => ref Unsafe.As<GraphicsContext, ComputeContext>(ref context);
    }
}
