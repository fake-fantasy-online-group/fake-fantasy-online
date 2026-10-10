using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class AvatarFuse
{
    public static bool Active;
    private static byte _tab;
    private static int _slot;
    private static byte _kind;

    // Official 1.5.8 0x060000a3.
    public static void Begin(byte bagTab, int bagSlot, byte kind = 0)
    {
        _tab = bagTab;
        _slot = bagSlot;
        _kind = kind;
        Active = true;
        CursorManager.I?.SetAvatarMode(true);
        EquipDlg.EnsureOpen();
    }

    // The host supplies the original equipment-window opening lifecycle explicitly.
    internal static void BeginForIndependentRuntime(byte bagTab, int bagSlot, byte kind, Action ensureEquipmentOpen)
    {
        _tab = bagTab;
        _slot = bagSlot;
        _kind = kind;
        Active = true;
        CursorManager.I?.SetAvatarMode(true);
        ensureEquipmentOpen();
    }

    public static void Cancel()
    {
        if (!Active) return;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
    }
}
}
