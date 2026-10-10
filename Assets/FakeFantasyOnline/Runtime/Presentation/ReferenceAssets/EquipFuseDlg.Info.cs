namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipFuseDlg
{
    public static void OnInfo(int mk, int mt, int mslot, int st, int sslot,
        string mnm, int mq, string snm, int sq, string mdesc, string sdesc,
        bool eok, bool post, string etip, string ecf, int esig)
    {
        if (I == null) return;
        I.mainName = mnm ?? "";
        I.mainQ = mq;
        I.mainDesc = mdesc ?? "";
        if (post)
        {
            I.subTab = -1;
            I.subIcon = null;
            I.subName = "";
            I.subDesc = "";
            I.subId = 0;
            I.subQ = 0;
            I.pickSub = false;
        }
        else
        {
            I.subName = snm ?? "";
            I.subQ = sq;
            I.subDesc = sdesc ?? "";
        }
        I.ok = eok;
        I.tip = etip ?? "";
        I.confirmText = ecf ?? "";
        I.sig = esig;
    }
}
}
