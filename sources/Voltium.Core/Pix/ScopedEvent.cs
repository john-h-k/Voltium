using System;
using System.Diagnostics;
using TerraFX.Interop;

namespace Voltium.Common.Pix
{
#pragma warning disable
    public unsafe static partial class ScopedEventExtensions
    {
        internal struct CtorDummy
        {
        }

        private static readonly CtorDummy NoContext = default;

        public readonly unsafe partial struct ScopedEvent : IDisposable
        {
            private readonly void* _context;
            private readonly ContextType _type;


            internal ScopedEvent(CtorDummy dummy)
            {
                _context = null;
                _type = ContextType.None;
            }

            internal ScopedEvent(ID3D12CommandQueue* context)
            {
                _context = context;
                _type = ContextType.Queue;
            }

            internal ScopedEvent(ID3D12GraphicsCommandList* context)
            {
                _context = context;
                _type = ContextType.List;
            }

            private enum ContextType
            {
                None = 1, // by doing this we get defaulted or uninit'd structs to hit the default as that is likely a _bug
                List,
                Queue
            }

            [Conditional("VERIFY")]
            private static void VerifyCom(void* p, Guid iid)
            {
#if DEBUG
                IUnknown* _;
                Debug.Assert(Windows.SUCCEEDED(((IUnknown*)p)->QueryInterface(&iid, (void**)&_)));
                _->Release();
#endif
            }

            [Conditional("DEBUG")]
            [Conditional("USE_PIX")]
            public void EndEvent()
            {
                Debug.Assert(_context != null || _type == ContextType.None);
                if (_type == ContextType.List)
                {
                    VerifyCom(_context, Windows.IID_ID3D12GraphicsCommandList);
                    PIXMethods.EndEvent((ID3D12GraphicsCommandList*)_context);
                }
                else if (_type == ContextType.Queue)
                {
                    VerifyCom(_context, Windows.IID_ID3D12CommandQueue);
                    PIXMethods.EndEvent((ID3D12CommandQueue*)_context);
                }
                else if (_type != ContextType.None)
                {
                    Debug.Fail("damn bro this ain't good");
                }
            }

            public void Dispose() => EndEvent();
        }
    }
}
