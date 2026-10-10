using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed class WorldFamilyStashItem
{
    public WorldFamilyStashItem(int itemId, int count, string name, string description,
        int currentDurability, int maximumDurability, int quality, string depositedBy)
    {
        ItemId = itemId; Count = count; Name = name ?? string.Empty;
        Description = description ?? string.Empty; CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability; Quality = quality; DepositedBy = depositedBy ?? string.Empty;
    }
    public int ItemId { get; }
    public int Count { get; }
    public string Name { get; }
    public string Description { get; }
    public int CurrentDurability { get; }
    public int MaximumDurability { get; }
    public int Quality { get; }
    public string DepositedBy { get; }
}

public sealed class WorldFamilyStashState
{
    public static readonly WorldFamilyStashState Empty = new WorldFamilyStashState(
        0, 0, false, false, Array.Empty<WorldFamilyStashItem>());
    public WorldFamilyStashState(int capacity, int takePosition, bool canTake, bool leader,
        IReadOnlyList<WorldFamilyStashItem> items)
    {
        Capacity = capacity; TakePosition = takePosition; CanTake = canTake; Leader = leader;
        Items = new List<WorldFamilyStashItem>(items).AsReadOnly();
    }
    public int Capacity { get; }
    public int TakePosition { get; }
    public bool CanTake { get; }
    public bool Leader { get; }
    public IReadOnlyList<WorldFamilyStashItem> Items { get; }
}

public interface IFamilyStashStateSource
{
    WorldFamilyStashState FamilyStashState { get; }
    long FamilyStashStateVersion { get; }
}
}
