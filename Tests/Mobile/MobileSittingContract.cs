using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine
{
    public static class Time { public static float time; }
    public static class GUIUtility { public static int keyboardControl; }
    public enum KeyCode { R }
    public static class Input { public static bool down; public static bool GetKeyDown(KeyCode key) => down; }
    public struct Color { public Color(float r, float g, float b, float a) { } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        private bool dead; private bool Chanting => Time.time < chantUntil;
        internal void SetFixture(bool deadValue = false, int working = 0, float chant = 0, float hurt = -99)
        { dead = deadValue; workSkill = working; chantUntil = chant; lastHurtT = hurt; }
        internal void KeyboardFixture() => HandleSitKey();
    }
    internal static class CharData { internal static bool riding; internal static int rideAnchorId, immobile; }
    internal static class CombatWorld { internal static bool Engaged; }
    internal static class ChatUI { internal static bool TypingNow; internal static string message; internal static void Add(string channel, string text, Color color) { message = text; } }
    internal static class Win05 { internal static bool GmTyping; }
    internal static class StallConfigDlg { internal static bool LocalStalling; }
    internal static class MobileClientPresentation { internal static bool GameplayInputBlocked; }
    internal class NetClient
    {
        internal static NetClient I = new NetClient(); internal int sends; internal bool last;
        internal void SitForIndependentRuntime(bool on) { sends++; last = on; }
    }
}
static class MobileSittingContract
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Rejected(PlayerController player, string name)
    {
        int sends = NetClient.I.sends;
        Check(!player.TryToggleMobileSitting() && !player.SittingForIndependentRuntime && NetClient.I.sends == sends, name);
    }
    static void Main()
    {
        Time.time = 20; var player = new PlayerController();
        Check(player.TryToggleMobileSitting() && player.SittingForIndependentRuntime && NetClient.I.sends == 0, "tap enters original native state before existing frame publication");
        player.UpdateSittingForIndependentRuntime();
        Check(NetClient.I.sends == 1 && NetClient.I.last, "existing carrier sends sit once");
        player.UpdateSittingForIndependentRuntime(); Check(NetClient.I.sends == 1, "repeated frame never resends unchanged sit");
        CombatWorld.Engaged = true;
        Check(player.TryToggleMobileSitting() && !player.SittingForIndependentRuntime, "standing remains possible in combat");
        player.UpdateSittingForIndependentRuntime(); Check(NetClient.I.sends == 2 && !NetClient.I.last, "standing uses same publication carrier");
        Rejected(player, "engaged combat rejects sitting"); Check(ChatUI.message == "战斗中无法打坐", "retains original combat feedback"); CombatWorld.Engaged = false;
        player.SetFixture(hurt: 18); Rejected(player, "recent injury rejects sitting"); player.SetFixture();
        player.SetFixture(deadValue: true); Rejected(player, "dead frame rejects"); player.SetFixture();
        CharData.riding = true; Rejected(player, "riding rejects"); CharData.riding = false;
        CharData.rideAnchorId = 2; Rejected(player, "passenger rejects"); CharData.rideAnchorId = 0;
        player.SetFixture(working: 10); Rejected(player, "work frame rejects"); player.SetFixture();
        CharData.immobile = 1; Rejected(player, "immobile frame rejects"); CharData.immobile = 0;
        StallConfigDlg.LocalStalling = true; Rejected(player, "stall-owned sitting cannot toggle"); StallConfigDlg.LocalStalling = false;
        player.SetFixture(chant: 25); Rejected(player, "chant guard rejects"); player.SetFixture();
        ChatUI.TypingNow = true; Rejected(player, "typing guard rejects"); ChatUI.TypingNow = false;
        Win05.GmTyping = true; Rejected(player, "GM typing guard rejects"); Win05.GmTyping = false;
        GUIUtility.keyboardControl = 7; Rejected(player, "text control guard rejects"); GUIUtility.keyboardControl = 0;
        MobileClientPresentation.GameplayInputBlocked = true; Rejected(player, "editor guard rejects"); MobileClientPresentation.GameplayInputBlocked = false;
        Check(player.TryToggleMobileSitting(), "guards clearing recovers"); player.BreakSit();
        Input.down = true; player.KeyboardFixture(); Check(player.SittingForIndependentRuntime, "desktop R still enters identical shared action");
        CombatWorld.Engaged = true; player.KeyboardFixture(); Check(!player.SittingForIndependentRuntime, "desktop R still permits stand in combat");
        player.KeyboardFixture(); Check(!player.SittingForIndependentRuntime, "desktop R retains combat gate");
        Console.WriteLine("PASS real PlayerController sit/toggle/publication contract: " + checks + " checks");
    }
}
