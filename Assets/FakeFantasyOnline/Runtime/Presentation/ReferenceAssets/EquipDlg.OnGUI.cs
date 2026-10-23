using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    private bool additionalEquipmentOpen;

    // 1.5.8 06000313 / 1.6.0 0600031a: native lookup, including the full Int32 default.
    private static int SlotPart(int ui) => ui switch
    {
        0 => 2, 1 => 6, 2 => 8, 3 => 9, 4 => 4, 5 => 1,
        6 => 3, 7 => 10, 8 => 7, 9 => 5, 10 => 11, _ => -1
    };

    private static Vector2[] SlotPositions() => new[]
    {
        new Vector2(20,80), new Vector2(20,120), new Vector2(20,160),
        new Vector2(20,200), new Vector2(18,252), new Vector2(212,80),
        new Vector2(212,120), new Vector2(212,160), new Vector2(212,200),
        new Vector2(213,252), new Vector2(64,403)
    };

    // 1.6.0 06000314: Unity is the only driver. No world-host, readiness, finite-scale or session gate.
    private void OnGUI()
    {
        if (!open) { ClearMobileEquipmentSelection(); MobileClientPresentation.RemovePanel(this); return; }
        if (!loaded) Load();
        if (bk == null) return;
        s = UIScale.Factor;
        if (!placed)
        {
            winX = (Screen.width - 257f * s) * 0.5f;
            winY = (Screen.height - 506f * s) * 0.5f;
            placed = true;
        }
        bool mobile = MobileClientPresentation.Enabled;
        if (mobile) PrepareMobileEquipment();
        else MobileClientPresentation.RemovePanel(this);
        Event ev = Event.current;
        var z = WinZ.Frame(this, new Rect(winX - 26f * s, winY, 313f * s, 506f * s), 10);
        GUI.depth = z.Item1;
        Vector2 savedPointer = ev.mousePosition;
        if (z.Item2) ev.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            if (mobile)
            {
                if (z.Item2) ClearMobileEquipmentSelection();
                else ReadMobileEquipment(ev);
            }
            bool actionsCover = mobile && ((mobileEquipmentActions && mobileEquipmentActionsRect.Contains(ev.mousePosition)) || UITip.MobileContainsPointer(ev.mousePosition));
            Vector2 pointer = ev.mousePosition;
            if (actionsCover) ev.mousePosition = new Vector2(-9999f, -9999f);
            // Native display-class90_0 captures X/Y before drag stores. The current pass
            // retains these coordinates; the next callback observes the new winX/winY.
            var f = new GuiFrame(this, winX, winY);
            bool overButton = f.L(230,18,20,19).Contains(ev.mousePosition) ||
                f.L(211,18,20,19).Contains(ev.mousePosition);
            if (ev.type == EventType.MouseDown && f.L(0,0,257,44).Contains(ev.mousePosition) && !overButton)
            {
                dragging = true;
                dragOff = ev.mousePosition - new Vector2(f.X, f.Y);
                ev.Use();
            }
            if (dragging && ev.type == EventType.MouseDrag)
            {
                winX = ev.mousePosition.x - dragOff.x;
                winY = ev.mousePosition.y - dragOff.y;
                ev.Use();
            }
            if (ev.type == EventType.MouseUp) dragging = false;
            GUI.DrawTexture(f.L(0,0,257,506), bk);
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape && titleOpen)
            {
                titleOpen = false;
                taskOpen = false;
                ev.Use();
            }
            Btn(f.L(211,18,20,19), helpBtn, ev); // Native deliberately ignores Help.
            if (Btn(f.L(230,18,20,19), closeBtn, ev))
            {
                Array.Clear(pend, 0, 7);
                open = false;
            }
            else
            {
                string race = RaceNames[Mathf.Clamp(CharData.race, 0, 4)];
                if (BitmapFont.Ready)
                {
                    BitmapFont.CenterOutline(f.L(56,22,140,12),
                        string.Format("等级 {0} {1}:{2}", CharData.level, race, CharData.name), Color.white, s);
                    if (!string.IsNullOrEmpty(CharData.title))
                        BitmapFont.CenterOutline(f.L(27,48,166,12), CharData.title,
                            new Color(1f,220f/255f,120f/255f,1f), s);
                }
                Rect titleButton = f.L(201,43,44,22);
                if (Btn(titleButton, titleBtn, ev))
                {
                    titleOpen = !titleOpen;
                    if (!titleOpen) taskOpen = false;
                }
                if (titleOpen && titleBtn[1] != null) GUI.DrawTexture(titleButton, titleBtn[1]);
                if (VTab(f.L(250,68,28,80), "装备", tab == 0, false)) tab = 0;
                if (VTab(f.L(250,148,28,80), "属性", tab == 1, false)) tab = 1;
                if (VTab(f.L(-24,68,33,80), "换装", false, true))
                    Win05.Toggle(Win05.ChangeEquipmentDialogName);
                if (VTab(f.L(-24,148,33,80), "魔法衣橱", false, true))
                {
                    Win05.Toggle(Win05.MagicWardrobeDialogName);
                    if (Win05.IsShowing(Win05.MagicWardrobeDialogName) && !InventoryDlg.IsOpen)
                        InventoryDlg.Toggle();
                }
                if (tab == 1) DrawAttributePage(f);
                bool showAdditionalEquipment = DrawAdditionalEquipmentControl(f, ev);
                if (!showAdditionalEquipment) DrawDoll(f);
                DrawSlots(f, ev);
                if (showAdditionalEquipment) DrawAdditionalEquipmentSlots(f, ev);
                if (!BitmapFont.Ready) GUI.Button(f.L(0,0,257,506), GUIContent.none, GUIStyle.none);
                else
                {
                    DrawPoints(f, ev);
                    if (titleOpen) DrawTitles(f, ev);
                }
            }
            if (actionsCover) ev.mousePosition = pointer;
            if (mobile && !z.Item2) DrawMobileEquipmentSelection(ev);
        }
        finally
        {
            if (z.Item2) ev.mousePosition = savedPointer;
            // There is no native GUI.depth restoration.
        }
    }

    private readonly struct GuiFrame
    {
        private readonly EquipDlg owner;
        internal readonly float X, Y;
        internal GuiFrame(EquipDlg owner, float x, float y) { this.owner = owner; X = x; Y = y; }
        internal Rect L(float x, float y, float w, float h) =>
            new Rect(X + x * owner.s, Y + y * owner.s, w * owner.s, h * owner.s);
        internal void V(float x, float y, string text) =>
            BitmapFont.DrawOutline(X + x * owner.s, Y + y * owner.s, text, Color.white, owner.s);
    }

    private void DrawAttributePage(GuiFrame f)
    {
        if (attrPage != null) GUI.DrawTexture(f.L(14,64,239,174), attrPage);
        if (!BitmapFont.Ready) return;
        string[] values = AttributePageValuesForIndependentRuntime();
        float[] x = { 134,134,124,124,147,147,113,111 };
        float[] y = { 12,32,52,72,92,112,132,152 };
        for (int index = 0; index < values.Length; index++)
            f.V(x[index] + 14f, y[index] + 64f, values[index]);
    }

    internal string[] AttributePageValuesForIndependentRuntime()
    {
        int hit = Mathf.Clamp(At(17) * 30 / 100 + 48, 5, 99);
        int dodge = Mathf.Clamp(At(18) * 30 / 100 + 48, 5, 99);
        int speed = Mathf.Max(At(33), 50);
        float intervalBase = CharData.AttackIdentityDistance >= 151 && CharData.weaponCType != 16 ||
            CharData.weaponCType == 8 ? 0.5f : 1f;
        float interval = intervalBase / (speed / 100f);
        float frequency = interval > 0f ? 1f / interval : 0f;
        return new[]
        {
            At(31).ToString() + "(" + At(29).ToString() + "%)",
            At(32).ToString() + "(" + At(30).ToString() + "%)",
            CritStr(35,19), CritStr(36,20), hit.ToString() + "%", dodge.ToString() + "%",
            At(9).ToString(), frequency.ToString("0.0")
        };
    }

    internal string CritStr(int basisPointIndex, int fallbackIndex)
    {
        int[] values = CharData.attrsTotal ?? CharData.attrs;
        if (values != null && basisPointIndex < values.Length)
            return (values[basisPointIndex] / 100f).ToString("0.0") + "%";
        return At(fallbackIndex).ToString() + "%";
    }

    private void DrawDoll(GuiFrame f)
    {
        PlayerAvatar avatar = PlayerController.I != null ? PlayerController.I.Doll : null;
        if (avatar != null && avatar.ok)
        {
            if (doll != null)
            {
                doll.Dispose();
                doll = null;
                dollVer = -1;
            }
        }
        else
        {
            if (tab == 0)
            {
                if (dollVer != CharData.apVersion)
                {
                    if (doll != null) doll.Dispose();
                    doll = null;
                    dollTry = 0;
                    dollVer = CharData.apVersion;
                }
                if (tab == 0 && (doll == null || !doll.ok) && dollTry < 5)
                {
                    PlayerAvatar.Appear appearance = CharData.CombinedAppear();
                    doll = PlayerAvatar.Build(CharData.gender, CharData.head, CharData.hair, appearance, 100f);
                    dollTry++;
                }
            }
            avatar = doll;
        }
        if (tab == 0 && avatar != null && avatar.ok && avatar.Still(4) != null)
        {
            Sprite sprite = avatar.Still(4);
            Rect rect = sprite.textureRect;
            float fit = Mathf.Min(1.3f, Mathf.Min(83f * s / (rect.width * s), 96f * s / (rect.height * s)));
            float width = rect.width * s * fit, height = rect.height * s * fit;
            GUI.DrawTextureWithTexCoords(
                new Rect(f.X + 131.5f * s - width * 0.5f, f.Y + 192f * s - height, width, height),
                sprite.texture, new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                    rect.width / sprite.texture.width, rect.height / sprite.texture.height));
        }
    }

    private void DrawSlots(GuiFrame f, Event ev)
    {
        Vector2[] positions = SlotPositions();
        for (int index = 0; index < 11; index++)
        {
            bool sharedSlot = (1 << index & 0x610) != 0;
            if (!sharedSlot && tab != 0) continue;
            Vector2 position = positions[index];
            Rect rect = f.L(position.x, position.y, 30, 30);
            bool hover = rect.Contains(ev.mousePosition) && !AttrWashDlg.MouseOver();
            int wantPart = SlotPart(index);
            int found = equipped.FindIndex(row => row.slot == wantPart || row.slot == 13 && wantPart == 4);
            if (found < 0)
            {
                if (hover)
                {
                    Color old = GUI.color;
                    GUI.color = new Color(1f,1f,1f,0.22f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                    GUI.color = old;
                }
            }
            else
            {
                EquippedItem item = equipped[found];
                if (item.icon != null) ItemIconUI.Draw(rect, item.icon, s);
                else if (BitmapFont.Ready)
                    BitmapFont.Draw(rect.x + 3f * s, rect.y + 10f * s,
                        item.name.Substring(0, Math.Min(2, item.name.Length)),
                        item.quality > 0 ? (Color)UITip.QualityColor(item.quality) : Color.white, s);
                if (hover && !MobileClientPresentation.Enabled) DrawSlotAction(item, ev);
            }
            if (!sharedSlot && checkbox != null && checkbox.Length != 0)
            {
                float x = f.X + (position.x + (position.x < 100f ? 40f : -21f)) * s;
                float y = f.Y + (position.y + 2f) * s;
                float size = 10f * s;
                for (int kind = 0; kind < 2; kind++)
                {
                    // The native two-pixel gap is not multiplied by UIScale.
                    Rect box = new Rect(x, y + (size + 2f) * kind, size, size);
                    DrawEffectCheckbox(box, wantPart, kind);
                    Rect hit = new Rect(box.x - 4f * s, box.y, box.width + 8f * s, box.height);
                    if (hit.Contains(ev.mousePosition))
                        UITip.Show(kind == 0 ? "是否显示变装特效" : "是否显示灵外观特效", s);
                    if (ev.type == EventType.MouseDown && ev.button == 0 && hit.Contains(ev.mousePosition))
                    {
                        ToggleEquipmentEffect(wantPart, kind);
                        ev.Use();
                    }
                }
            }
            if (index == 10 && checkbox != null && checkbox.Length != 0)
            {
                DrawEffectCheckbox(f.L(99,413,10,10), wantPart, 1);
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(f.X + 113f * s, f.Y + 412f * s, "灵效",
                        new Color(230f/255f,210f/255f,150f/255f,1f), s);
                Rect hit = f.L(95,411,48,14);
                if (hit.Contains(ev.mousePosition)) UITip.Show("是否显示法宝的灵外观特效", s);
                if (ev.type == EventType.MouseDown && ev.button == 0 && hit.Contains(ev.mousePosition))
                {
                    ToggleEquipmentEffect(wantPart, 1);
                    ev.Use();
                }
            }
            if (index == 10 && checkbox != null && checkbox.Length != 0)
            {
                DrawEffectCheckbox(f.L(99,397,10,10), 12, 1);
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(f.X + 113f * s, f.Y + 396f * s, "龙霸",
                        new Color(230f/255f,210f/255f,150f/255f,1f), s);
                Rect hit = f.L(95,395,48,14);
                if (hit.Contains(ev.mousePosition))
                    UITip.Show("是否显示龙霸特效\n★与\"灵效\"各管各的; 需面饰上融有霸者面灵+10", s);
                if (ev.type == EventType.MouseDown && ev.button == 0 && hit.Contains(ev.mousePosition))
                {
                    bool shown = CharData.EquipShown(12,1);
                    CharData.SetEquipShown(12,1,!shown);
                    CharData.SaveEquipFx();
                    if (!ReferenceEquals(NetClient.I,null)) NetClient.I.SendEquipFx();
                    ev.Use();
                }
            }
        }
    }

    // Higher server equipment slots live in the character window too. Keep the native
    // eleven slots, doll, attribute controls and window hit bounds unchanged; only the
    // doll area is borrowed while the player chooses to inspect additional worn gear.
    private bool DrawAdditionalEquipmentControl(GuiFrame f, Event ev)
    {
        int count = 0;
        foreach (EquippedItem item in equipped) if (item.slot >= 14 && item.slot <= 25) count++;
        if (count == 0) additionalEquipmentOpen = false;
        if (tab != 0 || count == 0) return false;
        Rect control = f.L(75, 213, 108, 22);
        Patch9(control);
        string text = additionalEquipmentOpen ? "返回人物" : "其他装备 (" + count + ")";
        if (BitmapFont.Ready)
            BitmapFont.CenterOutline(f.L(75, 218, 108, 12), text,
                new Color(230f / 255f, 210f / 255f, 150f / 255f, 1f), s);
        else GUI.Label(control, text);
        if (ev.type == EventType.MouseDown && ev.button == 0 && control.Contains(ev.mousePosition) &&
            !AttrWashDlg.MouseOver())
        {
            additionalEquipmentOpen = !additionalEquipmentOpen;
            ev.Use();
        }
        return additionalEquipmentOpen;
    }

    private void DrawAdditionalEquipmentSlots(GuiFrame f, Event ev)
    {
        for (int index = 0; index < 12; index++)
        {
            int slot = 14 + index;
            Rect rect = f.L(79 + index % 3 * 36, 77 + index / 3 * 34, 30, 30);
            Border(rect, new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.7f));
            int found = equipped.FindIndex(item => item.slot == slot);
            if (found < 0) continue;
            EquippedItem item = equipped[found];
            if (item.icon != null) ItemIconUI.Draw(rect, item.icon, s);
            else if (BitmapFont.Ready)
                BitmapFont.Draw(rect.x + 3f * s, rect.y + 10f * s,
                    item.name.Substring(0, Math.Min(2, item.name.Length)),
                    item.quality > 0 ? (Color)UITip.QualityColor(item.quality) : Color.white, s);
            if (!MobileClientPresentation.Enabled && rect.Contains(ev.mousePosition) && !AttrWashDlg.MouseOver()) DrawSlotAction(item, ev);
        }
    }

    private void DrawSlotAction(EquippedItem item, Event ev)
    {
        if (AvatarFuse.Active)
        {
            UITip.Show(EndgamePanel.EquippedTooltip(item, TooltipContent.Fmt(TooltipContent.QualityName(item.name,item.quality),item.desc)) +
                "\n点击将变装融合到此装备", s);
            if (ev.type == EventType.MouseDown && ev.button == 0)
            { AvatarFuse.PickTarget(item.slot,item.name,item.avatar > 0); ev.Use(); }
            if (ev.type == EventType.MouseDown && ev.button == 1) { AvatarFuse.Cancel(); ev.Use(); }
        }
        else if (ToolTarget.Active)
        {
            UITip.Show(EndgamePanel.EquippedTooltip(item, TooltipContent.Fmt(TooltipContent.QualityName(item.name,item.quality),item.desc)) +
                "\n点击对此装备" + ToolTarget.Verb, s);
            if (ev.type == EventType.MouseDown && ev.button == 0)
            { ToolTarget.PickEquip(item.slot,item.name,item.avatar > 0); ev.Use(); }
            if (ev.type == EventType.MouseDown && ev.button == 1) { ToolTarget.Cancel(); ev.Use(); }
        }
        else if (AttrWashDlg.Picking)
        {
            UITip.Show(EndgamePanel.EquippedTooltip(item, TooltipContent.Fmt(TooltipContent.QualityName(item.name,item.quality),item.desc)) +
                "\n点击放入属性洗炼", s);
            if (ev.type == EventType.MouseDown && ev.button == 0)
            { AttrWashDlg.SetTarget(1,0,item.slot,item.name,item.icon); ev.Use(); }
            if (ev.type == EventType.MouseDown && ev.button == 1) { AttrWashDlg.Open(); ev.Use(); }
        }
        else if (RepairDlg.Picking)
        {
            UITip.Show(EndgamePanel.EquippedTooltip(item, TooltipContent.Fmt(TooltipContent.QualityName(item.name,item.quality),item.desc)) +
                "\n点击修理此装备(特殊档: 不掉耐久上限)", s);
            if (ev.type == EventType.MouseDown && ev.button == 0) { RepairDlg.PickSlot(item.slot); ev.Use(); }
            if (ev.type == EventType.MouseDown && ev.button == 1) { RepairDlg.CancelPick(); ev.Use(); }
        }
        else
        {
            int skill = 0;
            if (item.slot == 11 && CharData.skills != null)
            {
                foreach (var pair in CharData.skills)
                    if (pair.Key >= 14001 && pair.Key < 14020 && pair.Value >= 1)
                    { skill = pair.Key; break; }
            }
            UITip.Show(EndgamePanel.EquippedTooltip(item, TooltipContent.Fmt(TooltipContent.QualityName(item.name,item.quality),item.desc)) +
                (skill != 0 ? "\n左键拖法宝技到快捷栏 · 右键卸下" : "\n右键卸下"), s);
            if (ev.type == EventType.MouseDown && ev.button == 0 && InventoryDlg.ShareModHeld(ev))
            { InventoryDlg.ShareToChat(2,0,item.slot,item.name); ev.Use(); return; }
            if (skill != 0 && ev.type == EventType.MouseDown && ev.button == 0)
            { GameHud.PickupSkill(skill,item.icon,false); ev.Use(); return; }
            if (ev.type == EventType.MouseDown && ev.button == 1 && NetClient.I != null)
            { NetClient.I.UnequipItemForIndependentRuntime(item.slot); ev.Use(); }
        }
    }

    private void DrawEffectCheckbox(Rect rect, int part, int kind)
    {
        if (checkbox[0] != null) GUI.DrawTexture(rect, checkbox[0]);
        if (CharData.EquipShown(part,kind) && checkbox.Length > 1 && checkbox[1] != null)
            GUI.DrawTexture(rect,checkbox[1]);
        Border(rect,new Color(120f/255f,120f/255f,120f/255f,1f));
    }

    private static void ToggleEquipmentEffect(int part, int kind)
    {
        CharData.SetEquipShown(part,kind,!CharData.EquipShown(part,kind));
        CharData.SaveEquipFx();
        NetClient.I?.SendEquipFx();
        if (kind == 0) NetClient.RefreshTransmogFx();
        else if (PlayerController.I != null) PlayerController.I.RefreshSoulFx();
    }
    internal static IReadOnlyList<int> SlotPartsForIndependentRuntime =>
        new[] { 2, 6, 8, 9, 4, 1, 3, 10, 7, 5, 11 };
    internal static IReadOnlyList<Vector2> SlotPositionsForIndependentRuntime() => SlotPositions();
    internal bool ActOnSlotForIndependentRuntime(int slot, int button)
    {
        int index = equipped.FindIndex(item => item.slot == slot);
        if (index < 0) return false;
        var ev = new Event { type = EventType.MouseDown, button = button };
        DrawSlotAction(equipped[index],ev);
        return ev.type == EventType.Used;
    }

}
}
