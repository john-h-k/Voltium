namespace Voltium.Common.Pix
{
    internal unsafe static class EventTypeInferer
    {
        public static PIXEventType BeginEvent(uint length, bool hasContext)
        {
            if (!hasContext || PIXEncoding.IsXbox)
            {
                return length == 0 ? PIXEventType.BeginEventNoArgs : PIXEventType.BeginEventVarArgs;
            }
            return length == 0 ? PIXEventType.BeginEventOnContextNoArgs : PIXEventType.BeginEventOnContextVarArgs;
        }

        public static PIXEventType SetMarker(uint length, bool hasContext)
        {
            if (!hasContext || PIXEncoding.IsXbox)
            {
                return length == 0 ? PIXEventType.SetMarkerNoArgs : PIXEventType.SetMarkerVarArgs;
            }

            return length == 0 ? PIXEventType.SetMarkerOnContextNoArgs : PIXEventType.SetMarkerOnContextVarArgs;
        }

        public static PIXEventType BeginOnContext(uint length)
        {
            return length == 0 ? PIXEventType.BeginEventOnContextNoArgs : PIXEventType.BeginEventOnContextVarArgs;
        }

        public static PIXEventType SetMarkerOnContext(uint length)
        {
            return length == 0 ? PIXEventType.SetMarkerOnContextNoArgs : PIXEventType.SetMarkerOnContextVarArgs;
        }

        public static PIXEventType EndEvent(uint length, bool hasContext)
        {
            return hasContext ? PIXEventType.EndEventOnContext : PIXEventType.EndEvent;
        }

        // Xbox and Windows store different types of events for context events.
        // On Xbox these include a context argument, while on Windows they do
        // not. It is important not to change the event types used on the
        // Windows version as there are OS components (eg debug layer & DRED)
        // that decode event structs.

        public static PIXEventType GpuBeginOnContext(bool varargs)
        {
#if PIX_XBOX
            return varargs ? PIXEventType.BeginEventOnContextVarArgs : PIXEventType.BeginEventOnContextNoArgs;
#else
            return varargs ? PIXEventType.BeginEventVarArgs : PIXEventType.BeginEventNoArgs;
#endif
        }

        public static PIXEventType GpuSetMarkerOnContext(bool varargs)
        {
#if PIX_XBOX
            return varargs ? PIXEventType.SetMarkerOnContextVarArgs : PIXEventType.SetMarkerOnContextNoArgs;
#else
            return varargs ? PIXEventType.SetMarkerVarArgs : PIXEventType.SetMarkerNoArgs;
#endif
        }
    }
}
