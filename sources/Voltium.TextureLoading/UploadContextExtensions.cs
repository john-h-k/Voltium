using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;

namespace Voltium.TextureLoading
{
    /// <summary>
    /// Provides shorthand methods for uploading a <see cref="FormatTexture"/> via an <see cref="UploadContext"/>
    /// </summary>
    public static class UploadContextExtensions
    {
        /// <summary>
        /// Uploads a texture
        /// </summary>
        /// <param name="upload">The <see cref="UploadContext"/> to use</param>
        /// <param name="desc">The <see cref="FormatTexture"/> to load</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public static Texture UploadTexture(this UploadContext upload, in FormatTexture desc)
            => upload.UploadTexture(desc.Data.Span, desc.SubresourceData.Span, desc.Desc);


        /// <summary>
        /// Uploads a texture
        /// </summary>
        /// <param name="upload">The <see cref="UploadContext"/> to use</param>
        /// <param name="desc">The <see cref="FormatTexture"/> to load</param>
        /// <param name="flags">Any <see cref="ResourceFlags"/> to upload the texture with</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public static Texture UploadTexture(this UploadContext upload, in FormatTexture desc, ResourceFlags flags)
        {
            var resDesc = desc.Desc;
            resDesc.ResourceFlags |= flags;
            return upload.UploadTexture(desc.Data.Span, desc.SubresourceData.Span, resDesc);
        }
    }
}
