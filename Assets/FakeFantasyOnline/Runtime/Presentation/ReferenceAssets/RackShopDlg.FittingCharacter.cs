using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private int fitSub;
        private GUIStyle _fitSearchSt;
        private int fitPetModel;
        private int fitPetItem;
        private int fitCatTab;

        // Official 0x06000a9f; null icon results are also cached by original item id.
        private Texture2D FitIconOf(int id)
        {
            if (!_fitIcon.TryGetValue(id, out Texture2D icon))
            {
                icon = NetClient.LoadItemIcon(id >= 90000000 ? 3385 : id);
                _fitIcon[id] = icon;
            }
            return icon;
        }

        // Official 0x06000abd.
        private static void SetApSlot(ref PlayerAvatar.Appear ap, string cat, int model)
        {
            switch (cat)
            {
                case "a": case "n": case "body": ap.Body = model; break;
                case "cap": case "cape": ap.Cap = model; break;
                case "backpack": ap.Backpack = model; break;
                case "face": ap.Face = model; break;
                case "weaponl": ap.WeaponL = model; break;
                case "weaponr":
                    ap.WeaponR = model;
                    if (SuitFxDB.IsDualWeaponModel(model)) ap.WeaponL = model;
                    break;
            }
        }

        // Official 0x06000ac4/0x06000ac5.
        private void ToggleTryOn(int id)
        {
            if (SuitFxDB.IsSoul(id))
            {
                string part = SoulPartOf(id);
                if (fitSouls.TryGetValue(part, out int current) && current == id) fitSouls.Remove(part);
                else fitSouls[part] = id;
            }
            else
            {
                var skin = SuitFxDB.Skin(id);
                if (string.IsNullOrEmpty(skin.Item1) || skin.Item2 < 1) return;
                if (fitOn.TryGetValue(skin.Item1, out int current) && current == id) fitOn.Remove(skin.Item1);
                else fitOn[skin.Item1] = id;
            }
        }
        private string SoulPartOf(int id)
            => fitById.TryGetValue(id, out FitItem item) && !string.IsNullOrEmpty(item.part) ? item.part : "灵";

        // Official 0x06000abc.
        private void DrawFitPet(Vector2 mouse, FittingRoomPreview preview)
        {
            if (StateBtn(L(150f, 545f, 52f, 24f), null, "左转", mouse)) preview.Rotate(-1);
            if (StateBtn(L(266f, 545f, 52f, 24f), null, "右转", mouse)) preview.Rotate(1);
            preview.ShowPet(fitPetModel);
        }

        // Official 0x06000abb. Usability and hue are sampled before the controls mutate mode.
        private void DrawFitCharacter(Vector2 mouse, FittingRoomPreview preview)
        {
            if (fitGender < 0) fitGender = CharData.gender;
            if (StateBtn(L(150f, 377f, 52f, 24f), null, "左转", mouse)) preview.Rotate(-1);
            if (StateBtn(L(266f, 377f, 52f, 24f), null, "右转", mouse)) preview.Rotate(1);
            if (StateBtn(L(48f, 222f, 44f, 24f), null, fitGender == 1 ? "男" : "女", mouse)) fitGender = 1 - fitGender;
            if (StateBtn(L(48f, 250f, 44f, 24f), null, preview.Sitting ? "起立" : "坐下", mouse)) preview.SetSit(!preview.Sitting);
            Lbl(46f, 412f, "变装", GRAY);
            for (int i = 0; i < FitSlotCat.Length; i++)
            {
                float x = 46f + i * 40f;
                Rect rect = L(x, 428f, 34f, 34f);
                FrameBox(rect);
                fitOn.TryGetValue(FitSlotCat[i], out int id);
                if (id < 1) LblC(rect, FitSlotName[i], GRAY);
                else
                {
                    Texture2D icon = FitIconOf(id);
                    if (icon != null) ItemIconUI.Draw(L(x + 2f, 430f, 30f, 30f), icon, s);
                    if (rect.Contains(mouse)) tipText = FitTip(id);
                }
                if (Click(rect, mouse) && id > 0) fitOn.Remove(FitSlotCat[i]);
            }
            Lbl(46f, 470f, "灵", GRAY);
            for (int i = 0; i < SoulPartOrder.Length; i++)
            {
                string part = SoulPartOrder[i];
                float x = 46f + i % 5 * 40f;
                float y = 486f + i / 5 * 38f;
                Rect rect = L(x, y, 34f, 34f);
                FrameBox(rect);
                if (!fitSouls.TryGetValue(part, out int id) || id < 1)
                {
                    LblC(rect, part.Length > 0 ? part.Substring(0, 1) : "灵", GRAY);
                    if (rect.Contains(mouse)) tipText = part;
                }
                else
                {
                    Texture2D icon = FitIconOf(id);
                    if (icon != null) ItemIconUI.Draw(L(x + 2f, y + 2f, 30f, 30f), icon, s);
                    if (rect.Contains(mouse)) tipText = FitTip(id);
                    if (Click(rect, mouse)) fitSouls.Remove(part);
                }
            }
            float checkY = 486f + Mathf.Max(1, (SoulPartOrder.Length + 4) / 5) * 38f + 6f;
            Rect check = L(46f, checkY, 16f, 16f);
            ArtworkSkin.Input(check, s, fitShow);
            if (fitShow) LblC(check, "√", GOLD);
            Lbl(68f, checkY + 1f, "在纸娃娃上展示试穿效果", Color.white);
            if (Click(L(46f, checkY, 220f, 18f), mouse)) fitShow = !fitShow;

            PlayerAvatar.Appear ap = default;
            var auras = new List<string>();
            var souls = new List<string>();
            if (fitShow)
            {
                var ids = new List<int>();
                foreach (var entry in fitOn)
                {
                    var skin = SuitFxDB.Skin(entry.Value);
                    if (skin.Item2 > 0) SetApSlot(ref ap, skin.Item1, skin.Item2);
                    ids.Add(entry.Value);
                }
                string aura = SuitFxDB.Resolve(ids);
                if (!string.IsNullOrEmpty(aura)) auras.Add(aura);
                foreach (int id in fitSouls.Values)
                {
                    string fx = SuitFxDB.SoulTFX(id);
                    if (!string.IsNullOrEmpty(fx)) souls.Add(fx);
                }
            }
            preview.ShowCharacter(fitGender, CharData.head, CharData.hair, ap, auras, souls);
            if (SuitFxDB.GlowTableCount < 1 || (!SuitFxDB.IsGlowWeaponModel(ap.WeaponR) && !SuitFxDB.IsGlowWeaponModel(ap.WeaponL)))
            {
                FittingRoomPreview.GlowOverride = -1;
                fitGlowMode = -1;
                return;
            }
            if (fitGlowMode < 0) fitGlowMode = Mathf.Clamp(CharData.glowMode, 1, 16);
            FittingRoomPreview.GlowOverride = fitGlowMode;
            bool usable = EffectPlayer.GlowUsable(fitGlowMode, CharData.glowUnlock);
            int oldMode = fitGlowMode;
            int oldHue = EffectPlayer.GlowHueOf(oldMode);
            if (StateBtn(L(48f, 278f, 44f, 24f), null, "换色", mouse))
            {
                if (oldMode < 10) fitGlowMode = fitGlowMode % 9 + 1;
                else
                {
                    int next = (oldHue + 3) % 9;
                    fitGlowMode = next < 2 ? next + 1 : next + 8;
                }
            }
            if (fitGlowMode > 2 && StateBtn(L(48f, 306f, 44f, 24f), null, oldMode < 10 ? "强化" : "还原", mouse))
                fitGlowMode = (oldMode < 10 ? 10 : 3) + oldHue;
            if (StateBtn(L(48f, 334f, 44f, 24f), null, "应用", mouse))
            {
                if (!usable)
                {
                    string name = EffectPlayer.GlowModeName[fitGlowMode];
                    ChatUI.Add("系统", string.Format("【{0}】还没解锁; 用【光效染色珠·{1}】可永久解锁这个颜色(含普通/·炽两档; 神奇货架「功能道具」有售)", name, name[0]),
                        new Color(1f, 180f / 255f, 120f / 255f, 1f));
                }
                else
                {
                    CharData.glowMode = fitGlowMode;
                    NetClient.I?.SetGlowModeForIndependentRuntime(fitGlowMode);
                    ChatUI.Add("系统", "武器光效→" + EffectPlayer.GlowModeName[fitGlowMode], new Color(1f, 200f / 255f, 120f / 255f, 1f));
                }
            }
            Lbl(44f, 362f, EffectPlayer.GlowModeName[fitGlowMode] + (usable ? "" : "(未解锁)"), usable ? GOLD : GRAY);
            if (L(48f, 278f, 44f, 84f).Contains(mouse))
                tipText = "武器光效: " + EffectPlayer.GlowModeName[fitGlowMode] + (usable
                    ? "(已解锁; 点「应用」即生效, 同图别人也看到)" : "(未解锁, 只能在这里试看; 需【光效染色珠】)");
        }
    }
}
