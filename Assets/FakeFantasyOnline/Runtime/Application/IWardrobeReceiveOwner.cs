namespace FakeFantasyOnline.Application
{
// The native row owner reads the existing Buf directly across the assembly boundary.
// This interface adds no cursor, packet copy, buffered row or deferred callback.
public interface IWardrobeReader
{
    int I32();
    int U16();
    int U8();
    string Str();
}

public interface IWardrobeReceiveOwner
{
    WorldWardrobeState ReceiveWardrobe(IWardrobeReader reader);
}

// A completed receipt owns its rows. Reading State explicitly requests the immutable
// Application projection; passing the observation between native owners does not.
public interface IWorldWardrobeObservation
{
    WorldWardrobeState State { get; }
}

public interface IWardrobeObservationReceiveOwner : IWardrobeReceiveOwner
{
    IWorldWardrobeObservation ReceiveWardrobeObservation(IWardrobeReader reader);
}

public interface IWorldWardrobeObservationSource
{
    IWorldWardrobeObservation WardrobeObservation { get; }
}
}
