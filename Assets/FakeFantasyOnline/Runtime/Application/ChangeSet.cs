using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
    public sealed class WorldChangeSetItem
    {
        public WorldChangeSetItem(int cell, int itemId, string name, string description,
            int currentDurability, int maximumDurability, int refine, byte quality, byte blocked)
        {
            Cell = cell; ItemId = itemId; Name = name; Description = description;
            CurrentDurability = currentDurability; MaximumDurability = maximumDurability;
            Refine = refine; Quality = quality; Blocked = blocked;
        }
        public int Cell { get; }
        public int ItemId { get; }
        public string Name { get; }
        public string Description { get; }
        public int CurrentDurability { get; }
        public int MaximumDurability { get; }
        public int Refine { get; }
        public byte Quality { get; }
        public byte Blocked { get; }
    }

    public interface IChangeSetSource
    {
        IReadOnlyList<WorldChangeSetItem> ChangeSetSnapshot { get; }
        int ChangeSetVersion { get; }
    }

    public interface IChangeSetCommands
    {
        Task<Result> ChangeSetPutAsync(byte bagTab, int bagSlot, int cell, CancellationToken cancellationToken);
        Task<Result> ChangeSetTakeAsync(int cell, CancellationToken cancellationToken);
        Task<Result> ChangeSetSwapAsync(CancellationToken cancellationToken);
    }
}
