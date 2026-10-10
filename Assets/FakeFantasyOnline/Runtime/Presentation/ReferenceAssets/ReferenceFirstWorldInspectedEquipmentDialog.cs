using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Map-local borrowing of the independently receipt-created ExEquipDlg root.</summary>
public sealed class ReferenceFirstWorldInspectedEquipmentDialog : IDisposable
{
    public const string BackgroundPath = "res/uires/equipdlg/equipdlgbk.gsa";
    public const string ClosePathBase = "ui/equipdlg05/miniclose_f";
    public const float WindowWidth = 257f;
    public const float WindowHeight = 506f;
    // Product diagnostics own this list; the native ExEquipDlg initializer does not allocate it.
    private static readonly int[] SlotParts = { 2, 6, 8, 9, 4, 1, 3, 10, 7, 5, 11 };
    private bool disposed;

    private ReferenceFirstWorldInspectedEquipmentDialog() { }

    public bool Ready => !disposed;
    public bool Visible => ExEquipDlg.IsOpen;
    public float WindowX => ExEquipDlg.I != null ? ExEquipDlg.I.WindowXForIndependentRuntime : -1f;
    public float WindowY => ExEquipDlg.I != null ? ExEquipDlg.I.WindowYForIndependentRuntime : -1f;
    public WorldInspectedPlayerEquipmentState Snapshot => ExEquipDlg.ObserveForIndependentRuntime();
    public static IReadOnlyList<int> EquipmentSlotParts => SlotParts;
    public static IReadOnlyList<Vector2> EquipmentSlotPositions => ExEquipDlg.SlotPositionsForIndependentRuntime();
    public static IReadOnlyList<string> EquipmentRaceNames => ExEquipDlg.RaceNamesForIndependentRuntime;

    public static bool TryCreate(ReferenceResourceLoader resources,
        out ReferenceFirstWorldInspectedEquipmentDialog owner)
    {
        // No art, font, resource-owner or native-window admission at a map bind.
        owner = new ReferenceFirstWorldInspectedEquipmentDialog();
        return true;
    }

    // Retained for the existing explicit Editor fixture entrance. Production receipt owners
    // call the same native bridge directly; maps never call Open or replay a session snapshot.
    public bool Open(WorldInspectedPlayerEquipmentState next)
    {
        ThrowIfDisposed();
        if (next == null) return false;
        ExEquipDlg.ReceiveForIndependentRuntime(next);
        return true;
    }

    public void Close() => ExEquipDlg.CloseNow();
    internal void CloseForUiEscForIndependentRuntime() => ExEquipDlg.CloseNow();
    public int At(int index) => ExEquipDlg.AtForIndependentRuntime(index);

    public bool MouseOver(Vector2 mousePosition, float scale) =>
        ExEquipDlg.I != null && ExEquipDlg.I.MouseOver();

    public bool Draw(float scale)
    {
        ThrowIfDisposed();
        // The independent native MonoBehaviour receives Unity OnGUI even without a map.
        // Compatibility-only host method; never invoke that callback a second time here.
        return true;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldInspectedEquipmentDialog));
    }
}

}
