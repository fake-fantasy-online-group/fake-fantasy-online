using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // ChgSlotPos/changeSet initializers are ordered in Win05.Initialization.cs.

    public struct ChgItem
    {
        public int cell, id;
        public string name, desc;
        public int curDur, maxDur, refine;
        public byte quality, blocked;
        public Texture2D icon;
    }

    // Original 06001049 / 0600104a / 0600104c.
    public static void SetChangeSet(List<ChgItem> list)
    {
        changeSet.Clear();
        if (list != null) changeSet.AddRange(list);
    }

    private static ChgItem? ChgAt(int cell)
    {
        foreach (ChgItem item in changeSet)
            if (item.cell == cell) return item;
        return null;
    }

    // The original fixed eleven-value lookup needs no managed owner array.
    private static int ChgSlotPart(int i) => i switch
    {
        0 => 2, 1 => 6, 2 => 8, 3 => 9, 4 => 4, 5 => 1,
        6 => 3, 7 => 10, 8 => 7, 9 => 5, 10 => 11, _ => -1
    };

    // Original 0600104b.
    public static void ChangeSetHotkey()
    {
        if (changeSet.Count == 0)
        {
            ChatUI.Add("系统", "没有装备可以换 —— 先在【换装】窗里把要换的装备拖进方案格。",
                new Color(1f, 180f / 255f, 120f / 255f, 1f));
            return;
        }
        if (NetClient.I != null) NetClient.I.ChangeSetSwapForIndependentRuntime();
    }

    // Original 0600104d. The pointer is the supplied GUI point, not Input.mousePosition.
    private int HitChangeCell(Vector2 mouse)
    {
        if (positioned)
            for (int i = 0; i < 11; i++)
                if (new Rect(winX + ChgSlotPos[i].x * s, winY + ChgSlotPos[i].y * s,
                    30f * s, 30f * s).Contains(mouse)) return ChgSlotPart(i);
        return 0;
    }

    // Every runtime pointer consumer uses the same native owner and Input read point.
    internal static bool MouseOverForBagInputForIndependentRuntime() => I != null && I.MouseOver();

    internal static bool NativeBagTargetHitForIndependentRuntime() => I != null &&
        (I.cur == "changeequipdlg" || I.cur == "magicstoredlg") && I.MouseOver();

    // Original 0600103f / 06001040: ARM64 0x3b4090 / 0x3b41bc,
    // x86_64 0x3c6f50 / 0x3c7020. Empty and occupied pet slots both admit the pointer.
    public static int HitPetSlot(Vector2 m) => I != null ? I.HitPetSlotImpl(m) : -1;

    private int HitPetSlotImpl(Vector2 m)
    {
        if (cur != "petdlg" || !positioned) return -1;
        for (int i = 0; i < 10; i++)
        {
            float y = i < 5 ? 377f : 414f;
            if (new Rect(winX + (50f + (i % 5) * 40f) * s, winY + y * s,
                32f * s, 32f * s).Contains(m)) return i;
        }
        return -1;
    }

    // Original 0600103a: itemId is intentionally unused; the server decides admissibility.
    public static bool TryDropChangeSet(byte bagTab, int bagSlot, int itemId, Vector2 mouse)
    {
        if (I == null || I.cur != "changeequipdlg" || !I.MouseOver()) return false;
        int cell = I.HitChangeCell(mouse);
        if (cell > 0 && NetClient.I != null) NetClient.I.ChangeSetPutForIndependentRuntime(bagTab, bagSlot, cell);
        return true;
    }

    // Original 0600103b: MouseOver reads Input, while itemId and mouse are unused.
    public static bool TryDropWardrobe(byte bagTab, int bagSlot, int itemId, Vector2 mouse)
    {
        if (I == null || I.cur != "magicstoredlg" || !I.MouseOver()) return false;
        if (NetClient.I != null) NetClient.I.WardrobeStoreForIndependentRuntime(bagTab, bagSlot);
        return true;
    }
}
}
