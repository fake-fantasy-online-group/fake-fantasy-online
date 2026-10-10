using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
    /// <summary>One attachment in the official MailDlg.MItem tuple list.</summary>
    public sealed class WorldMailAttachment
    {
        public WorldMailAttachment(int itemId, int count, string name, string description)
        {
            ItemId = itemId;
            Count = count;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public int ItemId { get; }
        public int Count { get; }
        public string Name { get; }
        public string Description { get; }
    }

    /// <summary>
    /// Immutable projection of the official MailDlg.MItem record.
    /// </summary>
    public sealed class WorldMailItemState
    {
        public WorldMailItemState(long id, string from, string title, string body,
            long money, long caiyu, byte type, bool read, bool canClaim, bool canReturn,
            long time, IReadOnlyList<WorldMailAttachment> items)
        {
            Id = id;
            From = from ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Money = money;
            Caiyu = caiyu;
            Type = type;
            Read = read;
            CanClaim = canClaim;
            CanReturn = canReturn;
            Time = time;
            Items = new ReadOnlyCollection<WorldMailAttachment>(
                new List<WorldMailAttachment>(items ?? Array.Empty<WorldMailAttachment>()));
        }

        public long Id { get; }
        public string From { get; }
        public string Title { get; }
        public string Body { get; }
        public long Money { get; }
        public long Caiyu { get; }
        public byte Type { get; }
        public bool Read { get; }
        public bool CanClaim { get; }
        public bool CanReturn { get; }
        public long Time { get; }
        public IReadOnlyList<WorldMailAttachment> Items { get; }
    }

    /// <summary>Server-authored MailDlg list metadata and immutable rows.</summary>
    public sealed class WorldMailState
    {
        public static readonly WorldMailState Empty = new WorldMailState(
            0, 0, 0, Array.Empty<WorldMailItemState>());

        public WorldMailState(int total, int capacity, int newCount,
            IReadOnlyList<WorldMailItemState> items)
        {
            Total = total;
            Capacity = capacity;
            NewCount = newCount;
            Items = new ReadOnlyCollection<WorldMailItemState>(
                new List<WorldMailItemState>(items ?? Array.Empty<WorldMailItemState>()));
        }

        public int Total { get; }
        public int Capacity { get; }
        public int NewCount { get; }
        public IReadOnlyList<WorldMailItemState> Items { get; }
    }

    /// <summary>Observation of one real mail packet, never a deferred gameplay callback.</summary>
    public sealed class WorldMailUpdate
    {
        public WorldMailUpdate(WorldMailState list, bool nativeOwnerApplied)
        { List = list; NativeOwnerApplied = nativeOwnerApplied; }
        public WorldMailUpdate(bool hasNew, bool nativeOwnerApplied)
        { HasNew = hasNew; NativeOwnerApplied = nativeOwnerApplied; }
        public WorldMailState List { get; }
        public bool HasNew { get; }
        // False explicitly identifies a data-only host without the native receive capability.
        public bool NativeOwnerApplied { get; }
    }

    /// <summary>Host-thread case tails; false means this host has no native mail owner.</summary>
    public interface IMailReceiveOwner
    {
        bool TryReceiveMailList(WorldMailState state);
        bool TryReceiveMailNew(bool hasNew);
    }

    public interface IMailStateSource
    {
        WorldMailState MailState { get; }
        long MailStateVersion { get; }
        WorldMailUpdate MailReceipt { get; }
        long MailReceiptVersion { get; }
    }
}
