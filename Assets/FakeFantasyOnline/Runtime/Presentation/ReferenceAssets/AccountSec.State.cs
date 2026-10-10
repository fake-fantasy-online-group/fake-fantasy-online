using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class AccountSec
{
    public static bool HasSecCode;
    private static float winUntil;
    private static byte pendTab;
    private static int pendSlot = -1;
    private static string pendName = "";

    public static void Seed(bool hasSec, int winMs)
    {
        HasSecCode = hasSec;
        winUntil = winMs > 0 ? Time.realtimeSinceStartup + winMs / 1000f : 0f;
    }

    public static bool NeedSecCodeForUnlock => HasSecCode && Time.realtimeSinceStartup >= winUntil;

    public static void NoteUnlockTarget(byte tab, int slot, string name)
    {
        pendTab = tab;
        pendSlot = slot;
        pendName = name ?? "";
    }
}
}
