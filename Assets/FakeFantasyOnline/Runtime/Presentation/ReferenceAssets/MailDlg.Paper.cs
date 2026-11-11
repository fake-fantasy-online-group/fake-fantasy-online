namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class MailDlg
{
    internal const int LetterPaperItemId = 3309;

    internal static void OpenFromPaper(byte tab, int slot)
    {
        // Resolve the live native bag row, including hotbar/mobile callers. A
        // stale binding or a locked item must not create a phantom compose entry.
        var paper = InventoryDlg.ItemAt(tab, slot);
        if (NetClient.I == null || paper.id != LetterPaperItemId ||
            paper.count < 1 || paper.locked) return;
        Ensure();
        // A repeated use must preserve the current recipient, text and attachments.
        if (ComposeVisible) return;
        I.visible = true;
        I.StartCompose(0, string.Empty, string.Empty);
    }

    // Plugin path: consume the official paper entry, fill the composer, then DoSend.
    // Completion is the same SendAsync task the 寄出 button assigns to pending.
    internal static bool TrySendForPlugins(string to, string title, string body, int paperItemId,
        out string error, out System.Threading.Tasks.Task<FakeFantasyOnline.Foundation.Result> pending)
    {
        error = null;
        pending = null;
        if (string.IsNullOrWhiteSpace(to))
        {
            error = "Recipient is required.";
            return false;
        }
        if (paperItemId <= 0) paperItemId = LetterPaperItemId;
        if (paperItemId != LetterPaperItemId)
        {
            error = "paper must be the official letter-paper item.";
            return false;
        }
        byte paperTab = 0;
        int paperSlot = -1;
        bool found = false;
        var tabs = InventoryDlg.Items;
        if (tabs != null)
        {
            for (int tab = 0; tab < tabs.Length; tab++)
            {
                if (tabs[tab] == null) continue;
                foreach (InventoryDlg.Item row in tabs[tab])
                {
                    if (row.id == paperItemId && row.count >= 1 && !row.locked)
                    {
                        paperTab = (byte)tab;
                        paperSlot = row.bagIdx;
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }
        }
        if (!found)
        {
            error = "The requested letter paper is not in the bag, or it is locked.";
            return false;
        }
        if (!ComposeVisible) OpenFromPaper(paperTab, paperSlot);
        if (!ComposeVisible || I == null)
        {
            error = "The native mail composer could not be opened from the letter paper.";
            return false;
        }
        I.toName = (to ?? string.Empty).Trim();
        if (I.toName.Length > 16) I.toName = I.toName.Substring(0, 16);
        I.title = (title ?? string.Empty);
        if (I.title.Length > 30) I.title = I.title.Substring(0, 30);
        I.body = (body ?? string.Empty);
        if (I.body.Length > 250) I.body = I.body.Substring(0, 250);
        I.DoSend();
        pending = I.pending;
        if (pending == null)
        {
            error = "Native DoSend did not start a send task.";
            return false;
        }
        return true;
    }
}
}
