using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    public sealed class WorldStallItemState
    {
        public WorldStallItemState(int itemId, int count, long price, string name, string description,
            byte blocked, byte quality, WorldInventoryPetState pet = null,
            int petAiUsed = 0, int petAiCapacity = 0, int petShengUsed = 0, int petShengCapacity = 0)
        {
            ItemId = itemId; Count = count; Price = price; Name = name ?? string.Empty;
            Description = description ?? string.Empty; Blocked = blocked; Quality = quality; Pet = pet;
            PetAiUsed = petAiUsed; PetAiCapacity = petAiCapacity;
            PetShengUsed = petShengUsed; PetShengCapacity = petShengCapacity;
        }
        public int ItemId { get; }
        public int Count { get; }
        public long Price { get; }
        public string Name { get; }
        public string Description { get; }
        public byte Blocked { get; }
        public byte Quality { get; }
        public WorldInventoryPetState Pet { get; }
        public int PetAiUsed { get; }
        public int PetAiCapacity { get; }
        public int PetShengUsed { get; }
        public int PetShengCapacity { get; }
    }

    public sealed class WorldStallSaleState
    {
        public WorldStallSaleState(long time, string item, int count, long money)
        { Time = time; Item = item ?? string.Empty; Count = count; Money = money; }
        public long Time { get; }
        public string Item { get; }
        public int Count { get; }
        public long Money { get; }
    }

    public sealed class WorldStallContentsState
    {
        public static readonly WorldStallContentsState Empty = new WorldStallContentsState(
            0, string.Empty, 0, string.Empty, Array.Empty<WorldStallItemState>(), Array.Empty<WorldStallSaleState>());
        public WorldStallContentsState(int ownerId, string ownerName, byte type, string shopName,
            IReadOnlyList<WorldStallItemState> items, IReadOnlyList<WorldStallSaleState> sales)
        {
            OwnerId = ownerId; OwnerName = ownerName ?? string.Empty; Type = type; ShopName = shopName ?? string.Empty;
            Items = new ReadOnlyCollection<WorldStallItemState>(new List<WorldStallItemState>(items ?? Array.Empty<WorldStallItemState>()));
            Sales = new ReadOnlyCollection<WorldStallSaleState>(new List<WorldStallSaleState>(sales ?? Array.Empty<WorldStallSaleState>()));
        }
        public int OwnerId { get; }
        public string OwnerName { get; }
        public byte Type { get; }
        public string ShopName { get; }
        public IReadOnlyList<WorldStallItemState> Items { get; }
        public IReadOnlyList<WorldStallSaleState> Sales { get; }
    }
}
