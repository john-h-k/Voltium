namespace Voltium.Core.DXGI
{
    /// <summary>
    /// The API to use when enumerating devices
    /// </summary>
    public enum DeviceEnumerationLayer
    {
        /// <summary>
        /// Uses the DXGI (DirectX Graphics Infrastructure) API to enumerate devices. This only supports graphics devices
        /// </summary>
        Dxgi,

        /// <summary>
        /// Uses the DXCore (DirectX Core) API to enumerate devices. This supports compute and graphics devices
        /// </summary>
        DxCore,
    }
}
