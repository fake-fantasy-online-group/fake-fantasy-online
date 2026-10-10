using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official 0600072c-0600072f: the four task requests. Each official body only builds the
    // request payload closure and hands it to NetClient.Send, so the payload is exactly
    // 0x1032 Str(npcName) / 0x1033 I32(taskId) / 0x1034 I32(taskId) / 0x1035 I32(taskId). This
    // tree carries outbound commands as bound writers (see NetClient.Horn.cs), so the same value
    // is forwarded verbatim and the opcode stays owned by the Protocol writer.
    private Action<string> writeTaskNpcList;
    private Action<int> writeTaskAccept;
    private Action<int> writeTaskAbandon;
    private Action<int> writeTaskComplete;

    internal void BindTaskCommandsForIndependentRuntime(
        Action<string> npcList,
        Action<int> accept,
        Action<int> abandon,
        Action<int> complete)
    {
        writeTaskNpcList = npcList;
        writeTaskAccept = accept;
        writeTaskAbandon = abandon;
        writeTaskComplete = complete;
    }

    public void TaskNpcListForIndependentRuntime(string npcName) => writeTaskNpcList?.Invoke(npcName);

    public void TaskAcceptForIndependentRuntime(int taskId) => writeTaskAccept?.Invoke(taskId);

    public void TaskAbandonForIndependentRuntime(int taskId) => writeTaskAbandon?.Invoke(taskId);

    public void TaskCompleteForIndependentRuntime(int taskId) => writeTaskComplete?.Invoke(taskId);
}
}
