using System;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

partial class EndgameUiContract
{
    static object HudCall(EndgamePanel panel, string method, params object[] args) =>
        typeof(EndgamePanel).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Invoke(panel, args);
    static void RiftHudProgressContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        var back = new Texture2D();
        var green = new Texture2D(2, 1, TextureFormat.RGBA32, false, false);
        green.SetPixels32(new[] { new Color32(12, 210, 34, 255), new Color32(2, 87, 13, 73) });
        ArtworkSkin.frameTextures["res/uires/gameui/intonatebarbk.gsa"] = new[] { back };
        ArtworkSkin.frameTextures["res/uires/gameui/intonatebar.gsa"] = new[] { green };
        Rect frame = new Rect(80, 96, 140, 34);
        Action<float, float> bar = (progress, time) => {
            GUI.uvTextures.Clear(); GUI.textures.Clear();
            HudCall(p, "DrawRiftProgressArtwork", frame, progress, time);
        };
        bar(0, 0);
        Check(GUI.textures.Count == 1 && GUI.uvTextures.Count == 0, "zero progress and time leave only one original frame");
        bar(.25f, .75f);
        var white = GUI.uvTextures[0].Item2;
        Check(GUI.textures.Count == 3 && GUI.uvTextures.Count == 2 && GUI.uvTextures[1].Item2 == green && white != green, "white time is drawn before green progress in one slot");
        Check(GUI.uvTextures[0].Item1.x == 92 && GUI.uvTextures[0].Item1.y == 108 && GUI.uvTextures[0].Item1.width == 87 && GUI.uvTextures[0].Item1.height == 10 && GUI.uvTextures[0].Item3.width == .75f, "time clips the original 116 by 10 inset without moving its origin");
        Check(GUI.uvTextures[1].Item1.x == 92 && GUI.uvTextures[1].Item1.y == 108 && GUI.uvTextures[1].Item1.width == 29 && GUI.uvTextures[1].Item3.width == .25f, "green uses the same inset and a shorter clipped width");
        Check(white.pixels[0].r == 210 && white.pixels[0].g == 210 && white.pixels[0].b == 210 && white.pixels[1].r == 87 && white.pixels[1].g == 87 && white.pixels[1].b == 87 && white.pixels[1].a == 73 && green.pixels[0].r == 12 && green.pixels[0].g == 210, "silver-white keeps original pixel highlights, shadows and alpha without recoloring the shared green asset");
        bar(.25f, .75f);
        Check(GUI.uvTextures[0].Item2 == white, "repeated GUI passes reuse the white texture");
        bar(.75f, .25f);
        Check(GUI.uvTextures.Count == 1 && GUI.uvTextures[0].Item2 == green, "green ahead completely hides white");
        bar(.5f, .5f);
        Check(GUI.uvTextures.Count == 1 && GUI.uvTextures[0].Item2 == green, "equal progress hides white without alpha edge bleed");
        bar(1, 1);
        Check(GUI.uvTextures.Count == 1 && GUI.uvTextures[0].Item1.width == 116, "100 percent green fills the original slot and hides time");
        bar(-1, 2);
        Check(GUI.uvTextures.Count == 1 && GUI.uvTextures[0].Item2 == white && GUI.uvTextures[0].Item1.width == 116, "ratios clamp at both ends and never exceed the slot");
        GUI.color = new Color(.2f, .3f, .4f, .5f); bar(.3f, .6f);
        Check(GUI.color.r == .2f && GUI.color.a == .5f, "drawing restores the caller color");
        string data = ",\"progress\":30,\"progressMax\":100,\"remainingSeconds\":90,\"totalSeconds\":120";
        DateTime received = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);
        Rift(c, "dual-run", "active", extra: data);
        Func<DateTime, int> remain = now => (int)HudCall(p, "RiftRemainingSeconds", c.Endgame.Rift, received, now);
        Check(remain(received.AddSeconds(20)) == 70 && remain(received.AddSeconds(-20)) == 90 && remain(received.AddDays(1)) == 0, "active countdown clamps clock skew and expiration");
        Check(Math.Abs((float)HudCall(p, "RiftElapsedRatio", c.Endgame.Rift, 90) - .25f) < .00001f, "elapsed time is one minus authoritative remaining over total");
        foreach (string phase in new[] { "completed", "closed", "reward_pending", "failed", "interrupted" })
        {
            Rift(c, "dual-run", phase, extra: data);
            Check(remain(received.AddDays(1)) == 90, "non-running phase preserves server time: " + phase);
        }
        Rift(c, "dual-run", "reserved", extra: data);
        Check((float)HudCall(p, "RiftElapsedRatio", c.Endgame.Rift, 0) == 0, "reserved runs have not started timing");
        Rift(c, "dual-run", "active", extra: ",\"remainingSeconds\":90");
        Check((float)HudCall(p, "RiftElapsedRatio", c.Endgame.Rift, 90) == 0, "old server without duration does not invent a 1500 second baseline");
        Rift(c, "dual-run", "active", extra: data);
        GameHud.I = new GameHud { uiScale = 2 };
        foreach (int width in new[] { 800, 1280, 1920 })
        {
            Screen.width = width; Screen.height = 720; UIScale.Factor = 1;
            GUI.textures.Clear(); GUI.uvTextures.Clear(); BitmapFont.labels.Clear(); ArtworkSkin.enableds.Clear();
            Draw(p);
            Check(GUI.textures[0].x == 160 && GUI.textures[0].y == 192 && GUI.textures[0].width == 280 && GUI.textures[0].height == 68, "viewport changes preserve native portrait-relative scale: " + width);
            Check(ArtworkSkin.enableds.Count == 0 && BitmapFont.labels.Count == 0 && EndgamePanel.RiftTargetHudOffset == 38, "compact HUD draws no adjacent caption and reserves 38 pixels for the selected target");
        }
        #if RIFT_HUD_TARGET_CONTRACT
        Rect target = GameHud.TargetBarRect();
        Check(target.x == 144 && target.y == 292 && target.width == 256 && target.height == 80, "native target hitbox uses both the same 38 pixel displacement and explicit HUD scale");
        TargetSelect.CurrentSourceForIndependentRuntime = new RemotePlayer();
        Input.mousePosition = new Vector3(160, Screen.height - 304, 0);
        Check(GameHud.I.MouseOverTargetForIndependentRuntime(), "pointer inside moved native target blocks a world click");
        Input.mousePosition = new Vector3(160, Screen.height - 220, 0);
        Check(!GameHud.I.MouseOverTargetForIndependentRuntime(), "old target location no longer consumes the progress-slot pointer");
        Set(p, "open", true); target = GameHud.TargetBarRect();
        Check(target.y == 216, "closing HUD restores native hitbox immediately without waiting for another GUI frame"); Set(p, "open", false);
#endif
        GameHud.I.uiScale = 0; UIScale.Factor = .75f; GUI.textures.Clear(); Draw(p);
        Check(GUI.textures[0].x == 60 && GUI.textures[0].y == 72 && GUI.textures[0].width == 105, "HUD falls back to the actual UIScale factor");
        Set(p, "open", true); Check(EndgamePanel.RiftTargetHudOffset == 0, "modal workbench restores target placement"); Set(p, "open", false);
        GameHud.I.showAvatar = false; Check(EndgamePanel.RiftTargetHudOffset == 0, "hidden portrait suppresses its attached rift bar"); GameHud.I.showAvatar = true;
        p.isActiveAndEnabled = false; Check(EndgamePanel.RiftTargetHudOffset == 0, "disabled panel cannot leave target displacement behind"); p.isActiveAndEnabled = true;
        c.EndgameInWorld = false; Check(EndgamePanel.RiftTargetHudOffset == 0, "disconnect removes target displacement immediately"); c.EndgameInWorld = true;
        int notices = NewbieTipBox.notices.Count; Call(p, "ObserveRiftPhase"); Draw(p); Draw(p); Call(p, "ObserveRiftPhase");
        Check(NewbieTipBox.notices.Count == notices, "restored snapshot and repeated draw passes never duplicate native notices");
        p.Unbind(c);
        Check(white.destroyed && !green.destroyed && EndgamePanel.RiftTargetHudOffset == 0, "unbind destroys only the owned white copy and restores native target layout");
        ArtworkSkin.frameTextures.Clear(); GameHud.I = null; UIScale.Factor = 1; Screen.width = 1280; Screen.height = 900;
    }
}

#if RIFT_HUD_TARGET_CONTRACT
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class GameHud { public bool showMenu = true, showTarget = true; }
    internal static class GameSettings { public static bool HotbarBottom; }
    internal static class TargetSelect { public static object CurrentSourceForIndependentRuntime; }
    internal class RemotePlayer { }
}
#endif
