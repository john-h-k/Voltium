namespace Voltium.Core.Devices
{
    using CQLock = CommandQueue.WorkLock;

    public readonly struct DeviceWorkLock
    {
        private readonly CQLock? _copy, _compute, _graphics, _videoDecode, _videoProcess, _videoEncode;

        public DeviceWorkLock(CQLock? copy, CQLock? compute, CQLock? graphics, CQLock? videoDecode, CQLock? videoProcess, CQLock? videoEncode)
        {
            _copy = copy;
            _compute = compute;
            _graphics = graphics;
            _videoDecode = videoDecode;
            _videoProcess = videoProcess;
            _videoEncode = videoEncode;
        }

        public void Release() => Dispose();
        public void Dispose()
        {
            _copy?.Release();
            _compute?.Release();
            _graphics?.Release();
            _videoDecode?.Release();
            _videoProcess?.Release();
            _videoEncode?.Release();
        }
    }
}
