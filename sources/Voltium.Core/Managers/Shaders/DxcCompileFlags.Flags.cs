using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;



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

namespace Voltium.Core.Managers
{
    public static partial class DxcCompileFlags
    {
        /// <summary>
        /// Display available options
        /// </summary>
        public static Flag DisplayAvailableOptions { get; } = new Flag("-help");
        /// <summary>
        /// Suppress copyright message
        /// </summary>
        public static Flag Nologo { get; } = new Flag("-nologo");
        /// <summary>
        /// Don't emit warning for unused driver arguments
        /// </summary>
        public static Flag DontWarnForUnusedArguments { get; } = new Flag("-Qunused-arguments");
        /// <summary>
        /// Enables agressive flattening
        /// </summary>
        public static Flag AllResourcesBound { get; } = new Flag("-all-resources-bound");
        /// <summary>
        /// Set auto binding space - enables auto resource binding in libraries
        /// </summary>

        public static Flag AutoBindingSpace(object value) => new Flag($"-auto-binding-space {value}");
        /// <summary>
        /// Output color coded assembly listings
        /// </summary>
        public static Flag OutputColorCodedAssemblyListings { get; } = new Flag("-Cc");
        /// <summary>
        /// Set default linkage for non-shader functions when compiling or linking to a library target(internal, external)
        /// </summary>

        public static Flag DefaultLinkage(string value) => new Flag($"-default-linkage {value}");

        /// <summary>
        /// select denormal value options(any, preserve, ftz). any is the default.
        /// </summary>
        public static Flag Denorm(ShaderDenormBehaviour value) => new Flag($"-denorm {value.ToDxcArg()}");
        /// <summary>
        /// Define macro
        /// </summary>

        public static Flag DefineMacro(string value) => new Flag($"-D {value}");

        /// <summary>
        /// Define macro
        /// </summary>
        public static Flag DefineMacro(string name, string value) => new Flag($"-D {name}={value}");

        /// <summary>
        /// Enable 16bit types and disable min precision types.Available in HLSL 2018 and shader model 6.2
        /// </summary>
        public static Flag Enable16BitTypes { get; } = new Flag("-enable-16bit-types");
        /// <summary>
        /// Set default encoding for text outputs(utf8|utf16) default=utf8
        /// </summary>

        public static Flag Encoding(OutputEncoding value) => new Flag($"-encoding {value.ToDxcArg()}");
        /// <summary>
        /// Only export shaders when compiling a library
        /// </summary>
        public static Flag ExportShadersOnly { get; } = new Flag("-export-shaders-only");
        /// <summary>
        /// Specify exports when compiling a library: export1[[, export1_clone, ...]=internal_name][;...]
        /// </summary>

        public static Flag Exports(string value) => new Flag($"-exports {value}");
        /// <summary>
        /// Entry point name
        /// </summary>

        public static Flag OutputAssemblyCodeListingFile(string file) => new Flag($"-Fc {file}");
        /// <summary>
        /// Print option name with mappable diagnostics
        /// </summary>
        public static Flag FdiagnosticsShowOption { get; } = new Flag("-fdiagnostics-show-option");
        /// <summary>
        /// Write debug information to the given file, or automatically named file in directory when ending in ''
        /// </summary>

        public static Flag WriteDebugInformationToFile(string file = "/") => new Flag($"-Fd {file}");
        /// <summary>
        /// Output warnings and errors to the given file
        /// </summary>

        public static Flag OutputWarningsAndErrorsToFile(string file) => new Flag($"-Fe {file}");
        /// <summary>
        /// Output header file containing object code
        /// </summary>

        public static Flag OutputHeaderFileContainingObjectCode(string file) => new Flag($"-Fh {file}");
        /// <summary>
        /// Expand the operands before performing token-pasting operation(fxc behavior)
        /// </summary>
        public static Flag FlegacyMacroExpansion { get; } = new Flag("-flegacy-macro-expansion");
        /// <summary>
        /// Reserve unused explicit register assignments for compatibility with shader model 5.0 and below
        /// </summary>
        public static Flag FlegacyResourceReservation { get; } = new Flag("-flegacy-resource-reservation");
        /// <summary>
        /// Do not print option name with mappable diagnostics
        /// </summary>
        public static Flag FnoDiagnosticsShowOption { get; } = new Flag("-fno-diagnostics-show-option");
        /// <summary>
        /// force root signature version (rootsig_1_1 if omitted)
        /// </summary>
        // should probs custom enum this
        public static Flag ForceRootsigVer(D3D_ROOT_SIGNATURE_VERSION profile) => new Flag($"-force-rootsig-ver {profile.ToString().Replace(nameof(D3D_ROOT_SIGNATURE_VERSION), "rootsig")}");
        /// <summary>
        /// Output object file
        /// </summary>

        public static Flag OutputObjectFile(string file) => new Flag($"-Fo {file}");
        /// <summary>
        /// Output reflection to the given file
        /// </summary>

        public static Flag OutputReflectionToFile(string file) => new Flag($"-Fre {file}");
        /// <summary>
        /// Output root signature to the given file
        /// </summary>

        public static Flag OutputRootSignatureToFile(string file) => new Flag($"-Frs {file}");
        /// <summary>
        /// Output shader hash to the given file
        /// </summary>

        public static Flag OutputShaderHashToFile(string file) => new Flag($"-Fsh {file}");
        /// <summary>
        /// Enable backward compatibility mode
        /// </summary>
        public static Flag EnableBackwardCompatibilityMode { get; } = new Flag("-Gec");
        /// <summary>
        /// Enable strict mode
        /// </summary>
        public static Flag EnableStrictMode { get; } = new Flag("-Ges");
        /// <summary>
        /// Avoid flow control constructs
        /// </summary>
        public static Flag AvoidFlowControlConstructs { get; } = new Flag("-Gfa");
        /// <summary>
        /// Prefer flow control constructs
        /// </summary>
        public static Flag PreferFlowControlConstructs { get; } = new Flag("-Gfp");
        /// <summary>
        /// Force IEEE strictness
        /// </summary>
        public static Flag ForceIeeeStrictness { get; } = new Flag("-Gis");
        /// <summary>
        /// HLSL version(2016, 2017, 2018). Default is 2018
        /// </summary>

        public static Flag HlslVersion(string value = "2018") => new Flag($"-HV {value}");
        /// <summary>
        /// Show header includes and nesting depth
        /// </summary>
        public static Flag ShowHeaderIncludesAndNestingDepth { get; } = new Flag("-H");
        /// <summary>
        /// Ignore line directives
        /// </summary>
        public static Flag IgnoreLineDirectives { get; } = new Flag("-ignore-line-directives");
        /// <summary>
        /// Add directory to include search path
        /// </summary>

        public static Flag AddDirectoryToIncludeSearchPath(string value) => new Flag($"-I {value}");
        /// <summary>
        /// Output hexadecimal literals
        /// </summary>
        public static Flag OutputHexadecimalLiterals { get; } = new Flag("-Lx");
        /// <summary>
        /// Output instruction numbers in assembly listings
        /// </summary>
        public static Flag OutputInstructionNumbersInAssemblyListings { get; } = new Flag("-Ni");
        /// <summary>
        /// Do not use legacy cbuffer load
        /// </summary>
        public static Flag NoLegacyCbufLayout { get; } = new Flag("-no-legacy-cbuf-layout");
        /// <summary>
        /// Suppress warnings
        /// </summary>
        public static Flag NoWarnings { get; } = new Flag("-no-warnings");
        /// <summary>
        /// Output instruction byte offsets in assembly listings
        /// </summary>
        public static Flag OutputInstructionByteOffsetsInAssemblyListings { get; } = new Flag("-No");
        /// <summary>
        /// Print the optimizer commands.
        /// </summary>
        public static Flag Dump { get; } = new Flag("-Odump");
        /// <summary>
        /// Disable optimizations
        /// </summary>
        public static Flag DisableOptimizations { get; } = new Flag("-Od");
        /// <summary>
        /// Optimize signature packing assuming identical signature provided for each connecting stage
        /// </summary>
        public static Flag PackOptimized { get; } = new Flag("-pack-optimized");
        /// <summary>
        /// Pack signatures preserving prefix-stable property - appended elements will not disturb placement of prior elements
        /// </summary>
        public static Flag PackPrefixStable { get; } = new Flag("-pack-prefix-stable");
        /// <summary>
        /// recompile from DXIL container with Debug Info or Debug Info bitcode file
        /// </summary>
        public static Flag Recompile { get; } = new Flag("-recompile");
        /// <summary>
        /// Assume that UAVs/SRVs may alias
        /// </summary>
        public static Flag ResMayAlias { get; } = new Flag("-res-may-alias");

        /// <summary>
        /// Read root signature from a
        /// </summary>
        public static Flag RootsigDefine(string value) => new Flag($"-rootsig-define {value}");

        /// <summary>
        /// Disable validation
        /// </summary>
        public static Flag DisableValidation { get; } = new Flag("-Vd");
        /// <summary>
        /// Display details about the include process.
        /// </summary>
        public static Flag DisplayDetailsAboutTheIncludeProcess { get; } = new Flag("-Vi");
        /// <summary>
        /// Use name as variable name in header file
        /// </summary>

        public static Flag UseNameAsVariableNameInHeaderFile(string name) => new Flag($"-Vn {name}");
        /// <summary>
        /// Treat warnings as errors
        /// </summary>
        public static Flag TreatWarningsAsErrors { get; } = new Flag("-WX");
        /// <summary>
        /// Enable debug information
        /// </summary>
        public static Flag EnableDebugInformation { get; } = new Flag("-Zi");
        /// <summary>
        /// Pack matrices in column-major order
        /// </summary>
        public static Flag PackMatricesInColumnMajorOrder { get; } = new Flag("-Zpc");
        /// <summary>
        /// Pack matrices in row-major order
        /// </summary>
        public static Flag PackMatricesInRowMajorOrder { get; } = new Flag("-Zpr");
        /// <summary>
        /// Compute Shader Hash considering only output binary
        /// </summary>
        public static Flag ComputeShaderHashConsideringOnlyOutputBinary { get; } = new Flag("-Zsb");
        /// <summary>
        /// Compute Shader Hash considering source information
        /// </summary>
        public static Flag ComputeShaderHashConsideringSourceInformation { get; } = new Flag("-Zss");

        /// <summary>
        /// Move uniform parameters from entry point to global scope
        /// </summary>
        public static Flag ExtractEntryUniforms { get; } = new Flag("-extract-entry-uniforms");
        /// <summary>
        /// Set extern on non-static globals
        /// </summary>
        public static Flag GlobalExternByDefault { get; } = new Flag("-global-extern-by-default");
        /// <summary>
        /// Write out user defines after rewritten HLSL
        /// </summary>
        public static Flag KeepUserMacro { get; } = new Flag("-keep-user-macro");
        /// <summary>
        /// Remove unused static globals and functions
        /// </summary>
        public static Flag RemoveUnusedGlobals { get; } = new Flag("-remove-unused-globals");
        /// <summary>
        /// Translate function definitions to declarations
        /// </summary>
        public static Flag SkipFnBody { get; } = new Flag("-skip-fn-body");
        /// <summary>
        /// Remove static functions and globals when used with -skip-fn-body
        /// </summary>
        public static Flag SkipStatic { get; } = new Flag("-skip-static");
        /// <summary>
        /// Rewrite HLSL, without changes.
        /// </summary>
        public static Flag Unchanged { get; } = new Flag("-unchanged");
        /// <summary>
        /// Specify whitelist of debug info category (file -  source -  line, tool)
        /// </summary>

        public static Flag FspvDebug(string value) => new Flag($"-fspv-debug {value}");
        /// <summary>
        /// Specify SPIR-V extension permitted to use
        /// </summary>

        public static Flag FspvExtension(string value) => new Flag($"-fspv-extension {value}");
        /// <summary>
        /// Flatten arrays of resources so each array element takes one binding number
        /// </summary>
        public static Flag FspvFlattenResourceArrays { get; } = new Flag("-fspv-flatten-resource-arrays");
        /// <summary>
        /// Emit additional SPIR-V instructions to aid reflection
        /// </summary>
        public static Flag FspvReflect { get; } = new Flag("-fspv-reflect");
        /// <summary>
        /// Specify the target environment: vulkan1.0 (default) or vulkan1.1
        /// </summary>

        public static Flag FspvTargetEnv(string value) => new Flag($"-fspv-target-env {value}");
        /// <summary>
        /// Specify Vulkan binding number shift for b-type register
        /// </summary>

        public static Flag FvkBShift(string shift, string space) => new Flag($"-fvk-b-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number and set number for the $Globals cbuffer
        /// </summary>

        public static Flag FvkBindGlobals(string binding, string set) => new Flag($"-fvk-bind-globals {binding} {set}");
        /// <summary>
        /// Specify Vulkan descriptor set and binding for a specific register
        /// </summary>

        public static Flag FvkBindRegister(string typeNumber, string space, string binding, string set) => new Flag($"-fvk-bind-register {typeNumber} {space} {binding} {set}");
        /// <summary>
        /// Negate SV_Position.y before writing to stage output in VS/DS/GS to accommodate Vulkan's coordinate system
        /// </summary>
        public static Flag FvkInvertY { get; } = new Flag("-fvk-invert-y");
        /// <summary>
        /// Specify Vulkan binding number shift for s-type register
        /// </summary>

        public static Flag FvkSShift(string shift, string space) => new Flag($"-fvk-s-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number shift for t-type register
        /// </summary>

        public static Flag FvkTShift(string shift, string space) => new Flag($"-fvk-t-shift {shift} {space}");
        /// <summary>
        /// Specify Vulkan binding number shift for u-type register
        /// </summary>

        public static Flag FvkUShift(string shift, string space) => new Flag($"-fvk-u-shift {shift} {space}");
        /// <summary>
        /// Use DirectX memory layout for Vulkan resources
        /// </summary>
        public static Flag FvkUseDxLayout { get; } = new Flag("-fvk-use-dx-layout");
        /// <summary>
        /// Reciprocate SV_Position.w after reading from stage input in PS to accommodate the difference between Vulkan and DirectX
        /// </summary>
        public static Flag FvkUseDxPositionW { get; } = new Flag("-fvk-use-dx-position-w");
        /// <summary>
        /// Use strict OpenGL std140/std430 memory layout for Vulkan resources
        /// </summary>
        public static Flag FvkUseGlLayout { get; } = new Flag("-fvk-use-gl-layout");
        /// <summary>
        /// Use scalar memory layout for Vulkan resources
        /// </summary>
        public static Flag FvkUseScalarLayout { get; } = new Flag("-fvk-use-scalar-layout");

        /// <summary>
        /// Specify a comma-separated list of SPIRV-Tools passes to customize optimization configuration(see http:// khr.io/hlsl2spirv#optimization)
        /// </summary>
        public static Flag Config(object value) => new Flag($"-Oconfig {value}");
        /// <summary>
        /// Generate SPIR-V code
        /// </summary>
        public static Flag Spirv { get; } = new Flag("-spirv");
        /// <summary>
        /// Load a binary file rather than compiling
        /// </summary>
        public static Flag Dumpbin { get; } = new Flag("-dumpbin");
        /// <summary>
        /// Extract root signature from shader bytecode (must be used with /Fo{file})
        /// </summary>
        public static Flag ExtractRootSignature { get; } = new Flag("-extractrootsignature");

        /// <summary>
        /// Save private data from shader blob
        /// </summary>

        public static Flag SavePrivateDataToFile(string file) => new Flag($"-getprivate {file}");

        /// <summary>
        /// Preprocess to file(must be used alone)
        /// </summary>

        public static Flag PreprocessToFile(string file) => new Flag($"-P {file}");
        /// <summary>
        /// Embed PDB in shader container(must be used with /Zi)
        /// </summary>
        public static Flag EmbedDebug { get; } = new Flag("-Qembed_debug");
        /// <summary>
        /// Strip debug information from 4_0+ shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static Flag StripDebug { get; } = new Flag("-Qstrip_debug");
        /// <summary>
        /// Strip private data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static Flag StripPriv { get; } = new Flag("-Qstrip_priv");
        /// <summary>
        /// Strip reflection data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static Flag StripReflect { get; } = new Flag("-Qstrip_reflect");
        /// <summary>
        /// Strip root signature data from shader bytecode(must be used with /Fo{file})
        /// </summary>
        public static Flag StripRootsignature { get; } = new Flag("-Qstrip_rootsignature");
        /// <summary>
        /// Private data to add to compiled shader blob
        /// </summary>

        public static Flag Setprivate(object file) => new Flag($"-setprivate {file}");
        /// <summary>
        /// Attach root signature to shader bytecode
        /// </summary>

        public static Flag Setrootsignature(object file) => new Flag($"-setrootsignature {file}");
        /// <summary>
        /// Verify shader bytecode with root signature
        /// </summary>

        public static Flag Verifyrootsignature(object file) => new Flag($"-verifyrootsignature {file}");

        /// <summary>
        /// Enable the specified warning
        /// </summary>
        public static Flag EnableWarning(int value) => new Flag($"-W{value}");


        /// <summary>
        /// Disable the specified warning
        /// </summary>
        public static Flag DisableWarning(int value) => new Flag($"-Wno{value}");
    }
}
