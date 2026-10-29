namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ItemActions
{
    // ov_item 3309, script “paper”; native ov_help “发送邮件” uses the bag item
    // to enter the existing composer. Opening/canceling never consumes a paper.
    private static bool HandleMailPaper(int id, byte tab, int slot)
    {
        if (id != MailDlg.LetterPaperItemId) return false;
        MailDlg.OpenFromPaper(tab, slot);
        return true;
    }
}
}
