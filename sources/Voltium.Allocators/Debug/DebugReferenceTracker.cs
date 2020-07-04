namespace Voltium.Allocators.DebugTools
{
#if DEBUG || ALLOCATION_TRACE
    internal class DebugReferenceTracker
#else
    internal struct DebugReferenceTracker
#endif
    {
    }
}
