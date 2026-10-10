using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // One existing native reader and cursor, with no wrapper/copy or new parsing policy.
    internal partial class Buf : IInspectedEquipmentReader { }
}
}
