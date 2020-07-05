namespace Voltium.Core
{

    ///// <summary>
    ///// Represents a context used to record and submit GPU commands
    ///// </summary>
    //public unsafe struct ContextManager
    //{
    //    private ComPtr<ID3D12GraphicsCommandList> _list;
    //    private ComPtr<ID3D12PipelineState> _pso;
    //    private ComPtr<ID3D12CommandAllocator> _allocator;
    //    private ListState _state;

    //    private enum ListState : byte
    //    {
    //        Recording,
    //        Closed,
    //    }

    //    /// <summary>
    //    /// Reset the command context so it can be reused
    //    /// </summary>
    //    /// <param name="newAllocator">Optionally, a new allocator to use</param>
    //    /// <param name="newPso">Optionally, a new default pipeline state to use</param>
    //    public void Reset(ID3D12CommandAllocator* newAllocator, ID3D12PipelineState* newPso = null)
    //    {
    //        if (newPso != null)
    //        {
    //            _pso = newPso;
    //        }

    //        Guard.ThrowIfFailed(_list.Get()->Reset(
    //            newAllocator,
    //            _pso.Get()
    //        ));

    //        _state = ListState.Recording;
    //    }

    //    /// <summary>
    //    /// Close the list, ending any new commands being recorded until <see cref="Reset"/> has been called
    //    /// </summary>
    //    public void Close()
    //    {
    //        Guard.ThrowIfFailed(_list.Get()->Close());
    //        _state = ListState.Closed;
    //    }

    //    /// <summary>
    //    /// Whether the underlying list is currently closed
    //    /// </summary>
    //    public bool IsClosed => _state == ListState.Closed;
    //}
}
