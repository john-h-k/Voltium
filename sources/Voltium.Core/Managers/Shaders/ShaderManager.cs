using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Managers.Shaders;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// A class used for management, compilation, and storing of shaders
    /// </summary>
    public sealed class ShaderManager
    {

        /// <summary>
        /// Reads a new <see cref="CompiledShader"/> from  a file
        /// </summary>
        /// <param name="filename">The path to the file containing the shader data</param>
        /// <param name="type">The type of the shader</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader ReadCompiledShader(string filename, ShaderType type)
        {
            return ReadCompiledShader(File.OpenRead(filename), type);
        }

        /// <summary>
        /// Reads a new <see cref="CompiledShader"/> from a stream
        /// </summary>
        /// <param name="stream">The stream containing the shader data</param>
        /// <param name="type">The type of the shader</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader ReadCompiledShader(Stream stream, ShaderType type)
        {
            var size = stream.Length;

            if (size > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("Shaders cannot exceed 2^31 bytes");
            }

            var buff = new byte[(int)size];

            stream.Read(buff);

            return ReadCompiledShader(buff, type);
        }

        /// <summary>
        /// Reads a new <see cref="CompiledShader"/> from bytes
        /// </summary>
        /// <param name="data">The bytes containing the shader data</param>
        /// <param name="type">The type of the shader</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader ReadCompiledShader(ReadOnlyMemory<byte> data, ShaderType type)
        {
            return new CompiledShader(data, type);
        }

        private static ComPtr<IDxcCompiler3> Compiler;
        private static ComPtr<IDxcIncludeHandler> DefaultIncludeHandler;
        private static ComPtr<IDxcUtils> Utils;

        unsafe static ShaderManager()
        {
            ComPtr<IDxcCompiler3> compiler = default;
            ComPtr<IDxcUtils> utils = default;
            ComPtr<IDxcIncludeHandler> defaultIncludeHandler = default;

            Guid clsid = Windows.CLSID_DxcCompiler;
            Guard.ThrowIfFailed(Windows.DxcCreateInstance(&clsid, compiler.Guid, ComPtr.GetVoidAddressOf(&compiler)));
            clsid = Windows.CLSID_DxcUtils;
            Guard.ThrowIfFailed(Windows.DxcCreateInstance(&clsid, utils.Guid, ComPtr.GetVoidAddressOf(&utils)));

            utils.Get()->CreateDefaultIncludeHandler(ComPtr.GetAddressOf(&defaultIncludeHandler));

            Compiler = compiler.Move();
            Utils = utils.Move();
            DefaultIncludeHandler = defaultIncludeHandler.Move();
        }

        private const uint DXC_CP_UTF8 = 65001;
        private const uint DXC_CP_UTF16 = 1200;
        private const uint DXC_CP_AP = 0;

        internal unsafe static CompiledShader CompileShader(
            ReadOnlySpan<char> shaderText,
            ReadOnlySpan<char> entrypoint,
            DxcCompileTarget target,
            DxcCompileFlags.Flag[] flags = null!
        )
        {
            flags ??= Array.Empty<DxcCompileFlags.Flag>();

            // TODO: POH pool

            ushort** pFlags = null;

            fixed (char* pText = shaderText)
            {
                DxcBuffer text;
                text.Ptr = pText;
                text.Size = (nuint)shaderText.Length;
                text.Encoding = DXC_CP_UTF16;

                using ComPtr<IDxcResult> compileResult = default;
                int hr = Compiler.Get()->Compile(
                    &text,
                    pFlags,
                    (uint)flags.Length,
                    DefaultIncludeHandler.Get(),
                    compileResult.Guid,
                    ComPtr.GetVoidAddressOf(&compileResult)
                );

                int statusHr;
                compileResult.Get()->GetStatus(&statusHr);
                if (Windows.FAILED(hr) || Windows.FAILED(statusHr))
                {
                    using ComPtr<IDxcBlobUtf16> errors = default;
                    using ComPtr<IDxcBlobUtf16> errorsBlobName = default;
                    Guard.ThrowIfFailed(compileResult.Get()->GetOutput(
                        DXC_OUT_KIND.DXC_OUT_ERRORS,
                        errors.Guid,
                        ComPtr.GetVoidAddressOf(&errors),
                        ComPtr.GetAddressOf(&errorsBlobName)
                    ));
                }
            }
        }


        internal static CompiledShader CompileShader(
            string shaderText,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> entrypoint,
            ShaderCompilationOptions options
        )
        {
            return CompileShader(shaderText, name, entrypoint, options);
        }
    }
}
