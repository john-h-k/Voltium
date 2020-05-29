using System;
using System.Globalization;
using Voltium.Common;

namespace Voltium.ModelLoader
{
    internal static class ParseUtils
    {
        public static ReadOnlySpan<char> ParseSingleArgument(ReadOnlySpan<char> args, string name)
        {
            // slightly strange way of checking if we have more than one arg
            if (args.IndexOf(' ') != args.LastIndexOf(' '))
            {
                ThrowHelper.ThrowArgumentException($"'{name}' line had multiple arguments '{args.ToString()}', when only one is valid");
            }

            if (args.IndexOf(' ') == -1)
            {
                ThrowHelper.ThrowArgumentException($"'{name}' line had no arguments, when only one is valid");
            }

            return args.Slice(args.IndexOf(' '));
        }

        private static void ParseTo3(ReadOnlySpan<char> args, out ReadOnlySpan<char> d0, out ReadOnlySpan<char> d1, out ReadOnlySpan<char> d2)
        {
            var firstDiv = args.IndexOf(' ');

            d0 = args.Slice(0, firstDiv);
            var gb = args.Slice(firstDiv);

            var secondDiv = gb.IndexOf(' ');
            d1 = gb.Slice(0, secondDiv);
            d2 = gb.Slice(secondDiv);
        }


        public static Double3 ParseDouble3(ReadOnlySpan<char> args)
        {
            ParseTo3(args, out var d0, out var d1, out var d2);
            return new Double3 { X = ParseDouble(d0), Y = ParseDouble(d1), Z = ParseDouble(d2) };
        }

        public static Rgb ParseRgbColor(ReadOnlySpan<char> args)
        {
            ParseTo3(args, out var r, out var g, out var b);

            return new Rgb { R = ParseDouble(r), G = ParseDouble(g), B = ParseDouble(b) };
        }

        public static double ParseDouble(ReadOnlySpan<char> args)
            => double.Parse(args, NumberStyles.Float | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite);
    }
}
