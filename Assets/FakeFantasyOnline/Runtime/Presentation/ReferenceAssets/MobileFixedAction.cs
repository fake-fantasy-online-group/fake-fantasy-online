using System.Globalization;
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

// A presentation filter over the server-supplied effect description, not names/IDs.
// The existing inventory wire has no typed recovery fields. Keep the native recovery
// grammar here (including food and shared HP/MP amounts); the server still owns use,
// target, amount, inventory and cooldown validation. Never infer cooldown from this.
internal static class MobilePotionEligibility
{
    private static readonly string[] OtherUses = { "宠物", "目标", "城门", "结界", "守护灵", "获得", "穿戴", "装备", "上限", "复活" };
    private const string Instant = @"(?:瞬间|立即)";
    private const string Verb = @"(?:恢复|回复|补充)";
    private const string Resource = @"(?:生命(?:值)?|血量|体力|法力(?:值)?|魔法(?:值)?)";
    private const string Resources = @"(?<resource>" + Resource + @")(?:\s*(?:和|与|及)\s*(?<resource>" + Resource + @"))*";
    private const string Amount = @"\s*(?<amount>\d+(?:\.\d+)?)\s*[%％]?\s*(?:点)?\s*";
    private static readonly Regex DirectRecovery = new Regex(
        @"(?:\d+(?:\.\d+)?\s*秒内|瞬间|立即)" + Verb + @"([^。，,；;！!\r\n]*)");
    private static readonly Regex ResourceAmount = new Regex(
        Resources + Amount + "|" + Amount + @"(?:的\s*)?" + Resources);
    // Native food also uses subject-before-verb: 生命恢复150 / 体力和魔法瞬间回复30%.
    private static readonly Regex ResourceFirst = new Regex(Resources + Instant + "?" + Verb + Amount);
    private static readonly Regex ShortRecovery = new Regex(Instant + @"(?<health>加血|回血)(?<mana>回法)?" + Amount);

    internal static bool Allows(MobileFixedAction action, string description)
    {
        if ((action != MobileFixedAction.HealthPotion && action != MobileFixedAction.ManaPotion) ||
            string.IsNullOrEmpty(description)) return false;
        foreach (string otherUse in OtherUses)
            if (description.Contains(otherUse)) return false;
        foreach (Match direct in DirectRecovery.Matches(description))
            foreach (Match amount in ResourceAmount.Matches(direct.Groups[1].Value))
                if (HasResource(action, amount)) return true;
        foreach (Match amount in ResourceFirst.Matches(description))
            if (HasResource(action, amount)) return true;
        foreach (Match amount in ShortRecovery.Matches(description))
            if (PositiveAmount(amount) && (action == MobileFixedAction.HealthPotion || amount.Groups["mana"].Success)) return true;
        return false;
    }

    private static bool PositiveAmount(Match match) =>
        double.TryParse(match.Groups["amount"].Value, NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out double amount) && amount > 0;

    private static bool HasResource(MobileFixedAction action, Match match)
    {
        if (!PositiveAmount(match)) return false;
        foreach (Capture resource in match.Groups["resource"].Captures)
        {
            bool mana = resource.Value.StartsWith("法力") || resource.Value.StartsWith("魔法");
            if (mana == (action == MobileFixedAction.ManaPotion)) return true;
        }
        return false;
    }
}
}
