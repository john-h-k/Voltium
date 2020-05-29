using System;
using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Strings;

namespace Voltium.Core.Managers
{
    // TODO: allow serializing to file sonehow
    /// <summary>
    /// In charge of creation, storing, and retrieving pipeline state objects (PSOs)
    /// </summary>
    [ThreadSafe]
    public unsafe sealed class PipelineManager : IDisposable
    {
        //private ComPtr<ID3D12PipelineLibrary> _psoLibrary;
        private Dictionary<string, ComPtr<ID3D12PipelineState>> _psos = new(16, new FastStringComparer());
        private ComPtr<ID3D12Device> _device;

        /// <summary>
        /// Creates a new <see cref="PipelineManager"/>
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to be associated with the pipeline states</param>
        public PipelineManager(ComPtr<ID3D12Device> device)
        {
            _device = device.Move();
        }

        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public void CreatePso(string name, D3D12_GRAPHICS_PIPELINE_STATE_DESC graphicsDesc)
        {
            using ComPtr<ID3D12PipelineState> pso = default;
            Guard.ThrowIfFailed(_device.Get()->CreateGraphicsPipelineState(
                &graphicsDesc,
                pso.Guid,
                ComPtr.GetVoidAddressOf(&pso)
            ));

            DirectXHelpers.SetObjectName(pso.Get(), $"Pipeline state object '{name}'");

            StorePso(name, pso.Move());
        }

        /// <summary>
        /// Retrives a pipeline state object by name
        /// </summary>
        /// <param name="name">The name of the PSO to retrieve</param>
        /// <returns>The PSO stored with the name</returns>
        public ComPtr<ID3D12PipelineState> RetrievePso(string name)
        {
            return _psos[name].Copy();
        }

        /// <summary>
        /// Store a pipeline state object with an associated name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pso"></param>
        /// <param name="overwrite"></param>
        public void StorePso(string name, ComPtr<ID3D12PipelineState> pso, bool overwrite = false)
        {
            if (overwrite)
            {
                _psos[name] = pso;
            }
            else
            {
                if (!_psos.TryAdd(name, pso))
                {
                    ThrowHelper.ThrowInvalidOperationException($"PSO with name '{name}' was already present, and the " +
                        $"overwrite parameter was set to false");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var value in _psos.Values)
            {
                value.Dispose();
            }
            _device.Dispose();
            //_psoLibrary.Dispose();
        }
    }
}
