using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class AccountSecDlg
{
    private int Rows => mode == 1 || !AccountSec.HasSecCode ? 3 : 4;
    private float ExtraStatusH => statusLines == null ? 0 : Mathf.Max(0, statusLines.Count - 1) * 13f;
    private float NH => 44f + Rows * 26f + ExtraStatusH + 26f;
    private static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("AccountSecDlg");
        I = go.AddComponent<AccountSecDlg>();
        DontDestroyOnLoad(go);
    }
    public static void Open(int mode)
    {
        Ensure();
        I.mode = mode;
        I.f4 = I.f3 = I.f2 = I.f1 = "";
        I.SetStatus("", false);
        I.busy = false;
        I.visible = true;
    }
    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - I.NH * scale) * 0.5f : I.winY;
        Vector3 mp = Input.mousePosition;
        return new Rect(x, y, NW * scale, I.NH * scale).Contains(new Vector2(mp.x, Screen.height - mp.y));
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(13f * s);
        return _e;
    }
    private void Fill(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
    private string Row(string ctrl, float y, string label, string value)
    {
        if (BitmapFont.Ready)
            BitmapFont.DrawOutline(winX + 14f * s, winY + (y + 4f) * s, label, new Color(1f, 0.9f, 0.6f, 1f), s);
        Rect rect = L(96, y, 158, 20);
        Fill(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), new Color(0.62f, 0.5f, 0.26f, 1f));
        Fill(rect, new Color(0.24f, 0.22f, 0.18f, 1f));
        GUI.SetNextControlName(ctrl);
        return GUI.PasswordField(new Rect(rect.x + 5f, rect.y + 2f, rect.width - 10f, rect.height - 4f), value ?? "", '*', 32, E());
    }
    private void Confirm()
    {
        if (busy) return;
        if (NetClient.I == null) { SetStatus("未连接服务器", true); return; }
        if (mode == 1)
        {
            if (f1.Length == 0 || f2.Length == 0) { SetStatus("请填写旧密码和新密码", true); return; }
            if (f2 != f3) { SetStatus("两次输入的新密码不一致", true); return; }
            SetStatus("正在提交…", false);
            busy = true;
            busySince = Time.realtimeSinceStartup;
            NetClient.I.ChangePwdForIndependentRuntime(f1, f2);
        }
        else
        {
            if (f1.Length == 0 || f3.Length == 0) { SetStatus("请填写账号密码和安全码", true); return; }
            bool hasCode = AccountSec.HasSecCode;
            if (hasCode && f2.Length == 0) { SetStatus("请填写原安全码", true); return; }
            if (f3 != f4) { SetStatus("两次输入的安全码不一致", true); return; }
            SetStatus("正在提交…", false);
            busy = true;
            busySince = Time.realtimeSinceStartup;
            NetClient.I.ChangeSecCodeForIndependentRuntime(f1, hasCode ? f2 : "", f3);
        }
    }
    private void OnGUI()
    {
        if (!visible) return;
        s = UIScale.Factor;
        if (busy && Time.realtimeSinceStartup - busySince > BusyTimeout)
        {
            busy = false;
            SetStatus("服务端没有响应, 这条功能可能还没部署", true);
        }
        if (!Mathf.Approximately(statusWrapScale, s)) RewrapStatus();
        float height = NH;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - height * s) * 0.5f;
        }
        Vector2 mp = Event.current.mousePosition;
        GUI.depth = 2;
        PopUI.Panel(L(0, 0, NW, height));
        Fill(L(2, 2, 264, 18), new Color(0.18f, 0.15f, 0.1f, 1f));
        string title = mode == 1 ? "修改密码" : AccountSec.HasSecCode ? "修改安全码" : "设置安全码";
        if (BitmapFont.Ready) BitmapFont.CenterOutline(L(0, 5, NW, 14), title, new Color(1f, 0.9f, 0.55f, 1f), s);
        float statusY;
        if (mode == 1)
        {
            f1 = Row("as_p1", 28, "旧密码", f1);
            f2 = Row("as_p2", 54, "新密码", f2);
            f3 = Row("as_p3", 80, "确认新密码", f3);
            statusY = 102;
        }
        else
        {
            f1 = Row("as_s1", 28, "账号密码", f1);
            float y = 54;
            if (AccountSec.HasSecCode) { f2 = Row("as_s2", y, "原安全码", f2); y += 26; }
            f3 = Row("as_s3", y, AccountSec.HasSecCode ? "新安全码" : "安全码", f3);
            f4 = Row("as_s4", y + 26, "确认安全码", f4);
            statusY = y + 48;
        }
        if (statusLines != null && BitmapFont.Ready)
        {
            Color color = statusBad ? new Color(1f, 0.55f, 0.5f, 1f) : new Color(0.7f, 0.9f, 1f, 1f);
            for (int i = 0; i < statusLines.Count; i++)
                BitmapFont.CenterOutline(L(0, statusY + i * 13f, NW, 12), statusLines[i], color, s);
        }
        Rect ok = L(66, height - 26, 60, 18), cancel = L(142, height - 26, 60, 18);
        bool overOk = ok.Contains(mp), overCancel = cancel.Contains(mp);
        Fill(ok, overOk ? new Color(0.62f, 0.5f, 0.2f, 1f) : new Color(0.42f, 0.34f, 0.18f, 1f));
        Fill(cancel, overCancel ? new Color(0.55f, 0.3f, 0.25f, 1f) : new Color(0.38f, 0.28f, 0.24f, 1f));
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(ok, "确定", new Color(1f, 0.95f, 0.7f, 1f), s);
            BitmapFont.CenterOutline(cancel, "取消", new Color(0.92f, 0.86f, 0.8f, 1f), s);
        }
        Event e = Event.current;
        if ((e.type == EventType.MouseDown && e.button == 0 && overOk) ||
            (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)))
        { Confirm(); e.Use(); }
        else if ((e.type == EventType.MouseDown && e.button == 0 && overCancel) ||
            (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))
        { CloseNow(); e.Use(); }
        WinDrag.Handle(this, L(0, 0, NW, 20), ref winX, ref winY);
        if (e.type == EventType.MouseDown && L(0, 0, NW, height).Contains(mp) && !SysMsgBox.Visible) e.Use();
    }
}
}
