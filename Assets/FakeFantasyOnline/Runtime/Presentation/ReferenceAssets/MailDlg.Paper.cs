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
}
}
