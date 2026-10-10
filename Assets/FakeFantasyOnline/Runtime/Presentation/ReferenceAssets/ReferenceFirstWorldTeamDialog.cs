using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Map borrower: binding and ordered state publication only. Native DDOL roots own their GUI/lifetime.
public sealed class ReferenceFirstWorldTeamDialog : IDisposable
{
    private readonly TeamDialogRuntime runtime;
    private readonly TeamDlg owner;
    private long appliedVersion = long.MinValue;
    private bool disposed;
    internal ReferenceFirstWorldTeamDialog(TeamDialogRuntime runtime)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        owner = TeamDlg.BindForIndependentRuntime(runtime);
    }
    public bool Ready => !disposed && owner != null && owner.IsBoundForIndependentRuntime(runtime);
    public bool Visible => Ready && TeamDlg.Visible;
    public void Toggle() { if (!Ready) throw new ObjectDisposedException(nameof(ReferenceFirstWorldTeamDialog)); TeamDlg.Toggle(); }
    public void OpenParty() { if (!Ready) throw new ObjectDisposedException(nameof(ReferenceFirstWorldTeamDialog)); TeamDlg.OpenParty(); }
    public void ApplyState(WorldPartyState next, long version)
    {
        if (!Ready || next == null || version <= appliedVersion) return;
        SocialClient.BindPartyForIndependentRuntime(next); appliedVersion = version;
    }
    public bool MouseOver(Vector2 pointer, float scale) => Ready &&
        (owner.ContainsPointer(pointer) || (FriendConfigDlg.I != null && FriendConfigDlg.I.ContainsPointer(pointer)) ||
         (MasterInfoDlg.I != null && MasterInfoDlg.I.ContainsPointer(pointer)));
    // Readiness only: forwarding OnGUI here would duplicate Unity's callback.
    public bool Draw(float scale) => Ready;
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (owner != null) owner.DetachForIndependentRuntime(runtime);
    }
}
}
