using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // Official 06000317 publishes these five fields in this order. Keep their
    // initializers in one partial declaration so source-file ordering cannot reorder them.
    private static readonly string[] RaceNames = { "战士", "剑客", "刺客", "药师", "术士" };

    // Original static slot +0x10 is distinct from CharData.ownedTitles. No consumer
    // is introduced here: preserve the original empty allocation, without copying titles.
    private static readonly List<string> taskTitles = new List<string>();

    private static readonly string[] TitleCats =
        { "职业称号", "我的称号", "师徒称号", "家族称号", "隐藏称号" };

    private static readonly List<EquippedItem> equipped = new List<EquippedItem>();

    private static readonly int[][] AutoTpl =
    {
        new[] {3,1,4,1,0,0,1}, new[] {2,2,4,1,0,0,1}, new[] {3,1,3,2,0,0,1},
        new[] {1,1,2,0,3,0,3}, new[] {1,1,2,0,3,0,3}
    };

}
}
