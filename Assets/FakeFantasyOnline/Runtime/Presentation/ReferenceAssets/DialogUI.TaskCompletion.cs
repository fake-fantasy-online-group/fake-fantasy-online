using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcTaskOfferState completionOffer;
    private int completionStep;
    private bool completionHadActive;
    private bool completionWasDone;
    private int completionNpc;

    // A sent command is not a successful hand-in. Keep the detail available for rejection
    // feedback; hide only after the corresponding authoritative log advances/removes it.
    internal void TrackTaskCompletion(int taskId)
    {
        if (!Visible || !InSubView || LastOfferIntent == null ||
            LastOfferIntent.TaskId != taskId) return;
        completionOffer = LastOfferIntent;
        completionNpc = CurrentNpcEntityId;
        TaskClient.ActiveTask active = TaskClient.GetActive(taskId);
        completionHadActive = active != null;
        completionStep = active == null ? -1 : active.step;
        completionWasDone = TaskClient.Done.Contains(taskId);
    }

    internal void ObserveTaskCompletionReceipt()
    {
        if (completionOffer == null) return;
        if (!Visible || !InSubView || !object.ReferenceEquals(completionOffer, LastOfferIntent) ||
            CurrentNpcEntityId != completionNpc)
        {
            completionOffer = null;
            return;
        }
        int id = completionOffer.TaskId;
        TaskClient.ActiveTask active = TaskClient.GetActive(id);
        bool completed = !completionWasDone && TaskClient.Done.Contains(id);
        bool advanced = completionHadActive && (active == null || active.step > completionStep);
        if (!completed && !advanced) return;
        completionOffer = null;
        Close();
    }
}
}
