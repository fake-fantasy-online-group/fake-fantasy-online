using System.Text.RegularExpressions;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal enum MobileFixedAction { Mount = 0, HealthPotion = 1, ManaPotion = 2, Sit = 3 }

internal readonly struct MobileFixedActionState
{
    internal MobileFixedActionState(string label, Texture2D icon, bool available, int count,
        float cooldownFraction, int itemId, string reason, bool active = false)
    {
        Label = label; Icon = icon; Available = available; Count = count;
        CooldownFraction = cooldownFraction; ItemId = itemId; Reason = reason; Active = active;
    }
    internal string Label { get; }
    internal Texture2D Icon { get; }
    internal bool Available { get; }
    internal int Count { get; }
    internal float CooldownFraction { get; }
    internal int ItemId { get; }
    internal string Reason { get; }
    internal bool Active { get; }
}

// This is a deliberately narrow presentation filter, not an item-effect implementation.
// The inventory protocol publishes descriptions but no typed HP/MP recovery category.
// Only the established direct self-recovery wording is eligible; names and IDs never decide.
internal static class MobilePotionEligibility
{
    private static readonly string[] OtherUses = { "宠物", "目标", "城门", "结界", "获得", "穿戴", "装备", "上限" };
    private static readonly Regex DirectRecovery = new Regex(
        @"(?:\d+(?:\.\d+)?\s*秒内|瞬间|立即)(?:恢复|回复)([^。\r\n]*)");
    private const string Amount = @"\d+(?:\.\d+)?\s*[%％]?\s*(?:点)?";
    private static readonly Regex HealthAmount = new Regex(@"生命\s*(?:值)?\s*" + Amount + "|" + Amount + @"\s*生命");
    private static readonly Regex ManaAmount = new Regex(@"法力\s*(?:值)?\s*" + Amount + "|" + Amount + @"\s*法力");

    internal static bool Allows(MobileFixedAction action, string description)
    {
        if ((action != MobileFixedAction.HealthPotion && action != MobileFixedAction.ManaPotion) ||
            string.IsNullOrEmpty(description)) return false;
        foreach (string otherUse in OtherUses)
            if (description.Contains(otherUse)) return false;
        Match direct = DirectRecovery.Match(description);
        if (!direct.Success) return false;
        return (action == MobileFixedAction.HealthPotion ? HealthAmount : ManaAmount).IsMatch(direct.Groups[1].Value);
    }
}
}
