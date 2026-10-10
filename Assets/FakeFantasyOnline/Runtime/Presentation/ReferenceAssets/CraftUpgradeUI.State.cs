using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CraftUpgradeUI
{
    internal sealed class Mat
    {
        public int id, need, have;
        public string name;
        public Texture2D icon;
    }

    public static int Mode, SelSlot = -1, CurLevel = -1, Cost;
    public static bool CanDo;
    public static int SuccessRate = 100, SafeLevel = 6;
    public static bool FailDestroys;
    public static readonly List<Mat> Mats = new List<Mat>();
    public static int RefTab = -1, RefBagSlot = -1, RefItemId;
    public static Texture2D RefIcon;
    public static string RefName = "";

    // Official 1.5.8 060001b3. SafeLevel/FailDestroys remain server-owned across reset.
    public static void Reset(int mode)
    {
        Mode = mode; SelSlot = -1; CurLevel = -1; Cost = 0; CanDo = false; SuccessRate = 100;
        Mats.Clear(); RefTab = -1; RefBagSlot = -1; RefItemId = 0; RefIcon = null; RefName = "";
    }

    public static void SetInfo(int mode, int slot, int curLevel, int cost, bool canDo,
        List<Mat> mats, int successRate = 100, int tab = -1, int bagSlot = -1, int itemId = 0)
    {
        Mode = mode; CurLevel = curLevel; Cost = cost; CanDo = canDo; SuccessRate = successRate;
        Mats.Clear(); if (mats != null) Mats.AddRange(mats);
        if (mode == 0)
        {
            if (curLevel == -3)
            {
                RefTab = -1; RefBagSlot = -1; RefItemId = 0; RefIcon = null; RefName = "";
                return;
            }
            if (itemId != 0) RefItemId = itemId;
        }
        else SelSlot = slot;
    }

    public static void Select(int slot)
    {
        SelSlot = slot; CurLevel = -1; Mats.Clear();
        if (NetClient.I != null) NetClient.I.CraftUpgradeForIndependentRuntime((byte)Mode, (byte)slot, 0);
    }

    public static void LoadBagItem(byte tab, int bagSlot, int id, string name, Texture2D icon)
    {
        RefTab = tab; RefBagSlot = bagSlot; RefItemId = id; RefName = name; RefIcon = icon;
        CurLevel = -1; Mats.Clear();
        if (NetClient.I != null) NetClient.I.CraftUpgradeForIndependentRuntime(0, 0, 0, tab, bagSlot);
    }

    public static void Execute(bool protect = false)
    {
        if (!CanDo || NetClient.I == null) return;
        if (Mode == 0)
        {
            if (RefBagSlot >= 0) NetClient.I.CraftUpgradeForIndependentRuntime(0, 0, 1, (byte)RefTab, RefBagSlot, (byte)(protect ? 1 : 0));
        }
        else if (SelSlot >= 0) NetClient.I.CraftUpgradeForIndependentRuntime(1, (byte)SelSlot, 1);
    }
}
}
