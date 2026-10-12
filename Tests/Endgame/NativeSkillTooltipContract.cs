// Executes production BuildGrowthSkillTip -> ComposeTip -> UITip.Layout/Render.
// The font records real production draw calls; only Unity, art and font I/O are fake.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class NativeSkillTooltipContract
{
    private static int checks;
    private static readonly BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly Color Blue = new Color32(86, 196, 255, 255);
    private static readonly Color Green = new Color32(120, 220, 120, 255);
    private static readonly Color Gray = new Color32(128, 128, 128, 255);
    private static readonly Color Orange = new Color32(255, 136, 48, 255);
    private const string CurrentEffects = "[\"造成123.45%法术伤害\",\"法力消耗: 12\",\"冷却时间: 2秒\"]";
    private const string NextEffects = "[\"造成125.45%法术伤害\",\"法力消耗: 13\",\"冷却时间: 1.8秒\"]";
    private const string Extra = ",\"nextRequiredLevel\":45,\"trainingSynergies\":[" +
        "{\"sourceId\":10402,\"sourceName\":\"火焰专精\",\"effect\":\"damage\",\"effectLabel\":\"伤害\",\"perPointValue\":100,\"unit\":\"percent_bp\",\"investedHardPoints\":3,\"contributionValue\":300,\"equipmentApplies\":false}," +
        "{\"sourceId\":10402,\"sourceName\":\"火焰专精\",\"effect\":\"targets\",\"effectLabel\":\"目标\",\"perPointValue\":2,\"unit\":\"flat\",\"investedHardPoints\":3,\"contributionValue\":6,\"equipmentApplies\":false}," +
        "{\"sourceId\":10405,\"sourceName\":\"余烬\",\"effect\":\"duration\",\"effectLabel\":\"持续时间\",\"perPointValue\":50,\"unit\":\"percent_bp\",\"investedHardPoints\":0,\"contributionValue\":0,\"equipmentApplies\":false}," +
        "{\"sourceId\":10408,\"sourceName\":\"寒冰专精\",\"effect\":\"damage\",\"effectLabel\":\"伤害\",\"perPointValue\":200,\"unit\":\"percent_bp\",\"investedHardPoints\":2,\"contributionValue\":400,\"equipmentApplies\":false}]," +
        "\"nativeModifierLines\":[\"天赋伤害加成: 5%\"]";

    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static bool Near(float left, float right) => Math.Abs(left - right) < 0.0001f;
    private static bool Same(Color left, Color right) => Near(left.r, right.r) && Near(left.g, right.g) && Near(left.b, right.b) && Near(left.a, right.a);
    private static void Reject(string json, string message)
    {
        try { EndgameResponse.Parse(json); }
        catch (FormatException) { checks++; return; }
        catch (ArgumentException) { checks++; return; }
        throw new Exception(message);
    }
    private static int Count(string text, string part) => (text.Length - text.Replace(part, "").Length) / part.Length;
    private static string Rank(int hard = 19, string soft = "7", string effective = "26", string next = "27",
        bool cast = true, bool invest = true, string grant = "0", bool additive = true, string current = CurrentEffects, string upcoming = NextEffects, string changes = null) =>
        "{\"id\":10401,\"hardRank\":" + hard + ",\"grantRank\":\"" + grant + "\",\"softRank\":\"" + soft +
        "\",\"effectiveRank\":\"" + effective + "\",\"canCast\":" + cast.ToString().ToLowerInvariant() +
        ",\"canInvest\":" + invest.ToString().ToLowerInvariant() + ",\"blockedReasons\":[\"需要完成职业任务\"]," +
        "\"currentEffectLines\":" + current + ",\"nextEffectLines\":" + upcoming +
        ",\"contributions\":[{\"sourceId\":\"item:equipment-only\",\"scope\":\"all\",\"levels\":7}]" +
        (additive ? ",\"nextEffectiveRank\":\"" + next + "\"" + Extra : "") + (changes == null ? "" : ",\"nextChangeLines\":" + changes) + "}";
    private static string Ranks(string rank, string revision = "1", int points = 3) =>
        "{\"kind\":\"skill.ranks\",\"ok\":true,\"revision\":\"" + revision + "\",\"skillRanks\":[" + rank +
        "],\"result\":{\"hardCap\":\"20\",\"playerCap\":\"100\",\"petCap\":\"80\",\"formulaVersion\":\"5\",\"unspentPoints\":\"" + points + "\"}}";
    private static EndgameClientState State(string rank = null)
    {
        var state = new EndgameClientState();
        Check(state.Begin(new EndgameRequest("hello")), "Start production hello state");
        Check(state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}")), "Apply production V5 capability");
        if (rank != null) Check(state.Apply(EndgameResponse.Parse(Ranks(rank))), "Apply production rank DTO");
        EndgamePanel.GrowthState = state;
        return state;
    }
    private static (List<(string, Color, bool)>, float, float) Layout(string text, float scale = 1f) =>
        ((List<(string, Color, bool)>, float, float))typeof(UITip).GetMethod("Layout", HiddenStatic).Invoke(null, new object[] { text, scale });
    private static void Render(string text, float scale, Vector2 mouse, string comparison = null)
    {
        BitmapFont.Draws.Clear(); GUI.Draws.Clear();
        typeof(UITip).GetMethod("Render", HiddenStatic).Invoke(null, new object[] { text, comparison, scale, mouse });
    }
    private static (string, Color, bool) Row(List<(string, Color, bool)> rows, string text) =>
        rows.Single(row => row.Item1.Trim() == text);
    private static void CheckRow(List<(string, Color, bool)> rows, string text, Color color, int padding = -1)
    {
        var row = Row(rows, text);
        Check(Same(row.Item2, color), "Actual native row color: " + text);
        Check(!row.Item3, "ComposeTip uses native space padding, not a new center marker: " + text);
        if (padding >= 0) Check(row.Item1 == new string(' ', padding) + text, "Native centered padding: " + text);
    }
    private static void CheckRender(string text, float scale, Vector2 mouse)
    {
        var layout = Layout(text, scale);
        var originalTint = new Color(0.2f, 0.3f, 0.4f, 0.5f);
        GUI.color = originalTint;
        Render(text, scale, mouse);
        Check(GUI.Draws.Count == 9 && GUI.Draws.Count(draw => draw.HasCoordinates) == 8, "Actual native DrawBox background and eight art edges");
        Check(Same(GUI.color, originalTint), "Native renderer restores GUI tint");
        Check(Same(GUI.Draws[0].Color, new Color32(16, 16, 16, 196)), "Native translucent background color");
        Check(BitmapFont.Draws.Count == layout.Item1.Count, "Actual Render reaches BitmapFont.Draw for every row, including spacers");
        float x = mouse.x + 16f * scale, y = mouse.y + 16f * scale;
        if (x + layout.Item2 > Screen.width) x = mouse.x - layout.Item2 - 4f * scale;
        if (y + layout.Item3 > Screen.height) y = mouse.y - layout.Item3 - 4f * scale;
        x = Math.Max(0f, x); y = Math.Max(0f, y);
        var box = GUI.Draws[0].Area;
        Check(Near(box.x, x) && Near(box.y, y) && Near(box.width, layout.Item2) && Near(box.height, layout.Item3), "Native box dimensions and screen placement at scale " + scale);
        for (int index = 0; index < layout.Item1.Count; index++)
        {
            var expected = layout.Item1[index]; var draw = BitmapFont.Draws[index];
            float rowX = expected.Item3 ? x + (layout.Item2 - BitmapFont.Measure(expected.Item1, scale)) * 0.5f : x + 6f * scale;
            Check(draw.Text == expected.Item1 && Same(draw.Color, expected.Item2), "Rendered row text/color " + index);
            Check(Near(draw.X, rowX) && Near(draw.Y, y + 6f * scale + index * 16f * scale) && Near(draw.Scale, scale), "Rendered row position and scale " + index);
            Check(draw.Text.All(value => !char.IsControl(value)), "No protocol markers leak into glyph rendering " + index);
            Check(BitmapFont.Measure(draw.Text, scale) <= 150f * scale, "Production wraps every row to native width " + index);
        }
        Check(Near(layout.Item2, 12f * scale + layout.Item1.Max(row => BitmapFont.Measure(row.Item1, scale))) &&
            Near(layout.Item3, 8f * scale + 16f * scale * layout.Item1.Count), "Native padding and row-height dimensions");
    }
    private static void HierarchyAndDrawing()
    {
        var state = State(Rank());
        Check(state.CanInvestSkill(10401), "Hard19 can invest with fresh authoritative state");
        string text = Win05.TestNativeGrowthTip();
        var rows = Layout(text).Item1;
        CheckRow(rows, "火弹术", Blue, 13);
        CheckRow(rows, "等级: 26", Color.white, 12);
        CheckRow(rows, "下一等级: 27", Color.white, 10);
        CheckRow(rows, "需要等级: 45", Green);
        CheckRow(rows, "投入点数: 19/20", Color.white);
        CheckRow(rows, "装备与状态: +7级", new Color32(96, 230, 230, 255));
        CheckRow(rows, "受到以下技能加成", Green, 8);
        CheckRow(rows, "火焰专精（投入3点）", Green);
        CheckRow(rows, "每点: 伤害 +1%", Green);
        CheckRow(rows, "每点: 目标 +2", Green);
        CheckRow(rows, "余烬（投入0点）", Green);
        CheckRow(rows, "每点: 持续时间 +0.5%", Green);
        CheckRow(rows, "装备加级不增加协同", Gray);
        CheckRow(rows, "天赋伤害加成: 5%", Green);
        Check(Count(text, "火焰专精（投入3点）") == 1, "Multiple effects for one contiguous source share the source heading");
        Check(text.Contains("装备与状态: +7级") && !text.Contains("item:equipment-only") && !text.Contains("+300%"), "Equipment source IDs and aggregate contribution are not rendered as per-hard-point training");
        Check(text.IndexOf("造成123.45%法术伤害", StringComparison.Ordinal) < text.IndexOf("下一等级", StringComparison.Ordinal) &&
            text.IndexOf("造成125.45%法术伤害", StringComparison.Ordinal) < text.IndexOf("受到以下技能加成", StringComparison.Ordinal), "Current, next and synergy sections stay separate and ordered");
        foreach (string effect in new[] { "造成123.45%法术伤害", "法力消耗: 12", "冷却时间: 2秒", "造成125.45%法术伤害", "法力消耗: 13", "冷却时间: 1.8秒" })
            CheckRow(rows, effect, Color.white);
        CheckRow(rows, "剩余技能点: 3", Orange);
        Check(text.Contains("\n\n"), "Native blank separator rows remain");
        CheckRender(text, 1f, new Vector2(30f, 40f));
        Check(GUI.Draws[0].Area.xMax <= 1280f && GUI.Draws[0].Area.yMax <= 900f,
            "Representative current/next card with three training sources fits 1280x900 at scale one");
        Console.WriteLine("INFO representative three-source card: " + rows.Count + " rendered rows, " + Layout(text).Item3 + "px at 1280x900 scale 1 (deterministic font).");
        Screen.width = 1920; Screen.height = 1080;
        CheckRender(text, 2f, new Vector2(80f, 90f));
        Screen.width = 1280; Screen.height = 900;
        CheckRender(text, 1f, new Vector2(Screen.width - 2f, Screen.height - 2f));
        Check(ResLoader.Paths.Count == 8 && ResLoader.Paths.All(path => path.StartsWith("ui/equipdlg05/", StringComparison.Ordinal)), "Production Ensure loads and reuses native tooltip chrome");
        var training = rows.Where(row => Same(row.Item2, Green) || Same(row.Item2, Gray)).Select(row => row.Item1).ToArray();
        State(Rank(soft: "184467440737095516170000001", effective: "184467440737095516170000020"));
        var moreEquipment = Layout(Win05.TestNativeGrowthTip()).Item1.Where(row => Same(row.Item2, Green) || Same(row.Item2, Gray)).Select(row => row.Item1).ToArray();
        Check(training.SequenceEqual(moreEquipment), "Increasing equipment ranks does not alter training-source or per-point rows");
    }
    private static void CapUnlearnedAndHotbar()
    {
        var capped = State(Rank(hard: 20, effective: "27", next: "28", invest: false));
        string text = Win05.TestNativeGrowthTip();
        Check(text.Contains("投入点数: 20/20") && text.Contains("等级: 27") && !capped.CanInvestSkill(10401), "Hard20 retains soft effective rank and blocks investment");
        Check(!text.Contains("下一等级") && !text.Contains("造成125.45%") && !text.Contains("需要等级: 45"), "Hard20 hides next investment effects and requirement even if server supplies them");
        CheckRow(Layout(text).Item1, "受到以下技能加成", Green, 8);
        State(Rank(hard: 0, effective: "0", next: "8", cast: false));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("等级: 未学习") && text.Contains("投入1点后: 等级 8") && text.Contains("装备与状态: +7级"), "Unlearned preview includes server's next effective rank and soft contribution");
        Check(!text.Contains("造成123.45%") && Count(text, "造成125.45%") == 1 && !text.Contains("下一等级"), "Unlearned first-investment preview appears once, never as current effects");
        Check(!text.Contains("双击或拖曳") && text.Contains("需要等级: 45"), "Unlearned card has requirement, no cast invitation");
        CheckRender(text, 1f, new Vector2(25f, 25f));
        State(Rank());
        text = Win05.TestNativeGrowthTip(includeNext: false);
        Check(text.Contains("造成123.45%") && !text.Contains("造成125.45%") && !text.Contains("下一等级"), "Hotbar shows only current effects");
        Check(!text.Contains("剩余技能点:") && !text.Contains("需要完成职业任务") && !text.Contains("需要等级: 45"), "Hotbar omits investment footer, blocked reasons and next requirement");
        Check(text.Contains("受到以下技能加成") && text.Contains("余烬（投入0点）"), "Hotbar retains actual training source disclosure");
        CheckRender(text, 1f, new Vector2(40f, 40f));
        State(Rank(hard: 0, soft: "0", effective: "3", next: "4", grant: "3"));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("授予等级: 3") && !text.Contains("装备与状态:"), "Granted casting rank is separate and zero equipment summary is omitted");
    }
    private static void SemanticNextPreview()
    {
        const string current = "[\"造成123.45%法术伤害\",\"对目标：寒冷持续2秒\",\"移动速度降低10%\",\"法力消耗: 12\",\"冷却时间: 2秒\"]";
        const string next = "[\"造成123.45%法术伤害\",\"对目标：寒冷持续3秒\",\"移动速度降低10%\",\"法力消耗: 12\",\"冷却时间: 2秒\"]";
        const string changes = "[\"对目标：寒冷持续3秒\",\"移动速度降低10%\",\"其余效果不变\"]";
        var state = State(Rank(current: current, upcoming: next, changes: changes));
        var rank = state.SkillRank(10401);
        Check(rank.CurrentEffectLines.Count == 5 && rank.NextEffectLines.Count == 5 && rank.NextChangeLines.Count == 3,
            "DTO keeps full current/next API effects separately from compact next-change preview");
        string text = Win05.TestNativeGrowthTip();
        Check(text.Contains("对目标：寒冷持续2秒") && text.Contains("对目标：寒冷持续3秒") &&
            Count(text, "移动速度降低10%") == 2, "Compact next preview preserves supplied target/status semantic block and child effect");
        Check(Count(text, "造成123.45%法术伤害") == 1 && Count(text, "法力消耗: 12") == 1 &&
            Count(text, "冷却时间: 2秒") == 1 && text.Contains("其余效果不变"), "Learned preview uses server changes with explicit unchanged message, full current retained");
        Check(rank.NextEffectLines.Contains("法力消耗: 12") && rank.NextEffectLines.Contains("冷却时间: 2秒"),
            "Compact presentation cannot discard full server next-effect API data");
        CheckRender(text, 1f, new Vector2(20f, 30f));
        string hotbar = Win05.TestNativeGrowthTip(includeNext: false);
        Check(!hotbar.Contains("寒冷持续3秒") && !hotbar.Contains("其余效果不变"), "Hotbar never leaks compact next preview");
        State(Rank(hard: 0, effective: "0", cast: false, current: current, upcoming: next, changes: changes));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("寒冷持续3秒") && text.Contains("法力消耗: 12") && text.Contains("冷却时间: 2秒") &&
            !text.Contains("其余效果不变") && !text.Contains("寒冷持续2秒"), "Unlearned first investment uses full next effects even when a compact change field is supplied");
        foreach (string optionalChanges in new string[] { null, "[]" })
        {
            State(Rank(current: current, upcoming: next, changes: optionalChanges));
            text = Win05.TestNativeGrowthTip();
            Check(Count(text, "法力消耗: 12") == 2 && text.Contains("寒冷持续3秒") && !text.Contains("其余效果不变"),
                "Missing or empty optional compact preview preserves full-next fallback");
        }
        State(Rank(hard: 20, invest: false, current: current, upcoming: next, changes: changes));
        Check(!Win05.TestNativeGrowthTip().Contains("寒冷持续3秒"), "Hard20 hides compact next-investment section too");
        Reject(Ranks(Rank(changes: "[42]")), "Compact next-preview rows must be strings");
    }
    private static void ExactRanksAndCompatibility()
    {
        foreach (string rank in new[] { "20", "21", "99", "100", "101", "255", "256", "65535", "65536", "184467440737095516170000001" })
        {
            const string next = "184467440737095516170000002";
            var state = State(Rank(effective: rank, next: next));
            string text = Win05.TestNativeGrowthTip();
            Check(text.Contains("等级: " + rank) && text.Contains("下一等级: " + next), "Exact decimal current and server next rank: " + rank);
            Check(state.SkillRank(10401).EffectiveRank == rank && state.SkillRank(10401).NextEffectiveRank == next, "DTO strings never narrow: " + rank);
            Render(text, 1f, new Vector2(0f, 0f));
            string drawn = string.Concat(BitmapFont.Draws.Select(draw => draw.Text));
            Check(drawn.Contains("等级: " + rank) && drawn.Contains("下一等级: " + next), "All exact digits survive actual wrap and render: " + rank);
        }
        var old = State(Rank(additive: false));
        string legacy = Win05.TestNativeGrowthTip();
        Check(old.SkillRank(10401).NextEffectiveRank == "" && old.SkillRank(10401).NextChangeLines.Count == 0 && old.SkillRank(10401).TrainingSynergies.Count == 0 && old.SkillRank(10401).NativeModifierLines.Count == 0, "Older V5 payload can omit all additive tooltip fields");
        Check(legacy.Contains("下一等级") && legacy.Contains("造成125.45%") && !legacy.Contains("等级: 27") && !legacy.Contains("受到以下技能加成"), "Older snapshot shows supplied effects without inventing next rank or synergies");
        State(Rank(current: "[]", upcoming: "[]"));
        string missingEffects = Win05.TestNativeGrowthTip();
        Check(missingEffects.Contains("服务器尚未提供技能效果") && !missingEffects.Contains("下一等级"), "Missing authoritative effects are explicit and never replaced by native rank-five prose");
        string valid = Ranks(Rank());
        Reject(valid.Replace("\"equipmentApplies\":false", "\"equipmentApplies\":true"), "Training DTO must reject equipment-derived synergy");
        Reject(valid.Replace("\"contributionValue\":300", "\"contributionValue\":1000"), "Training DTO must reject contribution not equal to hard points times rate");
        Reject(valid.Replace("\"sourceId\":10405", "\"sourceId\":11005"), "Life skill cannot be a training source");
        Reject(valid.Replace("\"sourceId\":10405", "\"sourceId\":10401"), "Self synergy cannot become a source");
    }
    private static void PendingStaleAndRouting()
    {
        var state = State();
        string text = Win05.TestNativeGrowthTip();
        Check(text.Contains("正在等待服务器技能资料") && !text.Contains("旧五级") && !text.Contains("下一等级"), "Missing rank is an explicit waiting card");
        var rows = Layout(text).Item1;
        CheckRow(rows, "火弹术", Blue);
        Check(rows.Skip(1).All(row => Same(row.Item2, Orange)), "Waiting message keeps native orange, including wrapped continuation");
        state = State(Rank());
        Check(state.CanInvestSkill(10401), "Fresh state allows investment");
        var request = new EndgameRequest("skill.upgrade", "1", skillID: 10401);
        Check(state.Begin(request) && state.Pending && !state.CanInvestSkill(10401), "Pending request blocks repeated investment");
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("（确认中）") && !text.Contains("（更新中）"), "Pending explanation wins over refresh message");
        Check(!Win05.TestNativeGrowthTip(includeNext: false).Contains("（确认中）"), "Hotbar omits pending investment footer");
        Check(state.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"requestId\":\"" + request.RequestID + "\",\"ok\":true,\"revision\":\"2\"}")), "Apply upgrade receipt without rank invention");
        text = Win05.TestNativeGrowthTip();
        Check(!state.Pending && !state.CanInvestSkill(10401) && text.Contains("（更新中）") && !text.Contains("（确认中）"), "Stale rank waits for authoritative refresh after receipt");
        Check(!state.Apply(EndgameResponse.Parse(Ranks(Rank(), "1"))), "Older rank response cannot resolve stale state");
        Check(state.Apply(EndgameResponse.Parse(Ranks(Rank(hard: 20, effective: "27", invest: false), "2", 2))), "Fresh rank completes update");
        Check(!Win05.TestNativeGrowthTip().Contains("（更新中）") && Win05.TestDisplayedSkillPoints == 2, "Fresh rank clears stale footer and refreshes points");
        foreach (int id in new[] { 10001, 10401, 10435 }) Check(Win05.TestUsesGrowthSkill(id), "Profession routes to V5: " + id);
        foreach (int id in new[] { 13601, 13602, 11005, 10403 }) Check(!Win05.TestUsesGrowthSkill(id), "Starter, life and undefined skills keep native route: " + id);
        EndgamePanel.GrowthState = null; CharData.skillPoints = 9;
        Check(!Win05.TestUsesGrowthSkill(10401) && Win05.TestDisplayedSkillPoints == 9, "No growth owner preserves original native route and points");
    }
    private static void RequestAndComparisonRendering()
    {
        State(Rank()); string text = Win05.TestNativeGrowthTip();
        var mouse = new Vector2(40f, 50f);
        UITip.ShowAtForIndependentRuntime(text, 1f, mouse);
        Check(UITip.RequestedTextForIndependentRuntime == text && Near(UITip.RequestedMouseForIndependentRuntime.x, mouse.x), "Hosted request reaches original native request fields");
        object renderer = typeof(UITip).GetField("renderer", HiddenStatic).GetValue(null);
        MethodInfo onGui = renderer.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);
        Event.current = new Event { type = EventType.Layout };
        BitmapFont.Draws.Clear(); onGui.Invoke(renderer, null);
        Check(BitmapFont.Draws.Count == Layout(text).Item1.Count && UITip.RequestedTextForIndependentRuntime == text, "Actual renderer OnGUI draws and retains request until repaint");
        Event.current.type = EventType.Repaint; BitmapFont.Draws.Clear(); onGui.Invoke(renderer, null);
        Check(BitmapFont.Draws.Count == Layout(text).Item1.Count && UITip.RequestedTextForIndependentRuntime == null, "Actual renderer OnGUI consumes request after repaint");
        Render(text, 1f, mouse, "\u0005比较技能\n等级: 2");
        Check(GUI.Draws.Count == 18 && BitmapFont.Draws.Count == Layout(text).Item1.Count + 2, "Native comparison branch renders a second box without replacing skill layout");
        BitmapFont.Ready = false; Render(text, 1f, mouse);
        Check(BitmapFont.Draws.Count == 0 && GUI.Draws.Count == 0, "Native renderer does not draw before font readiness");
        BitmapFont.Ready = true; Event.current = null;
    }
    private static void CheckServerFixture(string path)
    {
        object fixture = BoundedJson.Parse(File.ReadAllText(path));
        var names = new Dictionary<int, string>();
        IReadOnlyList<object> ranks;
        var document = fixture as IReadOnlyDictionary<string, object>;
        if (document != null)
        {
            ranks = (IReadOnlyList<object>)document["skillRanks"];
            object suppliedNames;
            if (document.TryGetValue("skillNames", out suppliedNames))
                foreach (var entry in (IReadOnlyDictionary<string, object>)suppliedNames)
                    names.Add(int.Parse(entry.Key, CultureInfo.InvariantCulture), (string)entry.Value);
        }
        else ranks = (IReadOnlyList<object>)fixture;
        string namePath = Path.Combine(Path.GetDirectoryName(path), "skill-tooltip-names-v5.json");
        if (File.Exists(namePath))
        {
            var suppliedNames = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(File.ReadAllText(namePath));
            foreach (var entry in suppliedNames)
                names[int.Parse(entry.Key, CultureInfo.InvariantCulture)] = (string)entry.Value;
            Check(names.Count == 149, "Verified server name map covers all 149 profession skills");
        }
        // Older bare-array fixtures may supply some names through their source rows.
        foreach (var value in ranks)
        {
            var rank = (IReadOnlyDictionary<string, object>)value;
            object synergies;
            if (!rank.TryGetValue("trainingSynergies", out synergies)) continue;
            foreach (var entry in (IReadOnlyList<object>)synergies)
            {
                var synergy = (IReadOnlyDictionary<string, object>)entry;
                int id = Convert.ToInt32(synergy["sourceId"], CultureInfo.InvariantCulture);
                if (!names.ContainsKey(id)) names.Add(id, (string)synergy["sourceName"]);
            }
        }
        bool fullCatalog = ranks.Count == 149;
        var fixtureIDs = new HashSet<int>(ranks.Select(value => Convert.ToInt32(((IReadOnlyDictionary<string, object>)value)["id"], CultureInfo.InvariantCulture)));
        Check(fullCatalog || ranks.Count == 2 && fixtureIDs.SetEquals(new[] { 10405, 10410 }), "Server fixture must be complete149 catalog or explicit equipped10405/10410 subset");
        string fixtureLabel = fullCatalog ? "all149" : "equipped10405/10410";
        Check(fixtureIDs.All(id => names.ContainsKey(id)), "Every fixture skill must have a verified server skill name");
        var seen = new HashSet<int>();
        var failures = new List<string>();
        foreach (var viewport in new[] { new[] { 1280, 900, 96 }, new[] { 1920, 1080, 0 } })
        {
            Screen.width = viewport[0]; Screen.height = viewport[1]; Screen.dpi = viewport[2];
            UIScale.User = 1f;
            float scale = UIScale.Factor;
            Check(Near(scale, viewport[0] == 1280 ? 1f : 2f), "Production UIScale resolves expected native factor");
            float worst = 0f; int worstID = 0, worstRows = 0, unnamed = 0; string worstName = ""; object worstValue = null;
            foreach (object value in ranks)
            {
                // The catalog may exceed the wire limit. Each real DTO is independently
                // serialized and passed through the actual bounded wire parser/state.
                var state = State(BoundedJson.Serialize(value));
                var rank = state.SkillRanks.Single();
                if (viewport[0] == 1280) Check(seen.Add(rank.ID), "Unique server fixture skill ID " + rank.ID);
                Check(EndgameSkillRankView.IsProfessionSkill(rank.ID), "Fixture uses actual profession skill ID " + rank.ID);
                if (!fullCatalog)
                {
                    int soft = rank.ID == 10405 ? 3 : 4;
                    Check(rank.HardRank == 19 && rank.SoftRank == soft.ToString(CultureInfo.InvariantCulture) &&
                        rank.EffectiveRank == (19 + soft).ToString(CultureInfo.InvariantCulture) && rank.NextEffectiveRank == (20 + soft).ToString(CultureInfo.InvariantCulture),
                        "Real equipped fixture preserves hard19 plus legal gear and full next rank " + rank.ID);
                    Check(rank.CanCast && rank.CanInvest && state.CanInvestSkill(rank.ID), "Real equipped fixture exercises below-cap next investment " + rank.ID);
                    Check(rank.Contributions.Sum(value => value.Levels) == soft && rank.Contributions.All(value => value.SourceID.StartsWith("item:", StringComparison.Ordinal)),
                        "Real equipped fixture retains exact independent gear-contribution records " + rank.ID);
                    var training = rank.TrainingSynergies.Single();
                    Check(training.SourceID == (rank.ID == 10405 ? 10402 : 10409) && training.InvestedHardPoints == 10 &&
                        training.PerPointValue == 100 && training.ContributionValue == 1000 && !training.EquipmentApplies,
                        "Actual equipped gear does not inflate source10 hard-point synergy " + rank.ID);
                    Check(rank.CurrentEffectLines.Count == (rank.ID == 10405 ? 7 : 8) && rank.NextEffectLines.Count == rank.CurrentEffectLines.Count && rank.NextChangeLines.Count == 2,
                        "Real high-rank current/full-next effect API remains complete beside compact preview " + rank.ID);
                }
                string name;
                if (!names.TryGetValue(rank.ID, out name)) { name = "技能 " + rank.ID; unnamed++; }
                string text = Win05.TestNativeGrowthTip(rank.ID, true, name);
                Check(text.Contains(name), "Fixture uses verified native skill name " + rank.ID);
                foreach (var equipment in rank.Contributions)
                    Check(!text.Contains(equipment.SourceID), "Equipment identity does not leak into training section " + rank.ID);
                foreach (string effect in rank.CurrentEffectLines)
                    Check(text.Contains(effect), "Every authoritative current effect is preserved " + rank.ID + ": " + effect);
                foreach (string change in rank.NextChangeLines)
                    Check(text.Contains(change), "Every authoritative semantic next-change block is preserved " + rank.ID + ": " + change);
                foreach (var synergy in rank.TrainingSynergies)
                    Check(text.Contains(synergy.SourceName + "（投入" + synergy.InvestedHardPoints + "点）"), "Every fixture training source remains visible " + rank.ID);
                var layout = Layout(text, scale);
                if (Environment.GetEnvironmentVariable("FFO_SKILL_TOOLTIP_DUMP_ID") == rank.ID.ToString(CultureInfo.InvariantCulture) && Screen.width == 1920)
                    for (int row = 0; row < layout.Item1.Count; row++) Console.WriteLine("ROW " + rank.ID + " " + row + ": " + layout.Item1[row].Item1);
                Render(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
                Check(BitmapFont.Draws.Count == layout.Item1.Count, "Actual Render draws every fixture row " + rank.ID);
                Check(BitmapFont.Draws.Select(row => row.Text).SequenceEqual(layout.Item1.Select(row => row.Item1)), "Fixture render drops no effect or wrapped row " + rank.ID);
                if (layout.Item3 > worst) { worst = layout.Item3; worstID = rank.ID; worstName = name; worstRows = layout.Item1.Count; worstValue = value; }
                Rect box = GUI.Draws[0].Area;
                if (box.x < 0f || box.y < 0f || box.xMax > Screen.width || box.yMax > Screen.height)
                    failures.Add(rank.ID + " " + name + ": " + layout.Item1.Count + " rows, " + layout.Item3 + "px exceeds " + Screen.width + "x" + Screen.height + " scale " + scale);
                else checks++;
            }
            Console.WriteLine("INFO " + fixtureLabel + " fixture " + Screen.width + "x" + Screen.height + " scale " + scale + ": worst " + worstID + " " + worstName +
                ", " + worstRows + " rows, " + worst + "px; " + unnamed + " names unavailable; deterministic font/art, no Unity pixel QA.");
            var worstState = State(BoundedJson.Serialize(worstValue));
            var upgrade = new EndgameRequest("skill.upgrade", "1", skillID: worstID);
            Check(worstState.Begin(upgrade), "Worst-card pending fixture starts real production upgrade state");
            foreach (string phase in new[] { "pending", "stale" })
            {
                if (phase == "stale")
                    Check(worstState.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"requestId\":\"" + upgrade.RequestID + "\",\"ok\":true,\"revision\":\"2\"}")), "Worst-card stale fixture applies actual receipt");
                string text = Win05.TestNativeGrowthTip(worstID, true, worstName);
                Check(text.Contains(phase == "pending" ? "（确认中）" : "（更新中）"), "Worst-card shows correct " + phase + " state");
                var layout = Layout(text, scale);
                Render(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
                Rect box = GUI.Draws[0].Area;
                if (box.x < 0f || box.y < 0f || box.xMax > Screen.width || box.yMax > Screen.height)
                    failures.Add(worstID + " " + worstName + " " + phase + ": " + layout.Item1.Count + " rows, " + layout.Item3 + "px exceeds " + Screen.width + "x" + Screen.height + " scale " + scale);
                else checks++;
                Console.WriteLine("INFO worst " + phase + " card " + worstID + " " + worstName + ": " + layout.Item1.Count + " rows, " + layout.Item3 + "px at " + Screen.width + "x" + Screen.height + " scale " + scale + ".");
            }
            // Explicitly synthetic layout stress, separate from the real all149 fixture:
            // retain its authoritative effect/source text and add only the extra soft-rank
            // summary row plus wider effective-rank headers. This is not a server formula test.
            if (fullCatalog)
            {
                var softFixture = ((IReadOnlyDictionary<string, object>)worstValue).ToDictionary(pair => pair.Key, pair => pair.Value);
                Check((string)softFixture["effectiveRank"] == "3", "Synthetic soft-rank layout fixture starts from real rank3 catalog");
                softFixture["softRank"] = "7"; softFixture["effectiveRank"] = "10"; softFixture["nextEffectiveRank"] = "11";
                var softState = State(BoundedJson.Serialize(softFixture));
                var softUpgrade = new EndgameRequest("skill.upgrade", "1", skillID: worstID);
                foreach (string phase in new[] { "fresh", "pending", "stale" })
                {
                    if (phase == "pending") Check(softState.Begin(softUpgrade), "Synthetic soft-rank stress starts actual pending state");
                    if (phase == "stale") Check(softState.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"requestId\":\"" + softUpgrade.RequestID + "\",\"ok\":true,\"revision\":\"2\"}")), "Synthetic soft-rank stress applies actual stale receipt");
                    string text = Win05.TestNativeGrowthTip(worstID, true, worstName);
                    Check(text.Contains("装备与状态: +7级") && text.Contains("等级: 10") && text.Contains("下一等级: 11"), "Synthetic soft-rank layout displays exact adjusted headers");
                    var layout = Layout(text, scale);
                    Render(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
                    Rect box = GUI.Draws[0].Area;
                    if (box.x < 0f || box.y < 0f || box.xMax > Screen.width || box.yMax > Screen.height)
                        failures.Add(worstID + " " + worstName + " synthetic soft7 " + phase + ": " + layout.Item1.Count + " rows, " + layout.Item3 + "px exceeds " + Screen.width + "x" + Screen.height + " scale " + scale);
                    else checks++;
                    Console.WriteLine("INFO synthetic soft7 " + phase + " card " + worstID + " " + worstName + ": " + layout.Item1.Count + " rows, " + layout.Item3 + "px at " + Screen.width + "x" + Screen.height + " scale " + scale + ".");
                }
            }
        }
        Screen.width = 1280; Screen.height = 900; Screen.dpi = 96f;
        Check(failures.Count == 0, fixtureLabel + " skill cards must fit the native viewport:\n" + string.Join("\n", failures));
    }
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        HierarchyAndDrawing(); CapUnlearnedAndHotbar(); SemanticNextPreview(); ExactRanksAndCompatibility();
        PendingStaleAndRouting(); RequestAndComparisonRendering();
        foreach (string fixture in args) CheckServerFixture(fixture);
        Console.WriteLine("PASS " + checks + " native skill tooltip production composition/layout/render contracts (deterministic Unity/art/font substitutes; no Unity or Player run).");
        return 0;
    }
}
