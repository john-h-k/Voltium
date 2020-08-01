namespace Voltium.Core.Devices
{
    ///// <summary>
    ///// The type in charge of managing per-frame resources
    ///// </summary>
    //public sealed unsafe class ResourceManager
    //{


    //    private static bool _initialized;
    //    private static readonly object Lock = new object();

    //    /// <summary>
    //    /// The single instance of this type. You must call <see cref="Initialize"/> before retrieving the instance
    //    /// </summary>
    //    public static ResourceManager Manager
    //    {
    //        get
    //        {
    //            Guard.Initialized(_initialized);

    //            return Value;
    //        }
    //    }

    //    private static readonly ResourceManager Value = new ResourceManager();

    //    private ResourceManager() { }

    //    internal static void Initialize(
    //        DeviceManager manager,
    //        ID3D12Device* device,
    //        IDXGISwapChain3* swapChain,
    //        GraphicalConfiguration config,
    //        ScreenData screenData
    //    )
    //    {
    //        // TODO could probably use CAS/System.Threading.LazyInitializer
    //        lock (Lock)
    //        {
    //            Debug.Assert(!_initialized);

    //            _initialized = true;
    //            Value.CoreInitialize(manager, device, swapChain, config, screenData);
    //        }
    //    }



    //    /// <inheritdoc/>
    //    public void Dispose()
    //    {
    //        GpuDispatchManager.Manager.BlockForGpuIdle();
    //    }
    //}
}
