namespace Voltium.Core.Devices
{
    ///// <summary>
    ///// Information needed for an acceleration structure build, retrieved from <see cref="ComputeDevice.GetBuildInfo(Layout, uint, BuildAccelerationStructureFlags)"/>
    ///// </summary>
    public readonly struct AccelerationStructureBuildInfo
    {
        /// <summary>
        /// The size required for the scratch buffer
        /// </summary>
        public ulong ScratchSize { init; get; }

        /// <summary>
        /// The size required for the destination acceleration structure
        /// </summary>
        public ulong DestSize { init; get; }
    }
}
