using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Original shared player-action menu. Unity owns its GUI and DDOL lifetime;
/// Build and Fire consume the original static owners without a world-view supplier.</summary>
internal sealed class PlayerActionMenu : MonoBehaviour
{
    // ---- static surface (official layout) ---------------------------------

    public const float W = 98f;
    public const float ROW = 22f;
    public const float BTN = 90f;
    public const float PADT = 3f;
    public const float PADB = 2f;
    public const float CORNER = 11f;
    private const float Edge = 3f;

    /// <summary>Background tint drawn behind the nine-slice (GUI.color before DrawTexture).</summary>
    /// <remarks>
    /// Official 1.5.8 arm64 constant pool at 0x011271c0 confirms these four float values;
    /// the independent reference renderer uses the same byte-normalized tint.
    /// </remarks>
    private static Color BK = new Color(26f / 255f, 27f / 255f, 22f / 255f, 196f / 255f);

    /// <summary>Title / row text colour. Official .cctor sets this to (1,1,1,1).</summary>
    private static readonly Color HOT = new Color(1f, 1f, 1f, 1f);

    /// <summary>Row hover fallback colour when rowBk is missing (0x3e20a0a1 / 0x3ec0c0c1 / 0x3f48c8c9).</summary>
    private static readonly Color HoverBlue = new Color(0.1569f, 0.3765f, 0.7843f, 1f);

    /// <summary>The global singleton instance, created lazily by <see cref="Ensure"/>.</summary>
    internal static PlayerActionMenu I { get; private set; }
    internal int LastActionForIndependentRuntime { get; private set; } = -1;

    // ---- nested Item (official value type) --------------------------------

    private struct Item
    {
        public string cap;
        public int act;

        public Item(string caption, int action)
        {
            cap = caption;
            act = action;
        }
    }

    // ---- instance fields (official layout) --------------------------------

    private bool visible;
    private byte tried;
    private int targetId;
    internal int TargetIdForIndependentRuntime => targetId;
    private string targetName = string.Empty;
    private Vector2 anchor;
    private float s;
    private Texture2D rowBk;
    private Texture2D pUL, pUR, pDL, pDR, pUM, pDM, pLM, pRM;
    private List<Item> items = new List<Item>();
    private string copyLine = string.Empty;
    private string copyBlock = string.Empty;

    // ---- lifecycle --------------------------------------------------------

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("PlayerActionMenu");
        // The official player always runs in play mode, where DontDestroyOnLoad is legal. Editor
        // audits are not play mode and Unity refuses the call there ("can only be used in play
        // mode"), so it is guarded rather than dropped: in game the behaviour is unchanged.
        if (UnityEngine.Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        I = go.AddComponent<PlayerActionMenu>();
    }

    private PlayerActionMenu()
    {
        targetName = string.Empty;
        copyLine = string.Empty;
        copyBlock = string.Empty;
        items = new List<Item>();
    }

    // ---- entry points (preserve the official 2-arg signature) ------------

    /// <summary>Official PlayerActionMenu.Open (0x0600096d, int playerId, string name).</summary>
    public static void Open(int playerId, string name)
    {
        Open(playerId, name, null, null);
    }

    /// <summary>Official PlayerActionMenu.Open (0x0600096e, int, string, string, string).</summary>
    public static void Open(int playerId, string name, string line, string block)
    {
        // The frozen bodies (arm64 0x290e98 / x86_64 0x29f8e0) call Ensure and then write straight
        // through I: every I read is an injected guard that jumps to the raise helper
        // (arm64 0x291090 -> 0x177a74), not a skip. The writes remain on the actual component
        // returned by Ensure; no host-ready or world-entry flag substitutes for that owner.
        Ensure();
        I.targetId = playerId;
        I.targetName = name ?? string.Empty;
        I.copyLine = Plain(line);
        I.copyBlock = Plain(block);
        I.anchor = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        I.Build();
        I.visible = I.items.Count > 0;
    }

    /// <summary>Strips the \eNN rich-text escape used by the official chat feed.</summary>
    public static string Plain(string value)
    {
        if (string.IsNullOrEmpty(value) || value.IndexOf('\\') < 0) return value ?? string.Empty;
        StringBuilder sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (i + 3 < value.Length &&
                value[i] == '\\' && value[i + 1] == 'e' &&
                IsHex(value[i + 2]) && IsHex(value[i + 3]))
            {
                i += 3;
                continue;
            }
            sb.Append(value[i]);
        }
        return sb.ToString();
    }

    /// <summary>Official Hide (0x06000970): only the visible flag is touched.</summary>
    public static void Hide()
    {
        if (I != null) I.visible = false;
    }

    /// <summary>Official get_Visible (0x06000971).</summary>
    public static bool Visible => I != null && I.visible;

    /// <summary>
    /// Official MouseOver (0x06000972). Has no official caller in 1.5.8; kept for surface fidelity.
    /// </summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        Vector2 p = Input.mousePosition;
        return I.Box().Contains(new Vector2(p.x, Screen.height - p.y));
    }

    internal static bool ContainsPointer(Vector2 topOriginPointer)
    {
        return I != null && I.visible && I.Box().Contains(topOriginPointer);
    }

    // ---- resource loading (nine fixed gsa, 0x06000973) --------------------

    private void Load()
    {
        tried = 1;
        if (ResLoader.I == null) { tried = 0; return; }
        rowBk = ResLoader.I.LoadGsa("res/uires/gameui/menubtn.gsa");
        pUL = ResLoader.I.LoadGsa("res/uires/patch/patchupleft.gsa");
        pUR = ResLoader.I.LoadGsa("res/uires/patch/patchupright.gsa");
        pDL = ResLoader.I.LoadGsa("res/uires/patch/patchdownleft.gsa");
        pDR = ResLoader.I.LoadGsa("res/uires/patch/patchdownright.gsa");
        pUM = ResLoader.I.LoadGsa("res/uires/patch/patchupmiddle.gsa");
        pDM = ResLoader.I.LoadGsa("res/uires/patch/patchdownmiddle.gsa");
        pLM = ResLoader.I.LoadGsa("res/uires/patch/patchleftmiddle.gsa");
        pRM = ResLoader.I.LoadGsa("res/uires/patch/patchrightmiddle.gsa");
    }

    // ---- Build (28-row guard table, 0x06000974) ---------------------------

    private void Build()
    {
        items.Clear();

        if (!string.IsNullOrEmpty(copyLine))
            items.Add(new Item("复制这一行", 50));
        if (!string.IsNullOrEmpty(copyBlock))
        {
            items.Add(new Item("复制聊天记录", 51));
            items.Add(new Item("选取复制…", 52));
        }

        // Official 06000974 entry gates (arm64 06000974.c:214-233, x86_64 06000974.c:165-176):
        // only the target NAME is tested. targetId (+0x24) is first read after both gates as the
        // StallDlg.IsStallOwner (06000db2) argument and is never compared with zero here. The self
        // gate is String.op_Equality(targetName, CharData.name) - metadata_usage_01441_kind1 is
        // TypeInfo:CharData and its static field 0 is the local name (CharData.SkillState.cs:10) -
        // so a name test, not an id test. Open writes name ?? "", so the null guard cannot fire.
        if (string.IsNullOrEmpty(targetName))
            return;
        if (targetName == CharData.name)
            return;
        if (StallDlg.IsStallOwner(targetId))
            items.Add(new Item("看　摊", 30));

        // Official 1.5.8 caption for act 3 is metadata_usage_17331_kind5, which both frozen usages
        // tables decode as 密 + U+0020 + 语; the full-width U+3000 this row used is absent from the
        // literal. The index does preserve U+3000 in other rows (16880/16881), so this is a real
        // divergence rather than an escaping artifact. BitmapFont advances U+0020 by 6px and falls
        // back to a whole cell otherwise, so restoring it also restores the drawn row width.
        items.Add(new Item("密 语", 3));
        items.Add(new Item("交谈/语音", 4));
        items.Add(new Item("邀请参加交易", 0));

        if (SocialClient.Party.Exists(member => member.Item1 == targetId))
        {
            if (SocialClient.PartyLeaderId != NetClient.SelfEntityId)
                items.Add(new Item("跟随队长", 14));
            else
            {
                items.Add(new Item("踢出队伍", 12));
                items.Add(new Item("转让队长", 13));
            }
        }
        else if (SocialClient.Party.Count < 1)
        {
            items.Add(new Item("创建队伍", 24));
            items.Add(new Item("申请加入", 26));
        }
        else
        {
            items.Add(new Item("邀请入队", 10));
        }

        items.Add(new Item("跟随玩家", 25));

        bool isFriend = false;
        if (SocialClient.Friends != null)
        {
            for (int i = 0; i < SocialClient.Friends.Count; i++)
            {
                if (SocialClient.Friends[i].Item1 == targetName) { isFriend = true; break; }
            }
        }
        if (isFriend) items.Add(new Item("删除好友", 23));
        else items.Add(new Item("加为好友", 22));

        // Official 0600097d captures this and compares only the apprentice's name with the
        // current targetName. A matching master short-circuits the apprentice lookup.
        if ((!string.IsNullOrEmpty(SocialClient.MasterName) && SocialClient.MasterName == targetName) ||
            SocialClient.Apprentices.Exists(apprentice => apprentice.Item1 == targetName))
            items.Add(new Item("解除师徒", 31));

        if (SocialClient.IsBlocked(targetName))
            items.Add(new Item("解除屏蔽", 27));
        else
        {
            items.Add(new Item("屏蔽此人", 28));
            items.Add(new Item("屏蔽他所有角色", 29));
        }

        items.Add(new Item("拜　师", 20));
        items.Add(new Item("收　徒", 21));
        items.Add(new Item("查看玩家装备", 40));
        items.Add(new Item("生死擂台", 44));

        int rideAnchor = CharData.rideAnchorId;
        if (rideAnchor != 0)
        {
            if (rideAnchor == targetId) items.Add(new Item("下　车", 42));
        }
        else if (!CharData.riding)
        {
            RemotePlayer remote = CombatWorld.FindPlayer(targetId);
            if (remote != null && remote.CanCarryPassenger)
                items.Add(new Item("搭乘坐骑", 41));
        }
        else if (CharData.rideSeats > 1 && targetId != 0)
            items.Add(new Item("邀请上车", 43));

        if (SocialClient.MyFamilyPos == 0 || SocialClient.FamilyCanInvite)
            items.Add(new Item("邀请加入家族", 2));
    }

    // ---- Box (0x06000975) ------------------------------------------------

    private Rect Box()
    {
        float scale = UIScale.Factor;
        int n = items.Count;
        float w = W * scale;
        float h = (n * ROW + PADT + PADB) * scale;
        float x = anchor.x;
        float y = anchor.y;
        if (Screen.width < w + x) x = Screen.width - w;
        if (Screen.height < y + h) y = Screen.height - h;
        if (x < 0f) x = 0f;
        if (y < 0f) y = 0f;
        return new Rect(x, y, w, h);
    }

    // ---- OnGUI (0x06000976) ----------------------------------------------

    private void OnGUI()
    {
        if (!visible) return;
        if (tried == 0) Load();
        s = UIScale.Factor;
        // Official order (arm64 0x2930c8 / x86_64 0x2a1da6): the event is read before GUI.depth and
        // Box, and every one of the nine Event.current reads in the body is an injected guard that
        // jumps to the non-returning raise helper - arm64 0x293804 -> 0x177a74, x86_64 0x2a25a7 ->
        // 0x16c1b0, nine guards in both slices at matching positions. A null event therefore throws;
        // it does not skip a row or the tail.
        Event e = Event.current;
        Vector2 mouse = e.mousePosition;
        GUI.depth = 1;
        Rect r = Box();
        // Official never reads GUI.color: four set_color and zero get_color (arm64 06000976.c
        // lines 80/86/247/252; x86_64 four calls to 0x1063f90). The body leaves GUI.color white on
        // every path that writes it - the second write before Draw9 and the one closing the hover
        // fill are both (1,1,1,1) - and restores nothing.
        GUI.color = BK;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        Draw9(r);

        for (int i = 0; i < items.Count; i++)
        {
            float x = r.x + s * 4f;
            float w = s * BTN;
            float y = r.y + s * PADT + i * s * ROW;
            // Official row hit test (arm64 0x293290-0x293330): lower bound closed, upper bound
            // open on both axes, i.e. Rect.Contains - not the closed maxima this used.
            bool hovered = mouse.x >= x && mouse.x < x + w &&
                           mouse.y >= y && mouse.y < y + s * ROW;
            if (hovered)
            {
                Rect fill = new Rect(x, y + s * 4f, s * BTN, s * 14f);
                if (rowBk != null)
                    GUI.DrawTexture(fill, rowBk);
                else
                {
                    GUI.color = HoverBlue;
                    GUI.DrawTexture(fill, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
            if (BitmapFont.Ready)
            {
                BitmapFont.CenterOutline(
                    new Rect(x, y + s * 5f, s * BTN, s * 12f),
                    items[i].cap,
                    HOT,
                    s);
            }
            if (hovered && e.type == EventType.MouseDown && e.button == 0 && !SysMsgBox.Visible)
            {
                e.Use();
                PlayerController.SuppressWorldClick();
                visible = false;
                Fire(items[i].act);
            }
        }

        // Official tail (arm64 0x293648-0x2937d0, x86_64 0x2a24e4 / 0x2a258c): two independent
        // dismiss arms, both running after the row loop and each preceded by
        // PlayerController.SuppressWorldClick. The first has no button test at all - any
        // MouseDown outside Box() closes - and the second closes on the right button wherever
        // it lands, including on the menu itself. A right click outside therefore runs both,
        // exactly as the frozen bodies do.
        if (e.type == EventType.MouseDown && !r.Contains(mouse))
        {
            PlayerController.SuppressWorldClick();
            visible = false;
        }

        if (e.type == EventType.MouseDown && e.button == 1)
        {
            PlayerController.SuppressWorldClick();
            visible = false;
        }
    }

    // ---- Fire (0x06000977) ----------------------------------------------

    private void Fire(int act)
    {
        LastActionForIndependentRuntime = act;
        if (act == 3) { ChatUI.StartWhisper(targetName); return; }
        if (act == 4) { GossipDlg.Open(targetName); return; }
        if (act == 50)
        {
            if (string.IsNullOrEmpty(copyLine)) return;
            GUIUtility.systemCopyBuffer = copyLine;
            UITip.Show("已复制这一行", 1.2f);
            return;
        }
        if (act == 51)
        {
            if (string.IsNullOrEmpty(copyBlock)) return;
            GUIUtility.systemCopyBuffer = copyBlock;
            int rows = 1;
            for (int i = 0; i < copyBlock.Length; i++)
                if (copyBlock[i] == '\n') rows++;
            UITip.Show("已复制 " + rows + " 行聊天记录", 1.2f);
            return;
        }
        if (act == 52)
        {
            if (string.IsNullOrEmpty(copyBlock)) return;
            string caption = string.IsNullOrEmpty(targetName)
                ? ReferenceChatCopyOverlay.HistoryTitle
                : ReferenceChatCopyOverlay.WithPlayerTitlePrefix + targetName +
                  ReferenceChatCopyOverlay.WithPlayerTitleSuffix;
            ChatCopyOverlay.Open(copyBlock, caption);
            return;
        }

        switch (act)
        {
            case 0: if (NetClient.I != null) NetClient.I.TradeRequestForIndependentRuntime(targetId); break;
            case 2: if (NetClient.I != null) NetClient.I.FamilyInviteForIndependentRuntime(targetId, targetName); break;
            case 10: if (NetClient.I != null) NetClient.I.PartyInviteForIndependentRuntime(targetId, targetName); break;
            case 12: if (NetClient.I != null) NetClient.I.PartyKickForIndependentRuntime(targetId); break;
            case 13: if (NetClient.I != null) NetClient.I.PartyLeaderForIndependentRuntime(targetId); break;
            case 14:
                if (PlayerController.I != null && SocialClient.PartyLeaderId != 0)
                    PlayerController.I.FollowMate(SocialClient.PartyLeaderId);
                break;
            case 20: if (NetClient.I != null) NetClient.I.ApprenticeReqForIndependentRuntime(targetId, 0, targetName); break;
            case 21: if (NetClient.I != null) NetClient.I.ApprenticeReqForIndependentRuntime(targetId, 1, targetName); break;
            case 22: if (NetClient.I != null) NetClient.I.FriendReqForIndependentRuntime(targetName); break;
            case 23:
            {
                string name = targetName;
                SysMsgBox.Show("确定删除好友【" + name + "】吗？", () =>
                {
                    if ((object)NetClient.I != null) NetClient.I.FriendDelForIndependentRuntime(name);
                }, null);
                break;
            }
            case 24:
                TeamConfigDlg.Open((string.IsNullOrEmpty(CharData.name) ? "我" : CharData.name) + "的小队", targetId);
                break;
            case 25:
                if (PlayerController.I != null) PlayerController.I.FollowMate(targetId);
                break;
            case 26: if (NetClient.I != null) NetClient.I.PartyJoinReqForIndependentRuntime(targetId, targetName); break;
            case 27:
            {
                long characterId = 0;
                foreach (var block in SocialClient.Blocks)
                {
                    if (block.Item2 != targetName) continue;
                    characterId = block.Item1;
                    break;
                }
                if (characterId == 0) return;
                SysMsgBox.Show("解除对【" + targetName + "】的屏蔽？\n解除后又能收到他的聊天和各种请求。", () =>
                {
                    if ((object)NetClient.I != null) NetClient.I.SendBlockDelForIndependentRuntime(characterId);
                }, null);
                break;
            }
            case 28:
            case 29:
            {
                string name = targetName;
                byte scope = act == 28 ? (byte)0 : (byte)1;
                string suffix = scope == 0
                    ? "】？\n他的世界/附近/队伍/家族发言、私聊、组队交易等请求、邮件都不会再打扰你。\n(喇叭广播是付费道具, 仍然会收到)\n对方不会收到任何提示。"
                    : "】的所有角色？\n他换成同一个账号下的任何角色上线，都不会再打扰你。\n(喇叭广播是付费道具, 仍然会收到)\n对方不会收到任何提示。";
                SysMsgBox.Show("屏蔽【" + name + suffix, () =>
                {
                    if ((object)NetClient.I != null) NetClient.I.SendBlockAddForIndependentRuntime(name, scope);
                }, null);
                break;
            }
            case 30:
                if (StallDlg.IsStallOwner(targetId) && NetClient.I != null)
                    NetClient.I.StallBrowseForIndependentRuntime(targetId);
                break;
            case 31:
            {
                string name = targetName;
                // Capture this menu, not a per-map borrower. Its target id is intentionally
                // read at confirmation; its name is the value captured when Fire ran.
                SysMsgBox.Show("确定解除与【" + name + "】的师徒关系吗？", () =>
                {
                    if ((object)NetClient.I != null) NetClient.I.ApprenticeReqForIndependentRuntime(targetId, 2, name);
                }, null);
                break;
            }
            case 40: if (NetClient.I != null) NetClient.I.InspectPlayerForIndependentRuntime(targetId); break;
            case 41: if (NetClient.I != null) NetClient.I.RideTogetherForIndependentRuntime(targetId); break;
            case 42: if (NetClient.I != null) NetClient.I.RideTogetherForIndependentRuntime(0); break;
            case 43: if (NetClient.I != null) NetClient.I.RideInviteForIndependentRuntime(targetId); break;
            case 44: DuelDlg.Open(targetId, targetName, 0); break;
        }
    }

    // ---- Draw9 (0x06000978) ----------------------------------------------

    private void Draw9(Rect r)
    {
        float corner = CORNER * s;
        float edge = Edge * s;
        D(pUM, new Rect(r.x + corner, r.y, r.width - 2f * corner, edge));
        D(pDM, new Rect(r.x + corner, r.yMax - edge, r.width - 2f * corner, edge));
        D(pLM, new Rect(r.x, r.y + corner, edge, r.height - 2f * corner));
        D(pRM, new Rect(r.xMax - edge, r.y + corner, edge, r.height - 2f * corner));
        D(pUL, new Rect(r.x, r.y, corner, corner));
        D(pUR, new Rect(r.xMax - corner, r.y, corner, corner));
        D(pDL, new Rect(r.x, r.yMax - corner, corner, corner));
        D(pDR, new Rect(r.xMax - corner, r.yMax - corner, corner, corner));
    }

    private static void D(Texture2D tex, Rect rect)
    {
        if (tex != null) GUI.DrawTexture(rect, tex);
    }

    // ---- helpers ---------------------------------------------------------

    private static bool IsHex(char c)
    {
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}

}
