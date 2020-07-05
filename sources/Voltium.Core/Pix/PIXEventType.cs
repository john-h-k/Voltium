namespace Voltium.Common.Pix
{
    /*
     * do NOT change, these are hardcoded and recognised by PIX
     * HasContext means it carries an extra context pointer with it
     * varargs means it has arguments in addition to context/color/format string
     */
    internal enum PIXEventType
    {
        EndEvent = 0x00,

        VarArgsOffset = -1,
        HasContext = 0x10,

        BeginEventNoArgs = 0x02,
        BeginEventVarArgs = BeginEventNoArgs + VarArgsOffset,

        SetMarkerNoArgs = 0x08,
        SetMarkerVarArgs = SetMarkerNoArgs + VarArgsOffset,

        EndEventOnContext = EndEvent | HasContext,

        BeginEventOnContextVarArgs = BeginEventVarArgs | HasContext,
        BeginEventOnContextNoArgs = BeginEventNoArgs | HasContext,
        SetMarkerOnContextVarArgs = BeginEventVarArgs | HasContext,
        SetMarkerOnContextNoArgs = BeginEventNoArgs | HasContext,
    };
}
