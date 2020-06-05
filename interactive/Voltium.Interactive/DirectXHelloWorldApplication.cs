using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Managers;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.D3D12_INPUT_CLASSIFICATION;
using Voltium.Core.Configuration.Graphics;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.Interactive
{
    internal class DirectXHelloWorldApplication : Application
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new DefaultRenderer();

        private GraphicsDevice _device = null!;

        [MemberNotNull(nameof(_device))]
        public override unsafe void Init(ScreenData data)
        {
            var config = new GraphicalConfiguration
            {
                ForceFullscreenAsWindowed = false,
                ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER.DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED,
                VSyncCount = 0,
                BackBufferFormat = DXGI_FORMAT_R8G8B8A8_UNORM,
                DepthStencilFormat = DXGI_FORMAT_D32_FLOAT,
                FullscreenScalingStrategy = DXGI_MODE_SCALING.DXGI_MODE_SCALING_UNSPECIFIED,
                MultiSamplingStrategy = new MsaaDesc(1, 0),
                RequiredDirect3DLevel = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                ScalingStrategy = DXGI_SCALING.DXGI_SCALING_NONE,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD,
                SwapChainBufferCount = 3
            };

            _device = GraphicsDevice.Create(config, data);

            _renderer.Init(_device, config, data);
            _renderer.Resize(data);
        }

        public override void Update(ApplicationTimer timer)
        {
            _renderer.Update(timer);
        }
        public override unsafe void Render()
        {
            using var commandList = GpuDispatchManager.Manager.BeginGraphicsContext(_renderer.GetInitialPso());

            commandList.SetViewportAndScissor(_device.ScreenData);
            _renderer.Render(commandList);

            GpuDispatchManager.Manager.End(commandList.Move());

            _device.Present();
        }

        public override void Destroy()
        {
            _device.Dispose();
        }

        public override void OnResize(ScreenData newScreenData)
        {
            _device.Resize(newScreenData);
            _renderer.Resize(newScreenData);
        }

        public override void OnKeyDown(byte key)
        {

        }

        public override void OnKeyUp(byte key)
        {

        }

        public override void OnMouseScroll(int scroll)
        {
            _renderer.OnMouseScroll(scroll);
        }
    }
}
