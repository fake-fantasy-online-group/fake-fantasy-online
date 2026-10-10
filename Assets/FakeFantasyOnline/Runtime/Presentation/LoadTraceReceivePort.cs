using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

/// <summary>
/// Host port that hands the official LoadTrace members to the recovered NetClient.Update
/// (0x0600077D) without letting Protocol reference Presentation or Unity.
/// </summary>
/// <remarks>
/// Every member is a straight call into the real LoadTrace implementation: On (0x060001EA),
/// Now (0x06000204), Ms (0x06000205), Net (0x060001EF) and Backlog (0x060001F0). The port adds
/// no gating, no constant and no fallback, so the official decisions in the Update body - when
/// On is read, when the timestamp is taken, which length a null payload reports - stay there.
/// The composition root binds this implementation, and the gateway hands the same instance to
/// NetClient, so the context-free Pump and the context-carrying Pump read the same real trace:
/// no path receives an empty trace, a constant false or a default timestamp.
/// </remarks>
public sealed class LoadTraceReceivePort : IWorldReceiveTracePort
{
    public bool On => LoadTrace.On;

    public long Now => LoadTrace.Now;

    public float Ms(long started) => LoadTrace.Ms(started);

    public void Net(int msgId, int length, float milliseconds) =>
        LoadTrace.Net(msgId, length, milliseconds);

    public void Backlog(int depth) => LoadTrace.Backlog(depth);
}

}
