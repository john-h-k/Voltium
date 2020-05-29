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

        //private static ComPtr<IDxcCompiler2> Compiler;

        //unsafe static ShaderManager()
        //{
        //    ComPtr<IDxcCompiler2> compiler = default;
        //    Guid clsid = Windows.CLSID_DxcCompiler;
        //    Guard.ThrowIfFailed(Windows.DxcCreateInstance(&clsid, compiler.Guid, ComPtr.GetVoidAddressOf(&compiler)));
        //    Compiler = compiler.Move();
        //}

        internal unsafe static CompiledShader CompileShader(
            ReadOnlySpan<byte> shaderTextAscii,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> entrypoint,
            ShaderCompilationOptions options
        )
        {
            fixed (byte* pData = shaderTextAscii)
            fixed (byte* pName = ToAscii(name))
            fixed (byte* pEntrypoint = ToAscii(entrypoint))
            fixed (byte* pTarget = AsTargetString(options.Target))
            fixed (D3D_SHADER_MACRO* pDefines = ToAsciiDefines(options.Defines.Span))
            fixed (byte* pSecondaryData = options.SecondaryData.Span)
            {
                ID3DBlob* data = default;
                ID3DBlob* error = default;
                int hr = Windows.D3DCompile2(
                    pData,
                    (nuint)shaderTextAscii.Length,
                    (sbyte*)pName,
                    pDefines,
                    (ID3DInclude*)Windows.D3D_COMPILE_STANDARD_FILE_INCLUDE,
                    (sbyte*)pEntrypoint,
                    (sbyte*)pTarget,
                    D3DCompileFlagsExtensions.GetCompileFlags(options.Flags),
                    0,
                    D3DCompileFlagsExtensions.GetSecDataFlags(options.Flags),
                    pSecondaryData,
                    (nuint)options.SecondaryData.Length,
                    &data,
                    &error
                );

                return ConstructShaderFromDataAndError(hr, data, error);
            }
        }

        internal static unsafe CompiledShader ConstructShaderFromDataAndError(int hr, ID3DBlob* data, ID3DBlob* error)
        {
            if (Windows.FAILED(hr))
            {

            }

            throw null!;
        }

        private static unsafe byte[] AsTargetString(D3DCompileTarget target)
        {
            throw new NotImplementedException();
        }

        internal static CompiledShader CompileShader(
            string shaderText,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> entrypoint,
            ShaderCompilationOptions options
        )
        {
            return CompileShader(ToAscii(shaderText), name, entrypoint, options);
        }

        [ThreadStatic]
        private unsafe static byte* _pAsciiBuff;
        [ThreadStatic]
        private unsafe static nuint _szAsciiBuff;

        private unsafe static void InitTlsMacroBuff()
        {
            if (_pAsciiBuff != null)
            {
                return;
            }

            nint sz = 1024 * 4;
            _pAsciiBuff = (byte*)Marshal.AllocHGlobal(sz);
            _szAsciiBuff = (nuint)sz;
        }

        private unsafe static void TlsMacroToPinnedAscii(ShaderDefine define, ref nuint bytesWritten, out byte* pName, out byte* pDef)
        {
            pName = WriteAsciiToBuffSafe(_pAsciiBuff, _szAsciiBuff, define.Name, ref bytesWritten);
            pDef = WriteAsciiToBuffSafe(_pAsciiBuff, _szAsciiBuff, define.Definition, ref bytesWritten);
        }

        private unsafe static byte* WriteAsciiToBuffSafe(byte* pBuff, nuint buffSz, string str, ref nuint bytesWritten)
        {
            bytesWritten += (uint)Encoding.ASCII.GetBytes(str, new Span<byte>(pBuff + bytesWritten, (int)(buffSz - bytesWritten)));
            pBuff[bytesWritten++] = 0;
            return &pBuff[bytesWritten];
        }

        private unsafe static D3D_SHADER_MACRO[] ToAsciiDefines(ReadOnlySpan<ShaderDefine> defines)
        {
            var macros = new D3D_SHADER_MACRO[defines.Length];

            InitTlsMacroBuff();
            nuint asciiBuffOffset = 0;

            for (var i = 0; i < defines.Length; i++)
            {
                var define = defines[i];
                TlsMacroToPinnedAscii(define, ref asciiBuffOffset, out var pName, out var pDef);
                macros[i] = new D3D_SHADER_MACRO { Name = (sbyte*)pName, Definition = (sbyte*)pDef };
            }

            return macros;
        }

        private unsafe static byte[] ToAscii(ReadOnlySpan<char> str)
        {
            var size = Encoding.ASCII.GetMaxByteCount(str.Length);

            var buff = new byte[size];

            Encoding.ASCII.GetBytes(str, buff);

            return buff;
        }
    }

    // TODO COM interop this as a 'ID3DInclude'
    //public sealed class ShaderIncluder
    //{
    //    private string[] _includeDirs;
    //    private Dictionary<string, string> _fileToStrings;
    //}
}
