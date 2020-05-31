using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a file
        /// </summary>
        /// <param name="filename">The filename containing the shader</param>
        /// <param name="target">The <see cref="DxcCompileTarget"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="DxcCompileFlags.Flag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            string filename,
            DxcCompileTarget target,
            DxcCompileFlags.Flag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default
        )
        {
            return CompileShader(filename, File.OpenText(filename), target, flags, entrypoint);
        }

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="stream">The <see cref="StreamReader"/> containing the shader text</param>
        /// <param name="target">The <see cref="DxcCompileTarget"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="DxcCompileFlags.Flag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            StreamReader stream,
            DxcCompileTarget target,
            DxcCompileFlags.Flag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default
        )
        {
            var size = stream.BaseStream.Length;

            if (size > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("Shaders cannot exceed 2^31 bytes");
            }

            var buff = new char[(int)size];

            stream.Read(buff);

            return CompileShader(name, buff, target, flags, entrypoint);
        }

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="shaderText">The <see cref="ReadOnlySpan{T}"/> containing the shader text</param>
        /// <param name="target">The <see cref="DxcCompileTarget"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="DxcCompileFlags.Flag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public unsafe static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> shaderText,
            DxcCompileTarget target,
            DxcCompileFlags.Flag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default
        )
        {
            if (target.Type == ShaderType.Library && !entrypoint.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException("Shader libraries cannot have an entrypoint");
            }

            flags ??= Array.Empty<DxcCompileFlags.Flag>();

            // biggest target possible (althought invalid) would be "-T lib_255_255", 14 chars (15 with null char)
            // entrypoint is 0 if empty, else "-E " + length of entrypoint + null char
            int targetAndEntrypointLength = (14 + 1 + (entrypoint.IsEmpty ? 0 : entrypoint.Length + 3 + 1)) * sizeof(char);

            // space for all the flag strings (and their null chars) + the actual pointers to these strings
            int flagPointerLength = 0;
            int flagLength = 0;
            foreach (var flag in flags)
            {
                flagLength += (flag.Value.Length + /* null char */ 1) * sizeof(char);
                flagPointerLength += (1 + flag.ArgCount) * sizeof(nuint);
            }

            flagPointerLength += (entrypoint.IsEmpty ? 2 : 4) *sizeof(nuint);

            // rent as short term usage TODO: POH pool
            using var rentedFlagBuff = RentedArray<byte>.Create(flagPointerLength + flagLength + targetAndEntrypointLength);

            // can't have Span<ushort*>
            var flagPointerBuff = MemoryMarshal.Cast<byte, nuint>(rentedFlagBuff.Value.AsSpan());
            var flagBuff = MemoryMarshal.Cast<byte, char>(rentedFlagBuff.Value.AsSpan().Slice(flagPointerLength));

            // write target and entrypoint first to get them out the way
            // target
            {
                // pointer to '-T '
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff));
                flagPointerBuff = flagPointerBuff.Slice(1);

                // pointer to value
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff.Slice(3)));
                flagPointerBuff = flagPointerBuff.Slice(1);

                flagBuff[0] = '-';
                flagBuff[1] = 'T';
                flagBuff[2] = '\0';

                flagBuff = flagBuff.Slice(3);

                var targetPrefix = DxcCompileTarget.ShaderNameMap[target.Type].AsSpan();
                targetPrefix.CopyTo(flagBuff);
                flagBuff[targetPrefix.Length] = '_';

                flagBuff = flagBuff.Slice(targetPrefix.Length + 1);

                target.Major.TryFormat(flagBuff, out int charsWritten);
                flagBuff[charsWritten] = '_';

                flagBuff = flagBuff.Slice(charsWritten + 1);

                target.Minor.TryFormat(flagBuff, out charsWritten);

                flagBuff = flagBuff.Slice(charsWritten + 1);
            }

            // entrypoint
            {
                if (!entrypoint.IsEmpty)
                {
                    // pointer to '-E '
                    flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff));
                    flagPointerBuff = flagPointerBuff.Slice(1);

                    // pointer to value
                    flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff.Slice(3)));
                    flagPointerBuff = flagPointerBuff.Slice(1);

                    flagBuff[0] = '-';
                    flagBuff[1] = 'E';
                    flagBuff[2] = '\0';

                    flagBuff = flagBuff.Slice(3);

                    entrypoint.CopyTo(flagBuff);
                    flagBuff[entrypoint.Length] = '\0';

                    flagBuff = flagBuff.Slice(entrypoint.Length + 1);
                }
            }

            foreach (var flag in flags)
            {
                // we know it is pinned. this is where the string is written
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff));
                flagPointerBuff = flagPointerBuff.Slice(1);

                var len = flag.Value.Length;

                var argPtr = flag.Value.AsSpan();
                var argIndex = argPtr.IndexOf(' ');
                argPtr = argPtr.Slice(argIndex);
                MemoryMarshal.GetReference(argPtr) = '\0';
                for (var i = 0; i < flag.ArgCount; i++)
                {
                    flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff.Slice(argIndex - 1)));
                    flagPointerBuff = flagPointerBuff.Slice(1);
                    argIndex = argPtr.IndexOf(' ');
                    argPtr = argPtr.Slice(argIndex);
                    MemoryMarshal.GetReference(argPtr) = '\0';
                }

                flag.Value.AsSpan().CopyTo(flagBuff);
                flagBuff[len] = '\0';

                flagBuff = flagBuff.Slice(len + 1);
            }

            fixed (char* pText = shaderText)
            fixed (byte* ppFlags = rentedFlagBuff.Value)
            {
                DxcBuffer text;
                text.Ptr = pText;
                text.Size = (nuint)(shaderText.Length * sizeof(char));
                text.Encoding = DXC_CP_UTF16;

                string debugView = new Span<char>(ppFlags, flagPointerLength + flagLength + targetAndEntrypointLength).ToString();
                string derefFirstArg = new string(*(char**)ppFlags);
                string derefSeconddArg = new string(*((char**)ppFlags + 1));
                
                using ComPtr<IDxcResult> compileResult = default;
                Guard.ThrowIfFailed(Compiler.Get()->Compile(
                    &text,
                    (ushort**)ppFlags,
                    // account for target flag, and possible entrypoint flag
                    (uint)(flagPointerLength / sizeof(nuint)),
                    DefaultIncludeHandler.Get(),
                    compileResult.Guid,
                    ComPtr.GetVoidAddressOf(&compileResult)
                ));

                int statusHr;
                Guard.ThrowIfFailed(compileResult.Get()->GetStatus(&statusHr));

                if (Windows.FAILED(statusHr))
                {
                    var result = TryGetOutputUtf8(compileResult.Get(), DXC_OUT_KIND.DXC_OUT_ERRORS, out var errors, out var errorName);
                    Debug.Assert(result);
                    _ = result; // prevent release warning

                    using (errors)
                    using (errorName)
                    {
                        var data = new ShaderCompilationData
                        {
                            Filename = name,
                            Errors = FromBlob(errors.Get()),
                            //Other = FromBlob(errorName.Get())
                        };
                        throw new ShaderCompilationException(data);
                    }
                }

                if (TryGetOutput(compileResult.Get(), DXC_OUT_KIND.DXC_OUT_PDB, out var pdb, out var pdbName))
                {
                    using (pdb)
                    using (pdbName)
                    {
                        HandlePdb(pdb.Get(), pdbName.Get());
                    }
                }


                using ComPtr<IDxcBlob> pBlob = default;
                Guard.ThrowIfFailed(compileResult.Get()->GetResult(ComPtr.GetAddressOf(&pBlob)));
                var shaderBytes = FromBlob(pBlob.Get());

                return new CompiledShader(shaderBytes.ToArray(), target.Type);
            }
        }

        private static unsafe void HandlePdb(IDxcBlobUtf16* pdb, IDxcBlobUtf16* pdbName)
        {
            throw new NotImplementedException();
        }

        private static unsafe ReadOnlySpan<byte> FromBlob(IDxcBlob* pBlob)
            => new ReadOnlySpan<byte>(pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize());

        private static unsafe ReadOnlySpan<char> FromBlob(IDxcBlobUtf8* pBlob)
            => Encoding.ASCII.GetString(new ReadOnlySpan<byte>(pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize()));

        private static unsafe ReadOnlySpan<char> FromBlob(IDxcBlobUtf16* pBlob)
            => new ReadOnlySpan<char>(pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize());

        private static unsafe bool TryGetOutput(IDxcResult* result, DXC_OUT_KIND kind, out ComPtr<IDxcBlobUtf16> data, out ComPtr<IDxcBlobUtf16> name)
        {
            if (result->HasOutput(kind) == Windows.TRUE)
            {
                fixed (ComPtr<IDxcBlobUtf16>* pData = &data)
                fixed (ComPtr<IDxcBlobUtf16>* pName = &name)
                {
                    Guard.ThrowIfFailed(result->GetOutput(
                        kind,
                        pData->Guid,
                        ComPtr.GetVoidAddressOf(pData),
                        ComPtr.GetAddressOf(pName)
                    ));

                    return true;
                }
            }

            data = default;
            name = default;

            return false;
        }

        private static unsafe bool TryGetOutputUtf8(IDxcResult* result, DXC_OUT_KIND kind, out ComPtr<IDxcBlobUtf8> data, out ComPtr<IDxcBlobUtf16> name)
        {
            if (result->HasOutput(kind) == Windows.TRUE)
            {
                fixed (ComPtr<IDxcBlobUtf8>* pData = &data)
                fixed (ComPtr<IDxcBlobUtf16>* pName = &name)
                {
                    Guard.ThrowIfFailed(result->GetOutput(
                        kind,
                        pData->Guid,
                        ComPtr.GetVoidAddressOf(pData),
                        ComPtr.GetAddressOf(pName)
                    ));

                    return true;
                }
            }

            data = default;
            name = default;

            return false;
        }
    }
}