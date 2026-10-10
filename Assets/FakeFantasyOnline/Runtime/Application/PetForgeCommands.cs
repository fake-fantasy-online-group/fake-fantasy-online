using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public sealed class PetForgeRequest
{
    public PetForgeRequest(byte op, byte kind, byte petTab, int petSlot, byte itemTab,
        int itemSlot, int skillId, byte pet2Tab = 0, int pet2Slot = -1)
    {
        Op = op; Kind = kind; PetTab = petTab; PetSlot = petSlot; ItemTab = itemTab;
        ItemSlot = itemSlot; SkillId = skillId; Pet2Tab = pet2Tab; Pet2Slot = pet2Slot;
    }
    public byte Op { get; }
    public byte Kind { get; }
    public byte PetTab { get; }
    public int PetSlot { get; }
    public byte ItemTab { get; }
    public int ItemSlot { get; }
    public int SkillId { get; }
    public byte Pet2Tab { get; }
    public int Pet2Slot { get; }
}

public interface IPetForgeCommands
{
    Task<Result> PetAwakenAsync(CancellationToken cancellationToken);
    Task<Result> PetForgeAsync(PetForgeRequest request, CancellationToken cancellationToken);
}
}
