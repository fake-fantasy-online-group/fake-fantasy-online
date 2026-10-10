using System;
using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Session lifetime, receive projection and the remaining subwindow GUI handoff live here.
public sealed class ReferenceFirstWorldMailDialog : IDisposable
{
    private readonly IMailStateSource source;
    private readonly MailDlg owner;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private bool hasWindowDriver;
    private bool disposed;

    public ReferenceFirstWorldMailDialog(IMailCommands commands, IMailStateSource stateSource)
    {
        source = stateSource;
        owner = MailDlg.BindIndependentRuntime(commands ?? throw new ArgumentNullException(nameof(commands)),
            (text, color) => ChatUI.Add("系统", text, color), lifetime.Token, null, null, null);
        // Receipt may precede any map window. Adopt the native state as-is, never replay an
        // initial Empty or an already published list and never reset view/selection/draft.
        MailDlg.SetIndependentGuiOwner(false);
    }
    public bool Ready => !disposed;
    public bool Visible => !disposed && MailDlg.Visible;
    public bool ComposeVisible => !disposed && MailDlg.ComposeVisible;
    public WorldMailState State => source?.MailState ?? WorldMailState.Empty;
    public int View => MailDlg.CurrentView;
    public void Open() { if (!MailDlg.Visible) MailDlg.Toggle(); }
    public void Close() => MailDlg.Hide();
    public void Toggle() => MailDlg.Toggle();

    internal void RebindWindow(Func<WorldInventoryItemState> heldItem, Action clearHeld,
        Func<WorldInventoryState> inventory)
    {
        ThrowIfDisposed();
        owner.BindWindowForIndependentRuntime(heldItem, clearHeld, inventory);
        hasWindowDriver = true;
        MailDlg.SetIndependentGuiOwner(true);
    }
    internal void DetachWindow()
    {
        if (disposed) return;
        owner.BindWindowForIndependentRuntime(null, null, null);
        hasWindowDriver = false;
        // Map teardown removes only the borrower. MailDlg keeps its automatic callback;
        // the remaining hosted subwindows resume theirs between routed hosts.
        MailDlg.SetIndependentGuiOwner(false);
    }
    public bool MouseOver(Vector2 pointer, float scale) => !disposed && (MailDlg.ContainsPointer(pointer, scale) || MailConfigDlg.ContainsPointer(pointer) || MailResistDlg.ContainsPointer(pointer));
    public bool Draw(float scale)
    {
        if (disposed || !hasWindowDriver) return false;
        // MailDlg owns its automatic OnGUI; only the remaining hosted subwindows use this event.
        return true;
    }
    public bool TryAddAttachment(WorldInventoryItemState item, Vector2 pointer) =>
        !disposed && item != null && MailDlg.TryDropAttach(item.Tab, item.BagIndex, pointer);
    internal void ReceiveNew(bool hasNew)
    {
        ThrowIfDisposed();
        MailDlg.SetHasNew(hasNew);
    }
    internal void ReceiveList(WorldMailState state)
    {
        ThrowIfDisposed();
        var list = new List<MailDlg.MItem>(state.Items.Count);
        foreach (WorldMailItemState mail in state.Items)
        {
            var item = new MailDlg.MItem { id = mail.Id, from = mail.From, title = mail.Title,
                body = mail.Body, money = mail.Money, caiyu = mail.Caiyu, type = mail.Type,
                read = mail.Read, canClaim = mail.CanClaim, canReturn = mail.CanReturn, time = mail.Time };
            foreach (WorldMailAttachment attachment in mail.Items)
                item.items.Add((attachment.ItemId, attachment.Count, attachment.Name, attachment.Description));
            list.Add(item);
        }
        MailDlg.SetMailList(state.Total, state.Capacity, state.NewCount, list);
    }
    public void Dispose() => Dispose(nativeNotificationCleared: false);

    internal void Dispose(bool nativeNotificationCleared)
    {
        if (disposed) return;
        disposed = true;
        hasWindowDriver = false;
        if (nativeNotificationCleared)
        {
            // Native teardown has already cleared HasNew and queued the root purge.
            // Retire only this borrower; the component and pending commands keep their state
            // until Unity performs deferred destruction.
            if (MailDlg.I == owner)
            {
                owner.BindWindowForIndependentRuntime(null, null, null);
                MailDlg.SetIndependentGuiOwner(false);
            }
            return;
        }
        lifetime.Cancel();
        lifetime.Dispose();
        if (MailDlg.I != owner) return;
        MailDlg.Hide();
        if (!nativeNotificationCleared) MailDlg.HasNew = false;
        owner.ReleaseBindingsForIndependentRuntime();
        MailDlg.IndependentGuiOwner = false;
        UnityEngine.Object.Destroy(owner.gameObject);
        if (MailConfigDlg.I != null)
        {
            UnityEngine.Object.Destroy(MailConfigDlg.I.gameObject);
        }
        if (MailResistDlg.I != null)
        {
            UnityEngine.Object.Destroy(MailResistDlg.I.gameObject);
        }
    }
    private void ThrowIfDisposed()
    {
        if (disposed) throw new ObjectDisposedException(nameof(ReferenceFirstWorldMailDialog));
    }
}
}
