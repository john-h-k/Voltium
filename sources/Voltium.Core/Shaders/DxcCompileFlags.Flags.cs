using TerraFX.Interop.DirectX;

//  DXC Compiler flags
// Common Options:
//   -help Display available options
//   -nologo Suppress copyright message
//   -Qunused-arguments Don't emit warning for unused driver arguments

// Compilation Options:
//   -all-resources-bound Enables agressive flattening
//   -auto-binding-space<value>
//                           Set auto binding space - enables auto resource binding in libraries
//   -Cc Output color coded assembly listings
//   -default-linkage<value>
//                           Set default linkage for non-shader functions when compiling or linking to a library target(internal, external)
//   -denorm<value> select denormal value options(any, preserve, ftz). any is the default.
//   -D<value> Define macro
//   -enable-16bit-types Enable 16bit types and disable min precision types.Available in HLSL 2018 and shader model 6.2
//   -encoding<value> Set default encoding for text outputs(utf8|utf16) default=utf8
//   -export-shaders-only Only export shaders when compiling a library
//   -exports<value> Specify exports when compiling a library: export1[[, export1_clone, ...]=internal_name][;...]
//   -E<value> Entry point name
//   -Fc<file> Output assembly code listing file
//   -fdiagnostics-show-option
//                           Print option name with mappable diagnostics
//   -Fd<file> Write debug information to the given file, or automatically named file in directory when ending in '\'
//   -Fe<file> Output warnings and errors to the given file
//   -Fh<file> Output header file containing object code
//   -flegacy-macro-expansion
//                           Expand the operands before performing token-pasting operation(fxc behavior)
//   -flegacy-resource-reservation
//                           Reserve unused explicit register assignments for compatibility with shader model 5.0 and below
//   -fno-diagnostics-show-option
//                           Do not print option name with mappable diagnostics
//   -force-rootsig-ver<profile>
//                           force root signature version (rootsig_1_1 if omitted)
//   -Fo<file> Output object file
//   -Fre<file> Output reflection to the given file
//   -Frs<file> Output root signature to the given file
//   -Fsh<file> Output shader hash to the given file
//   -Gec Enable backward compatibility mode
//   -Ges Enable strict mode
//   -Gfa Avoid flow control constructs
//   -Gfp Prefer flow control constructs
//   -Gis Force IEEE strictness
//   -HV<value> HLSL version(2016, 2017, 2018). Default is 2018
//   -H Show header includes and nesting depth
//   -ignore-line-directives Ignore line directives
//   -I<value> Add directory to include search path
//   -Lx Output hexadecimal literals
//   -Ni Output instruction numbers in assembly listings
//   -no-legacy-cbuf-layout Do not use legacy cbuffer load
//   -no-warnings Suppress warnings
//   -No Output instruction byte offsets in assembly listings
//   -Odump Print the optimizer commands.
//   -Od Disable optimizations
//   -pack-optimized Optimize signature packing assuming identical signature provided for each connecting stage
//   -pack-prefix-stable(default) Pack signatures preserving prefix-stable property - appended elements will not disturb placement of prior elements
//   -recompile recompile from DXIL container with Debug Info or Debug Info bitcode file
//   -res-may-alias Assume that UAVs/SRVs may alias
//   -rootsig-define<value> Read root signature from a
// #define
//   -T<profile> Set target profile.

// <profile>: ps_6_0, ps_6_1, ps_6_2, ps_6_3, ps_6_4, ps_6_5,
//      vs_6_0, vs_6_1, vs_6_2, vs_6_3, vs_6_4, vs_6_5,
//      gs_6_0, gs_6_1, gs_6_2, gs_6_3, gs_6_4, gs_6_5,
//      hs_6_0, hs_6_1, hs_6_2, hs_6_3, hs_6_4, hs_6_5,
//      ds_6_0, ds_6_1, ds_6_2, ds_6_3, ds_6_4, ds_6_5,
//      cs_6_0, cs_6_1, cs_6_2, cs_6_3, cs_6_4, cs_6_5,
//      lib_6_1, lib_6_2, lib_6_3, lib_6_4, lib_6_5,
//      ms_6_5,
//      as_6_5,

//   -Vd Disable validation
//   -Vi Display details about the include process.
//   -Vn<name> Use <name> as variable name in header file
//   -WX Treat warnings as errors
//   -Zi Enable debug information
//   -Zpc Pack matrices in column-major order
//   -Zpr Pack matrices in row-major order
//   -Zsb Compute Shader Hash considering only output binary
//   -Zss Compute Shader Hash considering source information

// Optimization Options:
//   -O0 Optimization Level 0
//   -O1 Optimization Level 1
//   -O2 Optimization Level 2
//   -O3 Optimization Level 3 (Default)

// Rewriter Options:
//   -extract-entry-uniforms Move uniform parameters from entry point to global scope
//   -global-extern-by-default
//                           Set extern on non-static globals
//   -keep-user-macro Write out user defines after rewritten HLSL
//   -remove-unused-globals Remove unused static globals and functions
//   -skip-fn-body Translate function definitions to declarations
//   -skip-static Remove static functions and globals when used with -skip-fn-body
//   -unchanged Rewrite HLSL, without changes.

// SPIR-V CodeGen Options:
//   -fspv-debug=< value > Specify whitelist of debug info category (file -> source -> line, tool)
//   -fspv-extension=<value> Specify SPIR-V extension permitted to use
//   -fspv-flatten-resource-arrays
//                           Flatten arrays of resources so each array element takes one binding number
//   -fspv-reflect Emit additional SPIR-V instructions to aid reflection
//   -fspv-target-env=<value>
//                           Specify the target environment: vulkan1.0 (default) or vulkan1.1
//   -fvk-b-shift<shift> <space>
//                           Specify Vulkan binding number shift for b-type register
//   -fvk-bind-globals<binding> <set>
//                           Specify Vulkan binding number and set number for the $Globals cbuffer
//   -fvk-bind-register<type-number> <space> <binding> <set>
//                           Specify Vulkan descriptor set and binding for a specific register
//   -fvk-invert-y Negate SV_Position.y before writing to stage output in VS/DS/GS to accommodate Vulkan's coordinate system
//   -fvk-s-shift<shift> <space>
//                           Specify Vulkan binding number shift for s-type register
//   -fvk-t-shift<shift> <space>
//                           Specify Vulkan binding number shift for t-type register
//   -fvk-u-shift<shift> <space>
//                           Specify Vulkan binding number shift for u-type register
//   -fvk-use-dx-layout Use DirectX memory layout for Vulkan resources
//   -fvk-use-dx-position-w Reciprocate SV_Position.w after reading from stage input in PS to accommodate the difference between Vulkan and DirectX
//   -fvk-use-gl-layout Use strict OpenGL std140/std430 memory layout for Vulkan resources
//   -fvk-use-scalar-layout Use scalar memory layout for Vulkan resources
//   -Oconfig=<value>        Specify a comma-separated list of SPIRV-Tools passes to customize optimization configuration(see http:// khr.io/hlsl2spirv#optimization)
//   -spirv Generate SPIR-V code

// Utility Options:
//   -dumpbin Load a binary file rather than compiling
//   -extractrootsignature Extract root signature from shader bytecode (must be used with /Fo<file>)
//   -getprivate<file> Save private data from shader blob
//   -P<value> Preprocess to file(must be used alone)
//   -Qembed_debug Embed PDB in shader container(must be used with /Zi)
//   -Qstrip_debug Strip debug information from 4_0+ shader bytecode(must be used with /Fo<file>)
//   -Qstrip_priv Strip private data from shader bytecode(must be used with /Fo<file>)
//   -Qstrip_reflect Strip reflection data from shader bytecode(must be used with /Fo<file>)
//   -Qstrip_rootsignature Strip root signature data from shader bytecode(must be used with /Fo<file>)
//   -setprivate<file> Private data to add to compiled shader blob
//   -setrootsignature<file>
//                         Attach root signature to shader bytecode
//   -verifyrootsignature<file>
//                         Verify shader bytecode with root signature

// Warning Options:
//   -W[no -]<warning> Enable/Disable the specified warning

// generated from root/scripts/gen_dxc_flags.py then fixed up by hand

namespace Voltium.Core.Devices
{
    public partial struct ShaderCompileFlag
    {
        /// <summary>
        /// Display available options
        /// </summary>
        public static ShaderCompileFlag DisplayAvailableOptions { get; } = new ShaderCompileFlag("-help");
        /// <summary>
        /// Suppress copyright message
        /// </summary>
        public static ShaderCompileFlag Nologo { get; } = new ShaderCompileFlag("-nologo");
        /// <summary>
        /// Don't emit warning for unused driver arguments
        /// </summary>
        public static ShaderCompileFlag DontWarnForUnusedArguments { get; } = new ShaderCompileFlag("-Qunused-arguments");
        /// <summary>
        /// Enables agressive flattening
        /// </summary>
        public static ShaderCompileFlag AllResourcesBound { get; } = new ShaderCompileFlag("-all-resources-bound");
        /// <summary>
        /// Set auto binding space - enables auto resource binding in libraries
        /// </summary>

        public static ShaderCompileFlag AutoBindingSpace(object value) => new ShaderCompileFlag($"-auto-binding-space {value}");
        /// <summary>
        /// Output color coded assembly listings
        /// </summary>
        public static ShaderCompileFlag OutputColorCodedAssemblyListings { get; } = new ShaderCompileFlag("-Cc");
        /// <summary>
        /// Set default linkage for non-shader functions when compiling or linking to a library target(internal, external)
        /// </summary>

        public static ShaderCompileFlag DefaultLinkage(string value) => new ShaderCompileFlag($"-default-linkage {value}");

        /// <summary>
        /// Select denormal value options(any, preserve, ftz). any is the default.
        /// </summary>
        public static ShaderCompileFlag Denorm(ShaderDenormBehaviour value) => new ShaderCompileFlag($"-denorm {value.ToDxcArg()}");

        /// <summary>
        /// Use optimization level 0
        /// </summary>
        public static ShaderCompileFlag OptimizationLevel0 { get; } = new ShaderCompileFlag("-O0");

        /// <summary>
        /// Use optimization level 1
        /// </summary>
        public static ShaderCompileFlag OptimizationLevel1 { get; } = new ShaderCompileFlag("-O1");

        /// <summary>
        /// Use optimization level 2
        /// </summary>
        public static ShaderCompileFlag OptimizationLevel2 { get; } = new ShaderCompileFlag("-O2");

        /// <summary>
        /// Use optimization level 3. This is the default
        /// </summary>
        public static ShaderCompileFlag OptimizationLevel3 { get; } = new ShaderCompileFlag("-O3");

        /// <summary>
        /// Define macro
        /// </summary>

        public static ShaderCompileFlag DefineMacro(string value) => new ShaderCompileFlag($"-D {value}");

        /// <summary>
        /// Define macro
        /// </summary>
        public static ShaderCompileFlag DefineMacro(string name, string value) => new ShaderCompileFlag($"-D {name}={value}");

        /// <summary>
        /// Enable 16bit types and disable min precision types.Available in HLSL 2018 and shader model 6.2
        /// </summary>
        public static ShaderCompileFlag Enable16BitTypes { get; } = new ShaderCompileFlag("-enable-16bit-types");

        //public static Flag Encoding(OutputEncoding value) => new Flag($"-encoding {value.ToDxcArg()}");
        /// <summary>
        /// Only export shaders when compiling a library
        /// </summary>
        public static ShaderCompileFlag ExportShadersOnly { get; } = new ShaderCompileFlag("-export-shaders-only");
        /// <summary>
        /// Specify exports when compiling a library: export1[[, export1_clone, ...]=internal_name][;...]
        /// </summary>

        public static ShaderCompileFlag Exports(string value) => new ShaderCompileFlag($"-exports {value}");

        /// <summary>
        /// Entry point name
        /// </summary>
        public static ShaderCompileFlag OutputAssemblyCodeListingFile(string file) => new ShaderCompileFlag($"-Fc {file}");

        /// <summary>
        /// Print option name with mappable diagnostics
        /// </summary>
        public static ShaderCompileFlag FdiagnosticsShowOption { get; } = new ShaderCompileFlag("-fdiagnostics-show-option");
        /// <summary>
        /// Write debug information to the given file, or automatically named file in directory
        /// </summary>
        public static ShaderCompileFlag WriteDebugInformationToFile(string file = "./") => new ShaderCompileFlag($"-Fd {file}");
        /// <summary>
        /// Output warnings and errors to the given file
        /// </summary>

        public static ShaderCompileFlag OutputWarningsAndErrorsToFile(string file) => new ShaderCompileFlag($"-Fe {file}");
        /// <summary>
        /// Output header file containing object code
        /// </summary>

        public static ShaderCompileFlag OutputHeaderFileContainingObjectCode(string file) => new ShaderCompileFlag($"-Fh {file}");
        /// <summary>
        /// Expand the operands before performing token-pasting operation(fxc behavior)
        /// </summary>
        public static ShaderCompileFlag FlegacyMacroExpansion { get; } = new ShaderCompileFlag("-flegacy-macro-expansion");
        /// <summary>
        /// Reserve unused explicit register assignments for compatibility with shader model 5.0 and below
        /// </summary>
        public static ShaderCompileFlag FlegacyResourceReservation { get; } = new ShaderCompileFlag("-flegacy-resource-reservation");
        /// <summary>
        /// Do not print option name with mappable diagnostics
        /// </summary>
        public static ShaderCompileFlag FnoDiagnosticsShowOption { get; } = new ShaderCompileFlag("-fno-diagnostics-show-option");
        /// <summary>
        /// force root signature version (rootsig_1_1 if omitted)
        /// </summary>
        // should probs custom enum this
        public static ShaderCompileFlag ForceRootSignatureVersion(D3D_ROOT_SIGNATURE_VERSION profile) => new ShaderCompileFlag($"-force-rootsig-ver {profile.ToString().Replace(nameof(D3D_ROOT_SIGNATURE_VERSION), "rootsig")}");
        /// <summary>
        /// Output object file
        /// </summary>

        public static ShaderCompileFlag OutputObjectFile(string file) => new ShaderCompileFlag($"-Fo {file}");
        /// <summary>
        /// Output reflection to the given file
        /// </summary>

        public static ShaderCompileFlag OutputReflectionToFile(string file) => new ShaderCompileFlag($"-Fre {file}");
        /// <summary>
        /// Output root signature to the given file
        /// </summary>

        public static ShaderCompileFlag OutputRootSignatureToFile(string file) => new ShaderCompileFlag($"-Frs {file}");
        /// <summary>
        /// Output shader hash to the given file
        /// </summary>

        public static ShaderCompileFlag OutputShaderHashToFile(string file) => new ShaderCompileFlag($"-Fsh {file}");
        /// <summary>
        /// Enable backward compatibility mode
        /// </summary>
        public static ShaderCompileFlag EnableBackwardCompatibilityMode { get; } = new ShaderCompileFlag("-Gec");
        /// <summary>
        /// Enable strict mode
        /// </summary>
        public static ShaderCompileFlag EnableStrictMode { get; } = new ShaderCompileFlag("-Ges");
        /// <summary>
        /// Avoid flow control constructs
        /// </summary>
        public static ShaderCompileFlag AvoidFlowControlConstructs { get; } = new ShaderCompileFlag("-Gfa");
        /// <summary>
        /// Prefer flow control constructs
        /// </summary>
        public static ShaderCompileFlag PreferFlowControlConstructs { get; } = new ShaderCompileFlag("-Gfp");

        /// <summary>
        /// Force IEEE strictness
        /// </summary>
        public static ShaderCompileFlag ForceIeeeStrictness { get; } = new ShaderCompileFlag("-Gis");
        /// <summary>
        /// HLSL version(2016, 2017, 2018). Default is 2018
        /// </summary>

        public static ShaderCompileFlag HlslVersion(string value = "2018") => new ShaderCompileFlag($"-HV {value}");
        /// <summary>
        /// Show header includes and nesting depth
        /// </summary>
        public static ShaderCompileFlag ShowHeaderIncludesAndNestingDepth { get; } = new ShaderCompileFlag("-H");
        /// <summary>
        /// Ignore line directives
        /// </summary>
        public static ShaderCompileFlag IgnoreLineDirectives { get; } = new ShaderCompileFlag("-ignore-line-directives");
        /// <summary>
        /// Add directory to include search path
        /// </summary>

        public static ShaderCompileFlag AddDirectoryToIncludeSearchPath(string value) => new ShaderCompileFlag($"-I {value}");
        /// <summary>
        /// Output hexadecimal literals
        /// </summary>
        public static ShaderCompileFlag OutputHexadecimalLiterals { get; } = new ShaderCompileFlag("-Lx");
        /// <summary>
        /// Output instruction numbers in assembly listings
        /// </summary>
        public static ShaderCompileFlag OutputInstructionNumbersInAssemblyListings { get; } = new ShaderCompileFlag("-Ni");
        /// <summary>
        /// Do not use legacy cbuffer load
        /// </summary>
        public static ShaderCompileFlag NoLegacyCbufLayout { get; } = new ShaderCompileFlag("-no-legacy-cbuf-layout");
        /// <summary>
        /// Suppress warnings
        /// </summary>
        public static ShaderCompileFlag NoWarnings { get; } = new ShaderCompileFlag("-no-warnings");
        /// <summary>
        /// Output instruction byte offsets in assembly listings
        /// </summary>
        public static ShaderCompileFlag OutputInstructionByteOffsetsInAssemblyListings { get; } = new ShaderCompileFlag("-No");
        /// <summary>
        /// Print the optimizer commands.
        /// </summary>
        public static ShaderCompileFlag Dump { get; } = new ShaderCompileFlag("-Odump");
        /// <summary>
        /// Disable optimizations
        /// </summary>
        public static ShaderCompileFlag DisableOptimizations { get; } = new ShaderCompileFlag("-Od");
        /// <summary>
        /// Optimize signature packing assuming identical signature provided for each connecting stage
        /// </summary>
        public static ShaderCompileFlag PackOptimized { get; } = new ShaderCompileFlag("-pack-optimized");
        /// <summary>
        /// Pack signatures preserving prefix-stable property - appended elements will not disturb placement of prior elements
        /// </summary>
        public static ShaderCompileFlag PackPrefixStable { get; } = new ShaderCompileFlag("-pack-prefix-stable");
        /// <summary>
        /// recompile from DXIL container with Debug Info or Debug Info bitcode file
        /// </summary>
        public static ShaderCompileFlag Recompile { get; } = new ShaderCompileFlag("-recompile");
        /// <summary>
        /// Assume that UAVs/SRVs may alias
        /// </summary>
        public static ShaderCompileFlag ResMayAlias { get; } = new ShaderCompileFlag("-res-may-alias");

        /// <summary>
        /// Read root signature from a #define
        /// </summary>
        public static ShaderCompileFlag RootSignatureDefine(string value) => new ShaderCompileFlag($"-rootsig-define {value}");

        /// <summary>
        /// Disable validation
        /// </summary>
        public static ShaderCompileFlag DisableValidation { get; } = new ShaderCompileFlag("-Vd");
        /// <summary>
        /// Display details about the include process.
        /// </summary>
        public static ShaderCompileFlag DisplayDetailsAboutTheIncludeProcess { get; } = new ShaderCompileFlag("-Vi");
        /// <summary>
        /// Use name as variable name in header file
        /// </summary>

        public static ShaderCompileFlag UseNameAsVariableNameInHeaderFile(string name) => new ShaderCompileFlag($"-Vn {name}");
        /// <summary>
        /// Treat warnings as errors
        /// </summary>
        public static ShaderCompileFlag TreatWarningsAsErrors { get; } = new ShaderCompileFlag("-WX");
        /// <summary>
        /// Enable debug information
        /// </summary>
        public static ShaderCompileFlag EnableDebugInformation { get; } = new ShaderCompileFlag("-Zi");
        /// <summary>
        /// Pack matrices in column-major order
        /// </summary>
        public static ShaderCompileFlag PackMatricesInColumnMajorOrder { get; } = new ShaderCompileFlag("-Zpc");
        /// <summary>
        /// Pack matrices in row-major order
        /// </summary>
        public static ShaderCompileFlag PackMatricesInRowMajorOrder { get; } = new ShaderCompileFlag("-Zpr");
        /// <summary>
        /// Compute Shader Hash considering only output binary
        /// </summary>
        public static ShaderCompileFlag ComputeShaderHashConsideringOnlyOutputBinary { get; } = new ShaderCompileFlag("-Zsb");
        /// <summary>
        /// Compute Shader Hash considering source information
        /// </summary>
        public static ShaderCompileFlag ComputeShaderHashConsideringSourceInformation { get; } = new ShaderCompileFlag("-Zss");

        /// <summary>
        /// Move uniform parameters from entry point to global scope
        /// </summary>
        public static ShaderCompileFlag ExtractEntryUniforms { get; } = new ShaderCompileFlag("-extract-entry-uniforms");
        /// <summary>
        /// Set extern on non-static globals
        /// </summary>
        public static ShaderCompileFlag GlobalExternByDefault { get; } = new ShaderCompileFlag("-global-extern-by-default");
        /// <summary>
        /// Write out user defines after rewritten HLSL
        /// </summary>
        public static ShaderCompileFlag KeepUserMacro { get; } = new ShaderCompileFlag("-keep-user-macro");
        /// <summary>
        /// Remove unused static globals and functions
        /// </summary>
        public static ShaderCompileFlag RemoveUnusedGlobals { get; } = new ShaderCompileFlag("-remove-unused-globals");
        /// <summary>
        /// Translate function definitions to declarations
        /// </summary>
        public static ShaderCompileFlag SkipFnBody { get; } = new ShaderCompileFlag("-skip-fn-body");
        /// <summary>
        /// Remove static functions and globals when used with -skip-fn-body
        /// </summary>
        public static ShaderCompileFlag SkipStatic { get; } = new ShaderCompileFlag("-skip-static");
        /// <summary>
        /// Rewrite HLSL, without changes.
        /// </summary>
        public static ShaderCompileFlag Unchanged { get; } = new ShaderCompileFlag("-unchanged");
        /// <summary>
        /// Specify whitelist of debug info category (file -  source -  line, tool)
        /// </summary>

        public static ShaderCompileFlag FspvDebug(string value) => new ShaderCompileFlag($"-fspv-debug {value}");
        /// <summary>
        /// Specify SPIR-V extension permitted to use
        /// </summary>

        public static ShaderCompileFlag FspvExtension(string value) => new ShaderCompileFlag($"-fspv-extension {value}");
        /// <summary>
        /// Flatten arrays of resources so each array element takes one binding number
        /// </summary>
        public static ShaderCompileFlag FspvFlattenResourceArrays { get; } = new ShaderCompileFlag("-fspv-flatten-resource-arrays");
        /// <summary>
        /// Emit additional SPIR-V instructions to aid reflection
        /// </summary>
        public static ShaderCompileFlag FspvReflect { get; } = new ShaderCompileFlag("-fspv-reflect");
        /// <summary>
        /// Specify the target environment: vulkan1.0 (default) or vulkan1.1
        /// </summary>

        public static ShaderCompileFlag FspvTargetEnv(string value) => new ShaderCompileFlag($"-fspv-target-env {value}");
        /// <summary>
        /// Specify Vulkan binding number shift for b-type register
        /// </summary>

        public static ShaderCompileFlag FvkBShift(string shift, string space) => new ShaderCompileFlag($"-fvk-b-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number and set number for the $Globals cbuffer
        /// </summary>

        public static ShaderCompileFlag FvkBindGlobals(string binding, string set) => new ShaderCompileFlag($"-fvk-bind-globals {binding} {set}");
        /// <summary>
        /// Specify Vulkan descriptor set and binding for a specific register
        /// </summary>

        public static ShaderCompileFlag FvkBindRegister(string typeNumber, string space, string binding, string set) => new ShaderCompileFlag($"-fvk-bind-register {typeNumber} {space} {binding} {set}");
        /// <summary>
        /// Negate SV_Position.y before writing to stage output in VS/DS/GS to accommodate Vulkan's coordinate system
        /// </summary>
        public static ShaderCompileFlag FvkInvertY { get; } = new ShaderCompileFlag("-fvk-invert-y");
        /// <summary>
        /// Specify Vulkan binding number shift for s-type register
        /// </summary>

        public static ShaderCompileFlag FvkSShift(string shift, string space) => new ShaderCompileFlag($"-fvk-s-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number shift for t-type register
        /// </summary>

        public static ShaderCompileFlag FvkTShift(string shift, string space) => new ShaderCompileFlag($"-fvk-t-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number shift for u-type register
        /// </summary>

        public static ShaderCompileFlag FvkUShift(string shift, string space) => new ShaderCompileFlag($"-fvk-u-shift {shift} {space}");
        /// <summary>
        /// Use DirectX memory layout for Vulkan resources
        /// </summary>
        public static ShaderCompileFlag FvkUseDxLayout { get; } = new ShaderCompileFlag("-fvk-use-dx-layout");
        /// <summary>
        /// Reciprocate SV_Position.w after reading from stage input in PS to accommodate the difference between Vulkan and DirectX
        /// </summary>
        public static ShaderCompileFlag FvkUseDxPositionW { get; } = new ShaderCompileFlag("-fvk-use-dx-position-w");
        /// <summary>
        /// Use strict OpenGL std140/std430 memory layout for Vulkan resources
        /// </summary>
        public static ShaderCompileFlag FvkUseGlLayout { get; } = new ShaderCompileFlag("-fvk-use-gl-layout");
        /// <summary>
        /// Use scalar memory layout for Vulkan resources
        /// </summary>
        public static ShaderCompileFlag FvkUseScalarLayout { get; } = new ShaderCompileFlag("-fvk-use-scalar-layout");

        /// <summary>
        /// Specify a comma-separated list of SPIRV-Tools passes to customize optimization configuration(see http:// khr.io/hlsl2spirv#optimization)
        /// </summary>
        public static ShaderCompileFlag Config(object value) => new ShaderCompileFlag($"-Oconfig {value}");
        /// <summary>
        /// Generate SPIR-V code
        /// </summary>
        public static ShaderCompileFlag Spirv { get; } = new ShaderCompileFlag("-spirv");
        /// <summary>
        /// Load a binary file rather than compiling
        /// </summary>
        public static ShaderCompileFlag Dumpbin { get; } = new ShaderCompileFlag("-dumpbin");
        /// <summary>
        /// Extract root signature from shader bytecode (must be used with /Fo{file})
        /// </summary>
        public static ShaderCompileFlag ExtractRootSignature { get; } = new ShaderCompileFlag("-extractrootsignature");

        /// <summary>
        /// Save private data from shader blob
        /// </summary>

        public static ShaderCompileFlag SavePrivateDataToFile(string file) => new ShaderCompileFlag($"-getprivate {file}");

        /// <summary>
        /// Preprocess to file(must be used alone)
        /// </summary>

        public static ShaderCompileFlag PreprocessToFile(string file) => new ShaderCompileFlag($"-P {file}");
        /// <summary>
        /// Embed PDB in shader container(must be used with /Zi)
        /// </summary>
        public static ShaderCompileFlag EmbedDebug { get; } = new ShaderCompileFlag("-Qembed_debug");
        /// <summary>
        /// Strip debug information from 4_0+ shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static ShaderCompileFlag StripDebug { get; } = new ShaderCompileFlag("-Qstrip_debug");
        /// <summary>
        /// Strip private data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static ShaderCompileFlag StripPriv { get; } = new ShaderCompileFlag("-Qstrip_priv");
        /// <summary>
        /// Strip reflection data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static ShaderCompileFlag StripReflect { get; } = new ShaderCompileFlag("-Qstrip_reflect");
        /// <summary>
        /// Strip root signature data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static ShaderCompileFlag StripRootsignature { get; } = new ShaderCompileFlag("-Qstrip_rootsignature");
        /// <summary>
        /// Private data to add to compiled shader blob
        /// </summary>

        public static ShaderCompileFlag Setprivate(object file) => new ShaderCompileFlag($"-setprivate {file}");
        /// <summary>
        /// Attach root signature to shader bytecode
        /// </summary>

        public static ShaderCompileFlag Setrootsignature(object file) => new ShaderCompileFlag($"-setrootsignature {file}");
        /// <summary>
        /// Verify shader bytecode with root signature
        /// </summary>

        public static ShaderCompileFlag Verifyrootsignature(object file) => new ShaderCompileFlag($"-verifyrootsignature {file}");

        /// <summary>
        /// Enable the specified warning
        /// </summary>
        public static ShaderCompileFlag EnableWarning(int value) => new ShaderCompileFlag($"-W{value}");


        /// <summary>
        /// Disable the specified warning
        /// </summary>
        public static ShaderCompileFlag DisableWarning(int value) => new ShaderCompileFlag($"-Wno{value}");
    }
}
