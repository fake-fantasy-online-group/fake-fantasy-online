namespace FakeFantasyOnline.Application
{
/// <summary>Main-thread owner for the local 0x803f hair-update receive branch.</summary>
public interface IPlayerHairReceiveContext
{
    int SelfEntityId { get; }
    void ReceiveLocalPlayerHair(int head, int hair);
}
}
