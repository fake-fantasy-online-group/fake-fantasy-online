using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;

class EndgameSkillVariantsContract
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    static void Reject(Action action, string name) { try { action(); } catch (FormatException) { checks++; return; } throw new Exception(name); }
    static Dictionary<string, object> Map(params object[] values) { var d = new Dictionary<string, object>(); for (int i = 0; i < values.Length; i += 2) d.Add((string)values[i], values[i+1]); return d; }
    static Dictionary<string, object> Variant() => Map("id", "sword_gale_fan", "name", "拂岳", "sourceUID", "9223372036854775807", "item", 123L, "description", "罡气剑改为近身扇面，最多4敌。", "maxTargets", 4L);
    static Dictionary<string, object> Behavior(Dictionary<string, object> variant) {
        var d = Map("mpCost", 19L, "prepareMs", 0L, "cooldownMs", 1200L, "castDistance", 100L,
            "usable", true, "requiresInvisible", false, "requiredWeaponType", 0L, "inputMode", variant == null ? "entity" : "direction",
            "targetSelf", false, "targetEnemy", true, "targetTeam", false, "allowsDeadTarget", false, "deadTargetOnly", false,
            "area", Map("shape", variant == null ? "none" : "cone", "center", "caster", "radius", variant == null ? 0L : 100L, "length", 0L, "width", 0L, "angle", variant == null ? 0L : 90L),
            "groundEffect", false, "pointRequired", false, "selfFallback", false);
        if (variant != null) d.Add("variant", variant); return d;
    }
    static EndgameResponse Response(Dictionary<string, object> variant, string revision = "1", bool rectangle = false) {
        var behavior = Behavior(variant);
        if (rectangle) behavior["area"] = Map("shape", "rectangle", "center", "caster", "radius", 0L, "length", 240L, "width", 48L, "angle", 0L);
        return EndgameResponse.Parse(BoundedJson.Serialize(Map("kind", "skill.ranks", "ok", true, "revision", revision,
            "skillRanks", new object[] { Map("id", 10102L, "hardRank", 1L, "grantRank", "0", "softRank", "0", "effectiveRank", "1", "canCast", true, "canInvest", true,
                "currentEffectLines", variant == null ? new[] { "原始技能描述" } : new[] { "装备形态：拂岳", "原始技能描述" },
                "contract", Map("name", "罡气剑", "icon", 3L, "profession", 2L, "kind", "single", "unlockLevel", 1L, "prerequisiteSkillIds", new object[0], "current", behavior, "next", null)) },
            "result", Map("hardCap", "20", "playerCap", "100", "petCap", "80", "unspentPoints", "3", "formulaVersion", "5"))));
    }
    static void Hello(EndgameClientState state, params string[] caps) { state.Begin(new EndgameRequest("hello")); state.Apply(EndgameResponse.Parse(BoundedJson.Serialize(Map("kind", "hello", "ok", true, "capabilities", caps)))); }
    static readonly string[] Caps = { "endgame.v1", "endgame.growth.v5", "endgame.skill-behavior.v1", "endgame.skill-variants.v1" };
    static void Main(string[] args) {
        Check(new EndgameRequest("hello").Json.Contains("endgame.skill-variants.v1"), "advertise variants");
        var s = new EndgameClientState(); EndgamePanel.GrowthState = s; CharData.race = 1;
        foreach (string missing in Caps) {
            var caps = new List<string>(Caps); caps.Remove(missing); Hello(s, caps.ToArray());
            Check(!s.SkillVariantsV1, "requires " + missing); s.Reset();
        }
        Hello(s, Caps[0], Caps[1], Caps[2]);
        Check(s.Apply(Response(null)) && s.CurrentSkillBehavior(10102).Variant == null, "old behavior remains valid");
        Check(!s.Apply(Response(Variant())) && s.CurrentSkillVariant(10102) == null && s.CurrentSkillBehavior(10102).InputMode == "entity", "unnegotiated transformed packet rejected atomically");
        Hello(s, Caps); Check(s.SkillVariantsV1 && !s.HasSkillRanks, "cap upgrade invalidates pre-variant rank snapshot");
        Check(s.Apply(Response(Variant())), "new behavior accepted");
        var v = s.CurrentSkillVariant(10102); var b = s.CurrentSkillBehavior(10102);
        Check(v.ID == "sword_gale_fan" && v.Name == "拂岳" && v.SourceUID == "9223372036854775807" && v.Item == 123 && v.MaxTargets == 4, "metadata including lossless UID");
        Check(b.MPCost == 19 && b.PrepareMilliseconds == 0 && b.CooldownMilliseconds == 1200, "single authoritative cost/timing plan");
        AuthoritativeProfessionSkill.TryInfo(10102, out var info);
        Check(info.name == "罡气剑" && info.pre == 0 && info.radius == 100 && info.rwidth == 90 && info.shape == 3 && info.tgt == 2, "original identity and transformed metadata");
        AuthoritativeProfessionSkill.TryResolve(10102, out var aim, out var range, out var radius, out var angle, out var length, out var width, out var trap);
        Check(aim == SkillTargeting.Aim.Direction && radius == 100 && angle == 90 && length == 0 && width == 0 && v.MaxTargets == 4 && !trap, "cone geometry and target count agree");
        Check(s.Apply(Response(Variant(), "1", true)), "same revision equipment replacement accepted");
        AuthoritativeProfessionSkill.TryResolve(10102, out aim, out range, out radius, out angle, out length, out width, out trap);
        Check(aim == SkillTargeting.Aim.Direction && length == 240 && width == 48 && radius == 0, "line geometry replaces cone without stale cache");
        AuthoritativeProfessionSkill.TryInfo(10102, out info); Check(info.rectl == 240 && info.rectw == 48 && info.shape == 1, "version cache invalidated");
        s.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"ok\":true,\"revision\":\"2\"}"));
        Check(s.CurrentSkillVariant(10102) == null && s.CurrentSkillBehavior(10102) == null, "stale behavior and form unavailable");
        Check(!s.Apply(Response(Variant())), "old snapshot cannot reenable variant");
        Check(s.Apply(Response(null, "2")), "unequip snapshot restores base skill");
        AuthoritativeProfessionSkill.TryResolve(10102, out aim, out range, out radius, out angle, out length, out width, out trap);
        Check(aim == SkillTargeting.Aim.Single && radius == 0 && s.CurrentSkillVariant(10102) == null, "unequip clears geometry and variant");
        Check(s.Apply(Response(Variant(), "2")), "re-equip same revision");
        s.Begin(new EndgameRequest("hello")); s.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":false,\"error\":\"busy\"}"));
        Check(s.SkillVariantsV1 && s.CurrentSkillVariant(10102) != null, "failed renegotiation preserves active ownership");
        Hello(s, Caps[0], Caps[1], Caps[2]); Check(!s.SkillVariantsV1 && !s.HasSkillRanks, "cap downgrade clears variant snapshots");
        Hello(s, Caps); s.Apply(Response(Variant())); s.Reset(); Check(!s.SkillVariantsV1 && s.CurrentSkillVariant(10102) == null, "disconnect clears variant state");
        foreach (string key in Variant().Keys) { var d = Variant(); d.Remove(key); Reject(() => Response(d), "missing " + key); }
        foreach (var test in new[] { Map("sourceUID", 42L), Map("sourceUID", "00"), Map("sourceUID", "0"), Map("sourceUID", "9223372036854775808"), Map("item", 0L), Map("item", (long)int.MaxValue+1), Map("maxTargets", 0L), Map("maxTargets", -1L), Map("id", new string('x',65)), Map("name", new string('x',129)), Map("description", new string('x',2049)), Map("extra", 1L) }) {
            var d = Variant(); foreach (var pair in test) d[pair.Key] = pair.Value; Reject(() => Response(d), "strict bounded variant " + string.Join(",", test.Keys));
        }
        foreach (var property in typeof(EndgameSkillVariantView).GetProperties()) Check(!property.CanWrite, "immutable variant " + property.Name);
        foreach (string path in args) {
            string text = File.ReadAllText(path); Check(Encoding.UTF8.GetByteCount(text) < EndgameResponse.MaximumBytes, "bounded real packet");
            var response = EndgameResponse.Parse(text); Hello(s, Caps); Check(s.Apply(response), "actual server fixture " + path);
            foreach (var row in s.SkillRanks) {
                var current = row.Contract?.Current; if (current?.Variant == null) continue;
                CharData.race = row.Contract.Profession - 1;
                AuthoritativeProfessionSkill.TryResolve(row.ID, out aim, out range, out radius, out angle, out length, out width, out trap);
                Check(radius == current.Area.Radius && angle == current.Area.Angle && length == current.Area.Length && width == current.Area.Width, "actual plan indicator " + row.ID);
                Check(s.CurrentSkillVariant(row.ID).MaxTargets == (row.ID == 10017 || row.ID == 10102 ? 4 : 1), "actual finite target cap " + row.ID);
                AuthoritativeProfessionSkill.TryInfo(row.ID, out info);
                Check(info.name == row.Contract.Name && info.tgt == (current.InputMode == "direction" || current.InputMode == "point" ? 2 : 1), "actual original identity/transformed input mode " + row.ID);
                if (row.ID == 10401) Check(current.PrepareMilliseconds == 0 && current.CooldownMilliseconds >= 1200 && current.MPCost > 0, "quick fire never bypasses MP or cooldown");
                Check(row.CurrentEffectLines.Count > 0 && row.CurrentEffectLines[0].Contains(current.Variant.Name), "actual variant first line " + row.ID);
            }
            Console.WriteLine("Server variant fixture: " + Path.GetFileName(path) + " " + Encoding.UTF8.GetByteCount(text) + " bytes");
            s.Reset();
        }
        Console.WriteLine("PASS " + checks + " skill variant DTO/capability/state/production targeting checks (no Unity/Player).");
    }
}
