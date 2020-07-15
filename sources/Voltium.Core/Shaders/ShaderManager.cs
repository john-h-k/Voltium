using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices.Shaders;
using ZLogger;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
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
        private static DxcIncludeHandler DefaultDxcIncludeHandler;
        private static FxcIncludeHandler DefaultFxcIncludeHandler;
        private static ComPtr<IDxcUtils> Utils;

        static unsafe ShaderManager()
        {
            ComPtr<IDxcCompiler3> compiler = default;
            ComPtr<IDxcUtils> utils = default;

            Guid clsid = CLSID_DxcCompiler;
            Guard.ThrowIfFailed(DxcCreateInstance(&clsid, compiler.Iid, ComPtr.GetVoidAddressOf(&compiler)));
            clsid = CLSID_DxcUtils;
            Guard.ThrowIfFailed(DxcCreateInstance(&clsid, utils.Iid, ComPtr.GetVoidAddressOf(&utils)));

            Compiler = compiler.Move();
            Utils = utils.Move();

            DefaultDxcIncludeHandler = new DxcIncludeHandler();
            DefaultDxcIncludeHandler.Init(Utils.Copy());

            DefaultFxcIncludeHandler = new FxcIncludeHandler();
            DefaultFxcIncludeHandler.Init();
        }

        internal const uint DXC_CP_UTF8 = 65001;
        internal const uint DXC_CP_UTF16 = 1200;
        internal const uint DXC_CP_AP = 0;

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a file
        /// </summary>
        /// <param name="filename">The filename containing the shader</param>
        /// <param name="target">The <see cref="ShaderModel"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            string filename,
            ShaderModel target,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16
        )
            => CompileShader(filename, File.OpenText(filename), target, flags, entrypoint, encoding, new FileInfo(filename).DirectoryName!);

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a file
        /// </summary>
        /// <param name="filename">The filename containing the shader</param>
        /// <param name="type">The <see cref="ShaderType"/> of the shader</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            string filename,
            ShaderType type,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16
        )
            => CompileShader(filename, ShaderModel.LatestVersion(type), flags, entrypoint, encoding);

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="stream">The <see cref="StreamReader"/> containing the shader text</param>
        /// <param name="type">The <see cref="ShaderType"/> of the shader</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <param name="shaderDir">Optionally, the directory to use when including shaders</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            StreamReader stream,
            ShaderType type,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16,
            string shaderDir = ""
        )
            => CompileShader(name, stream, ShaderModel.LatestVersion(type), flags, entrypoint, encoding, shaderDir);

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="stream">The <see cref="StreamReader"/> containing the shader text</param>
        /// <param name="target">The <see cref="ShaderModel"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <param name="shaderDir">Optionally, the directory to use when including shaders</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            StreamReader stream,
            ShaderModel target,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16,
            string shaderDir = ""
        )
        {
            var size = stream.BaseStream.Length;

            if (size > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("Shaders cannot exceed 2^31 bytes");
            }

            var buff = new char[(int)size];

            stream.Read(buff);

            return CompileShader(name, buff, target, flags, entrypoint, encoding, shaderDir);
        }

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="shaderText">The <see cref="ReadOnlySpan{T}"/> containing the shader text</param>
        /// <param name="type">The <see cref="ShaderType"/> of the shader</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <param name="shaderDir">Optionally, the directory to use when including shaders</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public unsafe static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> shaderText,
            ShaderType type,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16,
            string shaderDir = ""
        )
            => CompileShader(name, shaderText, type, flags, entrypoint, encoding, shaderDir);

        /// <summary>
        /// Compiles a new <see cref="CompiledShader"/> from a name and a <see cref="Stream"/>
        /// </summary>
        /// <param name="name">The name of the shader, for metadata</param>
        /// <param name="shaderText">The <see cref="ReadOnlySpan{T}"/> containing the shader text</param>
        /// <param name="target">The <see cref="ShaderModel"/> the shader targets</param>
        /// <param name="flags">An array of <see cref="ShaderCompileFlag"/> to pass to the compiler</param>
        /// <param name="entrypoint">The entrypoint to the shader, if it is not a <see cref="ShaderType.Library"/>,
        /// or 'main' by default</param>
        /// <param name="encoding">The <see cref="OutputEncoding"/> for any textual output</param>
        /// <param name="shaderDir">Optionally, the directory to use when including shaders</param>
        /// <returns>A new <see cref="CompiledShader"/></returns>
        public unsafe static CompiledShader CompileShader(
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> shaderText,
            ShaderModel target,
            ShaderCompileFlag[] flags = null!,
            ReadOnlySpan<char> entrypoint = default,
            OutputEncoding encoding = OutputEncoding.Utf16,
            string shaderDir = ""
        )
        {
            if (target.Type == ShaderType.Library && !entrypoint.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException("Shader libraries cannot have an entrypoint");
            }

            DefaultDxcIncludeHandler.ShaderDirectory = shaderDir;
            DefaultFxcIncludeHandler.ShaderDirectory = shaderDir;

            if (!target.IsDxil)
            {
                // Legacy FXC pipeline
                return FxcCompile(name, shaderText, target, flags, entrypoint, encoding, shaderDir);
            }

            flags ??= Array.Empty<ShaderCompileFlag>();

            // ok i am irrationally scared of the marshaller so for some reason i marshalled this by hand
            // we need to pass a 'wchar**' to 'IDxcCompiler3', where it is an array of strings, and each string
            // correspends to a flag or a flag arg. e.g '-Od' (disable opts), or, say you have the flag '-E Foo', we
            // pass that as '-E', 'Foo'. Or '-fvk-bind-register 1 2 3 4', is '-fvk-bind-register', '1', '2', '3', '4'

            // the Flag type handles a bunch of the work, by making the string value of flag seperated by nulls instead of spaces
            // our job is to then provide the pointers to these strings
            // we allocate space for all the strings and the pointers in one array, for efficiency
            // then the first sizeof(ptr) * numStrings bytes of it are used to store the pointers to the actual strings

            // biggest target possible (although invalid) would be "-T lib_255_255", 14 chars (15 with null char)
            // entrypoint is 0 if empty, else "-E " + length of entrypoint + null char
            // encoding is either 14 or 15 chars (UTF8 or UTF16) + null char
            Debug.Assert(encoding is OutputEncoding.Utf8 or OutputEncoding.Utf16);
            var encodingLength = (encoding == OutputEncoding.Utf16 ? 5 + 1 : 4 + 1) + 9 + 1;
            var targetLength = 14 + 1;
            var entryPointLength = entrypoint.IsEmpty ? 0 : entrypoint.Length + 3 + 1;
            int prefixLength = (encodingLength + targetLength + entryPointLength) * sizeof(char);

            // space for all the flag strings (and their null chars) + the actual pointers to these strings
            int flagPointerLength = 0;
            int flagLength = 0;
            foreach (var flag in flags)
            {
                flagLength += (flag.Value.Length + /* null char */ 1) * sizeof(char);
                flagPointerLength += (1 + flag.ArgCount) * sizeof(nuint);
            }

            // 4 for target + encoding (as each are flag + arg), and 2 extra for entrypoint if present
            flagPointerLength += (entrypoint.IsEmpty ? 4 : 6) * sizeof(nuint);

            // rent as short term usage TODO: POH pool
            using var rentedFlagBuff = RentedArray<byte>.Create(flagPointerLength + flagLength + prefixLength);

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

                var targetPrefix = ShaderModel.ShaderNameMap[target.Type].AsSpan();
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

            // encoding
            {
                // pointer to '-encoding '
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff));
                flagPointerBuff = flagPointerBuff.Slice(1);

                // pointer to value
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff.Slice(10)));
                flagPointerBuff = flagPointerBuff.Slice(1);

                flagBuff[0] = '-';
                flagBuff[1] = 'e';
                flagBuff[2] = 'n';
                flagBuff[3] = 'c';
                flagBuff[4] = 'o';
                flagBuff[5] = 'd';
                flagBuff[6] = 'i';
                flagBuff[7] = 'n';
                flagBuff[8] = 'g';
                flagBuff[9] = '\0';

                flagBuff = flagBuff.Slice(10);

                var encodingStr = encoding == OutputEncoding.Utf16 ? "utf16" : "utf8";
                encodingStr.AsSpan().CopyTo(flagBuff);
                flagBuff[encodingStr.Length] = '\0';

                flagBuff = flagBuff.Slice(encodingStr.Length + 1);
            }

            foreach (var flag in flags)
            {
                // flag ctor handles nulls for us

                // we know it is pinned. this is where the string is written
                flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff));
                flagPointerBuff = flagPointerBuff.Slice(1);

                var len = flag.Value.Length;

                var argPtr = flag.Value.AsSpan();
                var argIndex = argPtr.IndexOf('\0');
                argPtr = argPtr.Slice(argIndex + 1);

                for (var i = 0; i < flag.ArgCount; i++)
                {
                    flagPointerBuff[0] = (nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(flagBuff.Slice(argIndex + 1)));
                    flagPointerBuff = flagPointerBuff.Slice(1);
                    argIndex = argPtr.IndexOf('\0');
                    argPtr = argPtr.Slice(argIndex + 1);
                }

                flag.Value.AsSpan().CopyTo(flagBuff);

                flagBuff = flagBuff.Slice(len);
            }

            fixed (char* pText = shaderText)
            fixed (byte* ppFlags = rentedFlagBuff.Value)
            fixed (IDxcIncludeHandler* pInclude = DefaultDxcIncludeHandler)
            {
                DxcBuffer text;
                text.Ptr = pText;
                text.Size = (nuint)(shaderText.Length * sizeof(char));
                text.Encoding = DXC_CP_UTF16;

                using ComPtr<IDxcResult> compileResult = default;
                Guard.ThrowIfFailed(Compiler.Get()->Compile(
                    &text,
                    (ushort**)ppFlags,
                    (uint)(flagPointerLength / sizeof(nuint)),
                    pInclude,
                    compileResult.Iid,
                    ComPtr.GetVoidAddressOf(&compileResult)
                ));

                int statusHr;
                Guard.ThrowIfFailed(compileResult.Get()->GetStatus(&statusHr));

                if (FAILED(statusHr))
                {
                    var result = TryGetOutput(compileResult.Get(), DXC_OUT_KIND.DXC_OUT_ERRORS, out var errors, out var errorName);
                    Debug.Assert(result);
                    _ = result; // prevent release warning

                    using (errors)
                    using (errorName)
                    {
                        var data = new ShaderCompilationData
                        {
                            Filename = name,
                            Errors = AsString(errors.Get()),
                            Other = AsString(errorName.Get())
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

        private static unsafe CompiledShader FxcCompile(ReadOnlySpan<char> name, ReadOnlySpan<char> shaderText, ShaderModel target, ShaderCompileFlag[] flags, ReadOnlySpan<char> entrypoint, OutputEncoding encoding, string shaderDir)
        {
            // far less optimised because only a very small subset of DX12 drivers support DXBC (compiled with FXC) but not DXIL (compiled with DXC)
            // fuck you john tur

            int Count(int len) => Encoding.ASCII.GetMaxByteCount(len);

            if (entrypoint.IsEmpty)
            {
                // DXC assumes 'main' entrypoint, FXC doens't seem to
                entrypoint = "main";
            }

            var targetStr = target.ToString();

            // + 1 for null char
            var textLen = Count(shaderText.Length) + 1;
            var nameLen = Count(name.Length) + 1;
            var entrypointLen = Count(entrypoint.Length) + 1;
            var targetLen = Count(targetStr.Length) + 1;

            using var strBuff = RentedArray<byte>.Create(textLen + targetLen + nameLen + entrypointLen);

            int nameOffset = Encoding.ASCII.GetBytes(shaderText, strBuff.Value);
            strBuff.Value[nameOffset++] = 0;

            int targetOffset = nameOffset + Encoding.ASCII.GetBytes(name, strBuff.Value.AsSpan(nameOffset));
            strBuff.Value[targetOffset++] = 0;

            int entrypointOffset = targetOffset + Encoding.ASCII.GetBytes(targetStr, strBuff.Value.AsSpan(targetOffset));
            strBuff.Value[entrypointOffset++] = 0;

            int endOffset = entrypointOffset + Encoding.ASCII.GetBytes(entrypoint, strBuff.Value.AsSpan(entrypointOffset));
            strBuff.Value[endOffset++] = 0;

            var fxcFlags = GetFxcFlags(flags, out var macros);

            ID3DBlob* pCode;
            ID3DBlob* pError;

            fixed (byte* pSrcData = strBuff.Value)
            fixed (D3D_SHADER_MACRO* pDefines = macros)
            fixed (ID3DInclude* pInclude = DefaultFxcIncludeHandler)
            {
                int hr = D3DCompile2(
                    pSrcData,
                    (uint)shaderText.Length,
                    (sbyte*)(pSrcData + nameOffset),
                    pDefines,
                    pInclude,
                    (sbyte*)(pSrcData + entrypointOffset),
                    (sbyte*)(pSrcData + targetOffset),
                    Flags1: fxcFlags,
                    Flags2: 0, // effects flags, unused
                    SecondaryDataFlags: 0,
                    pSecondaryData: null,
                    SecondaryDataSize: 0,
                    &pCode,
                    &pError
                );

                if (FAILED(hr))
                {
                    var data = new ShaderCompilationData
                    {
                        Filename = name,
                        Errors = AsString(pError)
                    };
                    throw new ShaderCompilationException(data);
                }

                if (pError != null)
                {
                    _ = pError->Release();
                }

                var shaderBytes = new ReadOnlySpan<byte>(pCode->GetBufferPointer(), (int)pCode->GetBufferSize());

                return new CompiledShader(shaderBytes.ToArray(), target.Type);
            }
        }

        private static readonly Dictionary<ShaderCompileFlag, uint> FxcFlagMap = new()
        {
            [ShaderCompileFlag.EnableDebugInformation] = D3DCOMPILE_DEBUG,
            [ShaderCompileFlag.DisableValidation] = D3DCOMPILE_SKIP_VALIDATION,
            [ShaderCompileFlag.DisableOptimizations] = D3DCOMPILE_SKIP_OPTIMIZATION,
            [ShaderCompileFlag.PackMatricesInRowMajorOrder] = D3DCOMPILE_PACK_MATRIX_ROW_MAJOR,
            [ShaderCompileFlag.PackMatricesInColumnMajorOrder] = D3DCOMPILE_PACK_MATRIX_COLUMN_MAJOR,
            [ShaderCompileFlag.AvoidFlowControlConstructs] = D3DCOMPILE_AVOID_FLOW_CONTROL,
            [ShaderCompileFlag.PreferFlowControlConstructs] = D3DCOMPILE_PREFER_FLOW_CONTROL,
            [ShaderCompileFlag.OptimizationLevel0] = D3DCOMPILE_OPTIMIZATION_LEVEL0,
            [ShaderCompileFlag.OptimizationLevel1] = D3DCOMPILE_OPTIMIZATION_LEVEL1,
            [ShaderCompileFlag.OptimizationLevel2] = D3DCOMPILE_OPTIMIZATION_LEVEL2,
            [ShaderCompileFlag.OptimizationLevel3] = D3DCOMPILE_OPTIMIZATION_LEVEL3,
            [ShaderCompileFlag.TreatWarningsAsErrors] = D3DCOMPILE_WARNINGS_ARE_ERRORS,
            [ShaderCompileFlag.ResMayAlias] = D3DCOMPILE_RESOURCES_MAY_ALIAS,
            [ShaderCompileFlag.AllResourcesBound] = D3DCOMPILE_ALL_RESOURCES_BOUND
        };

        private static unsafe uint GetFxcFlags(ShaderCompileFlag[] flags, out Span<D3D_SHADER_MACRO> macros)
        {
            // 40k loc yay

            uint fxc = 0;

            int numMacros = 0;
            int macrosSize = 0;

            foreach (var flag in flags)
            {
                if (flag.IsMacro)
                {
                    numMacros++;
                    // no value macros are turned into the value '1' because FXC doesn't seem to support them
                    macrosSize += Encoding.ASCII.GetMaxByteCount(Math.Max(1, flag.Value.Length - 3 /* 3 is size of the prefix -D\0 */));
                    continue;
                }
                if (FxcFlagMap.TryGetValue(flag, out var fxcFlag))
                {
                    fxc |= fxcFlag;
                }
                else
                {
                    LogHelper.Logger.ZLogInformation(
                        "DXC Flag '{0}' skipped. This is not an error but may result in different behaviour when using legacy FXC pipeline",
                        flag
                    );
                }
            }

            // we need a dummy macro at the end to signify no-more macros
            // and we defined a macro FXC=1 to indicate compiling with FXC
            numMacros += 2;
            macrosSize += /* FXC */ 3 + /* 1 */ 1;

            var macroStructSize = numMacros * sizeof(D3D_SHADER_MACRO);
            var macroBytes = GC.AllocateUninitializedArray<byte>(macroStructSize + /* null chars*/ (numMacros * 2) + macrosSize, pinned: true);

            var macroStructs = MemoryMarshal.Cast<byte, D3D_SHADER_MACRO>(macroBytes);
            var macroData = macroBytes.AsSpan(macroStructSize);

            foreach (var flag in flags)
            {
                if (!flag.TryDeconstructMacro(out var name, out var value))
                {
                    continue;
                }

                Encoding.ASCII.GetBytes(name, macroData);
                macroData[name.Length] = 0;
                sbyte* pName = (sbyte*)Unsafe.AsPointer(ref macroData[0]);

                macroData = macroData.Slice(name.Length + 1);

                if (value.IsEmpty)
                {
                    value = "1";
                }

                Encoding.ASCII.GetBytes(value, macroData);
                macroData[value.Length] = 0;
                sbyte* pValue = (sbyte*)Unsafe.AsPointer(ref macroData[0]);

                macroData = macroData.Slice(value.Length + 1);

                macroStructs[0] = new D3D_SHADER_MACRO { Name = pName, Definition = pValue };
                macroStructs = macroStructs.Slice(1);
            }

            macroData[0] = (byte)'F';
            macroData[1] = (byte)'X';
            macroData[2] = (byte)'C';
            macroData[3] = 0;
            macroData[4] = (byte)'1';
            macroData[5] = 0;

            sbyte* pFxcMacro = (sbyte*)Unsafe.AsPointer(ref macroData[0]);

            macros = MemoryMarshal.Cast<byte, D3D_SHADER_MACRO>(macroBytes).Slice(0, numMacros);
            macros[^2] = new D3D_SHADER_MACRO { Name = pFxcMacro, Definition = pFxcMacro + 4 }; // FXC=1
            macros[^1] = default; // end

            return fxc;
        }

        private static unsafe void HandlePdb(IDxcBlob* pdb, IDxcBlobUtf16* pdbName)
        {
            using var file = File.OpenWrite(FromBlob(pdbName).ToString());
            file.Write(FromBlob(pdb));
        }


        private static unsafe ReadOnlySpan<char> AsString(ID3DBlob* blob)
            => blob == null ? null : new string((sbyte*)blob->GetBufferPointer(), 0, (int)blob->GetBufferSize());

        private static unsafe ReadOnlySpan<char> AsString(IDxcBlobUtf16* utf16)
            => utf16 == null ? null : new ReadOnlySpan<char>(utf16->GetStringPointer(), (int)utf16->GetStringLength());
        private static unsafe ReadOnlySpan<char> AsString(IDxcBlobUtf8* utf8)
            => Encoding.UTF8.GetString(new ReadOnlySpan<byte>(utf8->GetStringPointer(), (int)utf8->GetStringLength()));

        private static unsafe ReadOnlySpan<char> AsString(IDxcBlob* pBlob)
        {
            if (ComPtr.TryQueryInterface(pBlob, out IDxcBlobUtf8* utf8))
            {
                return AsString(utf8);
            }
            if (ComPtr.TryQueryInterface(pBlob, out IDxcBlobUtf16* utf16))
            {
                return AsString(utf16);
            }

            if (ComPtr.TryQueryInterface(pBlob, out IDxcBlobEncoding* _))
            {
                ThrowHelper.ThrowNotSupportedException("Unsupported encoding");
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException("Cannot decode binary data");
            }
            return null;
        }

        private static unsafe ReadOnlySpan<byte> FromBlob(IDxcBlob* pBlob)
            => new ReadOnlySpan<byte>(pBlob->GetBufferPointer(), (int)pBlob->GetBufferSize());

        private static unsafe ReadOnlySpan<char> FromBlob(IDxcBlobUtf8* pBlob)
            => Encoding.ASCII.GetString(new ReadOnlySpan<byte>(pBlob->GetStringPointer(), (int)pBlob->GetStringLength()));

        private static unsafe ReadOnlySpan<char> FromBlob(IDxcBlobUtf16* pBlob)
            => new ReadOnlySpan<char>(pBlob->GetStringPointer(), (int)pBlob->GetStringLength());

        private static unsafe bool TryGetOutput(
            IDxcResult* result,
            DXC_OUT_KIND kind,
            out ComPtr<IDxcBlob> data,
            out ComPtr<IDxcBlobUtf16> name
        )
        {
            if (result->HasOutput(kind) == Windows.TRUE)
            {
                fixed (ComPtr<IDxcBlob>* pData = &data)
                fixed (ComPtr<IDxcBlobUtf16>* pName = &name)
                {
                    Guard.ThrowIfFailed(result->GetOutput(
                        kind,
                        pData->Iid,
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
