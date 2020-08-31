using System;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Annotations;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Text
{
    [NativeComType]
    internal unsafe partial struct DWriteTextRenderer
    {
        public nuint Vtbl;

        private uint _refCount;

        private DWRITE_MATRIX _transform;
        private GraphicsDevice _device;
        private ComPtr<IDWriteFactory> _factory;
        private float _pixelsPerDip;
        private bool _isSnappingDisabled;

        public DWriteTextRenderer(GraphicsDevice device, in Texture renderTarget, bool isSnappingDisabled) : this()
        {
            _device = device;
            _factory = default;
            _pixelsPerDip = default;
            _isSnappingDisabled = isSnappingDisabled;
        }

        [NativeComMethod]
        public int QueryInterface(Guid* riid, void** ppvObject)
        {
            if (Helpers.IsGuidEqual(riid, _factory.Iid))
            {
                *ppvObject = _factory.Copy().Ptr;
                return S_OK;
            }

            return E_NOINTERFACE;
        }

        [NativeComMethod]
        public uint AddRef()
        {
            return Interlocked.Increment(ref _refCount);
        }

        [NativeComMethod]
        public uint Release()
        {
            var count = Interlocked.Decrement(ref _refCount);

            if (count == 0)
            {
                // delete this
            }

            return count;
        }

        [NativeComMethod]
        public int IsPixelSnappingDisabled(void* clientDrawingContext, int* isDisabled)
        {
            if (isDisabled is null)
            {
                return E_POINTER;
            }

            *isDisabled = Helpers.BoolToInt32(_isSnappingDisabled);
            return S_OK;
        }

        [NativeComMethod]
        public int GetCurrentTransform(void* clientDrawingContext, DWRITE_MATRIX* transform)
        {
            if (transform is null)
            {
                return E_POINTER;
            }

            *transform = _transform;

            return S_OK;
        }

        [NativeComMethod]
        public int GetPixelsPerDip(void* clientDrawingContext, float* pixelsPerDip)
        {
            if (pixelsPerDip is null)
            {
                return E_POINTER;
            }

            *pixelsPerDip = _pixelsPerDip;

            return S_OK;
        }

        [NativeComMethod]
        public int DrawGlyphRun(void* clientDrawingContext, float baselineOriginX, float baselineOriginY, DWRITE_MEASURING_MODE measuringMode, DWRITE_GLYPH_RUN* glyphRun, DWRITE_GLYPH_RUN_DESCRIPTION* glyphRunDescription, IUnknown* clientDrawingEffect)
        {
            if (glyphRun is null || glyphRunDescription is null)
            {
                return E_POINTER;
            }

            ref GraphicsContext context = ref Unsafe.AsRef<GraphicsContext>(clientDrawingContext);

            using ComPtr<IDWriteGlyphRunAnalysis> analysis = default;
            fixed (DWRITE_MATRIX* pTransform = &_transform)
            {
                _device.ThrowIfFailed(_factory.Ptr->CreateGlyphRunAnalysis(
                    glyphRun,
                    _pixelsPerDip,
                    pTransform,
                    DWRITE_RENDERING_MODE.DWRITE_RENDERING_MODE_ALIASED,
                    measuringMode,
                    baselineOriginX,
                    baselineOriginY,
                    ComPtr.GetAddressOf(&analysis)
                ));
            }

            var type = DWRITE_TEXTURE_TYPE.DWRITE_TEXTURE_ALIASED_1x1;

            RECT fullTex;
            _device.ThrowIfFailed(analysis.Ptr->GetAlphaTextureBounds(type, &fullTex));

            var size = GetSizeForTexture(type, &fullTex);
            using var buff = RentedArray<byte>.Create(size);
            fixed (byte* pBuff = buff.Value)
            {
                _device.ThrowIfFailed(analysis.Ptr->CreateAlphaTexture(type, &fullTex, pBuff, (uint)size));
            }

            return S_OK;
        }

        private static int GetSizeForTexture(DWRITE_TEXTURE_TYPE type, RECT* pRect)
        {
            if (type == DWRITE_TEXTURE_TYPE.DWRITE_TEXTURE_ALIASED_1x1)
            {
                return (pRect->top - pRect->bottom) * (pRect->right - pRect->left);
            }
            if (type == DWRITE_TEXTURE_TYPE.DWRITE_TEXTURE_ALIASED_1x1)
            {
                // 3 bytes horiz
                return (pRect->top - pRect->bottom) * (3 * (pRect->right - pRect->left));
            }
            return -1;
        }

        [NativeComMethod]
        public int DrawUnderline(void* clientDrawingContext, float baselineOriginX, float baselineOriginY, DWRITE_UNDERLINE* underline, IUnknown* clientDrawingEffect)
        {
            return E_NOTIMPL;
        }

        [NativeComMethod]
        public int DrawStrikethrough(void* clientDrawingContext, float baselineOriginX, float baselineOriginY, DWRITE_STRIKETHROUGH* strikethrough, IUnknown* clientDrawingEffect)
        {
            return E_NOTIMPL;
        }
    }
}
