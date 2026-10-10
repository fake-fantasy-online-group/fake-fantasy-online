namespace FakeFantasyOnline.Application
{

/// <summary>Live main-thread world fields used at the original receive read boundaries.</summary>
public interface IWorldReceiveContext
{
    bool Switching { get; }
    int CurrentMapId { get; }
}

/// <summary>Optional main-thread receive pump; keeps Unity objects outside Protocol.</summary>
public interface IWorldReceivePump
{
    void Pump(IWorldReceiveContext context);
}

/// <summary>Reads the native frame budget at each receive-loop time check.</summary>
public interface IWorldReceiveBudgetContext
{
    float DispatchMillisecondsPerFrame { get; }
}

/// <summary>Consumes already published world events on the receive caller's main thread.</summary>
public interface IWorldReceiveSceneContext
{
    void ApplyPublishedWorldEvents();
}

/// <summary>
/// Minimal host port for the official LoadTrace members the recovered NetClient.Update
/// (0x0600077D) reads: On (0x060001EA), Now (0x06000204), Ms (0x06000205), Net (0x060001EF)
/// and Backlog (0x060001F0). Protocol stays Unity-free and never references Presentation, so
/// the real implementation is injected by the composition root; the decisions about when On is
/// read, when Now is taken and how a null payload length is supplied stay in the Update body.
/// </summary>
public interface IWorldReceiveTracePort
{
    bool On { get; }

    long Now { get; }

    float Ms(long started);

    void Net(int msgId, int length, float milliseconds);

    void Backlog(int depth);
}

}
