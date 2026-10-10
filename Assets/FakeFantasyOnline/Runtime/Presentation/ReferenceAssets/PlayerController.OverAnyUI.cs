namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    /// <summary>
    /// Official PlayerController.OverAnyUI (0x060009ff) - arm64 0x2ac670 / x86_64 0x2bd2a0.
    /// </summary>
    /// <remarks>
    /// The frozen dual-architecture bodies (wb067-overanyui-arm64 / wb067-overanyui-x86_64,
    /// 060009ff.c) agree term by term and in short-circuit order. Every term is a static read or a
    /// static call, so the native body never touches the instance; the recovered member keeps the
    /// official instance signature and the same ordering.
    /// Edge 0: GameHud.I (TypeInfo:GameHud static +0x18, metadata usage 01708) is null-tested with
    /// UnityEngine.Object.op_Inequality and then drives GameHud.MouseOverHud (0x06000417,
    /// arm64 0x2ac7d8). A null GameHud.I skips the term instead of throwing.
    /// Edge 1: SkillTargeting static +0x0 (metadata usage 02410 = TypeInfo:SkillTargeting) is
    /// &lt;Active&gt;k__BackingField, i.e. SkillTargeting.Active.
    /// Edge 2: PlayerController static +0x28 (metadata usage 02196 = TypeInfo:PlayerController) is
    /// WorkTipOpen.
    /// The remaining edges are the static calls 0x06000971, 0x06000e38, 0x060005dd, 0x06000e1b,
    /// 0x06000db3, 0x06000d84, 0x06000da0, 0x06000946, 0x0600008f, 0x06000045, 0x06000034 and
    /// 0x0600005c in that order; 0x0600006d is the tail call whose value is returned, so a true
    /// ArenaResultDlg hit answers true and a false one answers false.
    /// </remarks>
    public bool OverAnyUI()
    {
        if (GameHud.I != null && GameHud.I.MouseOverHud()) return true;
        if (SkillTargeting.Active) return true;
        if (WorkTipOpen) return true;
        if (PlayerActionMenu.Visible) return true;
        if (SystemInfoBar.MouseOver()) return true;
        if (GossipDlg.MouseOver()) return true;
        if (SysMsgBox.Visible) return true;
        if (StallDlg.Visible) return true;
        if (StallConfigDlg.Active) return true;
        if (StallDetailDlg.Active) return true;
        if (PetForgeDlg.MouseOver()) return true;
        if (AttrWashDlg.MouseOver()) return true;
        if (ArenaDraftDlg.MouseOver()) return true;
        if (ArenaDeathDlg.MouseOver()) return true;
        if (ArenaEscDlg.MouseOver()) return true;
        return ArenaResultDlg.MouseOver();
    }
}

}
