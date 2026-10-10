using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Official 1.5.8 06000ebc. A failed, non-null PlayerAvatar is still the cached slot value.
    // Native guard order precedes the cache lookup, including ResLoader.I and its Pkg.
    private PlayerAvatar SlotDoll(int i)
    {
        if (i < 0 || i >= serverChars.Count) return null;
        if (ResLoader.I == null || ResLoader.I.Pkg == null) return null;
        if (slotDoll[i] == null)
        {
            ServerCharacterRecord row = serverChars[i];
            PlayerAvatar.Appear ap = new PlayerAvatar.Appear
            {
                Body = row.apBody,
                Cap = row.apCap,
                Backpack = row.apBackpack,
                WeaponR = row.apWeaponR,
                WeaponL = row.apWeaponL,
                Face = row.apFace
            };
            slotDoll[i] = PlayerAvatar.Build(row.Gender > 0 ? 1 : 0,
                Mathf.Clamp(row.Head, 1, 8), Mathf.Clamp(row.Hair, 1, 8), ap, 100f);
        }
        return slotDoll[i];
    }

    // Official 1.5.8 06000ee2. Still owns its bake. The title owns only the avatar reference.
    private void DrawDollInFrame(PlayerAvatar av, Rect client)
    {
        if (av == null || !av.ok) return;
        Sprite sprite = av.Still(0);
        if (sprite == null || sprite.texture == null) return;
        Texture2D texture = sprite.texture;
        int top = Mathf.Max(Mathf.Max(av.nameTopPx, av.headPx), 1);
        float available = Mathf.Max(client.height - top, 0f);
        int anchorX = DOLL_FLIP ? texture.width - av.anchorX : av.anchorX;
        int anchorY = av.anchorY;
        GUI.BeginGroup(client);
        Rect destination = new Rect(client.width * 0.5f - anchorX,
            client.height - available * 0.5f - anchorY, texture.width, texture.height);
        if (DOLL_FLIP)
            GUI.DrawTextureWithTexCoords(destination, texture, new Rect(1f, 0f, -1f, 1f));
        else
            GUI.DrawTexture(destination, texture);
        GUI.EndGroup();
    }

    // Official 1.5.8 06000ee3. Bottom-aligned fit, normal full UVs; no doll flip or group clip.
    private void DrawSprite(Texture2D t, Rect area)
    {
        if (!t) return;
        float factor = Mathf.Min(area.width / t.width, area.height / t.height);
        float width = factor * t.width;
        float height = factor * t.height;
        GUI.DrawTexture(new Rect(area.x + (area.width - width) * 0.5f,
            area.y + area.height - height, width, height), t);
    }

    // Official 1.5.8 06000efb. Resolve the slot on this title at the call, then read
    // the complete value row before the first CharData store, as in both native bodies.
    private void MirrorSlotToCharData(int slot)
    {
        if (slot < 0 || slot >= serverChars.Count)
        {
            CharData.name = string.Empty;
            return;
        }

        ServerCharacterRecord selected = serverChars[slot];
        string name = selected.DisplayName;
        int race = selected.Race;
        int gender = selected.Gender;
        int head = selected.Head;
        int hair = selected.Hair;
        int level = selected.Level;
        string lastLogin = selected.LastLogin;
        int body = selected.apBody;
        int cap = selected.apCap;
        int backpack = selected.apBackpack;
        int weaponR = selected.apWeaponR;
        int weaponL = selected.apWeaponL;
        int face = selected.apFace;

        CharData.name = name;
        CharData.race = race;
        CharData.gender = gender;
        CharData.head = head;
        CharData.hair = hair;
        CharData.level = level;
        CharData.lastLogin = lastLogin;
        CharData.SetAppearance(body, cap, backpack, weaponR, weaponL, face);
    }

    // Official 1.5.8 06000efc: this is a synchronous request, not a response waiter.
    private void EnterGame()
    {
        int slot = selSlot;
        if (slot < 0 || slot >= serverChars.Count || entering) return;
        entering = true;
        enterAtMs = Time.realtimeSinceStartup;
        setMessage("进入游戏...");

        // Re-read the selected row after the state stores. The native unsigned second
        // bounds check throws; it is not another admission return or a clamped slot.
        IReadOnlyList<ServerCharacterRecord> characters = serverChars;
        slot = selSlot;
        if ((uint)slot >= (uint)characters.Count) throw new IndexOutOfRangeException();
        NetClient client = NetClient.I;
        // EnterGame uses the managed-null receiver check, unlike OnDestroy's Unity test.
        if (ReferenceEquals(client, null)) throw new NullReferenceException();
        client.RequestTitleEntryForIndependentRuntime(characters[slot].DisplayName);
    }

    // Official 1.5.8 06000ef7, full paired body. No art admission, double-click entry, or
    // shared host busy gate. The only enabled override surrounds the ordinary Enter button.
    internal void DrawCharSelect()
    {
        if (actorbk) TexScale.Draw(10f, 70f, actorbk);
        int count = serverChars.Count;
        int gender = CharData.gender; // Native read is before any row click/mirror this draw.
        // Original usage10030 / field040017fa, metadata blob0x19a480: third slot is538, not539.
        float[] xs = { 64f, 301f, 538f };
        if (count == 0) selSlot = -1;
        else if (selSlot < 0 || selSlot >= serverChars.Count) selSlot = 0;

        for (int i = 0; i < 3; i++)
        {
            float x = xs[i];
            float width;
            float height;
            if (actorcover != null)
            {
                width = GsaDecoder.W(actorcover);
                height = GsaDecoder.H(actorcover);
            }
            else
            {
                width = 195f;
                height = 145f;
            }
            if (i < serverChars.Count)
            {
                ServerCharacterRecord row = serverChars[i];
                if (i == selSlot)
                {
                    if (actorcover) GUI.DrawTexture(new Rect(x, 121f, width, height), actorcover);
                    else RecoveredTitleCharacterSurface.DrawBorder(new Rect(x, 121f, width, height), 3f, Color.white);
                }
                PlayerAvatar av = SlotDoll(i);
                if (av != null && av.ok)
                    DrawDollInFrame(av, new Rect(x + 12f, 121f, 172f, 120f));
                else
                {
                    // Read table first, then head/hair, then the gender index as in the native row.
                    Texture2D[,,] fallback = nudeAv;
                    int head = Mathf.Clamp(row.Head, 1, 8);
                    int hair = row.Hair > 1 ? 2 : 1;
                    DrawSprite(fallback[row.Gender > 0 ? 1 : 0, head, hair],
                        new Rect(x + 12f, 121f, 172f, 120f));
                }
                GUI.Label(new Rect(x + 19f, 95f, 160f, 22f),
                    "<color=#F3DC40><b>" + row.DisplayName + "</b> (LV " + row.Level + ")</color>", center);
                GUI.Label(new Rect(x + 14f, 264f, 172f, 22f),
                    "<color=#F3DC40>" + (string.IsNullOrEmpty(row.LastLogin) ? "从未登陆" : row.LastLogin) + "</color>", center);
                if (!pendingDelete &&
                    GUI.Button(new Rect(x, 121f, 195f, 167f), GUIContent.none, GUIStyle.none) && i != selSlot)
                {
                    selSlot = i;
                    MirrorSlotToCharData(i);
                }
            }
            else
            {
                RecoveredTitleCharacterSurface.DrawBorder(new Rect(x, 121f, width, height), 1f, Color.white);
                GUI.Label(new Rect(x, height * 0.5f + 121f - 10f, 195f, 20f),
                    "<color=#B4B4B4>＋ 创建角色</color>", center);
                if (!pendingDelete && GUI.Button(new Rect(x, 121f, 195f, 167f), GUIContent.none, GUIStyle.none))
                {
                    prepareCreation(SelectedCharacterForIndependentRuntime);
                    setMessage(string.Empty);
                    _page = TitlePage.CharacterCreation;
                    action(TitleCharacterAction.Create);
                }
            }
        }

        if (!pendingDelete)
        {
            if (serverChars.Count < 3 && ImgBtn(644f, 425f, btnCreate))
            {
                prepareCreation(SelectedCharacterForIndependentRuntime);
                setMessage(string.Empty);
                _page = TitlePage.CharacterCreation;
                action(TitleCharacterAction.Create);
            }
            if (entering && Time.realtimeSinceStartup - enterAtMs > 15f)
            {
                entering = false;
                setMessage("进入游戏超时, 请重试");
            }
            GUI.enabled = !entering;
            if (count != 0)
            {
                if (selSlot >= 0 && ImgBtn(322f, 487f, btnEnter)) EnterGame();
                GUI.enabled = true;
                if (selSlot >= 0 && ImgBtn(644f, 455f, btnDelete ?? btnReturn)) pendingDelete = true;
            }
            else GUI.enabled = true;
            if (ImgBtn(644f, 498f, btnReturn))
            {
                _page = TitlePage.ServerSelection;
                action(TitleCharacterAction.ReturnToServerSelection);
            }
        }

        if (count != 0 && selSlot >= 0)
        {
            int race = Mathf.Clamp(CharData.race, 0, 4);
            Texture2D panel = selProp ?? proppanel;
            if (panel) TexScale.Draw(202f, 361f, panel);
            GUI.Label(new Rect(268f, 366f, 110f, 16f), "<b>" + CharData.name + "</b>", numStyle);
            GUI.Label(new Rect(412f, 366f, 110f, 16f), CLASS[race] + "·" + (gender > 0 ? "男" : "女"), numStyle);
            GUI.Label(new Rect(554f, 366f, 40f, 16f), CharData.level.ToString(), numStyle);
            int[] attributes = null;
            long honor = 0;
            if (selSlot >= 0 && selSlot < serverChars.Count)
            {
                ServerCharacterRecord selected = serverChars[selSlot];
                attributes = selected.Attributes;
                honor = selected.Honor;
            }
            int[] defaults = INIT[race];
            LabelValue(266f, 410f, AttributeAt(attributes, 4, defaults[7]));
            LabelValue(412f, 410f, AttributeAt(attributes, 6, defaults[8]));
            LabelValue(554f, 410f, (int)Math.Min(Math.Max(honor, 0L), int.MaxValue));
            LabelValue(266f, 427f, AttributeAt(attributes, 10, defaults[0]));
            LabelValue(412f, 427f, AttributeAt(attributes, 11, defaults[2]));
            LabelValue(554f, 427f, AttributeAt(attributes, 13, defaults[4]));
            LabelValue(266f, 443f, AttributeAt(attributes, 12, defaults[3]));
            LabelValue(412f, 443f, AttributeAt(attributes, 21, defaults[1]));
            LabelValue(554f, 443f, AttributeAt(attributes, 14, defaults[6]));
        }

        if (pendingDelete && selSlot >= 0 && selSlot < serverChars.Count)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            GUI.DrawTexture(new Rect(210f, 225f, 380f, 150f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            RecoveredTitleCharacterSurface.DrawBorder(new Rect(210f, 225f, 380f, 150f), 2f, Color.white);
            GUI.Label(new Rect(210f, 243f, 380f, 24f), "<color=#FF6666><b>删除角色</b></color>", center);
            GUI.Label(new Rect(222f, 277f, 356f, 40f),
                "确定删除【" + serverChars[selSlot].DisplayName + "】?\n删除后不可恢复!", centerWrap ?? center);
            if (GUI.Button(new Rect(254f, 333f, 110f, 30f), "确定删除"))
            {
                action(TitleCharacterAction.Delete);
                pendingDelete = false;
                setMessage("删除中...");
            }
            if (GUI.Button(new Rect(436f, 333f, 110f, 30f), "取消")) pendingDelete = false;
        }
    }
}
}
