using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
// The one official 1.5.8 command the arena death window emits: NetClient.ArenaRevive
// (0x06000746), opcode 0x1084, one payload byte from writer 0x06000837 (0 = revive, 1 = surrender).
// There is no separate "leave arena" packet in the dispatch tables - the server decides the outcome
// of both clicks, so this interface carries exactly that single byte and nothing else.
public interface IArenaReviveCommands
{
    Task<Result> ArenaReviveAsync(bool surrender, CancellationToken cancellationToken);
}
}
