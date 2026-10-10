using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private void RestoreTemplate(byte type)
    {
        string saved = _tpl.TryGetValue(TplKey(type), out string value) ? value : "";
        if (string.IsNullOrEmpty(saved)) return;
        foreach (string record in saved.Split(';'))
        {
            string[] parts = record.Split(':');
            if (parts.Length < 3 || !int.TryParse(parts[0], out int id) ||
                !long.TryParse(parts[1], out long price) || !int.TryParse(parts[2], out int count)) continue;
            for (int tab = 0; tab < 4; tab++)
            {
                int index = InventoryDlg.Items[tab].FindIndex(candidate =>
                    candidate.id == id && !candidate.pet.HasValue && candidate.blocked == 0 && !candidate.bound && !candidate.locked &&
                    !staged.Exists(entry => entry.bagTab == tab && entry.bagSlot == candidate.bagIdx));
                if (index < 0) continue;
                InventoryDlg.Item item = InventoryDlg.Items[tab][index];
                staged.Add(new Staged
                {
                    bagTab = (byte)tab, bagSlot = item.bagIdx, itemId = id,
                    count = type == 1 ? Mathf.Max(1, count) : Mathf.Clamp(count, 1, item.count),
                    price = price, name = item.name, icon = item.icon, quality = item.quality, desc = item.desc
                });
                break;
            }
        }
    }
}
}
