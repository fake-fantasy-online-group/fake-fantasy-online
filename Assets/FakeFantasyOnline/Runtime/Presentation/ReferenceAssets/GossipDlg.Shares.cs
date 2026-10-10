using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class GossipDlg
    {
        // Official 0x060005e4. No textual fallback or fabricated share replaces a missing item.
        private static List<Share> BuildLocalShares(List<(byte, byte, int)> refs)
        {
            if (refs == null || refs.Count == 0) return null;
            var result = new List<Share>();
            foreach (var reference in refs)
            {
                byte kind = reference.Item1;
                int index = reference.Item3;
                if (kind == 1)
                {
                    if (index < 0 || CharData.petSlots == null || index >= CharData.petSlots.Count) continue;
                    PetBrief pet = CharData.petSlots[index];
                    if (pet.model == 0) continue;
                    result.Add(new Share { kind = 1,
                        name = string.IsNullOrEmpty(pet.nickname) ? pet.species : pet.nickname, pet = pet });
                }
                else if (kind == 2)
                {
                    foreach (EquipDlg.EquippedItem item in EquipDlg.Equipped)
                    {
                        if (item.slot != index) continue;
                        result.Add(new Share { kind = 0, itemId = item.id,
                            quality = item.quality, name = item.name, desc = item.desc });
                        break;
                    }
                }
                else
                {
                    InventoryDlg.Item item = InventoryDlg.ItemAt(reference.Item2, index);
                    if (item.id == 0 && !item.pet.HasValue) continue;
                    result.Add(new Share { kind = item.pet.HasValue ? (byte)1 : (byte)0,
                        itemId = item.id, quality = item.quality, name = item.name,
                        desc = item.desc, pet = item.pet });
                }
            }
            return result.Count > 0 ? result : null;
        }
    }
}
