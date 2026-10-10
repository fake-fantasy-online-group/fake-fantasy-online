using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class SocketUI
{
    public static byte TargetKind, TgtTab;
    public static int TgtSlot = -1, ItemId, CurLevel = -1;
    public static string SelName = "";
    public static Texture2D SelIcon;
    public static readonly List<int> Sockets = new List<int>();
    public static readonly List<(int, string, Texture2D)> SocketNames = new List<(int, string, Texture2D)>();
    public static int NextCost, NextSuccess;
    public static bool CanDrill;
    public static int MaxHoles = 5;
    public static string Reason = "";
    public static readonly List<CraftUpgradeUI.Mat> DrillMats = new List<CraftUpgradeUI.Mat>();
    public static bool FailDestroys;
    public static int HoleCount => Sockets.Count;
    public static int FirstEmptyHole()
    {
        for (int i = 0; i < Sockets.Count; i++) if (Sockets[i] == 0) return i;
        return -1;
    }
    public static bool HasTarget => TgtSlot >= 0 && ItemId > 0;

    // Official 1.5.8 06000d5f preserves MaxHoles/Reason/FailDestroys until the next reply.
    public static void Reset()
    {
        TargetKind = 0; TgtTab = 0; TgtSlot = -1; ItemId = 0; CurLevel = -1;
        SelName = ""; SelIcon = null; Sockets.Clear(); SocketNames.Clear();
        NextCost = 0; NextSuccess = 0; CanDrill = false; DrillMats.Clear();
    }

    public static void SetInfo(int itemId, int curLevel, List<int> sockets, int nextCost,
        int nextSuccess, bool canDrill, List<CraftUpgradeUI.Mat> drillMats,
        List<(int, string, Texture2D)> socketNames, int maxHoles = 5, string reason = "")
    {
        ItemId = itemId; CurLevel = curLevel;
        Sockets.Clear(); if (sockets != null) Sockets.AddRange(sockets);
        SocketNames.Clear(); if (socketNames != null) SocketNames.AddRange(socketNames);
        NextCost = nextCost; NextSuccess = nextSuccess; CanDrill = canDrill;
        DrillMats.Clear(); if (drillMats != null) DrillMats.AddRange(drillMats);
        MaxHoles = Math.Max(0, maxHoles); Reason = reason ?? "";
    }

    public static void SelectBag(byte tab, int bagSlot, int id, string name, Texture2D icon)
    {
        TargetKind = 0; TgtTab = tab; TgtSlot = bagSlot; ItemId = id; SelName = name ?? "";
        SelIcon = icon; CurLevel = -1; Sockets.Clear();
        if (NetClient.I != null) NetClient.I.DrillForIndependentRuntime(0, tab, bagSlot, 0);
    }

    public static void Drill(bool protect = false)
    {
        if (HasTarget && CanDrill && NetClient.I != null)
            NetClient.I.DrillForIndependentRuntime(TargetKind, TgtTab, TgtSlot, 1, (byte)(protect ? 1 : 0));
    }

    public static void Inlay(int holeIdx, byte runeTab, int runeBagSlot)
    {
        if (HasTarget && NetClient.I != null)
            NetClient.I.InlayForIndependentRuntime(TargetKind, TgtTab, TgtSlot, (byte)holeIdx, runeTab, runeBagSlot);
    }
}
}
