namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Exact native NPC names, not a broad “城主” substring that could add actions to
    // quest actors. The first five are named by ov_help/ov_hint; the other three
    // are the main-city leaders present in the same original map_npcs catalogue.
    internal static bool IsFamilyCreationNpc(int mapId, string name)
    {
        switch (mapId)
        {
            case 7: return name == "城主成隍";       // 龙城 / qw0007
            case 19: return name == "执事官徐福";    // 出云城 / qw0019
            case 32: return name == "玉皇大帝";      // 天都 / qw0032
            case 58: return name == "监城官哈亚图";  // 楼兰城 / qw0058
            case 86: return name == "无泪城主绝聆";  // 无泪城 / qw0086
            case 154: return name == "城主六翼羽宸"; // 比翼城 / qw0154
            case 178: return name == "上古城主海煌"; // 上古城 / qw0178
            case 247: return name == "猫猫城主大王"; // 猫猫城 / qw0247
            default: return false;
        }
    }

    private void AddFamilyCreationOption()
    {
        if (!IsFamilyCreationNpc(GameBootstrap.CurMapId, NpcName)) return;
        Add(new ReferenceNpcDialogOption("创建家族", () =>
        {
            if (!IsFamilyCreationNpc(GameBootstrap.CurMapId, NpcName)) return;
            Close();
            // Reopening an NPC option must not toggle an already-open form closed.
            // Only the existing form's 确定 sends FamilyCreate; cancel is unchanged.
            if (!FamilyCreateDlg.get_Visible()) FamilyCreateDlg.Toggle();
        }));
    }
}
}
