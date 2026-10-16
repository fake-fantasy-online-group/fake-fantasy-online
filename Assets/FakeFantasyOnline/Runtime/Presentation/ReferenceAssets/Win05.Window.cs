using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Original 06001041. In particular this is a Def indexer, and the two pointer reads
    // come from Input, not Event.current or a host-supplied point/scale.
    public bool MouseOver()
    {
        if (InventoryDlg.HasHeldBarPet) return true;
        if (cur == null || !positioned) return false;
        Def def = Defs[cur];
        Vector2 mouse = new Vector2(Input.mousePosition.x,
            Screen.height - Input.mousePosition.y);
        if (new Rect(winX, winY, def.w * s, def.h * s).Contains(mouse)) return true;
        if (cur != "petdlg" || !petSkillPanelOpen) return false;
        float scale = s;
        float originX = winX;
        float originY = winY;
        float extra = PetSkillExtraH();
        return new Rect(originX + 257f * scale, originY + 120f * scale,
            166f * scale, (177f + extra) * s).Contains(mouse);
    }

    // Kept only for source-fixture/API compatibility. Actual consumers call MouseOver().
    public bool MouseOver(Vector2 pointer, float scale) => MouseOver();

    // Original Win05.OnGUI 06001042. Unity is the only runtime GUI driver. Native Open and
    // Toggle create this independent root; a map host only rebinds its content suppliers.
    private void OnGUI()
    {
        float originX = 0f;
        float originY = 0f;
        Rect L(float x, float y, float width, float height) =>
            new Rect(originX + x * s, originY + y * s, width * s, height * s);

        if (!TryGetCurrentDefinition(out Def def)) { MobileClientPresentation.RemovePanel(this); return; }
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        EnsureUiFrames(loader);
        s = UIScale.Factor;
        PrepareMobileSkillWindow(def);
        if (!positioned)
        {
            winX = (Screen.width - def.w * s) * .5f;
            winY = (Screen.height - def.h * s) * .5f;
            positioned = true;
        }

        Event ev = Event.current;
        float baseWidth = def.w * s;
        float extraWidth = cur == "petdlg" && petSkillPanelOpen ? 166f * s : 0f;
        float width = baseWidth + extraWidth;
        var stacking = WinZ.Frame(this, new Rect(winX, winY, width, def.h * s), 10);
        GUI.depth = stacking.Item1;
        mobileSkillInputBlocked = stacking.Item2;
        Vector2 oldMouse = ev.mousePosition;
        if (stacking.Item2) ev.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            if (IsMobileSkillWindow && stacking.Item2) ResetMobileSkills();
            if (IsMobileSkillWindow && cur != "petdlg" && ev.type == EventType.MouseDown && !UITip.MobileContainsPointer(ev.mousePosition) &&
                (!mobileSkillActions || !mobileSkillActionsRect.Contains(ev.mousePosition))) ResetMobileSkills();
            bool actionsCover = IsMobileSkillWindow && ((mobileSkillActions && mobileSkillActionsRect.Contains(ev.mousePosition)) ||
                UITip.MobileContainsPointer(ev.mousePosition));
            Vector2 pointer = ev.mousePosition;
            if (actionsCover) ev.mousePosition = new Vector2(-9999f, -9999f);
            originX = winX;
            originY = winY;
            Rect close = def.cx >= 0f ? L(def.cx, def.cy, 20f, 19f) :
                L(def.w - 22f, 6f, 20f, 19f);
            bool hasHelp = !def.nohelp && def.cx >= 0f;
            Rect help = hasHelp ? L(def.cx - 20f, def.cy, 20f, 19f) :
                new Rect(-99f, -99f, 0f, 0f);
            Rect header = L(0f, 0f, def.w, 30f);
            if (ev.type == EventType.MouseDown && header.Contains(ev.mousePosition) &&
                !close.Contains(ev.mousePosition) && !help.Contains(ev.mousePosition))
            {
                dragging = true;
                dragOff = ev.mousePosition - new Vector2(originX, originY);
                ev.Use();
            }
            if (dragging && ev.type == EventType.MouseDrag)
            {
                winX = ev.mousePosition.x - dragOff.x;
                winY = ev.mousePosition.y - dragOff.y;
                ev.Use();
            }
            if (ev.type == EventType.MouseUp) dragging = false;

            Texture2D background = Bk(def);
            if (background != null && cur == "historybar")
            {
                GUI.color = PopUI.Backdrop;
                GUI.DrawTexture(L(0f, 0f, def.w, def.h), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (background != null)
                GUI.DrawTexture(L(0f, 0f, def.w, def.h), background);
            else
            {
                PatchPanel(L(0f, 0f, def.w, def.h));
                if (BitmapFont.Ready)
                    BitmapFont.CenterOutline(L(0f, 14f, def.w, 12f), def.title, Color.white, s);
            }

            if (hasHelp && DrawWindowHelpButton(help, helpFr, ev)) HelpWinDlg.Toggle();
            bool closed = closeFr != null && closeFr[0] != null
                ? Btn(close, closeFr, ev) : GUI.Button(close, "✕");
            if (closed)
            {
                cur = null;
                ResetMobileSkills();
                return;
            }

            if (cur == "historybar") DrawHistory(L, def);
            else if (cur == "petdlg") DrawPet(L, def, ev);
            else if (cur == "changeequipdlg") DrawChangeEquip(L, def, ev);
            else if (cur == "magicstoredlg") DrawMagicStore(L, def, ev);
            else if (cur == "skilldlg" || cur == "studydlg") DrawSkill(L, def, ev);
            else if (cur == "teamdlg")
            {
                if (BitmapFont.Ready)
                    BitmapFont.CenterOutline(L(0f, 175f, def.w, 12f), "暂无队伍",
                        new Color(150f / 255f, 150f / 255f, 150f / 255f, 1f), s);
            }
            else if (BitmapFont.Ready)
                BitmapFont.CenterOutline(L(0f, def.h - 42f, def.w, 12f), "(功能待接入)",
                    new Color(150f / 255f, 150f / 255f, 150f / 255f, 1f), s);
            GameHud.DrawDragIcon(s);
            if (actionsCover) ev.mousePosition = pointer;
            if (IsMobileSkillWindow && !stacking.Item2) DrawMobileSkillActions(L, def);
        }
        finally
        {
            // The native finally restores masked input only. It never saves/restores GUI
            // color or depth, and it does not turn every background white before drawing.
            if (stacking.Item2) ev.mousePosition = oldMouse;
        }
    }

    private void RequestSkillTipForIndependentRuntime(string text, float scale)
    {
        // The existing page helper terminates at the original UITip.Show. Its own
        // renderer can service an autonomous Win05 pass; a map-host request buffer cannot
        // impose ordering on Unity's independently driven OnGUI callbacks.
        UITip.Show(text, scale);
    }

    private void ShowMagicMessageForIndependentRuntime(string message, Action affirmative, Action cancel)
    {
        // A bound adapter additionally registers a newly created SysMsgBox with the host.
        // Keep that side effect while bound; native-created windows also work without it.
        if (magicMessageBox != null) magicMessageBox.Show(message, affirmative, cancel);
        else SysMsgBox.Show(message, affirmative, cancel);
    }
}
}
