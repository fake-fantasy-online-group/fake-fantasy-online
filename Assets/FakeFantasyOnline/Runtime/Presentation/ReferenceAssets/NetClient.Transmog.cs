using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    // Original Dispatch 0x8038: one shared Buf cursor, with icon effects between row reads.
    // Only the completed native list is published; a later malformed row keeps earlier
    // icon effects but never replaces the wardrobe or starts the transmog continuation.
    internal static IWorldWardrobeObservation ReceiveWardrobeForIndependentRuntime(IWardrobeReader reader)
    {
        int capacity = reader.I32();
        int count = reader.U16();
        var rows = new ReceivedWardrobe(capacity, count);
        for (int index = 0; index < count; index++)
        {
            int id = reader.I32();
            byte category = (byte)reader.U8();
            bool worn = reader.U8() != 0;
            bool blocked = reader.U8() != 0;
            string name = reader.Str();
            string description = reader.Str();
            var row = new Win05.WardrobeItem
            {
                id = id,
                cat = category,
                worn = worn,
                blocked = blocked,
                name = name,
                desc = description,
                icon = LoadItemIcon(id)
            };
            rows.Add(row);
        }
        Win05.SetWardrobe(capacity, rows);
        _wardrobeAv.Clear();
        _wardrobeSlots.Clear();
        foreach (Win05.WardrobeItem row in rows)
        {
            if (!row.worn) continue;
            if (row.cat < _wcatToSlots.Length)
                foreach (string slot in _wcatToSlots[row.cat]) _wardrobeSlots.Add(slot);
            var skin = SuitFxDB.Skin(row.id);
            if (!string.IsNullOrEmpty(skin.Item1)) _wardrobeAv[skin.Item1] = row.id;
        }
        RecomputeTransmog();

        // The required native list is also the retained observation. Nothing projects or
        // copies its rows on receipt, after native publication, or during window polling.
        return rows;
    }

    // SetWardrobe copies these rows into its own static list. This original receipt list
    // is never mutated after return, so an older observation survives later replacements.
    private sealed class ReceivedWardrobe : List<Win05.WardrobeItem>, IWorldWardrobeObservation
    {
        private readonly int capacity;
        private WorldWardrobeState state;

        internal ReceivedWardrobe(int capacity, int count) : base(count)
        {
            this.capacity = capacity;
        }

        public WorldWardrobeState State
        {
            get
            {
                // Explicit diagnostic/Task observers retain the immutable DTO contract.
                // Cache one identity per receipt; a failed projection is not published or
                // swallowed, and cannot turn an already-completed native receipt into failure.
                lock (this)
                {
                    if (state != null) return state;
                    var items = new List<WorldWardrobeItemState>(Count);
                    foreach (Win05.WardrobeItem row in this)
                        items.Add(new WorldWardrobeItemState(row.id, row.cat, row.worn,
                            row.blocked, row.name, row.desc));
                    state = new WorldWardrobeState(capacity, items);
                    return state;
                }
            }
        }
    }

    private static void RebuildEquippedTransmogForIndependentRuntime(IReadOnlyList<WorldInventoryEquippedItemState> items)
    {
        _equipAv.Clear();
        foreach (WorldInventoryEquippedItemState item in items)
        {
            int avatar = item.Avatar;
            if (avatar <= 0) continue;
            var skin = SuitFxDB.Skin(avatar);
            if (!string.IsNullOrEmpty(skin.Item1)) _equipAv[skin.Item1] = avatar;
        }
        RecomputeTransmog();
    }

    public static void RefreshTransmogFx() => RecomputeTransmog();

    private static int CatToArmPart(string cat)
    {
        if (cat == "cap") return 2;
        if (cat == "body") return 8;
        if (cat == "face") return 1;
        if (cat == "backpack") return 10;
        if (cat == "weaponr") return 4;
        if (cat == "weaponl") return 4;
        return -1;
    }

    private static void RecomputeTransmog()
    {
        CharData.LoadEquipFx();
        int body = 0, cap = 0, face = 0, bag = 0, wr = 0, wl = 0;
        foreach (string cat in _avCats)
        {
            int skin = 0;
            if (_wardrobeSlots.Contains(cat))
            {
                if (_wardrobeAv.TryGetValue(cat, out int id) && id > 0) skin = SuitFxDB.Skin(id).Item2;
            }
            else if (_equipAv.TryGetValue(cat, out int id)) skin = SuitFxDB.Skin(id).Item2;
            if (cat == "body") body = skin;
            else if (cat == "cap") cap = skin;
            else if (cat == "face") face = skin;
            else if (cat == "backpack") bag = skin;
            else if (cat == "weaponr") wr = skin;
            else if (cat == "weaponl") wl = skin;
        }
        CharData.SetAvatarSkin(body, cap, bag, wr, wl, face);
        var ids = new List<int>();
        foreach (KeyValuePair<string, int> pair in _equipAv)
        {
            int part = CatToArmPart(pair.Key);
            if (part < 0 || CharData.EquipShown(part, 0)) ids.Add(pair.Value);
        }
        CharData.avEffect = SuitFxDB.Resolve(ids);
    }
}

}
