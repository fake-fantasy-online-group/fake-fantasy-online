using System;

namespace FakeFantasyOnline.Application
{
public sealed partial class ClientSessionCoordinator
{
    private bool _selectedEntryRequestActive;
    private bool _selectedEntryOperationDisposed;
    private WorldEntryRecord _selectedEntryAtReceipt;
    private Exception _selectedEntryReceiptFailure;

    public void BindTitleWorldEntryForIndependentRuntime(
        Action<WorldEntryRecord> receive, Action begin = null,
        IWorldEntryReceiptSource receiver = null, IWorldEntryAdmissionSource admissionSource = null)
    {
        IWorldEntryReceiptSource source = receiver ?? (_gateway as IWorldEntryReceiptSource);
        if (source == null) return;
        IWorldEntryAdmissionSource completionSource = admissionSource ??
            (receiver == null ? _gateway as IWorldEntryAdmissionSource : receiver as IWorldEntryAdmissionSource);
        Action<WorldEntryRecord> callback = receive == null ? null : entry =>
        {
            try
            {
                WorldEntryRecord selected = SelectWorldEntryForHostBuild(entry, completionSource);
                if (_selectedEntryRequestActive) _selectedEntryAtReceipt = selected;
                receive(selected);
            }
            catch (Exception exception)
            {
                if (_selectedEntryRequestActive) _selectedEntryReceiptFailure = exception;
                throw;
            }
        };
        if (source is IWorldEntryPrefixReceiptSource nativeSource)
            nativeSource.BindWorldEntryReceipt(callback, receive == null ? null : begin);
        else
            source.BindWorldEntryReceipt(callback);
    }

    private WorldEntryRecord SelectWorldEntryForHostBuild(WorldEntryRecord entry) =>
        SelectWorldEntryForHostBuild(entry, _gateway as IWorldEntryAdmissionSource);

    private WorldEntryRecord SelectWorldEntryForHostBuild(
        WorldEntryRecord entry, IWorldEntryAdmissionSource source)
    {
        WorldEntryRecord selectedEntry = entry.WithLevel(
            ResolveEnterWorldLevel(entry));
        Action buildReturned = source?.CaptureWorldEntryCompletion(entry);
        if (source != null && buildReturned == null)
            throw new OperationCanceledException("The selected entry was retired before host admission.");
        _entryAwaitingHostBuild = selectedEntry;
        _entryBuildReturned = buildReturned;
        TransitionTo(ClientSessionState.InWorld, string.Empty);
        return selectedEntry;
    }
}
}
