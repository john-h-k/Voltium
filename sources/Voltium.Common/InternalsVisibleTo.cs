using System.Runtime.CompilerServices;
using Voltium.Common;

[assembly: InternalsVisibleTo("Voltium.Core" + PublicKey.KeyDefinition)]
[assembly: InternalsVisibleTo("Voltium.Allocators" + PublicKey.KeyDefinition)]
[assembly: InternalsVisibleTo("Voltium.TextureLoading" + PublicKey.KeyDefinition)]
[assembly: InternalsVisibleTo("Voltium.PIX" + PublicKey.KeyDefinition)]
[assembly: InternalsVisibleTo("Voltium.ModelLoading" + PublicKey.KeyDefinition)]
[module: SkipLocalsInit]

namespace Voltium.Common
{
    internal static class PublicKey
    {
        public const string KeyDefinition = "";// "PublicKey=" + Key;

        public const string Key = "";
            // "00240000048000009400000006020000002400005253413100040000010001007930f8e8a41652e9" +
            // "c8d9c721dedd2f47dc521eb85c9712da9ad2fc2addac27637ba299222eea82878236f4b08756777a" +
            // "d8c324303a614d04cbc212d8d2ba4ea1b5647069d63467ff250df08b07f32ed2dec6fb09de2db325" +
            // "7b47aad43a73227584aed721764ceb4d5784b35e7a6b829f6ca266cb5607a24d068bdbdf5e6b91cf";
    }
}
