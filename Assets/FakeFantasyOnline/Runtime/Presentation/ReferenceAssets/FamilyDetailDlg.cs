using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 FamilyDetailDlg (家族详情窗口), recovered from the native 19-method surface
/// (tokens 0x0600036c..0x0600037e, including the OnGUI local functions and the compiler closure
/// type). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_DETAIL_DLG_TRANSLATION_ARM64_V1.json and
/// docs/contracts/VERSION_UPGRADE_1_5_8_FAMILY_DETAIL_DLG_VERIFICATION_X86_V1.json.
/// Logical window 386x513, twelve official .gsa assets, family header, editable proclamation,
/// the 谢绝申请 checkbox, a 13-row member list with scrollbar and right-click, and the
/// 关系/宣言/授予职位/罢免职位/转让族长/自定职位/踢出/列表/家族邮件/退出家族 actions.
/// The row/header adapter projects the original SocialClient family roots at the read site.
/// </summary>
public sealed class FamilyDetailDlg : MonoBehaviour
{
    public static FamilyDetailDlg I;

    /// <summary>Read-only projection; receipt-time native roots remain authoritative.</summary>
    public static WorldFamilyState State => SocialClient.FamilyStateForIndependentRuntime;

    /// <summary>Read the current native name, including any already-received rename.</summary>
    public static string MyName => CharData.name;

    private const float NW = 386f;
    private const float NH = 513f;
    private const int VisibleRows = 13;

    private bool visible;
    private bool tried;
    private bool editPro;
    private int selMember = -1;
    private string proText = "";
    private GUIStyle _proStyle;
    private int memberScroll;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoRelation;
    private Texture2D icoProclaim;
    private Texture2D icoJobCustom;
    private Texture2D icoJobOffer;
    private Texture2D icoJobFire;
    private Texture2D icoJobSell;
    private Texture2D icoKick;
    private Texture2D icoQuit;
    private Texture2D icoCheck;
    private Texture2D icoList;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("FamilyDetailDlg");
        I = go.AddComponent<FamilyDetailDlg>();
        Object.DontDestroyOnLoad(go);
    }

    /// <summary>Official Toggle (0x0600036d): Ensure, flip visible, and request a refresh when the
    /// window has just become visible.</summary>
    public static void Toggle()
    {
        Ensure();
        bool wasVisible = I.visible;
        I.visible = !wasVisible;
        if (!wasVisible) I.RequestRefresh();
    }

    /// <summary>Official OpenSelf (0x0600036e): Ensure and set visible=true. It does not refresh;
    /// the 0x8034 openFlag==1 path calls FamilyDlg.Hide() and then this.</summary>
    public static void OpenSelf()
    {
        Ensure();
        I.visible = true;
    }

    public static bool get_Visible()
    {
        return I != null && I.visible;
    }

    /// <summary>Window-level hit test used by adjacent dialogs (official MouseOver): true when the
    /// window exists, is visible and the real mouse is inside the 386x513 scaled rect. A NaN winX
    /// means the window has not been placed yet, so the centred rect is used for both axes.</summary>
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float factor = UIScale.Factor;
        float x = I.winX;
        float y = I.winY;
        if (float.IsNaN(x))
        {
            x = (Screen.width - factor * NW) * 0.5f;
            y = (Screen.height - factor * NH) * 0.5f;
        }
        Vector3 mp = Input.mousePosition; // bottom-left origin
        float mx = mp.x;
        float my = Screen.height - mp.y; // top-left origin
        return mx >= x && mx < x + factor * NW && my >= y && my < y + factor * NH;
    }

    /// <summary>Official RequestRefresh (0x0600036f): sends FamilyRefresh only while a family name
    /// is known and the client is live.</summary>
    public void RequestRefresh()
    {
        if (string.IsNullOrEmpty(State.Name)) return;
        if (NetClient.I != null) NetClient.I.FamilyRefreshForIndependentRuntime();
    }

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/family/familydetaildlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoRelation = loader.LoadGsa("res/uires/family/relationbtn.gsa");
        icoProclaim = loader.LoadGsa("res/uires/family/proclaimbtn.gsa");
        icoJobCustom = loader.LoadGsa("res/uires/family/jobcustombtn.gsa");
        icoJobOffer = loader.LoadGsa("res/uires/family/jobofferbtn.gsa");
        icoJobFire = loader.LoadGsa("res/uires/family/jobfirebtn.gsa");
        icoJobSell = loader.LoadGsa("res/uires/family/jobsellbtn.gsa");
        icoKick = loader.LoadGsa("res/uires/family/kickbtn.gsa");
        icoQuit = loader.LoadGsa("res/uires/family/quitbtn.gsa");
        icoCheck = loader.LoadGsa("res/uires/common/checkbox.gsa");
        icoList = loader.LoadGsa("res/uires/family/listbtn.gsa");
    }

    /// <summary>Rect in scaled window space: (winX + x*s, winY + y*s, w*s, h*s).</summary>
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    /// <summary>Official Lbl (0x06000374): DrawOutline at (winX+x*s, winY+y*s).</summary>
    private void Lbl(float x, float y, string text, Color fill)
    {
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, fill, s);
    }

    /// <summary>Official LblC (0x06000375): CenterOutline in a scaled rect.</summary>
    private void LblC(float x, float y, float w, float h, string text, Color fill)
    {
        if (BitmapFont.Ready)
            BitmapFont.CenterOutline(L(x, y, w, h), text, fill, s);
    }

    /// <summary>Official Note (0x06000376): orange system line into the chat window.</summary>
    private static void Note(string text)
    {
        ChatUI.Add("系统", text, new Color(1f, 220f / 255f, 150f / 255f, 1f), "");
    }

    private static void NeedLeader() => Note("只有族长可以管理家族");

    private static void NeedSel() => Note("请先在成员列表选中一名成员");

    /// <summary>Official &lt;OnGUI&gt;b__38_4: leave the family, then hide the window either way.</summary>
    private void LeaveFamily()
    {
        if (NetClient.I != null) NetClient.I.FamilyLeaveForIndependentRuntime();
        visible = false;
    }

    /// <summary>Official &lt;OnGUI&gt;c__DisplayClass38_0.&lt;OnGUI&gt;b__2 (踢出确认回调).</summary>
    private static void ConfirmKick(string memberName)
    {
        if (NetClient.I == null) return;
        NetClient.I.FamilyManageForIndependentRuntime(2, memberName, 0, "");
    }

    /// <summary>Official &lt;OnGUI&gt;c__DisplayClass38_0.&lt;OnGUI&gt;b__3 (转让族长确认回调).</summary>
    private static void ConfirmTransferLeader(string memberName)
    {
        if (NetClient.I == null) return;
        NetClient.I.FamilyManageForIndependentRuntime(0, memberName, 0, "");
    }

    /// <summary>The style OnGUI builds for the proclamation editor (GUIStyle.none + CnFont +
    /// wordWrap + UpperLeft + white text, 12 px per scale step).</summary>
    private GUIStyle ProStyle()
    {
        if (_proStyle == null)
        {
            _proStyle = new GUIStyle(GUIStyle.none);
            _proStyle.font = NpcChatter.CnFont();
            _proStyle.wordWrap = true;
            _proStyle.alignment = TextAnchor.UpperLeft;
            _proStyle.normal.textColor = Color.white;
        }
        _proStyle.fontSize = (int)System.Math.Round((double)(s * 12f));
        return _proStyle;
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();

        WorldFamilyState family = State ?? WorldFamilyState.Empty;
        string self = MyName ?? "";
        bool isLeader = SocialClient.IamFamilyLeader;
        IReadOnlyList<(string, int, string, string, bool)> members = family.Members;
        int count = members.Count;
        int maxScroll = Mathf.Max(0, count - VisibleRows);

        (int depth, bool covered) = WinZ.Frame(this, new Rect(winX, winY, s * NW, s * NH), 10);
        Event e = Event.current;
        Vector2 saved = e != null ? e.mousePosition : new Vector2(-9999f, -9999f);
        if (covered && e != null) e.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - s * NW) * 0.5f;
                winY = (Screen.height - s * NH) * 0.5f;
            }
            Vector2 mouse = e != null ? e.mousePosition : new Vector2(-9999f, -9999f);
            GUI.depth = depth;

            if (bg == null)
            {
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0f, 0f, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            else
            {
                GUI.DrawTexture(L(0f, 0f, NW, NH), bg);
            }

            DrawHeader(family, count);
            DrawProclaim(family);
            DrawResistRow(family, isLeader, mouse);
            DrawMemberList(members, count, maxScroll, mouse, self);
            DrawActions(family, self, isLeader, mouse);

            Rect closeRect = L(361f, 9f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
            if (FamUI.Click(closeRect, mouse)) visible = false;

            WinDrag.Handle(this, L(0f, 0f, NW, 28f), ref winX, ref winY);

            if (e != null && e.type == EventType.MouseDown &&
                L(0f, 0f, NW, NH).Contains(mouse) && !SysMsgBox.Visible &&
                !FamilyRelationDlg.MouseOver() && !JobCustomDlg.MouseOver() &&
                !JobOfferDlg.MouseOver() && !JobRightDlg.MouseOver() &&
                !FamilyMailDlg.MouseOver())
            {
                e.Use();
            }
            return;
        }
        finally
        {
            if (covered && e != null) e.mousePosition = saved;
        }
    }

    /// <summary>Family name, level, member count/capacity and the unfilled header slots the official
    /// code always draws (the two em dashes and the three literal "0" placeholders).</summary>
    private void DrawHeader(WorldFamilyState family, int count)
    {
        string name = string.IsNullOrEmpty(family.Name) ? "（无家族）" : family.Name;
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + 84f * s, winY + 43f * s, name, FamUI.Gold, s);

        LblC(60f, 61f, 58f, 12f, "Lv" + family.Level.ToString(), FamUI.Light);
        LblC(168f, 61f, 58f, 12f, count.ToString() + "/" + family.MemberCapacity.ToString(), FamUI.Light);
        LblC(60f, 78f, 58f, 12f, "—", FamUI.Light);
        LblC(258f, 60f, 60f, 12f, "—", FamUI.Light);
        LblC(172f, 79f, 36f, 12f, "0", FamUI.Light);
        LblC(220f, 79f, 36f, 12f, "0", FamUI.Light);
        LblC(268f, 79f, 36f, 12f, "0", FamUI.Light);
    }

    /// <summary>Icon slot backdrop plus the proclamation panel; the panel shows wrapped read-only
    /// lines or the 100-character editor while editPro is set.</summary>
    private void DrawProclaim(WorldFamilyState family)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(L(328f, 39f, 36f, 36f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        ArtworkSkin.Input(L(24f, 99f, 328f, 84f), s,
            editPro && GUI.GetNameOfFocusedControl() == "family_proclaim");

        if (!editPro)
        {
            bool hasProclaim = !string.IsNullOrEmpty(family.Proclaim);
            string text = hasProclaim ? family.Proclaim : "（家族宣言）";
            Color color = hasProclaim ? FamUI.Light : FamUI.Gray;
            float bottom = winY + 183f * s;
            float y = winY + 103f * s;
            List<string> lines = BitmapFont.Wrap(text, 316f * s, s);
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (bottom - 2f * s < y + 12f * s) break;
                    if (BitmapFont.Ready)
                        BitmapFont.DrawOutline(winX + 30f * s, y, line, color, s);
                    y += 16f * s;
                }
            }
        }
        else
        {
            GUI.SetNextControlName("family_proclaim");
            proText = GUI.TextArea(L(28f, 102f, 320f, 78f), proText ?? "", 100, ProStyle()) ?? "";
        }
    }

    /// <summary>谢绝申请 checkbox row: the box, the tick, its label and the leader-only write
    /// (op 4 carries the inverted flag in the permission slot).</summary>
    private void DrawResistRow(WorldFamilyState family, bool isLeader, Vector2 mouse)
    {
        Rect box = L(25f, 186f, 16f, 16f);
        if (icoCheck != null)
        {
            GUI.DrawTexture(box, icoCheck);
        }
        else
        {
            // Official literal: the packed word 0x3ecccccd is 0.4, and the x86_64 constant pool
            // pair at 0x0119d20 reads (0.0, 0.4) for exactly this slot.
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        if (family.Resist)
            // Official literals 0x3ecccccd, 0x3f800000, 0x3ecccccd, 0x3f800000 = (0.4, 1, 0.4, 1);
            // the x86_64 pool pair at 0x01197b0 reads (0.4, 1.0).
            Lbl(28f, 187f, "√", new Color(0.4f, 1f, 0.4f, 1f));
        Lbl(45f, 188f, "谢绝申请", FamUI.Gray);

        if (FamUI.Click(L(25f, 186f, 118f, 20f), mouse) && isLeader && NetClient.I != null)
        {
            NetClient.I.FamilyManageForIndependentRuntime(4, "", family.Resist ? 0 : 1, "");
        }
    }

    /// <summary>13-row member list, its scroll clamping and the right-click player menu.</summary>
    private void DrawMemberList(IReadOnlyList<(string, int, string, string, bool)> members,
        int count, int maxScroll, Vector2 mouse, string self)
    {
        ArtworkSkin.Input(L(19f, 235f, 336f, 222f), s);

        Rect panel = L(19f, 235f, 336f, 222f);
        Event e = Event.current;
        if (e != null && e.type == EventType.ScrollWheel && panel.Contains(mouse))
        {
            memberScroll += e.delta.y < 0f ? -1 : 1;
            memberScroll = Mathf.Clamp(memberScroll, 0, maxScroll);
            e.Use();
        }
        memberScroll = Mathf.Clamp(memberScroll, 0, maxScroll);

        if (count < 1)
        {
            Lbl(150f, 340f, "暂无成员", FamUI.Gray);
        }
        else
        {
            for (int i = 0; i < VisibleRows; i++)
            {
                int row = i + memberScroll;
                if (row >= count) break;
                (string memberName, int level, string field3, string field4, bool online) = members[row];
                Rect rowRect = L(22f, 239f + 16f * i, 316f, 15f);
                if (row == selMember) ArtworkSkin.Input(rowRect, s, true);
                else ArtworkSkin.Row(rowRect, s, rowRect.Contains(mouse));

                float textY = 240f + 16f * i;
                // Official literals 0x3ee66666, 0x3f733333, 0x3ecccccd, 0x3f800000 = (0.45, 0.95, 0.4, 1);
                // the x86_64 pool pairs are (0.45, 0.95) at 0x011abc0 and (0.4, 1.0) at 0x01197b0.
                Color nameColor = online ? new Color(0.45f, 0.95f, 0.4f, 1f) : FamUI.Gray;
                Lbl(28f, textY, BitmapFont.Elide(memberName, 72f * s, s), nameColor);
                if (!online) Lbl(104f, textY, "离", FamUI.Gray);
                if (!string.IsNullOrEmpty(field3))
                    Lbl(120f, textY, BitmapFont.Elide(field3, 72f * s, s), new Color(0.85f, 0.78f, 0.55f, 1f));
                if (!string.IsNullOrEmpty(field4))
                    Lbl(196f, textY, BitmapFont.Elide(field4, 96f * s, s), new Color(0.62f, 0.82f, 0.95f, 1f));
                Lbl(296f, textY, "Lv" + level.ToString(), FamUI.Gray);

                if (FamUI.Click(rowRect, mouse))
                {
                    selMember = row;
                }
                else if (memberName != self && FamUI.RClick(rowRect, mouse))
                {
                    selMember = row;
                    PlayerActionMenu.Open(CombatWorld.FindPlayerIdByName(memberName), memberName);
                }
            }
        }

        NativeScroll.BarRows(this, L(357.5f, 236f, 15f, 220f), VisibleRows, count, ref memberScroll);
    }

    /// <summary>The ten action buttons; the leader gate, the 选中成员 gate and the confirm boxes
    /// follow the official branches exactly.</summary>
    private void DrawActions(WorldFamilyState family, string self, bool isLeader, Vector2 mouse)
    {
        IReadOnlyList<(string, int, string, string, bool)> members = family.Members;
        int count = members.Count;
        string selName = selMember >= 0 && selMember < count ? members[selMember].Item1 : null;
        bool hasOtherSelection = selName != null && selName != self;

        if (FamUI.Btn(L(228f, 186f, 70f, 22f), icoRelation, "关系", mouse, s))
        {
            FamilyRelationDlg.Ensure();
            FamilyRelationDlg.Toggle();
        }

        if (FamUI.Btn(L(300f, 186f, 70f, 22f), icoProclaim, editPro ? "保存" : "宣言", mouse, s))
        {
            if (!isLeader)
            {
                NeedLeader();
            }
            else if (!editPro)
            {
                editPro = true;
                proText = family.Proclaim ?? "";
            }
            else
            {
                if (NetClient.I != null) NetClient.I.FamilyManageForIndependentRuntime(3, "", 0, proText ?? "");
                editPro = false;
            }
        }

        if (FamUI.Btn(L(12f, 460f, 90f, 22f), icoJobOffer, "授予职位", mouse, s))
        {
            if (!isLeader) NeedLeader();
            else if (hasOtherSelection) JobRightDlg.Open(selName);
            else NeedSel();
        }

        if (FamUI.Btn(L(102f, 460f, 90f, 22f), icoJobFire, "罢免职位", mouse, s))
        {
            if (!isLeader) NeedLeader();
            else if (hasOtherSelection)
            {
                if (NetClient.I != null) NetClient.I.FamilyManageForIndependentRuntime(1, selName, 10, "");
            }
            else NeedSel();
        }

        if (FamUI.Btn(L(192f, 460f, 90f, 22f), icoJobSell, "转让族长", mouse, s))
        {
            if (!isLeader) NeedLeader();
            else if (hasOtherSelection)
                // Official literal (metadata usage 16937) is "】？你将变为普通族人。", so the tail
                // carries a full-width question mark after the bracket.
                SysMsgBox.Show("确定将族长转让给【" + selName + "】？你将变为普通族人。",
                    () => ConfirmTransferLeader(selName));
            else NeedSel();
        }

        if (FamUI.Btn(L(282f, 460f, 90f, 22f), icoJobCustom, "自定职位", mouse, s))
        {
            JobCustomDlg.Toggle();
        }

        if (FamUI.Btn(L(12f, 484f, 90f, 22f), icoKick, "踢出", mouse, s))
        {
            if (!isLeader) NeedLeader();
            else if (hasOtherSelection)
                SysMsgBox.Show("确定将【" + selName + "】踢出家族？",
                    () => ConfirmKick(selName));
            else NeedSel();
        }

        if (FamUI.Btn(L(116f, 484f, 44f, 22f), icoList, "列表", mouse, s))
        {
            FamilyDlg.Toggle();
        }

        if (family.CanMail && FamUI.Btn(L(170f, 484f, 88f, 22f), null, "家族邮件", mouse, s))
        {
            FamilyMailDlg.Open();
        }

        if (FamUI.Btn(L(282f, 484f, 90f, 22f), icoQuit, "退出家族", mouse, s))
        {
            int others = 0;
            for (int i = 0; i < count; i++)
                if (members[i].Item1 != self) others++;
            string message = isLeader && others > 0
                ? string.Format(
                    "你是族长，退出将【解散整个家族】（还有 {0} 名成员）！\n如果只是想让出位置，请改用【转让族长】。\n确定要解散吗？",
                    others)
                : "确定退出家族【" + family.Name + "】？";
            SysMsgBox.Show(message, LeaveFamily);
        }
    }
}
}
