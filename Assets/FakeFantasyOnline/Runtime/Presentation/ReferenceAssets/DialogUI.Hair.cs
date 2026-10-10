namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private void AddHairSalonOptions()
    {
        if (!NpcName.Contains("歌姬") && !NpcName.Contains("歌伎") && !NpcName.Contains("舞伎") &&
            !NpcName.Contains("歌仙") && !NpcName.Contains("骊姜") && !NpcName.Contains("明子")) return;
        Add(new ReferenceNpcDialogOption("染发", () => { Visible = false; HairSalonDlg.Open(0); }));
        Add(new ReferenceNpcDialogOption("改发型", () => { Visible = false; HairSalonDlg.Open(1); }));
    }
}
}
