using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Session-owned observation adapter. Unity drives the original component; map windows
    // borrow this adapter, and packet receipt is independent of it.
    // It holds no per-map resources, commands, source snapshots or queued callbacks.
    public sealed class ReferenceFirstWorldRackShopDialog : IDisposable
    {
        private bool disposed;

        public bool Visible => !disposed && RackShopDlg.Visible;
        public bool MouseOver() => !disposed && RackShopDlg.MouseOver();
        public bool Ready => !disposed &&
            (RackShopDlg.I == null || RackShopDlg.I.ReadyForIndependentRuntime);
        public void Toggle()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldRackShopDialog));
            RackShopDlg.Toggle();
        }

        public void Dispose() => Dispose(detachOnly: false);

        internal void Dispose(bool detachOnly)
        {
            if (disposed) return;
            disposed = true;
            if (!detachOnly) RackShopDlg.ReleaseIndependentRuntime();
        }
    }
}
