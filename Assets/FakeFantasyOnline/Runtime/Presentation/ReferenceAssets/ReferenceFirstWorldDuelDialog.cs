using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Session command binding and observations; Unity drives the lazy native dialog.
public sealed class ReferenceFirstWorldDuelDialog : IDisposable
{
    private readonly Action releaseCommands;
    private bool disposed;
    public ReferenceFirstWorldDuelDialog(IDuelCommands commands, IDuelStateSource stateSource)
    {
        // Keep the public constructor shape for callers; the source is observation-only.
        // Native rows and selection are updated once per packet by the receipt owner.
        releaseCommands = DuelDlg.BindCommands(commands ?? throw new ArgumentNullException(nameof(commands)));
    }
    public bool Ready => !disposed;
    public bool Visible => !disposed && DuelDlg.Visible;
    public bool Typing => !disposed && DuelDlg.Typing;
    public int Tab => DuelDlg.CurrentTab;
    public WorldDuelState State => DuelDlg.Snapshot();
    public void Open(int targetId, string targetName, int tab = -1) => DuelDlg.Open(targetId, targetName, tab);
    public void Close() => DuelDlg.Close();
    public void Toggle() => DuelDlg.Toggle();
    public bool MouseOver(Vector2 pointer, float scale) => !disposed && DuelDlg.ContainsPointer(pointer);
    internal void RebindWindow() => DuelDlg.RebindAssetsForIndependentRuntime();
    public void Dispose() { if (disposed) return; disposed = true; releaseCommands(); }
}
}
