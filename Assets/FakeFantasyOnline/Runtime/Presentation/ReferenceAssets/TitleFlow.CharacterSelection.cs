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

    // Recovered 1.5.8 06000ef7 control flow with the approved selection-page presentation.
    // Existing avatar/selection/create/delete/entry routes stay in place; empty-roster
    // enter/delete controls are now visibly disabled rather than hidden.
    internal void DrawCharSelect()
    {
        EnsureSelectionPresentation();
        PrepareDeleteConfirmation();
        // The approved selection page has no smalllogo; the original three-slot stage remains.
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
                DrawSelectionLabel(new Rect(x + 19f, 95f, 160f, 22f),
                    row.DisplayName + " (LV " + row.Level + ")", new Color32(243, 220, 64, 255));
                DrawSelectionLabel(new Rect(x + 14f, 264f, 172f, 22f),
                    string.IsNullOrEmpty(row.LastLogin) ? "从未登陆" : row.LastLogin, new Color32(243, 220, 64, 255));
                if (!pendingDelete &&
                    GUI.Button(new Rect(x, 121f, 195f, 167f), GUIContent.none, GUIStyle.none) && i != selSlot)
                {
                    selSlot = i;
                    MirrorSlotToCharData(i);
                }
            }
            else
            {
                // Empty slots stay visually empty, as in the supplied original reference.
                // Keep the existing click-to-create target and its pending-delete guard.
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
            if (serverChars.Count < 3 && DrawSelectionButton(644f, 419f, SelectionButton.Create, "创建角色"))
            {
                prepareCreation(SelectedCharacterForIndependentRuntime);
                setMessage(string.Empty);
                _page = TitlePage.CharacterCreation;
                action(TitleCharacterAction.Create);
            }
            if (entering && Time.realtimeSinceStartup - enterAtMs > 15f)
            {
                entering = false;
                PublishPluginTitleResult("enter_timeout");
                setMessage("进入游戏超时, 请重试");
            }
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !entering && count != 0 && selSlot >= 0;
            if (DrawSelectionButton(322f, 487f, SelectionButton.Enter, "进入游戏")) EnterGame();
            // Preserve the existing deletion admission independently of the entry latch.
            // Empty-roster controls remain visible but cannot submit a character action.
            GUI.enabled = previousEnabled && count != 0 && selSlot >= 0;
            if (DrawSelectionButton(644f, 455f, SelectionButton.Delete, "删除角色")) OpenDeleteConfirmation();
            GUI.enabled = previousEnabled;
            if (DrawSelectionButton(644f, 498f, SelectionButton.Return, "返回上层"))
            {
                _page = TitlePage.ServerSelection;
                action(TitleCharacterAction.ReturnToServerSelection);
            }
        }

        // Keep the original property panel visible with blank values for an empty roster.
        Texture2D panel = selProp ?? proppanel;
        if (panel) TexScale.Draw(202f, 361f, panel);
        if (count != 0 && selSlot >= 0)
        {
            int race = Mathf.Clamp(CharData.race, 0, 4);
            DrawSelectionLabel(new Rect(268f, 366f, 110f, 16f), CharData.name, numStyle.normal.textColor, true);
            DrawSelectionLabel(new Rect(412f, 366f, 110f, 16f), CLASS[race] + "·" + (gender > 0 ? "男" : "女"), numStyle.normal.textColor, true);
            DrawSelectionLabel(new Rect(554f, 366f, 40f, 16f), CharData.level.ToString(), numStyle.normal.textColor, true);
            int[] attributes = null;
            long honor = 0;
            if (selSlot >= 0 && selSlot < serverChars.Count)
            {
                ServerCharacterRecord selected = serverChars[selSlot];
                attributes = selected.Attributes;
                honor = selected.Honor;
            }
            int[] defaults = INIT[race];
            SelectionValue(266f, 410f, AttributeAt(attributes, 4, defaults[7]));
            SelectionValue(412f, 410f, AttributeAt(attributes, 6, defaults[8]));
            SelectionValue(554f, 410f, (int)Math.Min(Math.Max(honor, 0L), int.MaxValue));
            SelectionValue(266f, 427f, AttributeAt(attributes, 10, defaults[0]));
            SelectionValue(412f, 427f, AttributeAt(attributes, 11, defaults[2]));
            SelectionValue(554f, 427f, AttributeAt(attributes, 13, defaults[4]));
            SelectionValue(266f, 443f, AttributeAt(attributes, 12, defaults[3]));
            SelectionValue(412f, 443f, AttributeAt(attributes, 21, defaults[1]));
            SelectionValue(554f, 443f, AttributeAt(attributes, 14, defaults[6]));
        }

        if (infoBar) GUI.DrawTexture(new Rect(114f, 523f, 529f, 21f), infoBar);
        DrawSelectionLabel(new Rect(114f, 523f, 529f, 21f),
            string.IsNullOrEmpty(netMsg) ? (count == 0 ? "请先创建角色" : "请选择当前登录角色!") : netMsg,
            new Color32(232, 224, 198, 255));

        if (DrawDeleteConfirmation())
        {
            action(TitleCharacterAction.Delete);
            CloseDeleteConfirmation();
            setMessage("删除中...");
        }
    }
}
}
