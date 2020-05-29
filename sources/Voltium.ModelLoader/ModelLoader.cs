using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ObjLoader;
using ObjLoader.Loader.Loaders;
using TerraFX.Interop;

namespace Voltium.ModelLoader
{
    /// <summary>
    /// The type used for loading of 2 and 3D models
    /// </summary>
    public static class ModelLoader
    {
        private static IObjLoader _loader = new ObjLoaderFactory().Create();

        internal static void Load(string filename)
        {
            var loadResult = _loader.Load(File.OpenRead(filename));
        }
    }
}
