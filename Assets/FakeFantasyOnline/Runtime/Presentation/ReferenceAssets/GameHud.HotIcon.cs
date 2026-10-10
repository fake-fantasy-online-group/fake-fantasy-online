using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{

    public static void LoadHotbar(int[] ids, int[] kind, bool expanded, int[] icons = null)
    {
        for (int i = 0; i < ids.Length && i < 30; i++)
        {
            hotSkill[i] = ids[i];
            hotKind[i] = kind[i];
            hotIcon[i] = null;
            hotServerIcon[i] = icons != null && i < icons.Length ? icons[i] : 0;
        }
        hotbarExpanded = expanded;
        if (!expanded) hotbarRow = 0;
    }

    internal static bool HotbarExpandedForIndependentRuntime { get => hotbarExpanded; set => hotbarExpanded = value; }
    internal static int HotbarRowForIndependentRuntime { get => hotbarRow; set => hotbarRow = value; }
    internal static (int id, int kind, int serverIcon) HotSlotStateForIndependentRuntime(int index) =>
        (hotSkill[index], hotKind[index], hotServerIcon[index]);
    internal static Texture2D HotIconForIndependentRuntime(int index) => HotIcon(index);

    internal static void ClearHotbarSlotForIndependentRuntime(int index)
    {
        hotSkill[index] = 0;
        hotIcon[index] = null;
        hotKind[index] = 0;
    }

    // Original slot replacement/swap writes. The current view still owns input admission,
    // held-state cleanup and the asynchronous send boundary.
    internal static void DropHotbarSlotForIndependentRuntime(int target, int id, Texture2D icon, int kind, int source)
    {
        int previousId = hotSkill[target];
        Texture2D previousIcon = hotIcon[target];
        int previousKind = hotKind[target];
        hotSkill[target] = id;
        hotIcon[target] = icon;
        hotKind[target] = kind;
        if (source >= 0 && source != target && previousId != 0)
        {
            hotSkill[source] = previousId;
            hotIcon[source] = previousIcon;
            hotKind[source] = previousKind;
        }
    }

    private static Texture2D HotIcon(int index)
    {
        if (hotIcon[index] != null || hotSkill[index] == 0) return hotIcon[index];
        int iconId = hotServerIcon[index];
        if (iconId == 0)
        {
            if (hotKind[index] == 2)
            {
                Texture2D item = NetClient.LoadItemIcon(hotSkill[index]);
                if (item != null)
                {
                    hotIcon[index] = item;
                    return item;
                }
            }
            if (hotKind[index] == 1)
            {
                if (CharData.petSkills != null)
                    foreach (CharData.PetSkill skill in CharData.petSkills)
                        if (skill.id == hotSkill[index])
                        {
                            iconId = skill.icon;
                            break;
                        }
                if (iconId == 0 && CharData.petSkillIcons != null)
                    CharData.petSkillIcons.TryGetValue(hotSkill[index], out iconId);
            }
            else iconId = Win05.SkillIconId(hotSkill[index]);
        }
        if (iconId > 0 && ResLoader.I != null)
        {
            byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", iconId));
            if (data != null)
            {
                DimgDecoder.Result result = DimgDecoder.Decode(data, 0);
                if (result.ok)
                {
                    hotIcon[index] = result.tex;
                    TexScale.Register(result.tex, result.scale);
                }
            }
        }
        if (hotIcon[index] == null && hotKind[index] != 1 && ResLoader.I != null)
        {
            int alternate = Win05.SkillIconId(hotSkill[index]);
            if (alternate > 0 && alternate != iconId)
            {
                byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", alternate));
                if (data != null)
                {
                    DimgDecoder.Result result = DimgDecoder.Decode(data, 0);
                    if (result.ok)
                    {
                        hotIcon[index] = result.tex;
                        TexScale.Register(result.tex, result.scale);
                    }
                }
            }
        }
        return hotIcon[index];
    }
}

}
