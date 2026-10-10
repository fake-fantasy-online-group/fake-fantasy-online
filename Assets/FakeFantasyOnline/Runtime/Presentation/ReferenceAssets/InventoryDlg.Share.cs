using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    public static bool ShareModHeld(Event ev)
    {
        return (ev != null && (ev.modifiers & (EventModifiers.Control | EventModifiers.Command)) != 0) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    }

    // Extracted original 0x06000527 branch, after prior picker/offer handling and before split/use.
    internal static bool TryShareBagClickForIndependentRuntime(Event ev, byte tab, Item item,
        Action<byte, byte, int, string> share)
    {
        if (ev == null || ev.type != EventType.MouseDown || ev.button != 0 || ev.clickCount == 2 ||
            heldSlot >= 0 || heldWhSlot >= 0 || !ShareModHeld(ev)) return false;
        string name = item.name;
        if (item.pet.HasValue)
        {
            PetBrief pet = item.pet.Value;
            name = string.IsNullOrEmpty(pet.nickname) ? pet.species : pet.nickname;
        }
        share?.Invoke(0, tab, item.bagIdx, name);
        ev.Use();
        return true;
    }

    // Official 1.6.0 0600051b: horn focus, then gossip focus, then ordinary chat.
    // The selected owner's return value is discarded; a failed horn insert never falls through.
    public static void ShareToChat(byte kind, byte tab, int slot, string name)
    {
        if (HornDialog.InputFocused)
        {
            HornDialog.InsertShare(kind, tab, slot, name);
            return;
        }
        if (GossipDlg.InputFocused)
        {
            GossipDlg.InsertShare(kind, tab, slot, name);
            return;
        }
        ChatUI.InsertShare(kind, tab, slot, name);
    }
}
}
