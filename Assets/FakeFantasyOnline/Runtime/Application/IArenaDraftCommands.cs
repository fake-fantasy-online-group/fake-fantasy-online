using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
// The two official 1.5.8 commands the arena talent draft window emits:
//   NetClient.ArenaPick   (0x06000744) -> opcode 0x1083, writer 0x06000835 -> one Buf.U8, the card
//                                         index the player clicked.
//   NetClient.ArenaReroll (0x06000745) -> opcode 0x1086, writer 0x060007a9, whose emitted extent is
//                                         a bare return, so the frame carries no payload.
// Both opcodes were read off the command bodies on arm64 and x86_64, never from the private server.
// What the picked card does and what the reroll costs are server-authored, so this interface carries
// exactly the clicked index for the pick and nothing at all for the reroll.
public interface IArenaDraftCommands
{
    Task<Result> ArenaPickAsync(int index, CancellationToken cancellationToken);

    Task<Result> ArenaRerollAsync(CancellationToken cancellationToken);
}
}
