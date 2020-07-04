namespace Voltium.ModelLoading
{
    //internal sealed class ObjLoader
    //{
    //    internal struct Face
    //    {
    //        public VertexDataIndices[] Vertices;
    //    }

    //    internal struct VertexDataIndices
    //    {
    //        public int? VertexIndex;
    //        public int? VertexTexIndex;
    //        public int? VertexNormIndex;
    //    }

    //    internal sealed class Builder
    //    {
    //        public string Name;
    //        public List<Double3> _vertices;
    //        public List<Double3> _vertexTex;
    //        public List<Double3> _vertexNorms;

    //        public List<VertexDataIndices[]> _faces;
    //    }

    //    public static void Load(ReadOnlyMemory<char> data)
    //    {
    //        var reader = new SequenceReader<char>(new ReadOnlySequence<char>(data));

    //        while (!reader.End)
    //        {
    //            _ = reader.AdvancePast(' '); // skip leading whitespace

    //            if (!reader.TryReadTo(out ReadOnlySpan<char> marker, ' ', advancePastDelimiter: true))
    //            {
    //                continue;
    //            }

    //            _ = reader.TryReadTo(out ReadOnlySpan<char> args, '\n', advancePastDelimiter: true);
    //            args = args.Trim();

    //            ParseObjLine(marker, args);
    //        }
    //    }

    //    private static void ParseObjLine(ReadOnlySpan<char> marker, ReadOnlySpan<char> args)
    //    {
    //        switch (marker.ToString() /* TODO remove */)
    //        {
    //            case "#": // comment
    //                break;
    //            case "v":
    //                ParseVertex(args);
    //                break;
    //            case "vn":
    //                ParseVertexNormal(args);
    //                break;
    //            case "vt":
    //                ParseVertexTex(args);
    //                break;
    //            case "f":
    //                ParseFace(args);
    //                break;
    //            case "o":
    //                ParseName(args);
    //                break;
    //            case "s":
    //                ParseSmoothingGroup(args);
    //                break;
    //            case "mtllib":
    //                ParseMtlLib(args);
    //                break;
    //            case "usemtl":
    //                ParseUseMtl(args);
    //                break;
    //            default:
    //                ThrowHelper.ThrowNotSupportedException($"Unsupported obj element '{marker.ToString()}'");
    //                break;
    //        }
    //    }

    //    private static Dictionary<string, MaterialProperties> _materials = new();
    //    private static MaterialProperties _currentMaterial;
    //    private static void ParseMtlLib(ReadOnlySpan<char> args)
    //    {
    //        if (args.IndexOf(' ') == -1)
    //        {
    //            ThrowHelper.ThrowArgumentException($"'mtllib' line had no arguments, when at least one is required");
    //        }

    //        int ind;
    //        while ((ind = args.IndexOf(' ')) != -1)
    //        {
    //            args = args.Slice(ind);
    //            MtlLoader.Load(args.ToString().AsMemory(), _materials);
    //        }
    //    }

    //    private static void ParseUseMtl(ReadOnlySpan<char> args)
    //    {
    //        _currentMaterial = _materials[ParseUtils.ParseSingleArgument(args, "usemtl").ToString()];
    //    }
    //}
}
