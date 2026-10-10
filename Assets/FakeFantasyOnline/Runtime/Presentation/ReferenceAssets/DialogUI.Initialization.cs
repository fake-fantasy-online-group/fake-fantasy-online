using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // 0600022c: keep all fallible static initializers in native declaration order.
    // Field initializers retain BeforeFieldInit; there is no explicit static constructor.
    // Official DialogUI.TitlePrefixes (040002c3), the 0600022c initializer slice.
    private static readonly string[] TitlePrefixes =
    {
        "少女", "少年", "道具商人", "守城卫兵", "护城将军", "治疗师", "宠物商人",
        "仓库管理员", "飞空艇仙子", "飞空艇小姐", "跑货商人", "盗墓王", "包打听",
        "客栈老板", "卖花的", "工匠", "巧匠", "木匠", "镖师"
    };

    // Original 040002c4, initialized immediately after TitlePrefixes.
    private static readonly string[] DeliverVerbs =
    {
        "交给", "送给", "送到", "送去", "赠给", "带给", "送还", "给"
    };

    private static readonly (string npc, string name, int map)[] HardDungeons =
    {
        ("斯芬尼克", "失落神殿", 20901),
        ("纯阳塔神卫", "纯阳宝塔", 20600),
        ("墨玉", "八仙的结界", 20701),
        ("二郎神杨戬", "天空之泉", 20801),
        ("寂仙子", "赤枭巢穴", 20300),
        ("病叟", "赤枭出没地", 20100)
    };

    private static readonly (string name, int map, float x, float y)[] NoviceVillages =
    {
        ("桃源东村", 61, 6923f, 1763f),
        ("桃源西村", 37, 5978f, 2432f),
        ("长乐东村", 62, 4863f, 3529f),
        ("长乐西村", 38, 4965f, 3681f)
    };

    private static readonly Color[] RICH = BuildRich();

    // Existing immutable presentation literals allocate no reference objects.
    private static readonly Color NpcNameColor =
        new Color(1f, 0.92f, 0.55f, 1f);
    private static readonly Color OptionFrameColor =
        new Color(
            0.1019607857f,
            0.1058823541f,
            0.0862745121f,
            0.7686274648f);

}
}
