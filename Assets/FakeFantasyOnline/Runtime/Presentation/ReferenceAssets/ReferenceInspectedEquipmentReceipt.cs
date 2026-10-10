using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static class ReferenceInspectedEquipmentReceipt
{
    internal static WorldInspectedPlayerEquipmentState Read(IInspectedEquipmentReader reader)
    {
        int ignored = reader.I32();
        string name = reader.Str();
        int level = reader.I32();
        int race = reader.U8();
        int gender = reader.U8();
        int head = reader.U8();
        int hair = reader.U8();
        string title = reader.Str();
        long honor = reader.I64();
        int body = reader.U16();
        int cap = reader.U16();
        int backpack = reader.U16();
        int rightWeapon = reader.U16();
        int leftWeapon = reader.U16();
        int face = reader.U16();
        reader.U8();
        reader.U8();
        reader.I16();
        int attributeCount = reader.U16();
        var attributes = new int[attributeCount];
        for (int i = 0; i < attributeCount; i++) attributes[i] = reader.I32();
        int maximumAttack = reader.I32();
        int minimumAttack = reader.I32();
        int defense = reader.I32();
        int magicAttack = reader.I32();
        int magicDefense = reader.I32();
        int hit = reader.I32();
        int count = reader.U8();
        var equipment = new List<EquipDlg.EquippedItem>();
        for (int i = 0; i < count; i++)
        {
            int slot = reader.U8();
            int id = reader.I32();
            string itemName = reader.Str();
            string description = reader.Str();
            int currentDurability = reader.I32();
            int maximumDurability = reader.I32();
            int avatar = reader.I32();
            int soulAvatar = reader.I32();
            // Original per-row effect: before Add and before the next row's first read.
            equipment.Add(new EquipDlg.EquippedItem
            {
                slot = slot, id = id, name = itemName,
                icon = NetClient.LoadItemIcon(avatar > 0 ? avatar : id),
                desc = description, curDur = currentDurability, maxDur = maximumDurability,
                avatar = avatar, soulAvatar = soulAvatar
            });
        }
        if (reader.Remaining >= 1)
        {
            int qualityCount = reader.U8();
            for (int i = 0; i < qualityCount && i < equipment.Count && reader.Remaining > 0; i++)
            {
                EquipDlg.EquippedItem item = equipment[i];
                item.quality = reader.U8();
                equipment[i] = item;
            }
        }
        // Dispatch goes directly to Show; trailing extensions are left unread.
        ExEquipDlg.Show(name, level, race, gender, head, hair, title, honor,
            body, cap, backpack, rightWeapon, leftWeapon, face, attributes,
            maximumAttack, minimumAttack, defense, magicAttack, magicDefense, hit, equipment);

        // Existing Application observation is built only after the native reads, per-row
        // icon effects and Show. It cannot prevent a reached native effect or cause a retry.
        var rows = new List<WorldInspectedEquipmentItem>(equipment.Count);
        foreach (EquipDlg.EquippedItem item in equipment)
            rows.Add(new WorldInspectedEquipmentItem(item.slot, item.id, item.name, item.desc,
                item.curDur, item.maxDur, item.avatar, item.soulAvatar, item.quality));
        return new WorldInspectedPlayerEquipmentState(ignored, name, level, race, gender,
            head, hair, title, honor, new CharacterAppearanceRecord(body, cap, backpack,
                rightWeapon, leftWeapon, face, 0, 0, 0), attributes,
            maximumAttack, minimumAttack, defense, magicAttack, magicDefense, hit, rows);
    }
}
}
