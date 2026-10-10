using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IHornCommands
{
    // The official horn payload carries the share triples the caller already holds
    // (0x060006dc / 0x060008ae), so the capability takes the plain .NET list and forwards it
    // unchanged; no Unity type crosses this boundary.
    Task<Result> SendHornAsync(string text, int itemId, byte skin,
        List<(byte, byte, int)> shares, CancellationToken cancellationToken);
}

public interface IHornDialogReceiveContext
{
    void OpenHorn(int itemId, int tier, int skin);
}
}
