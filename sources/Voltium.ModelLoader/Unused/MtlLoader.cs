namespace Voltium.ModelLoading
{
    //internal sealed class MtlLoader
    //{
    //    public static void Load(ReadOnlyMemory<char> data, Dictionary<string, MaterialProperties> materials)
    //    {
    //        var reader = new SequenceReader<char>(new ReadOnlySequence<char>(data));

    //        MaterialProperties mat = default;

    //        while (!reader.End)
    //        {
    //            reader.AdvancePast(' '); // skip leading whitespace

    //            if (!reader.TryReadTo(out ReadOnlySpan<char> marker, ' ', advancePastDelimiter: true))
    //            {
    //                continue;
    //            }

    //            _ = reader.TryReadTo(out ReadOnlySpan<char> args, '\n', advancePastDelimiter: true);
    //            args = args.Trim();

    //            if (marker == "newmtl")
    //            {
    //                materials[mat.Name!] = mat;
    //                string name = ParseUtils.ParseSingleArgument(args, "newmtl").ToString();
    //                mat = new MaterialProperties(name);
    //            }

    //            ParseMtlLine(ref mat, marker, args);
    //        }
    //    }

    //    private static void ParseMtlLine(ref MaterialProperties mat, ReadOnlySpan<char> marker, ReadOnlySpan<char> args)
    //    {
    //        switch (marker.ToString() /* TODO remove */)
    //        {
    //            case "Ns":
    //                mat.SpecularHighlights = ParseUtils.ParseDouble(args);
    //                break;
    //            case "Ka":
    //                mat.AmbientColor = ParseUtils.ParseRgbColor(args);
    //                break;
    //            case "Kd":
    //                mat.DiffuseColor = ParseUtils.ParseRgbColor(args);
    //                break;
    //            case "Ks":
    //                mat.SpecularColor = ParseUtils.ParseRgbColor(args);
    //                break;
    //            case "Ke":
    //                mat.EmissiveCoefficient = ParseUtils.ParseRgbColor(args);
    //                break;
    //            case "Ni":
    //                mat.OpticalDensity = ParseUtils.ParseDouble(args);
    //                break;
    //            case "d":
    //                mat.Dissolve = ParseUtils.ParseDouble(args);
    //                break;
    //            case "illum":
    //                mat.IlluminationMode = ParseIllumination(args);
    //                break;
    //            default:
    //                ThrowHelper.ThrowNotSupportedException($"Unsupported obj element '{marker.ToString()}'");
    //                break;
    //        }
    //    }

    //    private static IlluminationMode ParseIllumination(ReadOnlySpan<char> args)
    //    {
    //        var illum = (IlluminationMode)int.Parse(args);

    //        if (illum <= IlluminationMode.LoInvalid || illum >= IlluminationMode.HiInvalid)
    //        {
    //            ThrowHelper.ThrowInvalidDataException($"Illumination value '{(int)illum}' was not valid");
    //        }

    //        return illum;
    //    }
    //}
}
