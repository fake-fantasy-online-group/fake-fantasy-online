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
    private const string CurrentEffects = "[\"凝聚火焰之力，对敌人造成123.45%的魔法伤害。\",\"消耗: 12点法力\",\"技能吟唱: 瞬发\",\"技能间隔: 2秒\"]";
    private const string NextEffects = "[\"凝聚火焰之力，对敌人造成125.45%的魔法伤害。\",\"消耗: 13点法力\",\"技能吟唱: 瞬发\",\"技能间隔: 1.8秒\"]";
    private const string Extra = ",\"nextRequiredLevel\":45,\"trainingSynergies\":[" +
        "{\"sourceId\":10402,\"sourceName\":\"火焰专精\",\"effect\":\"damage\",\"effectLabel\":\"伤害\",\"perPointValue\":100,\"unit\":\"percent_bp\",\"investedHardPoints\":3,\"contributionValue\":300,\"equipmentApplies\":false}," +
        "{\"sourceId\":10402,\"sourceName\":\"火焰专精\",\"effect\":\"targets\",\"effectLabel\":\"目标\",\"perPointValue\":2,\"unit\":\"flat\",\"investedHardPoints\":3,\"contributionValue\":6,\"equipmentApplies\":false}," +
        "{\"sourceId\":10405,\"sourceName\":\"余烬\",\"effect\":\"duration\",\"effectLabel\":\"持续时间\",\"perPointValue\":50,\"unit\":\"percent_bp\",\"investedHardPoints\":0,\"contributionValue\":0,\"equipmentApplies\":false}," +
        "{\"sourceId\":10408,\"sourceName\":\"寒冰专精\",\"effect\":\"damage\",\"effectLabel\":\"伤害\",\"perPointValue\":200,\"unit\":\"percent_bp\",\"investedHardPoints\":2,\"contributionValue\":400,\"equipmentApplies\":false}]," +
        "\"nativeModifierLines\":[\"已学被动使本技能伤害提升5%\"]";

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
    private static void CheckRow(List<(string, Color, bool)> rows, string text, Color color, bool centered = true)
    {
        var row = Row(rows, text);
        Check(Same(row.Item2, color), "Actual native row color: " + text);
        Check(row.Item3 == centered, "Authored row alignment survives composition: " + text);
        Check(row.Item1 == text, "Centering must not add measured whitespace: " + text);
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
    private static void RenderSkill(string text, float scale, Vector2 mouse)
    {
        BitmapFont.Draws.Clear(); GUI.Draws.Clear();
        typeof(UITip).GetMethod("RenderSkill", HiddenStatic).Invoke(null, new object[] { text, scale, mouse });
    }
    private static void ResetSkillScroll() => typeof(UITip).GetMethod("ResetSkillScroll", HiddenStatic).Invoke(null, null);
    private static int FirstSkillRow => (int)typeof(UITip).GetField("skillFirstRow", HiddenStatic).GetValue(null);
    private static void CheckSkillRender(string text, float scale, Vector2 mouse)
    {
        ResetSkillScroll();
        var full = Layout(text, scale);
        var reached = new HashSet<int>();
        bool overflow = full.Item3 > Screen.height;
        int prior = -1;
        do
        {
            RenderSkill(text, scale, mouse);
            int first = FirstSkillRow;
            int count = BitmapFont.Draws.Count - (overflow ? 1 : 0);
            Rect box = GUI.Draws[0].Area;
            Check(box.x >= 0f && box.y >= 0f && box.xMax <= Screen.width && box.yMax <= Screen.height,
                "Skill viewport fits screen at exact native scale " + scale);
            Check(GUI.Draws.Count == 9, "Skill overflow reuses original one-box art");
            for (int i = 0; i < count; i++)
            {
                var actual = BitmapFont.Draws[i]; var expected = full.Item1[first + i];
                Check(actual.Text == expected.Item1 && Same(actual.Color, expected.Item2), "Full original row survives viewport scrolling " + (first + i));
                if (expected.Item3)
                    Check(Near(actual.X + BitmapFont.Measure(actual.Text, scale) * 0.5f, box.x + box.width * 0.5f),
                        "Centered skill row stays on final viewport midpoint, including after scrolling " + (first + i));
                Check(Near(actual.Scale, scale) && actual.Y >= box.y && actual.Y + 12f * scale <= box.yMax,
                    "Native-size glyphs are fully inside skill viewport " + (first + i));
                reached.Add(first + i);
            }
            if (!overflow)
            {
                Check(count == full.Item1.Count && Near(box.width, full.Item2) && Near(box.height, full.Item3),
                    "Fitting skills retain exact original dimensions and all rows");
                Check(!UITip.ScrollSkill(text, scale, 1f), "Fitting skill does not intercept tree wheel input");
                break;
            }
            var hint = BitmapFont.Draws.Last();
            Check(hint.Text.StartsWith("滚轮查看 ", StringComparison.Ordinal) && Same(hint.Color, Gray) && Near(hint.Scale, scale),
                "Only overflowing cards have native gray wheel affordance");
            Check(hint.Y + 12f * scale <= Screen.height, "Overflow hint itself is not clipped");
            if (first == prior) break;
            prior = first;
            Check(UITip.ScrollSkill(text, scale, 1f), "Oversized skill consumes wheel input");
        } while (true);
        Check(reached.Count == full.Item1.Count, "Every description, next-rank detail, footer and incoming source remains reachable");
    }
    private static void SkillScrollingAndIsolation()
    {
        Screen.width = 1920; Screen.height = 1080;
        State(Rank()); string text = Win05.TestNativeGrowthTip();
        CheckSkillRender(text, 2f, new Vector2(1900f, 1060f));
        CheckSkillRender(text, 2f, new Vector2(2f, 2f));
        CheckSkillRender(text, 1f, new Vector2(1900f, 1060f));
        string longText = text + string.Concat(Enumerable.Repeat("\n完整原始说明与全部效果不可省略", 40));
        CheckSkillRender(longText, 2f, new Vector2(120f, 120f));
        ResetSkillScroll();
        Event.current = new Event { type = EventType.ScrollWheel, delta = new Vector2(0f, 1f) };
        UITip.ShowSkill(longText, 2f);
        Check(Event.current.type == EventType.Used && FirstSkillRow > 0, "Hovered skill wheel is consumed once");
        int first = FirstSkillRow;
        UITip.ShowSkill(longText, 2f);
        Check(FirstSkillRow == first, "Already consumed wheel is not replayed on repeated hover");
        UITip.ScrollSkill(longText, 2f, -1f);
        Check(FirstSkillRow == 0, "Wheel up reaches first original row");
        UITip.ScrollSkill(longText, 2f, -1f);
        Check(FirstSkillRow == 0, "Wheel up clamps without losing original header");
        UITip.ScrollSkill(longText, 2f, 1f);
        RenderSkill(longText + "新技能", 2f, new Vector2(1f, 1f));
        Check(FirstSkillRow == 0, "Tooltip identity/content change resets scroll");
        UITip.ScrollSkill(longText, 2f, 1f);
        UITip.ShowSkill(longText, 2f);
        object renderer = typeof(UITip).GetField("renderer", HiddenStatic).GetValue(null);
        MethodInfo onGui = renderer.GetType().GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);
        Event.current.type = EventType.Repaint;
        onGui.Invoke(renderer, null); // consume current skill request
        onGui.Invoke(renderer, null); // hover ended / window closed: no request
        Check(FirstSkillRow == 0, "Hover leave or cancel clears old scroll before reentry");
        UITip.ShowSkill(longText, 2f); UITip.ScrollSkill(longText, 2f, 1f);
        const string equipment = "\u00035原装备\n完整原装备属性\n原装备要求";
        UITip.Show(equipment, 2f); BitmapFont.Draws.Clear(); GUI.Draws.Clear(); onGui.Invoke(renderer, null);
        Check(FirstSkillRow == 0 && BitmapFont.Draws.Select(row => row.Text).SequenceEqual(Layout(equipment, 2f).Item1.Select(row => row.Item1)),
            "Equipment request clears skill scrolling and uses unchanged native rows");
        CheckRender(equipment, 2f, new Vector2(15f, 15f));
        Event.current = null; Screen.width = 1280; Screen.height = 900;
    }
    private static int MobileRow(bool comparison = false) => (int)typeof(UITip)
        .GetField(comparison ? "mobileCompareRow" : "mobileMainRow", HiddenStatic).GetValue(null);
    private static void RenderMobile(string text, Vector2 anchor, EventType type = EventType.Repaint,
        Vector2? point = null, string comparison = null, float scale = 1f)
    {
        Time.frameCount++;
        Event.current = new Event { type = type, mousePosition = point ?? anchor };
        if (comparison == null) UITip.ShowAtForIndependentRuntime(text, scale, anchor);
        else UITip.ShowCompareAtForIndependentRuntime(text, comparison, scale, anchor);
        BitmapFont.Draws.Clear(); GUI.Draws.Clear();
        object renderer = typeof(UITip).GetField("renderer", HiddenStatic).GetValue(null);
        renderer.GetType().GetMethod("OnGUI", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(renderer, null);
    }
    private static void CheckInside(Rect box, Rect safe, string message) => Check(
        box.x >= safe.x - .0001f && box.y >= safe.y - .0001f &&
        box.xMax <= safe.xMax + .0001f && box.yMax <= safe.yMax + .0001f, message);
    private static void MobileNativeRendering()
    {
        MobileClientPresentation.Enabled = true;
        try
        {
            const string text = "\u0007\u0005原始装备\n\u0001绿色属性 +1\n\u0002原始要求\n\u0006完整原说明";
            var layout = Layout(text);
            var anchor = new Vector2(720f, 390f);
            var safe = new Rect(48f, 32f, 680f, 360f);
            MobileClientPresentation.SafeAreaGui = safe;
            var tint = new Color(.2f, .3f, .4f, .5f); GUI.color = tint;
            RenderMobile(text, anchor);
            Rect box = GUI.Draws[0].Area;
            CheckInside(box, safe, "Mobile native tooltip respects nonzero safe-area insets");
            Check(GUI.Draws.Count == 9 && GUI.Draws.Count(draw => draw.HasCoordinates) == 8,
                "Mobile tooltip uses original background and all eight native frame pieces");
            Check(GUI.Draws.Skip(1).All(draw => draw.Texture.Name.StartsWith("ui/equipdlg05/", StringComparison.Ordinal)),
                "Mobile frame retains native equipment tooltip artwork");
            Check(Same(GUI.Draws[0].Color, new Color32(16,16,16,196)) && Same(GUI.color,tint),
                "Mobile frame retains native background palette and restores GUI tint");
            Check(Near(box.width, layout.Item2) && Near(box.height, layout.Item3) && BitmapFont.Draws.Count == layout.Item1.Count,
                "Fitting mobile card retains original box dimensions and complete row count");
            for (int index = 0; index < layout.Item1.Count; index++)
            {
                var expected = layout.Item1[index]; var draw = BitmapFont.Draws[index];
                float x = expected.Item3 ? box.x + (box.width - BitmapFont.Measure(expected.Item1,1f)) / 2f : box.x + 6f;
                Check(draw.Text == expected.Item1 && Same(draw.Color,expected.Item2) && Near(draw.X,x) &&
                    Near(draw.Y,box.y+6f+16f*index) && Near(draw.Scale,1f),
                    "Mobile card retains exact native row text, color, alignment and pitch " + index);
            }
            var inside = new Vector2(box.x + 1,box.y + 1);
            Check(UITip.MobileContainsPointer(inside) && !UITip.MobileContainsPointer(new Vector2(safe.x-1,safe.y-1)),
                "Only the actual mobile tooltip rectangle owns its pointer");
            Time.frameCount += 2;
            Check(!UITip.MobileContainsPointer(inside), "Expired tooltip geometry no longer blocks panel input");

            string longText = string.Join("\n", Enumerable.Range(0,24).Select(index =>
                (index % 2 == 0 ? "\u0007\u0001" : "\u0002") + "完整原始行" + index));
            MobileClientPresentation.SafeAreaGui = safe = new Rect(34,26,480,88);
            anchor = new Vector2(60,35);
            var full = Layout(longText);
            var reached = new HashSet<int>();
            RenderMobile(longText,anchor);
            Check(BitmapFont.Draws.Count < full.Item1.Count, "Mobile overflow clips rows without changing original layout");
            for (int step = 0; step <= full.Item1.Count; step++)
            {
                box=GUI.Draws[0].Area;
                CheckInside(box,safe,"Scrollable mobile card remains inside safe area");
                int first=MobileRow();
                for(int index=0;index<BitmapFont.Draws.Count;index++)
                {
                    var expected=full.Item1[first+index];var draw=BitmapFont.Draws[index];
                    Check(draw.Text==expected.Item1 && Same(draw.Color,expected.Item2) && Near(draw.Scale,1f) &&
                        draw.Y>=box.y && draw.Y+12f<=box.yMax,
                        "Every visible mobile overflow row is an unchanged fully visible native row");
                    reached.Add(first+index);
                }
                if(reached.Count==full.Item1.Count)break;
                var start=new Vector2(box.x+box.width/2,box.yMax-8);
                var end=new Vector2(start.x,start.y-16);
                RenderMobile(longText,anchor,EventType.MouseDown,start);
                Check(Event.current.type==EventType.Used,"Tooltip drag start owns the event");
                RenderMobile(longText,anchor,EventType.MouseDrag,end);
                Check(Event.current.type==EventType.Used,"Tooltip scrolling consumes the drag event");
                RenderMobile(longText,anchor,EventType.MouseUp,end);
                Check(Event.current.type==EventType.Used,"Tooltip release cannot activate underlying UI");
            }
            Check(reached.Count==full.Item1.Count && MobileRow()>0,"Dragging reaches every original overflow row, including final footer");
            RenderMobile(text,anchor);
            Check(MobileRow()==0,"Changing tooltip content resets mobile scroll");

            // Long comparison rows retain their own independent native frame and scroll.
            string comparison=longText.Replace("完整原始行","对比原始行");
            MobileClientPresentation.SafeAreaGui=safe=new Rect(40,30,500,104);
            anchor=new Vector2(50,40);
            RenderMobile(longText,anchor,comparison:comparison);
            Check(GUI.Draws.Count==18,"Comparison remains two separate original framed cards");
            Rect main=GUI.Draws[0].Area,other=GUI.Draws[9].Area;
            CheckInside(main,safe,"Comparison main fits safe inset");CheckInside(other,safe,"Comparison secondary fits safe inset");
            var otherStart=new Vector2(other.x+other.width/2,other.yMax-8);
            RenderMobile(longText,anchor,EventType.MouseDown,otherStart,comparison);
            RenderMobile(longText,anchor,EventType.MouseDrag,new Vector2(otherStart.x,otherStart.y-16),comparison);
            Check(MobileRow()==0 && MobileRow(true)==1,"Comparison drag scrolls only its own original rows");
            RenderMobile(longText,anchor,EventType.MouseUp,new Vector2(otherStart.x,otherStart.y-16),comparison);

            // Neither comparison card may cover the other, even near the safe-area middle
            // or when a narrow display scales the original pair down together.
            foreach(float width in new[]{340f,200f})
            {
                MobileClientPresentation.SafeAreaGui=safe=new Rect(40,30,width,180);
                string wide="\u0007\u0005"+new string('W',24)+"\n原物品";
                string compare="\u0007\u0001"+new string('M',24)+"\n已装备";
                RenderMobile(wide,new Vector2(140,50),comparison:compare);
                Check(GUI.Draws.Count==18,"Width-limited comparison keeps two native frames");
                main=GUI.Draws[0].Area;other=GUI.Draws[9].Area;
                CheckInside(main,safe,"Width-limited main stays inside safe area");
                CheckInside(other,safe,"Width-limited comparison stays inside safe area");
                Check(main.xMax<=other.x+.0001f || other.xMax<=main.x+.0001f,
                    "Mobile comparison cards cannot overlap at safe width "+width);
                Check(BitmapFont.Draws.Select(row=>row.Text).SequenceEqual(Layout(wide).Item1.Concat(Layout(compare).Item1).Select(row=>row.Item1)),
                    "Width adaptation retains both separate native card contents");
            }
        }
        finally
        {
            MobileClientPresentation.Enabled=false;MobileClientPresentation.SafeAreaGui=new Rect(0,0,1280,900);
            Input.touches=new Touch[0];Event.current=null;
        }
        // Verify the real request renderer returns to the unchanged desktop route.
        Event.current=new Event {type=EventType.Repaint,mousePosition=new Vector2(25,35)};
        UITip.Show("桌面原提示",1f);
        object desktop=typeof(UITip).GetField("renderer",HiddenStatic).GetValue(null);
        BitmapFont.Draws.Clear();GUI.Draws.Clear();
        desktop.GetType().GetMethod("OnGUI",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(desktop,null);
        Check(Near(GUI.Draws[0].Area.x,41f)&&Near(GUI.Draws[0].Area.y,51f),"Desktop request restores original pointer placement after mobile mode");
        Event.current=null;
    }
    private static void CenteringAfterWrapping()
    {
        // Exercise the actual composition with a mixed-width title, a source that
        // wraps and colored incoming modifiers. No padding/box formula in this
        // assertion: every centered draw must share the actual box's midpoint.
        State(Rank());
        foreach (bool proportional in new[] { false, true })
        {
            BitmapFont.Proportional = proportional;
            foreach (float scale in new[] { 1f, 2f })
            {
                var authored = new List<(string, char, bool)>
                {
                    ("Wi:冰爆术", (char)5, true),
                    ("等级: 26", (char)0, true),
                    ("完整原技能描述与原来左对齐的细节保留", (char)0, false),
                    ("受到以下技能加成", (char)1, true),
                    ("很长的技能来源名称（等级10）", (char)1, true),
                    ("每级: 持续时间 +6%", (char)1, true),
                };
                string text = (string)typeof(Win05).GetMethod("ComposeTip", HiddenStatic).Invoke(null, new object[] { authored });
                Render(text, scale, new Vector2(35f, 45f));
                Rect box = GUI.Draws[0].Area;
                var layout = Layout(text, scale);
                Check(layout.Item1.Count > authored.Count, "Center regression covers wrapped continuations");
                for (int i = 0; i < layout.Item1.Count; i++)
                {
                    var draw = BitmapFont.Draws[i];
                    if (!authored[2].Item1.Contains(draw.Text))
                        Check(Near(draw.X + BitmapFont.Measure(draw.Text, scale) / 2f, box.x + box.width / 2f),
                            "Visible glyph midpoint matches actual card midpoint: " + draw.Text);
                    else Check(Near(draw.X, box.x + 6f * scale), "Original body remains left aligned");
                    Check(draw.Text.All(c => !char.IsControl(c)), "Alignment and color tokens never become measured glyphs");
                }
                Check(!text.Contains("  "), "Composition never inserts centering spaces");
                string withoutColor = text.Replace("\u0005", "").Replace("\u0001", "").Replace("\u0002", "");
                Check(Near(Layout(withoutColor, scale).Item2, layout.Item2), "Color markers do not inflate card width");
                CheckRender(text, scale, new Vector2(35f, 45f));
                CheckSkillRender(text + string.Concat(Enumerable.Repeat("\n完整技能说明", 65)), scale, new Vector2(100f, 100f));
            }
        }
        BitmapFont.Proportional = false;
        var nativeIncoming = Rank().Replace("已学被动使本技能伤害提升5%", "已学被动使本技能伤害提升5%\",\"原生来向技能持续时间加成");
        State(nativeIncoming);
        CheckRow(Layout(Win05.TestNativeGrowthTip()).Item1, "原生来向技能持续时间加成", Green);
    }

    private static void HierarchyAndDrawing()
    {
        var state = State(Rank());
        Check(state.CanInvestSkill(10401), "Hard19 can invest with fresh authoritative state");
        string text = Win05.TestNativeGrowthTip();
        var rows = Layout(text).Item1;
        CheckRow(rows, "火弹术", Blue);
        CheckRow(rows, "等级: 26", Color.white);
        CheckRow(rows, "下一等级", Color.white);
        CheckRow(rows, "需要等级: 45", Green, false);
        CheckRow(rows, "受到以下技能加成", Green);
        CheckRow(rows, "火焰专精（投入3点）", Green);
        CheckRow(rows, "每点: 伤害 +1%", Green);
        CheckRow(rows, "每点: 目标 +2", Green);
        CheckRow(rows, "余烬（投入0点）", Green);
        CheckRow(rows, "每点: 持续时间 +0.5%", Green);
        Check(!text.Contains("装备加级不增加协同"), "Retired equipment exclusion note is absent");
        Check(text.Contains("\u0001已学被动使本技能伤害提升5%"), "Original green passive-damage aggregate preserved before footer");
        Check(Count(text, "火焰专精（投入3点）") == 1, "One source heading for multiple incoming effects");
        Check(!text.Contains("投入点数:") && !text.Contains("装备与状态:") && !text.Contains("授予等级:") && !text.Contains("投入1点后:"),
            "Original card is not replaced by investment, gear or granted-rank diagnostic rows");
        Check(!text.Contains("item:equipment-only") && !text.Contains("+300%"), "No equipment IDs or aggregate training value mislabelled as per-point");
        string prefix = "\u0007\u0005火弹术\n\u0007等级: 26\n" +
            "凝聚火焰之力，对敌人造成123.45%的魔法伤害。\n消耗: 12点法力\n技能吟唱: 瞬发\n技能间隔: 2秒\n\n" +
            "\u0007下一等级\n凝聚火焰之力，对敌人造成125.45%的魔法伤害。\n消耗: 13点法力\n技能吟唱: 瞬发\n技能间隔: 1.8秒\n" +
            "\u0001需要等级: 45\n\u0001已学被动使本技能伤害提升5%\n\n\u0006您现在的剩余技能点数为3\n\u0006双击或拖曳至快捷栏以使用这个技能";
        Check(text.StartsWith(prefix + "\n\n\u0007\u0001", StringComparison.Ordinal),
            "Original complete current/next prose, labels, blank rows and footer are unchanged apart from explicit header alignment; incoming synergy is append-only");
        CheckRender(text, 1f, new Vector2(30f, 40f));
        Screen.width = 1920; Screen.height = 1080;
        CheckRender(text, 2f, new Vector2(80f, 90f));
        Screen.width = 1280; Screen.height = 900;
        CheckRender(text, 1f, new Vector2(Screen.width - 2f, Screen.height - 2f));
        Check(ResLoader.Paths.Count == 8 && ResLoader.Paths.All(path => path.StartsWith("ui/equipdlg05/", StringComparison.Ordinal)), "Native tooltip art reused");
        var training = text.Substring(text.IndexOf("受到以下技能加成", StringComparison.Ordinal));
        State(Rank(soft: "184467440737095516170000001", effective: "184467440737095516170000020"));
        string equipped = Win05.TestNativeGrowthTip();
        Check(training == equipped.Substring(equipped.IndexOf("受到以下技能加成", StringComparison.Ordinal)), "Receiving skill equipment ranks do not replace the server's source-specific legacy rows");
    }
    private static string EquipmentTrainingRank(int rate = 300, string sourceRank = "10", long contribution = 3000, int invested = 3) =>
        Rank().Replace(Extra, ",\"nextRequiredLevel\":45,\"trainingSynergies\":[" +
            "{\"sourceId\":10402,\"sourceName\":\"火焰专精\",\"effect\":\"damage\",\"effectLabel\":\"伤害\",\"perPointValue\":" + rate +
            ",\"unit\":\"percent_bp\",\"investedHardPoints\":" + invested + ",\"sourceEffectiveRank\":\"" + sourceRank +
            "\",\"contributionValue\":" + contribution + ",\"equipmentApplies\":true}],\"nativeModifierLines\":[\"已学被动使本技能伤害提升5%\"]");
    private static void EquipmentTrainingDrawing()
    {
        State(Rank());
        string original = Win05.TestNativeGrowthTip();
        string originalBody = original.Substring(0, original.IndexOf("受到以下技能加成", StringComparison.Ordinal));
        foreach (int rate in new[] { 300, 400, 500, 600 })
        {
            State(EquipmentTrainingRank(rate, contribution: rate * 10));
            string text = Win05.TestNativeGrowthTip();
            var rows = Layout(text).Item1;
            CheckRow(rows, "受到以下技能加成", Green);
            CheckRow(rows, "火焰专精（等级10）", Green);
            CheckRow(rows, "每级: 伤害 +" + (rate / 100) + "%", Green);
            Check(text.Substring(0, text.IndexOf("受到以下技能加成", StringComparison.Ordinal)) == originalBody,
                "Equipment synergy changes only the append-only incoming rows, preserving every original body row");
            Check(Count(text, "火焰专精（等级10）") == 1 && !text.Contains("（投入3点）") && !text.Contains("装备加级") &&
                !text.Contains("每点:") && !text.Contains("item:equipment-only"),
                "Equipment source shows its actual effective rank once, without contradictory or extra explanatory notes");
            string hotbar = Win05.TestNativeGrowthTip(includeNext: false);
            Check(hotbar.Contains("火焰专精（等级10）") && hotbar.Contains("每级: 伤害 +" + (rate / 100) + "%") &&
                !hotbar.Contains("下一等级"), "Hotbar shares source-effective incoming rows with its original current-only body");
            foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(text, scale, new Vector2(300f, 200f));
        }
        State(EquipmentTrainingRank(sourceRank: "0", contribution: 0, invested: 0));
        string unlearned = Win05.TestNativeGrowthTip();
        CheckRow(Layout(unlearned).Item1, "火焰专精（等级0）", Green);
        Check(unlearned.Contains("每级: 伤害 +3%"), "Unlearned equipment-only source remains visible at server rank zero for planning");
        State(EquipmentTrainingRank(rate: 600, sourceRank: "2147483647", contribution: 1288490188200L));
        string high = Win05.TestNativeGrowthTip();
        Render(high, 1f, new Vector2(0f, 0f));
        Check(string.Concat(BitmapFont.Draws.Select(draw => draw.Text)).Contains("火焰专精（等级2147483647）"),
            "Full effective source rank survives actual native wrapping without numeric narrowing");
        foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(high, scale, new Vector2(300f, 200f));
    }
    private static void CapUnlearnedAndHotbar()
    {
        var capped = State(Rank(hard: 20, effective: "27", next: "28", invest: false));
        string text = Win05.TestNativeGrowthTip();
        Check(text.Contains("等级: 27") && !capped.CanInvestSkill(10401), "Hard20 retains exact effective rank");
        Check(!text.Contains("下一等级") && !text.Contains("125.45%") && !text.Contains("需要等级: 45"), "Hard20 hides next investment block");
        Check(text.Contains("您现在还无法升级这个技能"), "Capped card restores original orange cannot-upgrade line");
        State(Rank(hard: 0, effective: "0", next: "8", cast: false));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("等级: 未学习") && !text.Contains("投入1点后:"), "Original unlearned heading and layout");
        Check(!text.Contains("123.45%") && Count(text, "125.45%") == 1 && !text.Contains("下一等级"), "Unlearned shows complete first-investment preview once");
        Check(text.Contains("双击或拖曳") && text.Contains("需要等级: 45"), "Original active-skill hint and requirement remain for unlearned skill");
        CheckRender(text, 1f, new Vector2(25f, 25f));
        State(Rank()); text = Win05.TestNativeGrowthTip(includeNext: false);
        Check(text.Contains("123.45%") && !text.Contains("125.45%") && !text.Contains("下一等级"), "Hotbar shows complete current block only");
        Check(!text.Contains("剩余技能点数") && !text.Contains("需要等级: 45") && !text.Contains("需要完成职业任务"), "Hotbar has no investment footer or requirement");
        Check(text.Contains("受到以下技能加成") && text.Contains("余烬（投入0点）"), "Hotbar includes incoming sources");
        State(Rank(hard: 0, soft: "0", effective: "3", next: "4", grant: "3"));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("等级: 3") && !text.Contains("授予等级:"), "Granted rank uses original single level heading");
    }
    private static void FullNextPreview()
    {
        const string current = "[\"冻结敌人的行动，使其移动速度降低10%，持续2秒。\",\"消耗: 12点法力\",\"技能吟唱: 瞬发\",\"技能间隔: 2秒\"]";
        const string next = "[\"冻结敌人的行动，使其移动速度降低10%，持续3秒。\",\"消耗: 12点法力\",\"技能吟唱: 瞬发\",\"技能间隔: 2秒\"]";
        const string changes = "[\"持续3秒\",\"其余效果不变\"]";
        var state = State(Rank(current: current, upcoming: next, changes: changes));
        var rank = state.SkillRank(10401);
        Check(rank.CurrentEffectLines.Count == 4 && rank.NextEffectLines.Count == 4 && rank.NextChangeLines.Count == 2, "Compatibility DTO retains all supplied arrays");
        string text = Win05.TestNativeGrowthTip();
        Check(Count(text, "冻结敌人的行动，使其移动速度降低10%") == 2 && text.Contains("持续2秒。") && text.Contains("持续3秒。"), "Both original full descriptions, including unchanged semantics, survive");
        foreach (string line in new[] { "消耗: 12点法力", "技能吟唱: 瞬发", "技能间隔: 2秒" })
            Check(Count(text, line) == 2, "Next rank repeats original complete detail row: " + line);
        Check(!text.Contains("其余效果不变"), "Changed-only summary must never replace full next-rank block");
        var incomingState = State(Rank().Replace("已学被动使本技能伤害提升5%", "火焰专精：附加燃烧效果"));
        string incomingOnly = Win05.TestNativeGrowthTip();
        Check(incomingOnly.IndexOf("火焰专精：附加燃烧效果", StringComparison.Ordinal) >
            incomingOnly.IndexOf("双击或拖曳至快捷栏以使用这个技能", StringComparison.Ordinal),
            "Additional native passive incoming details are appended after the entire original card too");
        string onlyNative = Rank(additive: false);
        onlyNative = onlyNative.Substring(0, onlyNative.Length - 1) + ",\"nativeModifierLines\":[\"火焰专精：附加燃烧效果\"]}";
        State(onlyNative);
        incomingOnly = Win05.TestNativeGrowthTip();
        Check(incomingOnly.Contains("受到以下技能加成") && incomingOnly.Contains("火焰专精：附加燃烧效果") &&
            !incomingOnly.Contains("装备加级不增加协同"), "Native-only incoming bonus remains append-only without a misleading hard-point note");
        State(Rank(current: current, upcoming: next, changes: changes));
        CheckRender(text, 1f, new Vector2(20f, 30f));
        string hotbar = Win05.TestNativeGrowthTip(includeNext: false);
        Check(!hotbar.Contains("持续3秒") && Count(hotbar, "消耗: 12点法力") == 1, "Hotbar contains one full current block");
        State(Rank(hard: 0, effective: "0", cast: false, current: current, upcoming: next, changes: changes));
        text = Win05.TestNativeGrowthTip();
        Check(text.Contains("持续3秒") && text.Contains("消耗: 12点法力") && !text.Contains("其余效果不变") && !text.Contains("持续2秒"), "Unlearned preview uses complete first-investment prose");
        State(Rank(hard: 20, invest: false, current: current, upcoming: next, changes: changes));
        Check(!Win05.TestNativeGrowthTip().Contains("持续3秒"), "Hard20 has no next preview");
        Reject(Ranks(Rank(changes: "[42]")), "Legacy changed-only API metadata is still validated");
    }
    private static void ExactRanksAndCompatibility()
    {
        foreach (string rank in new[] { "20", "21", "99", "100", "101", "255", "256", "65535", "65536", "184467440737095516170000001" })
        {
            const string next = "184467440737095516170000002";
            var state = State(Rank(effective: rank, next: next));
            string text = Win05.TestNativeGrowthTip();
            Check(text.Contains("等级: " + rank) && text.Contains("下一等级"), "Exact decimal rank and original next heading: " + rank);
            Check(state.SkillRank(10401).EffectiveRank == rank && state.SkillRank(10401).NextEffectiveRank == next, "DTO ranks never narrow: " + rank);
            Render(text, 1f, new Vector2(0f, 0f));
            Check(string.Concat(BitmapFont.Draws.Select(draw => draw.Text)).Contains("等级: " + rank), "All rank digits survive wrapping: " + rank);
        }
        var old = State(Rank(additive: false)); string legacy = Win05.TestNativeGrowthTip();
        Check(old.SkillRank(10401).NextEffectiveRank == "" && old.SkillRank(10401).TrainingSynergies.Count == 0, "Older V5 may omit additive metadata");
        Check(legacy.Contains("下一等级") && legacy.Contains("125.45%") && !legacy.Contains("受到以下技能加成"), "Older payload keeps complete supplied blocks without invented sources");
        State(Rank(current: "[]", upcoming: "[]"));
        Check(Win05.TestNativeGrowthTip().Contains("服务器尚未提供技能效果"), "Absent authoritative block is explicit");
        string valid = Ranks(Rank());
        Reject(valid.Replace("\"equipmentApplies\":false", "\"equipmentApplies\":true"), "Equipment-derived synergy requires authoritative source rank");
        Reject(valid.Replace("\"contributionValue\":300", "\"contributionValue\":1000"), "Training validates hard points times rate");
        Reject(valid.Replace("\"sourceId\":10405", "\"sourceId\":11005"), "Life skill cannot be training source");
        Reject(valid.Replace("\"sourceId\":10405", "\"sourceId\":10401"), "Self-source rejected");
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
        var usable = new Dictionary<int, bool>();
        string usablePath = Path.Combine(Path.GetDirectoryName(path), "skill-tooltip-usable-v5.json");
        foreach (var entry in (IReadOnlyDictionary<string, object>)BoundedJson.Parse(File.ReadAllText(usablePath)))
            usable.Add(int.Parse(entry.Key, CultureInfo.InvariantCulture), (bool)entry.Value);
        Check(usable.Count == 149, "Original 149-skill kinds determine real active/passive hint admission");
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
                    // Current content v6 makes 2610 an 冰箭术-only grant; the
                    // legacy v5 frost-wide +1 must not inflate 冰爆术 here.
                    int soft = 3;
                    Check(rank.HardRank == 19 && rank.SoftRank == soft.ToString(CultureInfo.InvariantCulture) &&
                        rank.EffectiveRank == (19 + soft).ToString(CultureInfo.InvariantCulture) && rank.NextEffectiveRank == (20 + soft).ToString(CultureInfo.InvariantCulture),
                        "Real equipped fixture preserves hard19 plus legal gear and full next rank " + rank.ID);
                    Check(rank.CanCast && rank.CanInvest && state.CanInvestSkill(rank.ID), "Real equipped fixture exercises below-cap next investment " + rank.ID);
                    Check(rank.Contributions.Sum(value => value.Levels) == soft && rank.Contributions.All(value => value.SourceID.StartsWith("item:", StringComparison.Ordinal)),
                        "Real equipped fixture retains exact independent gear-contribution records " + rank.ID);
                    var training = rank.TrainingSynergies.Single();
                    Check(training.SourceID == (rank.ID == 10405 ? 10402 : 10409) && training.InvestedHardPoints == 10 &&
                        training.PerPointValue == 100 && training.ContributionValue == 1000 && !training.EquipmentApplies,
                        "Legacy V5 fixture retains its authored hard-only source10 synergy " + rank.ID);
                    Check(rank.CurrentEffectLines.Count >= 4 && rank.NextEffectLines.Count >= 4,
                        "Real high-rank original prose and native three-row details remain complete " + rank.ID);
                }
                string name;
                if (!names.TryGetValue(rank.ID, out name)) { name = "技能 " + rank.ID; unnamed++; }
                string text = Win05.TestNativeGrowthTip(rank.ID, true, name, usable[rank.ID]);
                Check(text.Contains(name), "Fixture uses verified native skill name " + rank.ID);
                Check(text.Contains("双击或拖曳") == usable[rank.ID], "Native active/passive skill hint matches original type " + rank.ID);
                foreach (var equipment in rank.Contributions)
                    Check(!text.Contains(equipment.SourceID), "Equipment identity does not leak into training section " + rank.ID);
                foreach (string effect in rank.CurrentEffectLines)
                    Check(text.Contains(effect), "Every authoritative current effect is preserved " + rank.ID + ": " + effect);
                foreach (string effect in rank.NextEffectLines)
                    Check(text.Contains(effect), "Every complete next-rank description/detail is preserved " + rank.ID + ": " + effect);
                Check(!text.Contains("其余效果不变"), "No compact-summary substitution in actual fixture " + rank.ID);
                foreach (var synergy in rank.TrainingSynergies)
                    Check(text.Contains(synergy.SourceName + "（投入" + synergy.InvestedHardPoints + "点）"), "Every fixture training source remains visible " + rank.ID);
                var layout = Layout(text, scale);
                if (Environment.GetEnvironmentVariable("FFO_SKILL_TOOLTIP_DUMP_ID") == rank.ID.ToString(CultureInfo.InvariantCulture) && Screen.width == 1920)
                    for (int row = 0; row < layout.Item1.Count; row++) Console.WriteLine("ROW " + rank.ID + " " + row + ": " + layout.Item1[row].Item1);
                CheckSkillRender(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
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
                string text = Win05.TestNativeGrowthTip(worstID, true, worstName, usable[worstID]);
                Check(text.Contains(phase == "pending" ? "（确认中）" : "（更新中）"), "Worst-card shows correct " + phase + " state");
                var layout = Layout(text, scale);
                CheckSkillRender(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
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
                    string text = Win05.TestNativeGrowthTip(worstID, true, worstName, usable[worstID]);
                    Check(!text.Contains("装备与状态:") && text.Contains("等级: 10") && text.Contains("下一等级"), "Synthetic soft-rank layout displays exact adjusted headers");
                    var layout = Layout(text, scale);
                    CheckSkillRender(text, scale, new Vector2(Screen.width / 2f, Screen.height / 2f));
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
    private static void ReducedManaCostPreservesOriginalBlock()
    {
        string next = NextEffects.Replace("13点法力", "11点法力");
        var state = State(Rank(current: CurrentEffects, upcoming: next));
        string card = Win05.TestNativeGrowthTip(10401, true, "火焰术");
        foreach (string line in state.SkillRank(10401).CurrentEffectLines) Check(card.Contains(line), "Reduced cost retains every authored current row");
        foreach (string line in state.SkillRank(10401).NextEffectLines) Check(card.Contains(line), "Reduced cost retains every authored next row");
        Check(Count(card, "消耗: 12点法力") == 1 && Count(card, "消耗: 11点法力") == 1,
            "Current and next cost are displayed once in their original rows");
        Check(card.IndexOf("消耗: 12点法力", StringComparison.Ordinal) < card.IndexOf("技能间隔: 2秒", StringComparison.Ordinal),
            "Original cost and timing row order is retained");
        var rows = Layout(card).Item1;
        Check(!Row(rows, "消耗: 12点法力").Item3 && !Row(rows, "消耗: 11点法力").Item3,
            "Original consumption rows retain original alignment");
        CheckRow(rows, "受到以下技能加成", Green);
        CheckRow(rows, "火焰专精（投入3点）", Green);
        string hotbar = Win05.TestNativeGrowthTip(10401, false, "火焰术");
        Check(Count(hotbar, "消耗: 12点法力") == 1 && !hotbar.Contains("消耗: 11点法力"),
            "Hotbar shows only current actual cost");
        Check(!card.Contains("法力效率") && !card.Contains("零头") && !card.Contains("返还"),
            "No efficiency or settlement explanation replaces the direct cost display");
        foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(card, scale, new Vector2(300, 200));
        State(Rank(hard: 0, soft: "0", effective: "0", next: "1", cast: false, current: "[]", upcoming: next));
        string preview = Win05.TestNativeGrowthTip(10401, true, "火焰术");
        Check(Count(preview, "消耗: 11点法力") == 1 && !preview.Contains("消耗: 12点法力"),
            "Unlearned card shows first-rank actual cost preview once");
    }
    private static void ServerActualManaCostTips(string path)
    {
        var cases = (IReadOnlyList<object>)BoundedJson.Parse(File.ReadAllText(path));
        int previous = int.MaxValue;
        foreach (var item in cases)
        {
            var sample = (IReadOnlyDictionary<string, object>)item;
            var behavior = new EndgameSkillBehaviorView((IReadOnlyDictionary<string, object>)sample["payload"]);
            Check(behavior.MPCost > 0 && behavior.MPCost <= previous,
                "Actual server integer cost remains positive and never rises across sampled ranks");
            previous = behavior.MPCost;
            string rank = ((long)sample["rank"]).ToString(CultureInfo.InvariantCulture);
            int id = Convert.ToInt32(sample["id"], CultureInfo.InvariantCulture);
            string name = (string)sample["name"];
            Check(id == 10301 && name == "治愈术", "Only the approved healing skill supplies this reduced-cost fixture");
            string lines = BoundedJson.Serialize(sample["lines"]);
            State(Rank(hard: 20, effective: rank, next: "", invest: false, current: lines, upcoming: "[]").Replace("\"id\":10401", "\"id\":10301"));
            string text = Win05.TestNativeGrowthTip(id, false, name);
            foreach (string line in (IReadOnlyList<object>)sample["lines"])
                Check(Count(text, line) == 1, "Actual server prose/cost/timing renders exactly once at " + rank);
            Check(text.Contains("消耗: " + behavior.MPCost + "点法力"), "Server integer admission amount remains original consumption line");
            Check(!text.Contains("法力效率") && !text.Contains("返还") && !text.Contains("零头"),
                "Actual server tooltip shows only direct cost, without efficiency or settlement prose");
            Check(((IReadOnlyList<object>)sample["lines"]).Count == 4, "Original description and three detail rows remain the complete block");
            foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(text, scale, new Vector2(300, 200));
        }
        Check(cases.Count == 7 && previous == 1, "Real healing fixture covers ranks19/20/21/30/50/100/1000000 including positive one-MP gate");
    }
    private static void ServerActualGrowthTips(string path)
    {
        var ids = new HashSet<int>(); int samples = 0;
        foreach (string json in File.ReadLines(path))
        {
            var response = EndgameResponse.Parse(json);
            var state = new EndgameClientState();
            state.Begin(new EndgameRequest("hello"));
            state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\",\"endgame.skill-behavior.v1\"]}"));
            Check(state.Apply(response), "Actual new-growth response is accepted by strict parser and state");
            EndgamePanel.GrowthState = state;
            var rank = state.SkillRanks.Single(); var contract = rank.Contract;
            ids.Add(rank.ID); samples++;
            string text = Win05.TestNativeGrowthTip(rank.ID, true, contract.Name, contract.Current.Usable);
            Check(Layout(text).Item1[0].Item1 == contract.Name, "New growth retains original title");
            foreach (var line in rank.CurrentEffectLines) Check(text.Contains(line), "Actual current growth prose remains complete");
            foreach (var line in rank.NextEffectLines) Check(text.Contains(line), "Actual next growth prose remains complete");
            string hotbar = Win05.TestNativeGrowthTip(rank.ID, false, contract.Name, contract.Current.Usable);
            foreach (string line in rank.CurrentEffectLines) Check(Count(hotbar, line) == 1, "Current growth line appears exactly once on hotbar");
            Check(!text.Contains("法力效率") && !text.Contains("抵抗概率降至") && !text.Contains("参数"), "Retired growth mechanics are absent from real server cards");
            foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(text, scale, new Vector2(300, 200));
            state.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"ok\":true,\"revision\":\"2\"}"));
            Check(Win05.TestNativeGrowthTip(rank.ID, true, contract.Name).Contains("正在等待服务器技能资料"), "Newer revision hides stale attribute/effect-hit growth");
        }
        Check(ids.Count == 148 && samples > 300, "Real growth fixtures cover every resolved profession skill and all policy boundaries");
    }
    private static void EquipmentVariantTips(string fixtureDirectory)
    {
        foreach (string path in Directory.GetFiles(fixtureDirectory, "*.json"))
        {
            var response = EndgameResponse.Parse(File.ReadAllText(path));
            var state = new EndgameClientState();
            state.Begin(new EndgameRequest("hello"));
            state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\",\"endgame.skill-behavior.v1\",\"endgame.skill-variants.v1\"]}"));
            Check(state.Apply(response), "Real equipment variant state accepted");
            EndgamePanel.GrowthState = state;
            foreach (var rank in state.SkillRanks)
            {
                string text = Win05.TestNativeGrowthTip(rank.ID, true, rank.Contract.Name);
                Check(Layout(text).Item1[0].Item1 == rank.Contract.Name, "Variant keeps original title");
                foreach (string line in rank.CurrentEffectLines) Check(text.Contains(line), "Variant retains complete current effect");
                foreach (string line in rank.NextEffectLines) Check(text.Contains(line), "Variant retains complete next effect");
                Check(Count(text, rank.CurrentEffectLines[0]) <= 2, "Variant line is not separately duplicated as extra explanation");
                CheckSkillRender(text, 1f, new Vector2(Screen.width / 2f, Screen.height / 2f));
                string hotbar = Win05.TestNativeGrowthTip(rank.ID, false, rank.Contract.Name);
                Check(Count(hotbar, rank.CurrentEffectLines[0]) == 1, "Hotbar shows one equipment form line");
                state.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"ok\":true,\"revision\":\"999999\"}"));
                string stale = Win05.TestNativeGrowthTip(rank.ID, true, rank.Contract.Name);
                Check(stale.Contains("正在等待服务器技能资料") && !stale.Contains(rank.Contract.Current.Variant.Description), "Stale form is not displayed after equipment revision advances");
            }
        }
    }
    private static void ServerEquipmentSynergyTips(string path)
    {
        Check(File.Exists(path), "Migration74 equipment synergy fixture is required");
        var response = EndgameResponse.Parse(File.ReadAllText(path));
        var state = State();
        Check(state.Apply(response), "Production migration74 equipment synergy payload is accepted");
        bool equipmentIncreasedRank = false, unlearnedSource = false;
        var rates = new HashSet<int>();
        foreach (var rank in state.SkillRanks)
        {
            Check(rank.Contract != null, "Production equipment fixture retains actual skill identity and behavior");
            string text = Win05.TestNativeGrowthTip(rank.ID, true, rank.Contract.Name, rank.Contract.Current != null && rank.Contract.Current.Usable);
            foreach (string line in rank.CurrentEffectLines) Check(text.Contains(line), "Equipped server current prose remains complete");
            if (rank.HardRank < 20)
                foreach (string line in rank.NextEffectLines) Check(text.Contains(line), "Equipped server next prose remains complete");
            foreach (var synergy in rank.TrainingSynergies)
            {
                Check(synergy.EquipmentApplies && text.Contains(synergy.SourceName + "（等级" + synergy.SourceEffectiveRank + "）"),
                    "Production equipment source displays its exact authoritative level");
                Check(text.Contains("每级: " + synergy.EffectLabel + " +" + (synergy.PerPointValue / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%"),
                    "Production beneficiary tier displays the exact server-authored percentage");
                rates.Add(synergy.PerPointValue);
                equipmentIncreasedRank |= synergy.SourceEffectiveRank != synergy.InvestedHardPoints.ToString(CultureInfo.InvariantCulture);
                unlearnedSource |= synergy.SourceEffectiveRank == "0";
            }
            Check(!text.Contains("装备加级") && !text.Contains("每点:"), "Production equipment card has no stale hard-only note or extra explanation");
            foreach (float scale in new[] { 1f, 2f }) CheckSkillRender(text, scale, new Vector2(300f, 200f));
        }
        Check(equipmentIncreasedRank && unlearnedSource, "Real equipped fixture includes both increased learned source and zero-level unlearned source");
        Check(rates.SetEquals(new[] { 300, 400, 600 }), "Production Warlock fixture covers its three actual beneficiary tiers; synthetic rate test separately covers 5%");
    }
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CenteringAfterWrapping(); HierarchyAndDrawing(); EquipmentTrainingDrawing(); CapUnlearnedAndHotbar(); FullNextPreview(); ExactRanksAndCompatibility();
        PendingStaleAndRouting(); RequestAndComparisonRendering(); SkillScrollingAndIsolation(); ReducedManaCostPreservesOriginalBlock(); MobileNativeRendering();
        foreach (string fixture in args) CheckServerFixture(fixture);
        ServerActualManaCostTips(Environment.GetEnvironmentVariable("FFO_SKILL_MANA_FIXTURE"));
        ServerActualGrowthTips(Environment.GetEnvironmentVariable("FFO_SKILL_GROWTH_FIXTURE"));
        ServerEquipmentSynergyTips(Environment.GetEnvironmentVariable("FFO_SKILL_SYNERGY_FIXTURE"));
        string variants = Environment.GetEnvironmentVariable("FFO_VARIANT_FIXTURE_DIR");
        if (!string.IsNullOrEmpty(variants)) EquipmentVariantTips(variants);
        Console.WriteLine("PASS " + checks + " native skill tooltip production composition/layout/render contracts (deterministic Unity/art/font substitutes; no Unity or Player run).");
        return 0;
    }
}
