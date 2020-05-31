
/// <summary>
/// Display available options
/// </summary>
		public static Flag DisplayAvailableOptions { get; } = "-help";
/// <summary>
/// Suppress copyright message
/// </summary>
		public static Flag Nologo { get; } = "-nologo";
/// <summary>
/// Don't emit warning for unused driver arguments
/// </summary>
		public static Flag QunusedArguments { get; } = "-Qunused-arguments";
/// <summary>
/// Enables agressive flattening
/// </summary>
		public static Flag AllResourcesBound { get; } = "-all-resources-bound";
/// <summary>
/// Set auto binding space - enables auto resource binding in libraries
/// </summary>

		public static Flag AutoBindingSpace(object value) => $"-auto-binding-space=\"{value}\"";
/// <summary>
/// Output color coded assembly listings
/// </summary>
		public static Flag OutputColorCodedAssemblyListings { get; } = "-Cc";
/// <summary>
/// Set default linkage for non-shader functions when compiling or linking to a library target(internal, external)
/// </summary>

		public static Flag DefaultLinkage(object value) => $"-default-linkage=\"{value}\"";
/// <summary>
/// select denormal value options(any, preserve, ftz). any is the default.
/// </summary>

		public static Flag Denorm(object value) => $"-denorm=\"{value}\"";
/// <summary>
/// Define macro
/// </summary>

		public static Flag DefineMacro(object value) => $"-D=\"{value}\"";
/// <summary>
/// Enable 16bit types and disable min precision types.Available in HLSL 2018 and shader model 6.2
/// </summary>
		public static Flag Enable16BitTypes { get; } = "-enable-16bit-types";
/// <summary>
/// Set default encoding for text outputs(utf8|utf16) default=utf8
/// </summary>

		public static Flag Encoding(object value) => $"-encoding=\"{value}\"";
/// <summary>
/// Only export shaders when compiling a library
/// </summary>
		public static Flag ExportShadersOnly { get; } = "-export-shaders-only";
/// <summary>
/// Specify exports when compiling a library: export1[[, export1_clone, ...]=internal_name][;...]
/// </summary>

		public static Flag Exports(object value) => $"-exports=\"{value}\"";
/// <summary>
/// Entry point name
/// </summary>

		public static Flag EntryPointName(object value) => $"-E=\"{value}\"";
/// <summary>
/// Output assembly code listing file
/// </summary>

		public static Flag OutputAssemblyCodeListingFile(object file) => $"-Fc=\"{file}\"";
/// <summary>
/// Print option name with mappable diagnostics
/// </summary>
		public static Flag FdiagnosticsShowOption { get; } = "-fdiagnostics-show-option";
/// <summary>
/// Write debug information to the given file, or automatically named file in directory when ending in ''
/// </summary>

		public static Flag WriteDebugInformationToTheGivenFile,OrAutomaticallyNamedFileInDirectoryWhenEndingIn''(object file) => $"-Fd=\"{file}\"";
/// <summary>
/// Output warnings and errors to the given file
/// </summary>

		public static Flag OutputWarningsAndErrorsToTheGivenFile(object file) => $"-Fe=\"{file}\"";
/// <summary>
/// Output header file containing object code
/// </summary>

		public static Flag OutputHeaderFileContainingObjectCode(object file) => $"-Fh=\"{file}\"";
/// <summary>
/// Expand the operands before performing token-pasting operation(fxc behavior)
/// </summary>
		public static Flag FlegacyMacroExpansion { get; } = "-flegacy-macro-expansion";
/// <summary>
/// Reserve unused explicit register assignments for compatibility with shader model 5.0 and below
/// </summary>
		public static Flag FlegacyResourceReservation { get; } = "-flegacy-resource-reservation";
/// <summary>
/// Do not print option name with mappable diagnostics
/// </summary>
		public static Flag FnoDiagnosticsShowOption { get; } = "-fno-diagnostics-show-option";
/// <summary>
/// force root signature version (rootsig_1_1 if omitted)
/// </summary>

		public static Flag ForceRootsigVer(object profile) => $"-force-rootsig-ver=\"{profile}\"";
/// <summary>
/// Output object file
/// </summary>

		public static Flag OutputObjectFile(object file) => $"-Fo=\"{file}\"";
/// <summary>
/// Output reflection to the given file
/// </summary>

		public static Flag OutputReflectionToTheGivenFile(object file) => $"-Fre=\"{file}\"";
/// <summary>
/// Output root signature to the given file
/// </summary>

		public static Flag OutputRootSignatureToTheGivenFile(object file) => $"-Frs=\"{file}\"";
/// <summary>
/// Output shader hash to the given file
/// </summary>

		public static Flag OutputShaderHashToTheGivenFile(object file) => $"-Fsh=\"{file}\"";
/// <summary>
/// Enable backward compatibility mode
/// </summary>
		public static Flag EnableBackwardCompatibilityMode { get; } = "-Gec";
/// <summary>
/// Enable strict mode
/// </summary>
		public static Flag EnableStrictMode { get; } = "-Ges";
/// <summary>
/// Avoid flow control constructs
/// </summary>
		public static Flag AvoidFlowControlConstructs { get; } = "-Gfa";
/// <summary>
/// Prefer flow control constructs
/// </summary>
		public static Flag PreferFlowControlConstructs { get; } = "-Gfp";
/// <summary>
/// Force IEEE strictness
/// </summary>
		public static Flag ForceIeeeStrictness { get; } = "-Gis";
/// <summary>
/// HLSL version(2016, 2017, 2018). Default is 2018
/// </summary>

		public static Flag HlslVersion.DefaultIs2018(object value) => $"-HV=\"{value}\"";
/// <summary>
/// Show header includes and nesting depth
/// </summary>
		public static Flag ShowHeaderIncludesAndNestingDepth { get; } = "-H";
/// <summary>
/// Ignore line directives
/// </summary>
		public static Flag IgnoreLineDirectives { get; } = "-ignore-line-directives";
/// <summary>
/// Add directory to include search path
/// </summary>

		public static Flag AddDirectoryToIncludeSearchPath(object value) => $"-I=\"{value}\"";
/// <summary>
/// Output hexadecimal literals
/// </summary>
		public static Flag OutputHexadecimalLiterals { get; } = "-Lx";
/// <summary>
/// Output instruction numbers in assembly listings
/// </summary>
		public static Flag OutputInstructionNumbersInAssemblyListings { get; } = "-Ni";
/// <summary>
/// Do not use legacy cbuffer load
/// </summary>
		public static Flag NoLegacyCbufLayout { get; } = "-no-legacy-cbuf-layout";
/// <summary>
/// Suppress warnings
/// </summary>
		public static Flag NoWarnings { get; } = "-no-warnings";
/// <summary>
/// Output instruction byte offsets in assembly listings
/// </summary>
		public static Flag OutputInstructionByteOffsetsInAssemblyListings { get; } = "-No";
/// <summary>
/// Print the optimizer commands.
/// </summary>
		public static Flag Odump { get; } = "-Odump";
/// <summary>
/// Disable optimizations
/// </summary>
		public static Flag DisableOptimizations { get; } = "-Od";
/// <summary>
/// Optimize signature packing assuming identical signature provided for each connecting stage
/// </summary>
		public static Flag PackOptimized { get; } = "-pack-optimized";
/// <summary>
/// Pack signatures preserving prefix-stable property - appended elements will not disturb placement of prior elements
/// </summary>
		public static Flag PackPrefixStable { get; } = "-pack-prefix-stable";
/// <summary>
/// recompile from DXIL container with Debug Info or Debug Info bitcode file
/// </summary>
		public static Flag Recompile { get; } = "-recompile";
/// <summary>
/// Assume that UAVs/SRVs may alias
/// </summary>
		public static Flag ResMayAlias { get; } = "-res-may-alias";
/// <summary>
/// Read root signature from a
/// </summary>

		public static Flag RootsigDefine(object value) => $"-rootsig-define=\"{value}\"";
/// <summary>
/// Set target profile.
/// </summary>

		public static Flag SetTargetProfile(object profile) => $"-T=\"{profile}\"";
/// <summary>
/// Disable validation
/// </summary>
		public static Flag DisableValidation { get; } = "-Vd";
/// <summary>
/// Display details about the include process.
/// </summary>
		public static Flag DisplayDetailsAboutTheIncludeProcess { get; } = "-Vi";
/// <summary>
/// Use  name  as variable name in header file
/// </summary>

		public static Flag UseNameAsVariableNameInHeaderFile(object name, object name) => $"-Vn=\"{name}\"";
/// <summary>
/// Treat warnings as errors
/// </summary>
		public static Flag TreatWarningsAsErrors { get; } = "-WX";
/// <summary>
/// Enable debug information
/// </summary>
		public static Flag EnableDebugInformation { get; } = "-Zi";
/// <summary>
/// Pack matrices in column-major order
/// </summary>
		public static Flag PackMatricesInColumnMajorOrder { get; } = "-Zpc";
/// <summary>
/// Pack matrices in row-major order
/// </summary>
		public static Flag PackMatricesInRowMajorOrder { get; } = "-Zpr";
/// <summary>
/// Compute Shader Hash considering only output binary
/// </summary>
		public static Flag ComputeShaderHashConsideringOnlyOutputBinary { get; } = "-Zsb";
/// <summary>
/// Compute Shader Hash considering source information
/// </summary>
		public static Flag ComputeShaderHashConsideringSourceInformation { get; } = "-Zss";
/// <summary>
/// Optimization Level 0
/// </summary>
		public static Flag OptimizationLevel0 { get; } = "-O0";
/// <summary>
/// Optimization Level 1
/// </summary>
		public static Flag OptimizationLevel1 { get; } = "-O1";
/// <summary>
/// Optimization Level 2
/// </summary>
		public static Flag OptimizationLevel2 { get; } = "-O2";
/// <summary>
/// Optimization Level 3 (Default)
/// </summary>
		public static Flag OptimizationLevel3 { get; } = "-O3";
/// <summary>
/// Move uniform parameters from entry point to global scope
/// </summary>
		public static Flag ExtractEntryUniforms { get; } = "-extract-entry-uniforms";
/// <summary>
/// Set extern on non-static globals
/// </summary>
		public static Flag GlobalExternByDefault { get; } = "-global-extern-by-default";
/// <summary>
/// Write out user defines after rewritten HLSL
/// </summary>
		public static Flag KeepUserMacro { get; } = "-keep-user-macro";
/// <summary>
/// Remove unused static globals and functions
/// </summary>
		public static Flag RemoveUnusedGlobals { get; } = "-remove-unused-globals";
/// <summary>
/// Translate function definitions to declarations
/// </summary>
		public static Flag SkipFnBody { get; } = "-skip-fn-body";
/// <summary>
/// Remove static functions and globals when used with -skip-fn-body
/// </summary>
		public static Flag SkipStatic { get; } = "-skip-static";
/// <summary>
/// Rewrite HLSL, without changes.
/// </summary>
		public static Flag Unchanged { get; } = "-unchanged";
/// <summary>
/// Specify whitelist of debug info category (file -  source -  line, tool)
/// </summary>

		public static Flag FspvDebug(object value) => $"-fspv-debug=\"{value}\"";
/// <summary>
/// Specify SPIR-V extension permitted to use
/// </summary>

		public static Flag FspvExtension(object value) => $"-fspv-extension=\"{value}\"";
/// <summary>
/// Flatten arrays of resources so each array element takes one binding number
/// </summary>
		public static Flag FspvFlattenResourceArrays { get; } = "-fspv-flatten-resource-arrays";
/// <summary>
/// Emit additional SPIR-V instructions to aid reflection
/// </summary>
		public static Flag FspvReflect { get; } = "-fspv-reflect";
/// <summary>
/// Specify the target environment: vulkan1.0 (default) or vulkan1.1
/// </summary>

		public static Flag FspvTargetEnv(object value) => $"-fspv-target-env=\"{value}\"";
/// <summary>
/// Specify Vulkan binding number shift for b-type register
/// </summary>

		public static Flag FvkBShift(object shift, object space) => $"-fvk-b-shift=\"{shift}\"";
/// <summary>
/// Specify Vulkan binding number and set number for the $Globals cbuffer
/// </summary>

		public static Flag FvkBindGlobals(object binding, object set) => $"-fvk-bind-globals=\"{binding}\"";
/// <summary>
/// Specify Vulkan descriptor set and binding for a specific register
/// </summary>

		public static Flag FvkBindRegister(object type-number, object space, object binding, object set) => $"-fvk-bind-register=\"{type-number}\"";
/// <summary>
/// Negate SV_Position.y before writing to stage output in VS/DS/GS to accommodate Vulkan's coordinate system
/// </summary>
		public static Flag FvkInvertY { get; } = "-fvk-invert-y";
/// <summary>
/// Specify Vulkan binding number shift for s-type register
/// </summary>

		public static Flag FvkSShift(object shift, object space) => $"-fvk-s-shift=\"{shift}\"";
/// <summary>
/// Specify Vulkan binding number shift for t-type register
/// </summary>

		public static Flag FvkTShift(object shift, object space) => $"-fvk-t-shift=\"{shift}\"";
/// <summary>
/// Specify Vulkan binding number shift for u-type register
/// </summary>

		public static Flag FvkUShift(object shift, object space) => $"-fvk-u-shift=\"{shift}\"";
/// <summary>
/// Use DirectX memory layout for Vulkan resources
/// </summary>
		public static Flag FvkUseDxLayout { get; } = "-fvk-use-dx-layout";
/// <summary>
/// Reciprocate SV_Position.w after reading from stage input in PS to accommodate the difference between Vulkan and DirectX
/// </summary>
		public static Flag FvkUseDxPositionW { get; } = "-fvk-use-dx-position-w";
/// <summary>
/// Use strict OpenGL std140/std430 memory layout for Vulkan resources
/// </summary>
		public static Flag FvkUseGlLayout { get; } = "-fvk-use-gl-layout";
/// <summary>
/// Use scalar memory layout for Vulkan resources
/// </summary>
		public static Flag FvkUseScalarLayout { get; } = "-fvk-use-scalar-layout";
/// <summary>
///        Specify a comma-separated list of SPIRV-Tools passes to customize optimization configuration(see http:// khr.io/hlsl2spirv#optimization)
/// </summary>

		public static Flag Oconfig(object value) => $"-Oconfig=\"{value}\"";
/// <summary>
/// Generate SPIR-V code
/// </summary>
		public static Flag Spirv { get; } = "-spirv";
/// <summary>
/// Load a binary file rather than compiling
/// </summary>
		public static Flag Dumpbin { get; } = "-dumpbin";
/// <summary>
/// Extract root signature from shader bytecode (must be used with /Fo<file>)
/// </summary>
		public static Flag Extractrootsignature { get; } = "-extractrootsignature";
/// <summary>
/// Save private data from shader blob
/// </summary>

		public static Flag Getprivate(object file) => $"-getprivate=\"{file}\"";
/// <summary>
/// Preprocess to file(must be used alone)
/// </summary>

		public static Flag PreprocessToFile(object value) => $"-P=\"{value}\"";
/// <summary>
/// Embed PDB in shader container(must be used with /Zi)
/// </summary>
		public static Flag Qembed_Debug { get; } = "-Qembed_debug";
/// <summary>
/// Strip debug information from 4_0+ shader bytecode(must be used with /Fo<file>)
/// </summary>
		public static Flag Qstrip_Debug { get; } = "-Qstrip_debug";
/// <summary>
/// Strip private data from shader bytecode(must be used with /Fo<file>)
/// </summary>
		public static Flag Qstrip_Priv { get; } = "-Qstrip_priv";
/// <summary>
/// Strip reflection data from shader bytecode(must be used with /Fo<file>)
/// </summary>
		public static Flag Qstrip_Reflect { get; } = "-Qstrip_reflect";
/// <summary>
/// Strip root signature data from shader bytecode(must be used with /Fo<file>)
/// </summary>
		public static Flag Qstrip_Rootsignature { get; } = "-Qstrip_rootsignature";
/// <summary>
/// Private data to add to compiled shader blob
/// </summary>

		public static Flag Setprivate(object file) => $"-setprivate=\"{file}\"";
/// <summary>
/// Attach root signature to shader bytecode
/// </summary>

		public static Flag Setrootsignature(object file) => $"-setrootsignature=\"{file}\"";
/// <summary>
/// Verify shader bytecode with root signature
/// </summary>

		public static Flag Verifyrootsignature(object file) => $"-verifyrootsignature=\"{file}\"";
/// <summary>
/// -]<warning> Enable/Disable the specified warning
/// </summary>
		public static Flag ]<Warning>Enable/DisableTheSpecifiedWarning { get; } = "-W[no";