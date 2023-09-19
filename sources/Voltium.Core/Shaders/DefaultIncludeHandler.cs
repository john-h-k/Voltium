using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.DirectX;
using Voltium.Common;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.ERROR;
using static TerraFX.Interop.Windows.Windows;

namespace Voltium.Core.Devices
{
    internal unsafe struct IncludeHandler
    {
        public string ShaderDirectory { get; set; }
        public string AppDirectory { get; set; }

        private string? _lastDirectory;

        public int LoadSource(string filename, out string text)
        {
            text = null!;

            if (filename == null || Helpers.IsNullOut(out text))
            {
                return E_POINTER;
            }

            try
            {
                if (!TryGetFile(filename, out var file))
                {
                    return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                }

                text = File.ReadAllText(file.FullName);

                return S_OK;
            }
            catch (Exception e)
            {
                return e is FileNotFoundException ? HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) : E_FAIL;
            }
        }

        public int LoadSource(string filename, out byte[] text)
        {
            text = null!;

            if (filename == null || Helpers.IsNullOut(out text))
            {
                return E_POINTER;
            }

            try
            {
                if (!TryGetFile(filename, out var file))
                {
                    return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
                }

                text = File.ReadAllBytes(file.FullName);

                return S_OK;
            }
            catch (Exception e)
            {
                return e is FileNotFoundException ? HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) : E_FAIL;
            }
        }


        private bool TryGetFile(string filename, [NotNullWhen(true)] out FileInfo? file)
        {
            var shaderLocalPath = Path.Combine(ShaderDirectory, filename);
            var appLocalPath = Path.Combine(AppDirectory, filename);

            if (File.Exists(shaderLocalPath))
            {
                file = new FileInfo(shaderLocalPath);
            }
            else if (File.Exists(appLocalPath))
            {
                file = new FileInfo(appLocalPath);
            }
            else if (_lastDirectory is not null && Path.Combine(_lastDirectory, filename) is var lastPath && File.Exists(lastPath))
            {
                file = new FileInfo(lastPath);
            }
            else
            {
                file = default;
                return false;
            }

            _lastDirectory = file.DirectoryName;

            return true;
        }

        internal int LoadSource(ushort* pFilename, IDxcBlob** ppIncludeSource)
        {
            throw new NotImplementedException();
        }
    }
}
