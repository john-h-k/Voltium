using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using static TerraFX.Interop.D3D12_AUTO_BREADCRUMB_OP;
using static TerraFX.Interop.D3D12_DRED_ALLOCATION_TYPE;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Exceptions
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public sealed class DredWalker
    {
        public Breadcrumb? FirstBreadcrumb { get; }

        public ulong? PageFaultVirtualAddress { get; }
        public Allocation? FirstRecentFree { get; }
        public Allocation? FirstRecentAllocation { get; }

        public unsafe DredWalker(DeviceDisconnectedException ex)
        {
            var device = ex.Device;
            // TODO support ID3D12DeviceRemovedExtendedData (not 1)
            if (!device.TryQueryInterface<ID3D12DeviceRemovedExtendedData1>(out var dred))
            {
                ThrowHelper.ThrowNotSupportedException("DRED is not supported on current OS");
            }

            using (dred)
            {
                D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT1 crumb;
                int hr = dred.Get()->GetAutoBreadcrumbsOutput1(&crumb);

                if (hr != DXGI_ERROR_UNSUPPORTED)
                {
                    Guard.ThrowIfFailed(hr, "dred.Get()->GetAutoBreadcrumbsOutput1(&crumb)");

                    FirstBreadcrumb = crumb.pHeadAutoBreadcrumbNode is null ? null : new Breadcrumb(crumb.pHeadAutoBreadcrumbNode);
                }

                D3D12_DRED_PAGE_FAULT_OUTPUT1 fault;
                hr = dred.Get()->GetPageFaultAllocationOutput1(&fault);

                if (hr != DXGI_ERROR_UNSUPPORTED)
                {
                    Guard.ThrowIfFailed(hr, "dred.Get()->GetAutoBreadcrumbsOutput1(&crumb)");
                    PageFaultVirtualAddress = fault.PageFaultVA;
                    FirstRecentAllocation = fault.pHeadExistingAllocationNode is null ? null : new Allocation(fault.pHeadExistingAllocationNode);
                    FirstRecentFree = fault.pHeadRecentFreedAllocationNode is null ? null : new Allocation(fault.pHeadRecentFreedAllocationNode);
                }
            }
        }

        public unsafe class Allocation
        {
            private D3D12_DRED_ALLOCATION_NODE1* pNext;
            private Lazy<Allocation>? _next;

            internal Allocation(D3D12_DRED_ALLOCATION_NODE1* node)
            {
                pNext = node->pNext;

                Name = new string((char*)node->ObjectNameW);
                Type = (AllocationType)node->AllocationType;
                _next = pNext is null ? null : new Lazy<Allocation>(() => new Allocation(pNext));
            }

            public string Name { get; }
            public AllocationType Type { get; }
            public Allocation? Next => _next?.Value;
        }

        public enum AllocationType : uint
        {
            CommandQueue = D3D12_DRED_ALLOCATION_TYPE_COMMAND_QUEUE,
            CommandAllocator = D3D12_DRED_ALLOCATION_TYPE_COMMAND_ALLOCATOR,
            PipelineStateObject = D3D12_DRED_ALLOCATION_TYPE_PIPELINE_STATE,
            CommandContext = D3D12_DRED_ALLOCATION_TYPE_COMMAND_LIST,
            Fence = D3D12_DRED_ALLOCATION_TYPE_FENCE,
            DescriptorHeap = D3D12_DRED_ALLOCATION_TYPE_DESCRIPTOR_HEAP,
            Heap = D3D12_DRED_ALLOCATION_TYPE_HEAP,
            QueryHeap = D3D12_DRED_ALLOCATION_TYPE_QUERY_HEAP,
            CommandSignature = D3D12_DRED_ALLOCATION_TYPE_COMMAND_SIGNATURE,
            PipelineLibrary = D3D12_DRED_ALLOCATION_TYPE_PIPELINE_LIBRARY,
            VideoDecoder = D3D12_DRED_ALLOCATION_TYPE_VIDEO_DECODER,
            VideProcessor = D3D12_DRED_ALLOCATION_TYPE_VIDEO_PROCESSOR,
            Resource = D3D12_DRED_ALLOCATION_TYPE_RESOURCE,
            RenderPass = D3D12_DRED_ALLOCATION_TYPE_PASS,
            CryptoSession = D3D12_DRED_ALLOCATION_TYPE_CRYPTOSESSION,
            CryptoSessionPolicy = D3D12_DRED_ALLOCATION_TYPE_CRYPTOSESSIONPOLICY,
            ProtectedResourceSession = D3D12_DRED_ALLOCATION_TYPE_PROTECTEDRESOURCESESSION,
            VideDecoderHeap = D3D12_DRED_ALLOCATION_TYPE_VIDEO_DECODER_HEAP,
            CommandPool = D3D12_DRED_ALLOCATION_TYPE_COMMAND_POOL,
            CommandRecorder = D3D12_DRED_ALLOCATION_TYPE_COMMAND_RECORDER,
            StateObject = D3D12_DRED_ALLOCATION_TYPE_STATE_OBJECT,
            MetaCommand = D3D12_DRED_ALLOCATION_TYPE_METACOMMAND,
            SchedulingGroup = D3D12_DRED_ALLOCATION_TYPE_SCHEDULINGGROUP,
            MotionEstimator = D3D12_DRED_ALLOCATION_TYPE_VIDEO_MOTION_ESTIMATOR,
            MotionVectorHeap = D3D12_DRED_ALLOCATION_TYPE_VIDEO_MOTION_VECTOR_HEAP,
            VideoExtensionCommand = D3D12_DRED_ALLOCATION_TYPE_VIDEO_EXTENSION_COMMAND,
        }

        public unsafe class Breadcrumb
        {
            private Operation* pOperations;
            private int operationCount;
            private D3D12_AUTO_BREADCRUMB_NODE1* pNext;
            private Lazy<Breadcrumb>? _next;

            internal Breadcrumb(D3D12_AUTO_BREADCRUMB_NODE1* node)
            {
                pNext = node->pNext;

                CommandQueueName = new string((char*)node->pCommandQueueDebugNameW);
                ContextName = new string((char*)node->pCommandListDebugNameW);
                pOperations = (Operation*)node->pCommandHistory;
                operationCount = (int)node->BreadcrumbCount;
                IndexOfLastExecutedOperation = (int)*node->pLastBreadcrumbValue;

                _next = node is null ? null : new Lazy<Breadcrumb>(() => new Breadcrumb(pNext));
            }

            public string CommandQueueName { get; }
            public string ContextName { get; }
            public ReadOnlySpan<Operation> ContextOperations => new ReadOnlySpan<Operation>(pOperations, operationCount);
            public int IndexOfLastExecutedOperation { get; }

            public Breadcrumb? Next => _next?.Value;
        }

        public enum Operation
        {
            SetMarker = D3D12_AUTO_BREADCRUMB_OP_SETMARKER,
            BeginEvent = D3D12_AUTO_BREADCRUMB_OP_BEGINEVENT,
            EndEvent = D3D12_AUTO_BREADCRUMB_OP_ENDEVENT,
            DrawInstanced = D3D12_AUTO_BREADCRUMB_OP_DRAWINSTANCED,
            DrawIndexedInstanced = D3D12_AUTO_BREADCRUMB_OP_DRAWINDEXEDINSTANCED,
            ExecuteIndirect = D3D12_AUTO_BREADCRUMB_OP_EXECUTEINDIRECT,
            Dispatch = D3D12_AUTO_BREADCRUMB_OP_DISPATCH,
            CopyBufferRegion = D3D12_AUTO_BREADCRUMB_OP_COPYBUFFERREGION,
            CopyTextureRegion = D3D12_AUTO_BREADCRUMB_OP_COPYTEXTUREREGION,
            CopyResource = D3D12_AUTO_BREADCRUMB_OP_COPYRESOURCE,
            CopyTiles = D3D12_AUTO_BREADCRUMB_OP_COPYTILES,
            ResolveSubresource = D3D12_AUTO_BREADCRUMB_OP_RESOLVESUBRESOURCE,
            ClearRenderTargetView = D3D12_AUTO_BREADCRUMB_OP_CLEARRENDERTARGETVIEW,
            ClearUnorderedAccessView = D3D12_AUTO_BREADCRUMB_OP_CLEARUNORDEREDACCESSVIEW,
            ClearDepthStencilView = D3D12_AUTO_BREADCRUMB_OP_CLEARDEPTHSTENCILVIEW,
            ResourceBarrier = D3D12_AUTO_BREADCRUMB_OP_RESOURCEBARRIER,
            ExecuteBundle = D3D12_AUTO_BREADCRUMB_OP_EXECUTEBUNDLE,
            Present = D3D12_AUTO_BREADCRUMB_OP_PRESENT,
            ResolveQueryData = D3D12_AUTO_BREADCRUMB_OP_RESOLVEQUERYDATA,
            BeginSubmission = D3D12_AUTO_BREADCRUMB_OP_BEGINSUBMISSION,
            EndSubmission = D3D12_AUTO_BREADCRUMB_OP_ENDSUBMISSION,
            DecodeFrame = D3D12_AUTO_BREADCRUMB_OP_DECODEFRAME,
            ProcessFrames = D3D12_AUTO_BREADCRUMB_OP_PROCESSFRAMES,
            AtomicCopyBufferUInt = D3D12_AUTO_BREADCRUMB_OP_ATOMICCOPYBUFFERUINT,
            AtomicCopyBufferUInt64 = D3D12_AUTO_BREADCRUMB_OP_ATOMICCOPYBUFFERUINT64,
            ResolveSubresourceRegion = D3D12_AUTO_BREADCRUMB_OP_RESOLVESUBRESOURCEREGION,
            WriteBufferImmediate = D3D12_AUTO_BREADCRUMB_OP_WRITEBUFFERIMMEDIATE,
            DecodeFrame1 = D3D12_AUTO_BREADCRUMB_OP_DECODEFRAME1,
            SetProtectedResourceSession = D3D12_AUTO_BREADCRUMB_OP_SETPROTECTEDRESOURCESESSION,
            DecodeFrame2 = D3D12_AUTO_BREADCRUMB_OP_DECODEFRAME2,
            ProcessFrames1 = D3D12_AUTO_BREADCRUMB_OP_PROCESSFRAMES1,
            BuildRayTracingAccelerationStructure = D3D12_AUTO_BREADCRUMB_OP_BUILDRAYTRACINGACCELERATIONSTRUCTURE,
            EmitRayTracingAccelerationStructurePostBuildInfo = D3D12_AUTO_BREADCRUMB_OP_EMITRAYTRACINGACCELERATIONSTRUCTUREPOSTBUILDINFO,
            CopyRayTracingAccelerationStructure = D3D12_AUTO_BREADCRUMB_OP_COPYRAYTRACINGACCELERATIONSTRUCTURE,
            DispatchRays = D3D12_AUTO_BREADCRUMB_OP_DISPATCHRAYS,
            InitializeMetaCommand = D3D12_AUTO_BREADCRUMB_OP_INITIALIZEMETACOMMAND,
            ExecuteMetaCommand = D3D12_AUTO_BREADCRUMB_OP_EXECUTEMETACOMMAND,
            EsimateMotion = D3D12_AUTO_BREADCRUMB_OP_ESTIMATEMOTION,
            ResolveMotionVectorHeap = D3D12_AUTO_BREADCRUMB_OP_RESOLVEMOTIONVECTORHEAP,
            SetPipelineState1 = D3D12_AUTO_BREADCRUMB_OP_SETPIPELINESTATE1,
            InitializeExtensionCommand = D3D12_AUTO_BREADCRUMB_OP_INITIALIZEEXTENSIONCOMMAND,
            ExecuteExtensionCommand = D3D12_AUTO_BREADCRUMB_OP_EXECUTEEXTENSIONCOMMAND,
            DispatchMesh = D3D12_AUTO_BREADCRUMB_OP_DISPATCHMESH
        }
    }
}
