using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed class ReferenceReviveDialogBinding : IDisposable
{
    private readonly ReviveDlg core;
    private bool disposed;

    internal ReferenceReviveDialogBinding()
    {
        ReviveDlg.Ensure();
        core = ReviveDlg.I;
    }

    public bool Ready => !disposed && core != null && ReferenceEquals(core, ReviveDlg.I);
    public bool Visible => Ready && ReviveDlg.Visible;

    internal bool ObserveReceipt(WorldLocalPlayerLifeState state) =>
        Ready && core.ObserveLifeReceiptForIndependentRuntime(state);

    internal bool Open(int flags)
    {
        if (!Ready || flags < 0 || flags > byte.MaxValue)
            return false;
        ReviveDlg.Open((byte)flags);
        return true;
    }

    internal void Hide()
    {
        if (Ready) ReviveDlg.Hide();
    }

    public void Dispose() => disposed = true;
}
}
