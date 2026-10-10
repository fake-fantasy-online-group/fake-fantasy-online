using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Map supplier borrower. Native TeamConfigDlg owns controls, OnGUI and root lifetime.
public sealed class ReferenceFirstWorldTeamConfigDialog : IDisposable
{
    private readonly TeamConfigDlg owner;
    private bool disposed;
    public ReferenceFirstWorldTeamConfigDialog(ReferenceResourceLoader resources, IPartyCommands commands,
        Func<IReadOnlyList<PartyInviteTarget>> nearbySource)
    {
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        owner = TeamConfigDlg.BindForIndependentRuntime(this, commands, nearbySource);
    }
    public bool Ready => !disposed && owner != null && owner.IsBoundForIndependentRuntime(this);
    public bool Visible => Ready && TeamConfigDlg.Visible;
    public int PendingInvite => owner.PendingInviteForIndependentRuntime;
    public IReadOnlyCollection<int> Picked => owner.PickedForIndependentRuntime;
    public void Toggle() { if (!Ready) throw new ObjectDisposedException(nameof(ReferenceFirstWorldTeamConfigDialog)); TeamConfigDlg.Toggle(); }
    public void Open(string defaultName, int inviteId)
    { if (!Ready) throw new ObjectDisposedException(nameof(ReferenceFirstWorldTeamConfigDialog)); TeamConfigDlg.Open(defaultName, inviteId); }
    public bool MouseOver(Vector2 pointer, float scale) => Ready && owner.ContainsPointer(pointer);
    // Unity is the sole driver, even when there is no map borrower.
    public bool Draw(float scale) => Ready;
    public void Dispose() { if (disposed) return; disposed = true; if (owner != null) owner.DetachForIndependentRuntime(this); }
}
}
