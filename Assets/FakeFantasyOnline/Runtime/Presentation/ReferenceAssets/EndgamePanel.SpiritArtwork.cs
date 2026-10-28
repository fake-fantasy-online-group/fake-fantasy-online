using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Coordinates and package paths follow the shipped NPC Lua windows. No loose art.
    private sealed class SpiritLayout
    {
        public int Width, Height; public string Background, Buttons, Close;
        public Rect Spirit, Essence, Stats, Action, Back, Clear, CloseRect;
    }
    private SpiritLayout SpiritArtwork()
    {
        string key = spiritDefinition == null ? "glovesoul" : spiritDefinition.UIKey.ToLowerInvariant();
        bool process = spiritMode == "process", reset = spiritMode == "reset";
        bool small = key == "glovesoul" || key == "shoering" || key == "wingsoul";
        bool modern = key == "clothsoul" || key == "honorsoul" || key == "weaponsoul";
        string dir = key == "clothsoul" ? "ClothSoul" : key == "honorsoul" ? "HonorSoul" : key == "weaponsoul" ? "WeaponSoul" : key == "facesoul" ? "FaceSoul" : key == "neckring" && (process || reset) ? "NeckRing" : key;
        string file = modern ? dir + (process ? "Process" : reset ? "Reset" : "Relieve") :
            key == "wingsoul" || key == "facesoul" ? (process ? "process" : reset ? "reset" : "dearchive") : process ? "jiagongbk" : reset ? "guiyuanbk" : "jiefengbk";
        var l = new SpiritLayout { Width = small ? (process ? 223 : 225) : modern || process ? 249 : reset ? 262 : 248,
            Height = small ? (process ? 273 : 232) : process ? (modern ? 304 : 305) : 262,
            Background = "res/uires/" + dir + "/" + file + ".gsa", Buttons = "res/uires/" + (small ? "shoering" : modern ? dir : "RingSoul") + "/",
            Close = "res/uires/" + (small ? "common" : "newcommon") + "/miniclose.gsa" };
        if (small)
        {
            l.Spirit = new Rect(process ? 45 : 38, process ? 99 : 95, 36, 36); l.Essence = new Rect(process ? 46 : 38, process ? 162 : 154, 36, 36);
            l.Stats = new Rect(107, process ? 96 : 88, 108, 100); l.CloseRect = new Rect(l.Width - 25, 10, 22, 23);
            l.Action = new Rect(process ? 8 : 14, process ? 238 : 197, process ? 66 : 94, 24);
            l.Back = new Rect(79, 238, 66, 24); l.Clear = new Rect(process ? 150 : 119, process ? 238 : 197, process ? 66 : 94, 24);
        }
        else
        {
            int sx = modern ? (key == "weaponsoul" ? 53 : process ? 57 : 59) : reset && key == "neckring" ? 67 : process ? 52 : 54;
            l.Spirit = new Rect(sx, process ? 122 : 116, 36, 36); l.Essence = new Rect(sx, 182, 36, 36);
            l.Stats = new Rect(modern ? 114 : reset ? 124 : 111, key == "weaponsoul" ? 103 : 114, l.Width - (modern ? 114 : reset ? 124 : 111) - 12, 110);
            l.CloseRect = new Rect(modern ? 225 : l.Width - 29, 20, 22, 23);
            l.Action = new Rect(process ? (modern ? 33 : 27) : (modern ? 45 : 40), process ? 270 : 228, 69, 24);
            l.Back = new Rect(modern ? 105 : 100, 270, 69, 24); l.Clear = new Rect(process ? (modern ? 177 : 171) : (modern ? 162 : 157), process ? 270 : 228, 69, 24);
        }
        return l;
    }
    private float spiritWindowX = float.NaN, spiritWindowY, spiritWindowScale;
    private int spiritWindowGeneration = -1;
    private Rect SpiritScreenRect(float scale)
    {
        if (WinDrag.Stale(spiritWindowX, ref spiritWindowGeneration) || spiritWindowScale != scale)
        { spiritWindowX = (Screen.width - SpiritWindowWidth * scale) * .5f; spiritWindowY = (Screen.height - SpiritWindowHeight * scale) * .5f; spiritWindowScale = scale; }
        return new Rect(spiritWindowX, spiritWindowY, SpiritWindowWidth * scale, SpiritWindowHeight * scale);
    }
    private void HandleSpiritDrag(Rect window, float scale)
    {
        if (!SysMsgBox.Visible && !InventoryDlg.HasHeld)
            WinDrag.Handle(this, new Rect(window.x + 30 * scale, window.y + 10 * scale, (SpiritWindowWidth - 65) * scale, 25 * scale), ref spiritWindowX, ref spiritWindowY);
    }
    private static string SpiritStatName(int attr)
    {
        switch (attr) { case 1: return "力量"; case 3: return "智力"; case 5: return "体质"; case 7: return "敏捷"; case 9: return "灵巧"; case 61: return "精神"; case 13: return "防御"; case 15: return "魔攻"; case 17: return "魔防"; case 47: return "攻击"; case 69: return "伤抗"; case 70: return "魔抗"; default: return StatName(attr); }
    }
    private float SpiritWindowWidth => SpiritArtwork().Width;
    private float SpiritWindowHeight => SpiritArtwork().Height;
    private string SpiritButtonPath(SpiritLayout l, string action)
    {
        bool small = l.Width <= 225;
        return l.Buttons + (small ? action == "unseal" ? "jiefeng" : action == "process" ? "jiagong" : action == "rollback" ? "huitui" : action == "reset" ? "guiyuan" : "qingkong" : action == "unseal" ? "RelieveBtn" : action == "process" ? "ProcessBtn" : action == "rollback" ? "BackBtn" : action == "reset" ? "ResetBtn" : "ClearBtn") + ".gsa";
    }
    private void DrawSpirit()
    {
        var l = SpiritArtwork(); var bg = CubeTexture(l.Background);
        if (bg != null) GUI.DrawTexture(new Rect(0, 0, l.Width, l.Height), bg);
        if (CubeNativeButton(l.CloseRect, l.Close, "关闭", true)) { Close(); return; }
        if (!spiritBagOpened) { if (!InventoryDlg.IsOpen) InventoryDlg.Toggle(); spiritBagOpened = true; }
        DrawSpiritSlot(l.Spirit, spiritSelection, false); DrawSpiritSlot(l.Essence, spiritMaterial, true);
        var view = State.SpiritCurrent ? State.SpiritView : null;
        string stats = view == null ? (State.SpiritLoading ? "灵息探查中…" : "请放入" + spiritDefinition.Name) : view.Sealed ? "尚未解封" : "加工 " + view.Level + "/15";
        if (view != null) foreach (var a in view.Attrs)
            stats += "\n" + SpiritStatName(a.Attr) + " " + (a.Value - a.Last) + "+" + a.Last + "=" + a.Value;
        if (view != null) stats += "\n元神：" + (spiritMode == "unseal" ? view.UnsealEssence : spiritMode == "reset" ? view.ResetEssence : view.ProcessEssence);
        Text(l.Stats, stats, small, Gold);
        bool ready = SpiritEditable && view != null && spiritMaterial != null;
        bool allowed = view != null && (spiritMode == "unseal" ? view.CanUnseal : spiritMode == "reset" ? view.CanReset : view.CanProcess);
        if (CubeNativeButton(l.Action, SpiritButtonPath(l, spiritMode), spiritMode, (ready && allowed) || State.PendingRequest?.Op == "spirit." + spiritMode)) SubmitSpirit(spiritMode);
        if (spiritMode == "process" && CubeNativeButton(l.Back, SpiritButtonPath(l, "rollback"), "回退", (ready && view.CanRollback) || State.PendingRequest?.Op == "spirit.rollback")) SubmitSpirit("rollback");
        if (CubeNativeButton(l.Clear, SpiritButtonPath(l, "clear"), "清空", !State.Pending)) ClearSpiritItems();
        if (l.Stats.Contains(Event.current.mousePosition) && view != null)
        {
            string help = "每项属性独立加工；单次增加20为完美加工。\n回退、归元保留完美加工。";
            foreach (var a in view.Attrs) help += "\n" + SpiritStatName(a.Attr) + "：" + a.Level + "/15，完美" + a.PerfectLevel + "次";
            if (spiritMode == "process") help += "\n本次加工铜币：" + view.ProcessMoney + "；回退元神：" + view.RollbackEssence;
            ShowPanelTooltip(help);
        }
    }
}
}
