
using TerraFX.Interop;

namespace Voltium.Core.Devices
{
    public enum DeviceFeature
    {
        Raytracing,
        InlineRaytracing,
        VariableRateShading,
        ExtendedVariableRateShading,
        CopyQueueTimestamps
    }

    public struct RenderPassDesc
    {
        
    }

    public enum LoadOperation
    {
        Discard
#if D3D12
             = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
#else
            = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_DONT_CARE,
#endif
        Clear
#if D3D12
             = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
#else
            = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_CLEAR,
#endif
        Preserve
#if D3D12
             = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
#else
            = VkAttachmentLoadOp.VK_ATTACHMENT_LOAD_OP_LOAD,
#endif
    }
    public enum StoreOperation
    {

    }

    public struct DepthStencilRenderPassDesc
    {

    }
}
