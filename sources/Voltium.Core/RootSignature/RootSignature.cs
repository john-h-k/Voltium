using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Managers;

namespace Voltium.Core
{
    /// <summary>
    /// Defines a root signature
    /// </summary>
    public sealed unsafe class RootSignature : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="RootSignature"/> from a <see cref="CompiledShader"/>
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> used to create the root signature</param>
        /// <param name="rootSignatureShader"></param>
        /// <param name="deserialize"></param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public static RootSignature Create(ID3D12Device* device, CompiledShader rootSignatureShader, bool deserialize = false)
        {
            fixed (byte* pSignature = rootSignatureShader)
            {
                using ComPtr<ID3D12RootSignature> rootSig = default;
                Guard.ThrowIfFailed(device->CreateRootSignature(
                    0 /* TODO: MULTI-GPU */,
                    pSignature,
                    (uint)rootSignatureShader.Length,
                    rootSig.Guid,
                    ComPtr.GetVoidAddressOf(&rootSig)
                ));

                if (deserialize)
                {
                    RootSignatureDeserializer.DeserializeSignature(device, pSignature, rootSignatureShader.Length);
                }

                return new RootSignature(rootSig.Move(), null, null);
            }
        }


        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> used to create the root signature</param>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public static RootSignature Create(ID3D12Device* device, ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers)
        {
            using var rootParams = RentedArray<D3D12_ROOT_PARAMETER>.Create(rootParameters.Length);
            using var samplers = RentedArray<D3D12_STATIC_SAMPLER_DESC>.Create(staticSamplers.Length);

            TranslateRootParameters(rootParameters, rootParams.Value);
            TranslateStaticSamplers(staticSamplers, samplers.Value);

            fixed (D3D12_ROOT_PARAMETER* pRootParams = rootParams.Value)
            fixed (D3D12_STATIC_SAMPLER_DESC* pSamplerDesc = samplers.Value)
            {
                var desc = new D3D12_ROOT_SIGNATURE_DESC
                {
                    NumParameters = (uint)rootParameters.Length,
                    pParameters = pRootParams,
                    NumStaticSamplers = (uint)staticSamplers.Length,
                    pStaticSamplers = pSamplerDesc,
                    Flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT // TODO provide finer grained control
                };

                var versionedDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC
                {
                    Version = D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1
                };

                versionedDesc.Anonymous.Desc_1_0 = desc;

                ID3DBlob* pBlob = default;
                ID3DBlob* pError = default;
                int hr = Windows.D3D12SerializeVersionedRootSignature(
                    &versionedDesc,
                    &pBlob,
                    &pError
                );

                if (Windows.FAILED(hr))
                {
                    ThrowHelper.ErrorWithBlob(hr, pError);
                }

                using ComPtr<ID3D12RootSignature> rootSig = default;
                Guard.ThrowIfFailed(device->CreateRootSignature(
                    0 /* TODO: MULTI-GPU */,
                    pBlob->GetBufferPointer(),
                    pBlob->GetBufferSize(),
                    rootSig.Guid,
                    ComPtr.GetVoidAddressOf(&rootSig)
                ));

                return new RootSignature(rootSig.Move(), rootParameters, staticSamplers);
            }
        }

        private static void TranslateRootParameters(ReadOnlyMemory<RootParameter> rootParameters, Memory<D3D12_ROOT_PARAMETER> outRootParams)
        {
            var span = rootParameters.Span;
            var outSpan = outRootParams.Span;
            for (var i = 0; i < span.Length; i++)
            {
                var inRootParam = span[i];
                D3D12_ROOT_PARAMETER outRootParam = new D3D12_ROOT_PARAMETER
                {
                    ParameterType = (D3D12_ROOT_PARAMETER_TYPE)inRootParam.Type,
                    ShaderVisibility = (D3D12_SHADER_VISIBILITY)inRootParam.Visibility
                };
                switch (inRootParam.Type)
                {
                    case RootParameterType.DescriptorTable:
                        outRootParam.Anonymous.DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE
                        {
                            NumDescriptorRanges = (uint)inRootParam.DescriptorTable!.Length,
                            // IMPORTANT: we *know* this is pinned, because it can only come from RootParameter.CreateDescriptorTable, which strictly makes sure it is pinned
                            pDescriptorRanges = (D3D12_DESCRIPTOR_RANGE*)Unsafe.AsPointer(
                                ref MemoryMarshal.GetArrayDataReference(inRootParam.DescriptorTable))
                        };
                        break;

                    case RootParameterType.DwordConstants:
                        outRootParam.Anonymous.Constants = inRootParam.Constants;
                        break;

                    case RootParameterType.ConstantBufferView:
                    case RootParameterType.ShaderResourceView:
                    case RootParameterType.UnorderedAccessView:
                        outRootParam.Anonymous.Descriptor = inRootParam.Descriptor;
                        break;
                }

                outSpan[i] = outRootParam;
            }
        }

        private static void TranslateStaticSamplers(ReadOnlyMemory<StaticSampler> staticSamplers, Memory<D3D12_STATIC_SAMPLER_DESC> samplers)
        {
            var span = staticSamplers.Span;
            var outSpan = samplers.Span;
            for (var i = 0; i < span.Length; i++)
            {
                var staticSampler = span[i];
                var desc = staticSampler.Sampler.GetDesc();

                D3D12_STATIC_BORDER_COLOR staticBorderColor;
                var borderColor = RgbaColor.FromPointer(desc.BorderColor);

                if (borderColor == StaticSampler.OpaqueBlack)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK;
                }
                else if (borderColor == StaticSampler.OpaqueWhite)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_OPAQUE_WHITE;
                }
                else if (borderColor == StaticSampler.TransparentBlack)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK;
                }
                else
                {
                    ThrowHelper.ThrowArgumentException("Static sampler must have opaque black, opaque white, or transparent black border color");
                    staticBorderColor = default;
                }

                var sampler = new D3D12_STATIC_SAMPLER_DESC
                {
                    AddressU = desc.AddressU,
                    AddressW = desc.AddressW,
                    AddressV = desc.AddressV,
                    ComparisonFunc = desc.ComparisonFunc,
                    BorderColor = staticBorderColor,
                    Filter = desc.Filter,
                    MaxAnisotropy = desc.MaxAnisotropy,
                    MaxLOD = desc.MaxLOD,
                    MinLOD = desc.MinLOD,
                    MipLODBias = desc.MipLODBias,
                    RegisterSpace = staticSampler.RegisterSpace,
                    ShaderRegister = staticSampler.ShaderRegister,
                    ShaderVisibility = (D3D12_SHADER_VISIBILITY)staticSampler.Visibility
                };

                outSpan[i] = sampler;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _value.Dispose();
        }

        private ComPtr<ID3D12RootSignature> _value;

        /// <summary>
        /// The underlying value of the root signature
        /// </summary>
        internal /* does this need to be public? */ ID3D12RootSignature* Value => _value.Get();

        /// <summary>
        /// The <see cref="RootParameter"/>s for this root signature, in order
        /// </summary>
        public readonly ReadOnlyMemory<RootParameter> Parameters;

        /// <summary>
        /// The <see cref="StaticSampler"/>s for this root signature
        /// </summary>
        public readonly ReadOnlyMemory<StaticSampler> StaticSamplers;

        // TODO root sig flags (when exposed)

        private RootSignature(
            ComPtr<ID3D12RootSignature> value,
            ReadOnlyMemory<RootParameter> parameters,
            ReadOnlyMemory<StaticSampler> staticSamplers
        )
        {
            _value = value.Move();
            Parameters = parameters;
            StaticSamplers = staticSamplers;
        }

#if TRACE_DISPOSABLES || DEBUG
        /// <summary>
        /// 🖕
        /// </summary>
        ~RootSignature()
        {
            Guard.MarkDisposableFinalizerEntered();
            ThrowHelper.NeverReached();
        }
#endif
    }
}
