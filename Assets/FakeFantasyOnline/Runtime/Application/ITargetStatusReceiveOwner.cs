namespace FakeFantasyOnline.Application
{
/// <summary>The original reader operations reached by the 0x805e case.</summary>
public interface ITargetStatusReader
{
    int I32();
    int U8();
    string Str();
}

/// <summary>Host-thread 0x805e value-list read followed by the native setter.</summary>
public interface ITargetStatusReceiveOwner
{
    // This reader is the existing packet Buf, not a DTO projection. The owner creates
    // native TgSt values and reaches the setter only after every required read succeeds.
    void ReceiveTargetStatus(ITargetStatusReader reader);
}
}
