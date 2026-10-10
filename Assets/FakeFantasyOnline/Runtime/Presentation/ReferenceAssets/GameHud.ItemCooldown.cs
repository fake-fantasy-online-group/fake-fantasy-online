using System.Text.RegularExpressions;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    private static float ItemHotSecs(int itemId)
    {
        string description = InventoryDlg.ItemInfo(itemId).Item2;
        if (string.IsNullOrEmpty(description)) return 0f;
        Match match = Regex.Match(description, @"(\d+)\s*秒内");
        if (match.Success && float.TryParse(match.Groups[1].Value, out float seconds)) return seconds;
        return 0f;
    }

    public static float CdFrac(int id) => SkillCdFrac(id);
    public static void StartItemCd(int itemId) => StartItemCooldownForIndependentRuntime(itemId);

    // Reports the existing facade's result without parsing the description or starting twice.
    internal static bool StartItemCooldownForIndependentRuntime(int itemId)
    {
        float seconds = ItemHotSecs(itemId);
        if (seconds > 0f)
        {
            StartSkillCooldown(itemId, seconds);
            return itemId >= 1;
        }
        return false;
    }
}

}
