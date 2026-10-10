using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class PetForgeDlg
{
    public static void Open(int kind, int tab, int slot, int itemId, string name, Texture2D icon)
    {
        Ensure();
        I.visible = true;
        I.kind = kind;
        I.selSkill = 0;
        I.itemTab = tab;
        I.itemSlot = slot;
        I.itemId = itemId;
        I.itemName = name ?? "";
        I.itemIcon = icon;
        I.petTab = -1;
        I.petBrief = null;
        I.petIcon = null;
        I.pet2Tab = -1;
        I.pet2Brief = null;
        I.pet2Icon = null;
        I.pickB = false;
        I.tierOdds = new List<(int, int)>();
        I.skills = new List<(int, string, int, int, int, byte)>();
        I.tip = "";
        I.ok = false;
        I.confirmText = "";
        I.scroll = Vector2.zero;
        I.Query();
    }

    public static void SetItem(int kind, int tab, int slot, int itemId, string name, Texture2D icon)
    {
        if (I == null || !I.visible) return;
        I.kind = kind;
        I.itemTab = tab;
        I.itemSlot = slot;
        I.itemId = itemId;
        I.itemName = name ?? "";
        I.itemIcon = icon;
        I.selSkill = 0;
        I.Query();
    }

    // Original SetPet 06000941; a default brief is still a present nullable value.
    public static void SetPet(int tab, int slot, PetBrief brief, Texture2D icon)
    {
        if (I == null || !I.visible) return;
        if (I.kind != 2)
        {
            I.petTab = tab;
            I.petSlot = slot;
            I.petBrief = brief;
            I.petIcon = icon;
            I.selSkill = 0;
            I.Query();
            return;
        }
        bool sameA = I.petTab == tab && I.petSlot == slot;
        bool sameB = I.pet2Tab == tab && I.pet2Slot == slot;
        if (sameA)
        {
            I.petTab = -1;
            I.petBrief = null;
            I.petIcon = null;
            I.pickB = false;
            I.Query();
            return;
        }
        if (sameB)
        {
            I.pet2Tab = -1;
            I.pet2Brief = null;
            I.pet2Icon = null;
            I.pickB = true;
            I.Query();
            return;
        }
        if ((!I.pickB && I.petTab < 0) || I.pet2Tab >= 0)
        {
            I.petTab = tab;
            I.petSlot = slot;
            I.petBrief = brief;
            I.petIcon = icon;
            I.pickB = true;
        }
        else
        {
            I.pet2Tab = tab;
            I.pet2Slot = slot;
            I.pet2Brief = brief;
            I.pet2Icon = icon;
            I.pickB = false;
        }
        I.Query();
    }

    private void Query()
    {
        NetClient.I?.PetForgeForIndependentRuntime(0, unchecked((byte)kind), unchecked((byte)(petTab < 0 ? 0 : petTab)),
            petTab < 0 ? -1 : petSlot, unchecked((byte)(itemTab < 0 ? 0 : itemTab)),
            itemTab < 0 ? -1 : itemSlot, selSkill, unchecked((byte)(pet2Tab < 0 ? 0 : pet2Tab)),
            pet2Tab < 0 ? -1 : pet2Slot);
    }
}

}
