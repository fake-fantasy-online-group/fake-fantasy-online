using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private ClientRootView guiHost;
    private RecoveredTitleCharacterCreationSurface creationGui;
    private const string RememberAccountKey = "login_remember_acc";
    private const string RememberPasswordKey = "login_remember_pwd";
    private const string SavedAccountKey = "login_saved_acc";
    private const string SavedPasswordKey = "login_saved_pwd";

    // Official 1.5.8 TitleFlow.btnComplete (+0x58) and TitleFlow.btnReturn (+0x60): the two
    // image buttons every official title form draws through FormButtons (0x06000edb), which
    // reads them off the instance every frame and sizes them with GsaDecoder.W/H
    // (0x06000bc4/0x06000bc5). They are the same two assets the recovered character-creation
    // title screen loads from res/uires/titleui/.
    private Texture2D _loginPanel => LoginPanelForIndependentRuntime;
    private Texture2D _completeButton => CompleteButtonForIndependentRuntime;
    private Texture2D _returnButton => ReturnButtonForIndependentRuntime;

    private GUIStyle _center;
    private GUIStyle _placeholderStyle;
    private GUIStyle _disclaimerStyle;
    private Texture2D _buttonBackground;
    private Texture2D _panelBackground;
    private Texture2D _inputBackground;
    // Official 1.5.8 TitleFlow declares these three as process-level statics: the frozen atlas
    // (ReferenceReverseIndex-1.5.8/index.sqlite3 -> fields) lists rememberAcc ordinal 24 at
    // offset 0x10, rememberPwd ordinal 25 at 0x11 and loginPrefLoaded ordinal 26 at 0x12, all
    // with is_static = 1 under owner TitleFlow. DrawLogin (06000ee6, arm64 :181-228) reaches them
    // through the static-field slot at metadata_usage_02576_kind1 + 0xb8, not through the
    // instance pointer that carries account (+0x1d8) / password (+0x1e0), so the flags and the
    // "already loaded" latch survive every teardown of the title surface while the typed text
    // does not. The official default is false (zero-initialized), and the write order is
    // unchanged here: LoadLoginPrefs writes all three on the first frame, the DrawLogin
    // change arm writes the two flags, and title destruction never clears them.
    private static bool _rememberAccount;
    private static bool _rememberPassword;
    private static bool _loginPreferencesLoaded;
    private bool _resolutionListOpen;
    // Official 1.5.8 TitleFlow keeps the two selector flags as instance fields:
    // resListOpen at +0x300 and fpsListOpen at +0x308 (atlas fields, is_static = 0).
    // The frame-rate selector owns no mirror of GameSettings.FrameRateMode; the open flag
    // is the only instance state it adds.
    private bool _frameRateListOpen;
    // Official 1.5.8 TitleFlow.curRes (+0x304) is seeded with -1 by the instance constructor
    // (06000efe, arm64 06000efe.c:129 `*(int *)(param_1 + 0x304) = 0xffffffff`), and
    // DrawResSelector (06000ecd) is what resolves the sentinel into GameSettings.ResIndex()
    // (06000e10) on the first frame. The selector keeps the resolved value in the instance
    // field afterwards, so a click is what rewrites it - there is no per-frame re-read.
    // Official 1.5.8 TitleFlow.state (+0x28) == 1 is the registration form: DrawLogin
    // (0x06000ee6) writes 1 on the 申请账号 click and OnGUI (0x06000ec7) then dispatches
    // DrawRegister (0x06000eef) instead of DrawLogin. The five form fields are the instance
    // strings regAcc (+0x1f0), regPw (+0x1f8), regPw2 (+0x200), regSec (+0x208) and regInvite
    // (+0x210); they are cleared when the form opens (0x06000ee6 arm64 :413-422) and by the
    // zero-code arm of the reply handler (0x06000f06 arm64 :37-46) - the return arm of
    // FormButtons (0x06000edb) leaves them filled.
    // Official 1.5.8 TitleFlow.state (+0x28) is ONE int that selects the page, and every writer
    // overwrites it: DrawLogin (0x06000ee6) writes 1 on 申请账号 (arm64 :409) and 2 on 找回密码
    // (arm64 :454), FormButtons (0x06000edb) writes 0 on 返回 (arm64 :110-112) and both reply
    // handlers write 0 on a zero code (0x06000f06 arm64 :35, 0x06000f07 arm64 :35). A late reply
    // therefore leaves whatever form is open at that moment: opening the other form after the
    // request does not protect it, and no request is cancelled or fenced by a generation - the
    // reply simply sets the page back to the login form. Two independent bools cannot express
    // that, so this component carries the single title page, including states 4 through 6.
    private TitlePage _page;
    // Official 1.5.8 TitleFlow.state (+0x28) == 2 is the password-recovery form: DrawLogin
    // (0x06000ee6, arm64 :454-465 / x86_64 the same arm) writes 2, then netMsg (+0x250) = ""
    // (usage 10335) and then clears the four recover fields in the order recPw2 (+0x230) /
    // recPw (+0x228) / recSec (+0x220) / recAcc (+0x218); OnGUI (0x06000ec7) then dispatches
    // DrawRecover (0x06000eeb) instead of DrawLogin. The four fields are the instance strings
    // recAcc (+0x218), recSec (+0x220), recPw (+0x228) and recPw2 (+0x230) named by the frozen
    // atlas. Two arms clear them, both in that same order: the 找回密码 arm above and the
    // zero-code arm of the reply handler (0x06000f07, arm64 :37-44 / x86_64 :31-38). The return
    // arm of FormButtons (0x06000edb) clears netMsg only and leaves the four fields filled,
    // and neither the register form, the register reply nor the page change touches them.
    // Official 1.5.8 TitleFlow.state (+0x28) == 3 is the theft-appeal form: DrawLogin
    // (0x06000ee6, arm64 :485-496 / x86_64 :344-355) writes 3, then netMsg (+0x250) = ""
    // (usage 10335) and then assigns apAcc (+0x268) = account.Trim(), apWhen (+0x270) = "" and
    // apReason (+0x278) = "" — the entry arm pre-fills the account from the login field instead
    // of leaving the form blank, and that assignment IS the only clear the three fields ever get.
    // No dedicated reply handler touches them: the atlas NetClient field set has no appeal-result
    // slot and SetupNet (0x06000ebe) binds only the eight handlers b__96_0..7, so after a
    // successful send the form stays open with 提交申诉... and nothing is cleared. (Unproven, not
    // excluded: a generic server notice or error reaching another handler is not ruled out by those
    // two facts, and the private server does not implement 0x1090.)
    internal void BindLoginInputForIndependentRuntime(ClientRootView host, Uri noticeUri,
        RecoveredTitleCharacterCreationSurface creation)
    {
        guiHost = host ?? throw new ArgumentNullException(nameof(host));
        creationGui = creation;
        pluginTitleOwner = this;
        _changelogEndpoint = (noticeUri ?? throw new ArgumentNullException(nameof(noticeUri))).AbsoluteUri;
    }

    internal bool RegisterOpen
    {
        get { return _page == TitlePage.Register; }
    }

    internal string RegisterAccount
    {
        get { return _registerAccount; }
    }

    internal string RegisterPassword
    {
        get { return _registerPassword; }
    }

    internal string RegisterSecurityCode
    {
        get { return _registerSecurityCode; }
    }

    internal string RegisterInviteCode
    {
        get { return _registerInviteCode; }
    }

    internal bool RecoverOpen
    {
        get { return _page == TitlePage.Recover; }
    }

    internal string RecoverAccount
    {
        get { return _recoverAccount; }
    }

    internal string RecoverSecurityCode
    {
        get { return _recoverSecurityCode; }
    }

    internal string RecoverPassword
    {
        get { return _recoverPassword; }
    }

    /// <summary>
    /// Official DrawLogin (0x06000ee6) 找回密码 arm: <c>state = 2</c> and then the four recover
    /// fields are cleared, in the field order 确认新密码 (+0x230), 新密码 (+0x228), 安全码
    /// (+0x220), 账号 (+0x218). The same click also writes netMsg (+0x250) = "" with usage
    /// 10335; the message and form fields belong to this component.
    /// </summary>
    internal void OpenRecoverForm()
    {
        _page = TitlePage.Recover;
        netMsg = string.Empty;
        ClearRecoverFields();
    }

    /// <summary>
    /// The four-field clear, in the official order 确认新密码 (+0x230), 新密码 (+0x228), 安全码
    /// (+0x220), 账号 (+0x218). Two frozen arms perform it: the 找回密码 arm of DrawLogin
    /// (0x06000ee6, arm64 :458-465) and the zero-code arm of the reply handler (0x06000f07,
    /// arm64 :37-44 / x86_64 :31-38), both after the state/message write they belong to.
    /// </summary>
    internal void ClearRecoverFields()
    {
        _recoverPasswordConfirm = string.Empty;
        _recoverPassword = string.Empty;
        _recoverSecurityCode = string.Empty;
        _recoverAccount = string.Empty;
    }

    internal bool AppealOpen
    {
        get { return _page == TitlePage.Appeal; }
    }

    /// <summary>
    /// Official DrawLogin (0x06000ee6) 被盗申诉 arm: <c>state = 3</c>, then netMsg (+0x250) = ""
    /// (usage 10335), then <c>apAcc (+0x268) = account.Trim()</c>, <c>apWhen (+0x270) = ""</c>
    /// and <c>apReason (+0x278) = ""</c> in that order. Unlike the 申请账号 and 找回密码 arms,
    /// this one does not clear a form — it <em>overwrites</em> all three appeal fields with the
    /// login account plus two empty strings, which is why reopening the form always shows the
    /// login account and never the text typed on the previous visit.
    /// A null login account dereferences in the native Trim and raises (arm64 FUN_00177a74),
    /// which is what the bare <c>Trim()</c> below does too.
    /// </summary>
    internal void OpenAppealForm(string account)
    {
        _page = TitlePage.Appeal;
        netMsg = string.Empty;
        _appealAccount = account.Trim();
        _appealWhen = string.Empty;
        _appealReason = string.Empty;
    }

    /// <summary>
    /// Official FormButtons (0x06000edb) return arm, reached from DrawAppeal: <c>state = 0</c>
    /// then <c>netMsg = ""</c>. The three appeal fields are <em>not</em> cleared here, exactly as
    /// the recover form's four fields are not — but the entry arm above rewrites all three on the
    /// next visit, so nothing survives to be shown again.
    /// </summary>
    internal void CloseAppealForm()
    {
        _page = TitlePage.Login;
    }

    /// <summary>
    /// Official 0x06000f07 (the OnResetResult handler) zero-code arm ends with
    /// <c>state = 0</c>, which is the login page - whichever form happens to be open at that
    /// moment, including one opened after the request was sent. The caller writes the login
    /// account and the message it owns itself; this only leaves the form, and it never touches
    /// the register fields (state alone changes pages in the original).
    /// </summary>
    internal void CloseRecoverForm()
    {
        _page = TitlePage.Login;
    }

    // 06000ec0: retain the selected component receiver and original store/read order.
    // Session stage routing stays at the existing host call site; no receipt is synthesized.
    internal void NoteDisconnected()
    {
        _page = TitlePage.Login;
        guiHost._creatingCharacter = false;
        loggingIn = false;
        entering = false;
        string reason = string.Empty;
        if (NetClient.I != null)
        {
            reason = NetClient.I.LastDropReasonForIndependentRuntime;
        }
        netMsg = string.Concat(
            "与服务器的连接已断开，请重新登录",
            string.IsNullOrEmpty(reason) ? string.Empty : string.Concat(" (", reason, ")"));
    }

    /// <summary>
    /// Official DrawLogin (0x06000ee6) 申请账号 arm: <c>state = 1</c> and then the five register
    /// fields are cleared, in the field order 邀请码 (+0x210), 安全码 (+0x208), 确认密码
    /// (+0x200), 密码 (+0x1f8), 账号 (+0x1f0). The same click also writes netMsg (+0x250) = ""
    /// with usage 10335; the message and form fields belong to this component.
    /// </summary>
    internal void OpenRegisterForm()
    {
        _page = TitlePage.Register;
        netMsg = string.Empty;
        ClearRegisterFields();
    }

    /// <summary>
    /// The five-field clear, in the official order 邀请码 (+0x210), 安全码 (+0x208), 确认密码
    /// (+0x200), 密码 (+0x1f8), 账号 (+0x1f0). Two frozen arms perform it: the 申请账号 arm of
    /// DrawLogin (0x06000ee6, arm64 :413-422) and the zero-code arm of the reply handler
    /// (0x06000f06, arm64 :37-46), both after the state/message write they belong to.
    /// </summary>
    internal void ClearRegisterFields()
    {
        _registerInviteCode = string.Empty;
        _registerSecurityCode = string.Empty;
        _registerPasswordConfirm = string.Empty;
        _registerPassword = string.Empty;
        _registerAccount = string.Empty;
    }

    /// <summary>
    /// Official 0x06000f06 (OnRegisterResult) zero-code arm and FormButtons' return arm both end
    /// with <c>state = 0</c>, which is the login page - whichever form happens to be open at that
    /// moment, including one opened after the request was sent. The caller writes the login
    /// account and the message it owns itself; this only leaves the form, and it never touches
    /// the recover fields (state alone changes pages in the original).
    /// </summary>
    internal void CloseRegisterForm()
    {
        _page = TitlePage.Login;
    }

    // Official 1.5.8 06000ebf, public instance: state = 0, with no other side effect.
    public void GotoLogin()
    {
        _page = TitlePage.Login;
    }

    // Existing host lifecycle calls project only the original title page store. They never
    // change application session state, publish events or overwrite this field every frame.
    internal void SelectTitlePageForIndependentRuntime(int state)
    {
        _page = (TitlePage)state;
    }

    internal void ReleaseGeneratedLoginTexturesForIndependentRuntime()
    {
        RecoveredTitleLoginSurface.DestroyTexture(_buttonBackground);
        RecoveredTitleLoginSurface.DestroyTexture(_panelBackground);
        RecoveredTitleLoginSurface.DestroyTexture(_inputBackground);
    }

    private void DrawLogin()
    {
        if (_loginPanel)
        {
            GUI.DrawTexture(
                new Rect(
                    237f,
                    348f,
                    GsaTextureScale.Width(_loginPanel),
                    GsaTextureScale.Height(_loginPanel)),
                _loginPanel);
        }

        Rect accountBox = new Rect(319f, 392f, 150f, 22f);
        Rect passwordBox = new Rect(319f, 429f, 150f, 22f);
        GUI.Box(accountBox, GUIContent.none, _inputStyle);
        GUI.Box(passwordBox, GUIContent.none, _inputStyle);

        Rect accountField = new Rect(327f, 392f, 134f, 22f);
        Rect passwordField = new Rect(327f, 429f, 134f, 22f);
        string editedAccount = GUI.TextField(accountField, account ?? string.Empty, 20, _fieldStyle);
        if (!string.Equals(account, editedAccount, StringComparison.Ordinal)) pluginTitleRevision++;
        account = editedAccount;
        string editedPassword = DrawMaskedField(passwordField, password ?? string.Empty);
        // Only an opaque counter leaves this component. Never retain credential values or
        // their hashes in plugin fingerprints, observations, receipts, or action journals.
        if (!string.Equals(password, editedPassword, StringComparison.Ordinal)) pluginTitleRevision++;
        password = editedPassword;

        if (string.IsNullOrEmpty(account))
        {
            GUI.Label(accountField, " 账号", _placeholderStyle);
        }

        if (string.IsNullOrEmpty(password))
        {
            GUI.Label(passwordField, " 密码", _placeholderStyle);
        }

        // Official DrawLogin (06000ee6, arm64 :181-228): the account box is offered first and the
        // password box second, both results are captured before anything is written, and the
        // static flags are only touched when the pair actually changed -
        // `if (rememberAcc == newAcc && rememberPwd == newPwd)` falls through without a write.
        // The change arm writes both new values, then forces rememberAcc (0x10) to 1 when the new
        // password flag is set (:219-227), and calls SaveLoginPrefs (06000ed3) exactly once on
        // either arm (:216 and :225) - never twice, and never outside the gate.
        bool previousRememberAccount = _rememberAccount;
        bool previousRememberPassword = _rememberPassword;
        bool nextRememberAccount = LoginCheck(accountBox, _rememberAccount, "记住账号");
        bool nextRememberPassword = LoginCheck(passwordBox, _rememberPassword, "记住密码");
        if (previousRememberAccount != nextRememberAccount ||
            previousRememberPassword != nextRememberPassword)
        {
            _rememberAccount = nextRememberAccount;
            _rememberPassword = nextRememberPassword;
            if (_rememberPassword)
            {
                _rememberAccount = true;
            }

            SaveLoginPrefs();
        }

        GUI.enabled = !loggingIn && !RecoveredTitleLoginSurface.NeedUpdate;
        string loginLabel = RecoveredTitleLoginSurface.NeedUpdate ? "请先更新" : loggingIn ? "登录中…" : "登录游戏";
        if (GUI.Button(new Rect(316f, 482f, ButtonWidth, ButtonHeight), loginLabel, _regButtonStyle))
        {
            // Official DrawLogin (06000ee6) evaluates account.Trim() exactly once per click and
            // keeps the result in the closure only — TitleFlow.account (+0x1d8) never receives
            // it, so the text field keeps its spaces. A null account or password raises
            // NullReferenceException in the native body (both jump to the FUN_00177a74 stub),
            // which is what a bare Trim/Length dereference does here too.
            string submittedAccount = account.Trim();

            // The empty gate tested account.Trim().Length and password.Length in that order and
            // wrote usage 18068 = "请输入账号和密码!" into TitleFlow.netMsg (+0x250) for either
            // miss. No intent leaves the surface, so no connect starts and nothing is saved.
            if (submittedAccount.Length == 0 || password.Length == 0)
            {
                netMsg = "请输入账号和密码!";
            }
            else
            {
                // The original call returns here, before GUI.enabled and the later controls.
                // A synchronous connect callback/send exception stops this reached prefix.
                BeginLogin(submittedAccount);
            }
        }

        GUI.enabled = true;
        if (GUI.Button(new Rect(644f, 498f, ButtonWidth, ButtonHeight), "退出游戏", _regButtonStyle))
        {
            GameBootstrap.QuitGame();
        }

        if (GUI.Button(new Rect(644f, 459f, ButtonWidth, ButtonHeight), "申请账号", _regButtonStyle))
        {
            OpenRegisterForm();
        }

        if (GUI.Button(new Rect(644f, 420f, ButtonWidth, ButtonHeight), "找回密码", _regButtonStyle))
        {
            OpenRecoverForm();
        }

        // Official DrawLogin (0x06000ee6): the fifth and last title button, RegBtn(644, 381, 152,
        // usage 17985 "被盗申诉") on x86_64 and the inlined GUI.Button(0x44210000 = 644,
        // 0x43be8000 = 381, 0x43180000 = 152, 0x42040000 = 33) on arm64. Drawn last in the
        // native body, after 退出游戏 (498), 申请账号 (459) and 找回密码 (420), and the click arm
        // writes state (+0x28) = 3, netMsg (+0x250) = "" and then apAcc = account.Trim(),
        // apWhen = "", apReason = "" at this exact button site.
        if (GUI.Button(new Rect(644f, 381f, ButtonWidth, ButtonHeight), "被盗申诉", _regButtonStyle))
        {
            OpenAppealForm(account);
        }

        string hint = string.IsNullOrEmpty(netMsg) ? "请输入账号和密码!" : netMsg;
        DrawHintBox(400f, 520f, hint, 560f);
        DrawResSelector();
        DrawFpsSelector();
        DrawVersionBadge();

        GUI.Label(
            new Rect(150f, 580f, 500f, 16f),
            "本软件仅用于个人学习使用，请下载后24H内删除",
            _disclaimerStyle);

        // Original DrawLogin ends with this gate; OnGUI then draws the notice button.
        if (RecoveredTitleLoginSurface.NeedUpdate)
            DrawUpdateGate();

    }

    private void DrawNoticeButton()
    {
        if (GUI.Button(new Rect(640f, 8f, ButtonWidth, ButtonHeight), "更新公告", _regButtonStyle))
        {
            _noticeOpen = true;
            _noticeScroll = 0f;

            // Official DrawNoticeButton (06000ec9): first click marks tried before baked
            // loading, then creates/starts FetchChangelog on this same title instance.
            if (!_changelogTried)
            {
                _changelogTried = true;
                LoadBakedChangelog();
                StartCoroutine(FetchChangelog());
            }
        }

    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DrawRegister (0x06000eef, arm64 0x3867b0 / x86_64 0x3996e0).
    /// </summary>
    /// <remarks>
    /// The body is, in order: one panel Box, the title Label, five FormRow (0x06000eda) calls,
    /// FormButtons (0x06000edb) with an <c>Action</c> bound to DoRegister (0x06000ef0), and
    /// HintBox (0x06000ed7). Every constant below was read on both frozen architectures: the
    /// arm64 body carries the immediates (0x433e0000 = 190, 0x43d20000 = 420, 0x43830000 = 262,
    /// 0x43760000 = 246, 0x43880000 = 272, rows 0x439a0000 = 308 / 0x43aa0000 = 340 /
    /// 0x43ba0000 = 372 / 0x43ca0000 = 404 / 0x43da0000 = 436, buttons 0x43ec0000 = 472, hint
    /// 0x43c80000 = 400 / 0x44020000 = 520 / 0x440c0000 = 560) and the x86_64 constant pool
    /// (__TEXT,__const file offset 17923072, va 0x1117c00) holds the same values.
    /// The component field netMsg (+0x250): DrawRegister shows it, falling back to
    /// usage 18081 when it is null or empty, and DoRegister writes into it.
    /// </remarks>
    private void DrawRegister()
    {
        EnsureStyles();
        Matrix4x4 previousMatrix = BeginGuiFrameForIndependentRuntime(false);
        // GUI.Box(190, 262, 420, 246, GUIContent.none, style +0x2b0): the panel style is the
        // 14px black 0.78 panel with the gold two-pixel border, i.e. this surface's
        // _panelStyle (OnGUI 0x06000ec7, MakeRoundTex(0,0,0,0.78 / 0.72,0.58,0.3,1, 14, 2)).
        GUI.Box(new Rect(190f, 262f, 420f, 246f), GUIContent.none, _panelStyle);

        // GUI.Label(190, 272, 420, 24, usage 17744 "注 册 新 账 号", style +0x288): skin
        // label, MiddleCenter, richText, clipping Clip - the same construction as _center.
        GUI.Label(new Rect(190f, 272f, 420f, 24f), "注 册 新 账 号", _center);

        // FormRow(190, 420, y, this, label, ref field, pwd) in this order.
        FormRow(190f, 420f, 308f, "账号", ref _registerAccount, false);
        FormRow(190f, 420f, 340f, "密码", ref _registerPassword, true);
        FormRow(190f, 420f, 372f, "确认密码", ref _registerPasswordConfirm, true);
        FormRow(190f, 420f, 404f, "安全码", ref _registerSecurityCode, false);
        FormRow(190f, 420f, 436f, "邀请码", ref _registerInviteCode, false);

        FormButtons(472f, DoRegister);
        string hint = string.IsNullOrEmpty(netMsg)
            ? "账号+密码+确认密码+安全码 必填; 没有邀请码? 关注官方频道 @qqdiaoxiang 每天抽取"
            : netMsg;
        DrawHintBox(400f, 520f, hint, 560f);
        GUI.matrix = previousMatrix;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DrawRecover (0x06000eeb, arm64 0x386a8c / x86_64 0x3999a0).
    /// </summary>
    /// <remarks>
    /// The body is, in order: one panel Box, the title Label, four FormRow (0x06000eda) calls,
    /// FormButtons (0x06000edb) with an <c>Action</c> bound to DoRecover (0x06000eec), and
    /// HintBox (0x06000ed7). Every constant below was read on both frozen architectures: the
    /// arm64 body carries the immediates (0x433e0000 = 190, 0x438b8000 = 279, 0x43d20000 = 420,
    /// 0x43560000 = 214, title 0x43908000 = 289 / 0x41c00000 = 24, rows 0x43a28000 = 325 /
    /// 0x43b28000 = 357 / 0x43c28000 = 389 / 0x43d28000 = 421, buttons 0x43e48000 = 457, hint
    /// 0x43c80000 = 400 / 0x43fb0000 = 502 / 0x440c0000 = 560) and the x86_64 constant pool
    /// holds the same values (DAT_0111bf70 = 190, DAT_0111bf74 = 279, DAT_0111bf80 = 420,
    /// DAT_0111bf84 = 214, DAT_0111bf94 = 289, DAT_0111b914 = 325, DAT_0111b918 = 357,
    /// DAT_0111b91c = 389, DAT_0111ab30 = 421, DAT_0111b920 = 457, DAT_0111a768 = 400,
    /// DAT_0111b924 = 502, DAT_0111b904 = 560). The panel style is +0x2b0 and the title style
    /// +0x288, the same two styles DrawRegister (0x06000eef) uses.
    /// The component field netMsg (+0x250): DrawRecover shows it, falling back to
    /// usage 18118 "输入账号+注册时的安全码, 即可重设新密码" when it is null or empty, and
    /// DoRecover writes into it.
    /// </remarks>
    private void DrawRecover()
    {
        EnsureStyles();
        Matrix4x4 previousMatrix = BeginGuiFrameForIndependentRuntime(false);
        // GUI.Box(190, 279, 420, 214, GUIContent.none, style +0x2b0): the 14px black 0.78
        // panel with the gold two-pixel border, i.e. this surface's _panelStyle.
        GUI.Box(new Rect(190f, 279f, 420f, 214f), GUIContent.none, _panelStyle);

        // GUI.Label(190, 289, 420, 24, usage 17499 "找 回 密 码", style +0x288): the same
        // skin-label MiddleCenter richText Clip construction the register title uses.
        GUI.Label(new Rect(190f, 289f, 420f, 24f), "找 回 密 码", _center);

        // FormRow(190, 420, y, this, label, ref field, pwd) in this order. The label strings
        // are usage 18080 "账号", usage 17301 "安全码", usage 17598 "新密码" and usage
        // 17862 "确认新密码"; the two password rows are the ones drawn through MaskField.
        FormRow(190f, 420f, 325f, "账号", ref _recoverAccount, false);
        FormRow(190f, 420f, 357f, "安全码", ref _recoverSecurityCode, false);
        FormRow(190f, 420f, 389f, "新密码", ref _recoverPassword, true);
        FormRow(190f, 420f, 421f, "确认新密码", ref _recoverPasswordConfirm, true);

        FormButtons(457f, DoRecover);
        string hint = string.IsNullOrEmpty(netMsg)
            ? "输入账号+注册时的安全码, 即可重设新密码"
            : netMsg;
        DrawHintBox(400f, 502f, hint, 560f);
        GUI.matrix = previousMatrix;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.FormButtons (0x06000edb, arm64 0x38e588 / x86_64 0x3a1bd0): the
    /// complete button is placed at <c>388 - GsaDecoder.W(btnComplete)</c> and the return button
    /// at the literal x 412, both at the caller's y, both drawn through ImgBtn (0x06000ec8).
    /// A missing texture falls back to the native default size 110x30 (arm64 0x42dc0000 /
    /// 0x41f00000; x86_64 DAT_01117d68 = 110.0 and DAT_01117c78 = 30.0 in the same constant
    /// pool). The complete x is 388 (x86_64 DAT_0111b98c) and the return x 412
    /// (x86_64 _DAT_0111c750).
    /// The complete button invokes its supplied Action; the return button writes <c>state = 0</c> and
    /// <c>netMsg = ""</c> (usage 10335) in that order.
    /// </summary>
    private void FormButtons(float y, Action onComplete)
    {
        float completeWidth = _completeButton ? GsaTextureScale.Width(_completeButton) : 110f;
        float completeHeight = _completeButton ? GsaTextureScale.Height(_completeButton) : 30f;
        float returnWidth = _returnButton ? GsaTextureScale.Width(_returnButton) : 110f;
        float returnHeight = _returnButton ? GsaTextureScale.Height(_returnButton) : 30f;

        if (RecoveredTitleLoginSurface.DrawImageButton(
                new Rect(388f - completeWidth, y, completeWidth, completeHeight),
                _completeButton))
        {
            onComplete();
        }

        if (RecoveredTitleLoginSurface.DrawImageButton(new Rect(412f, y, returnWidth, returnHeight), _returnButton))
        {
            _page = TitlePage.Login;
            netMsg = string.Empty;
        }

    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DoRegister (0x06000ef0, arm64 0x390704 / x86_64 0x3a3990).
    /// </summary>
    /// <remarks>
    /// The native order is: <c>regAcc.Trim()</c> must be non-empty, then <c>regPw</c> must be
    /// non-empty (a null account or password dereferences and raises, and both empty cases fall
    /// through to usage 18083 "账号和密码不能为空"), then <c>regPw != regPw2</c> writes usage
    /// 16980 "两次密码不一致", then <c>regSec.Trim()</c> must be non-empty or usage 18059
    /// "请填写找回密码安全码" is written. Only then does the body write usage 17557
    /// "提交注册..." and call NetClient.ConnectAsync. The host callback starts the existing
    /// connect at that point, before FormButtons evaluates 返回 and DrawRegister reads the hint.
    /// </remarks>
    private void DoRegister()
    {
        if (_registerAccount.Trim().Length != 0 && _registerPassword.Length != 0)
        {
            if (!string.Equals(_registerPassword, _registerPasswordConfirm, StringComparison.Ordinal))
            {
                netMsg = "两次密码不一致";
                return;
            }

            if (_registerSecurityCode.Trim().Length == 0)
            {
                netMsg = "请填写找回密码安全码";
                return;
            }

            netMsg = "提交注册...";
            BeginRegisterAccount();
            return;
        }

        netMsg = "账号和密码不能为空";
        return;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DoRecover (0x06000eec, arm64 0x390338 / x86_64 0x3a35a0).
    /// </summary>
    /// <remarks>
    /// The native order is: <c>recAcc.Trim()</c> must be non-empty, then <c>recSec.Trim()</c>
    /// must be non-empty — either miss falls through to usage 18082 "账号和安全码不能为空" —
    /// then <c>recPw</c> must be non-empty (no Trim, exactly like <c>regPw</c> in DoRegister) or
    /// usage 18061 "请填写新密码" is written, then <c>recPw != recPw2</c> writes usage 16981
    /// "两次新密码不一致". Only then does the body write usage 17556 "提交找回..." and call
    /// NetClient.ConnectAsync with <c>&lt;DoRecover&gt;b__172_0</c> (0x06000f08). A null
    /// recAcc, recSec or recPw dereferences and raises (arm64 FUN_00177a74, x86_64
    /// FUN_0016c1b0), which is what a bare Trim/Length dereference does here too, and a null
    /// NetClient.I does the same at the call. The component starts the existing connect
    /// here, before FormButtons evaluates 返回 and DrawRecover reads the hint.
    /// </remarks>
    private void DoRecover()
    {
        if (_recoverAccount.Trim().Length != 0 && _recoverSecurityCode.Trim().Length != 0)
        {
            if (_recoverPassword.Length == 0)
            {
                netMsg = "请填写新密码";
                return;
            }

            if (!string.Equals(
                    _recoverPassword,
                    _recoverPasswordConfirm,
                    StringComparison.Ordinal))
            {
                netMsg = "两次新密码不一致";
                return;
            }

            netMsg = "提交找回...";
            BeginResetPassword();
            return;
        }

        netMsg = "账号和安全码不能为空";
        return;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DrawAppeal (0x06000eed, arm64 0x386d4c / x86_64 0x399c20).
    /// </summary>
    /// <remarks>
    /// The body is, in order: one panel Box, the title Label, two FormRow (0x06000eda) calls, a
    /// plain Label for the reason caption, an input Box, a <c>GUI.TextArea</c> for the reason
    /// itself, FormButtons (0x06000edb) with an <c>Action</c> bound to DoAppeal (0x06000eee) and
    /// HintBox (0x06000ed7). Every constant below was read on both frozen architectures: the
    /// arm64 body carries the immediates (0x432a0000 = 170, 0x43830000 = 262, 0x43e60000 = 460,
    /// 0x437a0000 = 250, title 0x43880000 = 272 / 0x41c00000 = 24, rows 0x439a0000 = 308 /
    /// 0x43aa0000 = 340, caption 0x43520000 = 210 / 0x43ba0000 = 372 / 0x42c80000 = 100,
    /// reason frame 0x439e0000 = 316 / 0x43890000 = 274 / 0x42980000 = 76, reason field
    /// 0x43a10000 = 322 / 0x43bb0000 = 374 / 0x42900000 = 72, buttons 0x43e50000 = 458, hint
    /// 0x43c80000 = 400 / 0x44040000 = 528 / 0x440c0000 = 560) and the x86_64 constant pool
    /// holds the same values (DAT_0111bfa0 = 170,262 / DAT_0111bfb0 = 460,250,
    /// DAT_0111bfc0 = 170,272 / DAT_0111bfd0 = 460,24, DAT_0111b804 = 170, DAT_01117d10 = 460,
    /// DAT_01117c80 = 308, DAT_01117d28 = 340, DAT_0111bfe0 = 210,372 / DAT_0111bff0 = 100,24,
    /// DAT_0111c000 = 316,372 / DAT_0111c010 = 274,76, DAT_0111c020 = 322,374 /
    /// DAT_0111c030 = 262,72, DAT_0111b928 = 458, DAT_0111a768 = 400, DAT_0111b92c = 528,
    /// DAT_0111b904 = 560). The panel style is +0x2b0, the title style +0x288, the caption style
    /// +0x2a8, the input frame style +0x2b8 and the reason field style +0x290 — the same five
    /// skins the register and recover forms use.
    /// The two rows are the only <c>FormRow</c> calls and both pass <c>pwd = 0</c>, so neither is
    /// drawn through MaskField; the reason is the only multi-line control and its maxLength is
    /// the literal 200.
    /// The component field netMsg (+0x250): DrawAppeal shows it, falling back to
    /// usage 17254 "填写账号、大概什么时候被盗的、以及经过(越具体越好: 最后一次正常登录、有没有
    /// 在别处登录过、有没有交易过号)" when it is null or empty, and DoAppeal writes into it.
    /// There is no keyboard submit branch: like the recover form, this body reads no
    /// <c>Event.current</c> and no <c>KeyCode</c>, so 完成 is the only way to send.
    /// </remarks>
    private void DrawAppeal()
    {
        EnsureStyles();
        Matrix4x4 previousMatrix = BeginGuiFrameForIndependentRuntime(false);
        // GUI.Box(170, 262, 460, 250, GUIContent.none, style +0x2b0).
        GUI.Box(new Rect(170f, 262f, 460f, 250f), GUIContent.none, _panelStyle);

        // GUI.Label(170, 272, 460, 24, usage 17982 "被 盗 申 诉", style +0x288).
        GUI.Label(new Rect(170f, 272f, 460f, 24f), "被 盗 申 诉", _center);

        // FormRow(170, 460, y, this, label, ref field, 0): usage 18080 "账号" and usage
        // 17984 "被盗时间". The panel is 40 wider than the recover one, so this is its own
        // row geometry rather than the shared 190/420 pair.
        FormRow(170f, 460f, 308f, "账号", ref _appealAccount, false);
        FormRow(170f, 460f, 340f, "被盗时间", ref _appealWhen, false);

        // GUI.Label(210, 372, 100, 24, usage 17911 "经过/理由", style +0x2a8): the gold bold
        // 15px label, placed at px + 40 like every FormRow caption.
        GUI.Label(new Rect(210f, 372f, 100f, 24f), "经过/理由", _loginLabel);

        // GUI.Box(316, 372, 274, 76, GUIContent.none, style +0x2b8): the taller input frame
        // behind the text area, i.e. _inputStyle.
        GUI.Box(new Rect(316f, 372f, 274f, 76f), GUIContent.none, _inputStyle);

        // GUI.TextArea(322, 374, 262, 72, apReason, 200, style +0x290): maxLength is the
        // literal 200 (0xc8), far above the 0x18 the single-line rows use.
        _appealReason = GUI.TextArea(
            new Rect(322f, 374f, 262f, 72f),
            _appealReason,
            200,
            _fieldStyle);

        FormButtons(458f, DoAppeal);
        string hint = string.IsNullOrEmpty(netMsg)
            ? "填写账号、大概什么时候被盗的、以及经过(越具体越好: 最后一次正常登录、有没有在别处登录过、有没有交易过号)"
            : netMsg;
        DrawHintBox(400f, 528f, hint, 560f);
        GUI.matrix = previousMatrix;
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DoAppeal (0x06000eee, arm64 0x3904ec / x86_64 0x3a3750).
    /// </summary>
    /// <remarks>
    /// The native order is: <c>apAcc.Trim()</c> must be non-empty or usage 18063 "请填写账号" is
    /// written; then <c>apReason.Trim()</c> must be at least five characters long or usage
    /// 18065 "请把经过写清楚一些(至少 5 个字)" is written — <c>apWhen</c> is never validated. Only
    /// then does the body write usage 17558 "提交申诉..." and copy <c>apAcc.Trim()</c>,
    /// <c>apWhen.Trim()</c> and <c>apReason.Trim()</c> into
    /// <c>&lt;&gt;c__DisplayClass174_0</c> (+0x10 / +0x18 / +0x20) <em>before</em> it calls
    /// <c>NetClient.ConnectAsync(&lt;DoAppeal&gt;b__0)</c>. A null apAcc, apWhen or apReason
    /// dereferences and raises (arm64 FUN_00177a74, x86_64 FUN_0016c1b0), which is what a bare
    /// <c>Trim()</c> does here too.
    /// The snapshot is the point of this method: the recovery form reads its fields again inside
    /// the connect callback (0x06000f08), the appeal form does not. The native side stores it in a
    /// display class that DoAppeal allocates <em>per invocation</em> (0x06000eee, ctor 0x06000f0e),
    /// which is why a second click cannot change what the first callback sends. The three
    /// invocation-local strings below are captured by the callback, with this component
    /// retained as the message receiver.
    /// No dedicated reply arm exists to model: <c>&lt;DoAppeal&gt;b__0</c> (0x06000f0f) sends on
    /// <c>ok == true</c> and writes usage 18146 "连接失败, 请重试" on <c>ok == false</c>, and no
    /// appeal-result handler is registered, so a successful send leaves "提交申诉..." on screen with
    /// the form still open. That is the proven client-side behaviour only; it does not prove that a
    /// server answers nothing at all through some generic notice or error handler.
    /// </remarks>
    private void DoAppeal()
    {
        if (_appealAccount.Trim().Length == 0)
        {
            netMsg = "请填写账号";
            return;
        }

        if (_appealReason.Trim().Length < 5)
        {
            netMsg = "请把经过写清楚一些(至少 5 个字)";
            return;
        }

        netMsg = "提交申诉...";
        string submittedAccount = _appealAccount.Trim();
        string submittedWhen = _appealWhen.Trim();
        string submittedReason = _appealReason.Trim();
        BeginTheftAppeal(submittedAccount, submittedWhen, submittedReason);
        return;
    }

    private void LoadLoginPrefs()
    {
        if (_loginPreferencesLoaded)
        {
            return;
        }

        _loginPreferencesLoaded = true;
        _rememberAccount = PlayerPrefs.GetInt(RememberAccountKey, 0) != 0;
        _rememberPassword = PlayerPrefs.GetInt(RememberPasswordKey, 0) != 0;

        // Official 1.5.8 TitleFlow.LoadLoginPrefs (06000ed2): two independent branches guarded
        // by the static loginPrefLoaded (+0x12). The account is restored when rememberAcc holds
        // and the account is still blank; the password is restored by a separate if when
        // rememberPwd holds and the password is still blank — there is no else between them,
        // so ticking both boxes fills both fields even when one of them was already typed.
        if (_rememberAccount && string.IsNullOrEmpty(account))
        {
            account = PlayerPrefs.GetString(SavedAccountKey, string.Empty);
        }

        if (_rememberPassword && string.IsNullOrEmpty(password))
        {
            password = RecoveredTitleLoginSurface.Deobfuscate(PlayerPrefs.GetString(SavedPasswordKey, string.Empty));
        }
    }

    private void SaveLoginPrefs()
    {
        PlayerPrefs.SetInt(RememberAccountKey, _rememberAccount ? 1 : 0);
        PlayerPrefs.SetInt(RememberPasswordKey, _rememberPassword ? 1 : 0);

        if (_rememberAccount)
        {
            PlayerPrefs.SetString(SavedAccountKey, account ?? string.Empty);
        }
        else
        {
            PlayerPrefs.DeleteKey(SavedAccountKey);
        }

        if (_rememberPassword)
        {
            PlayerPrefs.SetString(SavedPasswordKey, RecoveredTitleLoginSurface.Obfuscate(password));
        }
        else
        {
            PlayerPrefs.DeleteKey(SavedPasswordKey);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DrawResSelector (06000ecd, arm64 0x38beb0 / x86_64 0x39f3b0).
    /// DrawLogin (06000ee6) calls it immediately before DrawFpsSelector (06000ece) and
    /// DrawVersionBadge (06000ee8) - arm64 06000ee6.c:504-506, x86_64 06000ee6.c:366-368 - so
    /// the two selectors are the only two list widgets on the title screen and they are drawn
    /// in that order every frame. The body is: resolve the curRes sentinel, one head button,
    /// one panel Box and one option button per entry of GameSettings.ResOpts. It never calls
    /// Screen.SetResolution and never names a FullScreenMode: the click only passes the index
    /// to GameSettings.ApplyRes (06000e11). Verified against 1.5.8, not against the 1.3.4
    /// geometry this file was originally annotated with.
    /// </summary>
    private void DrawResSelector()
    {
        if (_resolutionIndex < 0)
        {
            _resolutionIndex = GameSettings.ResIndex();
        }

        (int Width, int Height) selected = GameSettings.ResOpts[_resolutionIndex];
        string label = string.Format("分辨率 {0}×{1}", selected.Width, selected.Height);
        if (GUI.Button(new Rect(8f, 559f, ButtonWidth, ButtonHeight), label, _regButtonStyle))
        {
            _resolutionListOpen = !_resolutionListOpen;
            if (!_resolutionListOpen)
            {
                return;
            }

            // Opening the resolution list collapses the frame-rate list in the same frame
            // (arm64 06000ecd.c:105, x86_64 06000ecd.c:86 write +0x308 = 0). DrawFpsSelector
            // mirrors this on its own side (+0x300 = 0), so the two lists are mutually
            // exclusive in both directions.
            _frameRateListOpen = false;
        }
        else if (!_resolutionListOpen)
        {
            return;
        }

        float height = GameSettings.ResOpts.Length * 30f + 16f;
        float y = 559f - height - 4f;
        GUI.Box(new Rect(8f, y, ButtonWidth, height), GUIContent.none, _panelStyle);
        for (int index = 0; index < GameSettings.ResOpts.Length; index++)
        {
            (int Width, int Height) option = GameSettings.ResOpts[index];
            string item = string.Format("{0}×{1}", option.Width, option.Height);
            if (index == _resolutionIndex)
            {
                item = "◆ " + item;
            }

            if (!GUI.Button(
                    new Rect(16f, y + 8f + index * 30f, 136f, 28f),
                    item,
                    _regButtonStyle))
            {
                continue;
            }

            // Official write order on a click: curRes = index, then resListOpen = 0, then
            // GameSettings.ApplyRes(index) (arm64 06000ecd.c:166-171, x86_64 :150-155). The
            // loop keeps running after a hit - there is no early return - so the remaining
            // option buttons of the same frame are still drawn even though the list flag has
            // already been cleared.
            _resolutionIndex = index;
            _resolutionListOpen = false;
            GameSettings.ApplyRes(index);
        }
    }

    /// <summary>
    /// Official 1.5.8 TitleFlow.DrawFpsSelector (06000ece, arm64 0x38c36c / x86_64 0x39f890).
    /// DrawLogin calls it between DrawResSelector and DrawVersionBadge, and the whole body is
    /// one EnsureLoaded call, one head button, one panel Box and exactly three option buttons
    /// - there is no fourth preset, no click-outside close, no Event.Use() and no focus/id
    /// bookkeeping. The only state it writes besides GameSettings.FrameRateMode is the
    /// instance open flag at +0x308 (and +0x300 when it opens, which collapses the resolution
    /// list the same frame).
    /// </summary>
    private void DrawFpsSelector()
    {
        GameSettings.EnsureLoaded();
        string head = "帧率 " + GameSettings.FpsLabel(GameSettings.FrameRateMode);
        if (GUI.Button(new Rect(8f, 522f, ButtonWidth, ButtonHeight), head, _regButtonStyle))
        {
            _frameRateListOpen = !_frameRateListOpen;
            if (!_frameRateListOpen)
            {
                return;
            }

            _resolutionListOpen = false;
        }
        else if (!_frameRateListOpen)
        {
            return;
        }

        GUI.Box(new Rect(8f, 412f, ButtonWidth, 106f), GUIContent.none, _panelStyle);
        for (int mode = 0; mode < 3; mode++)
        {
            // The native body is unrolled: option i uses the literal y 420/450/480 and writes
            // FrameRateMode, then Save(), then ApplyFrameRate(), then closes the list. Reading
            // FrameRateMode again every iteration is what makes the ◆ marker follow a click
            // within the same frame in the official code too.
            string item = (GameSettings.FrameRateMode == mode ? "◆ " : string.Empty) +
                GameSettings.FpsLabel(mode);
            if (!GUI.Button(
                    new Rect(16f, 420f + 30f * mode, 136f, 28f),
                    item,
                    _regButtonStyle))
            {
                continue;
            }

            GameSettings.FrameRateMode = mode;
            GameSettings.Save();
            GameSettings.ApplyFrameRate();
            _frameRateListOpen = false;
        }
    }

    private void DrawVersionBadge()
    {
        // Official 06000ee8 snapshots the publication once, before choosing either row value.
        string serverVersion = NetClient.LatestVer;
        Color serverColor = string.IsNullOrEmpty(serverVersion)
            ? new Color(0.7f, 0.7f, 0.7f, 1f)
            : serverVersion == RecoveredTitleLoginSurface.ClientVer
                ? new Color(0.55f, 0.9f, 0.55f, 1f)
                : new Color(1f, 0.55f, 0.45f, 1f);
        string serverText = string.IsNullOrEmpty(serverVersion)
            ? "服务端 未知"
            : "服务端 v" + serverVersion;

        GUIStyle style = new GUIStyle(_loginLabel)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12
        };
        style.normal.textColor = serverColor;
        GUI.Label(new Rect(586f, 566f, 210f, 16f), serverText, style);
        style.normal.textColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        GUI.Label(new Rect(586f, 581f, 210f, 16f), "客户端 v1.6.0", style);
    }

    private void EnsureStyles()
    {
        if (_center == null)
        {
            _center = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                clipping = TextClipping.Overflow
            };

            // Official field style (TitleFlow field +0x290, built in OnGUI 06000ec7) writes
            // alignment, fontSize, padding, overflow, contentOffset, fixedWidth/Height, wordWrap,
            // the eight state backgrounds and the eight state text colours — and no clipping, so
            // the value inherited from GUI.skin.textField is kept.
            _fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                padding = new RectOffset(2, 2, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                contentOffset = Vector2.zero,
                fixedWidth = 0f,
                fixedHeight = 0f,
                wordWrap = false
            };
            RecoveredTitleLoginSurface.ClearStyleBackgrounds(_fieldStyle);
            RecoveredTitleLoginSurface.SetAllTextColors(_fieldStyle, Color.white);

            _placeholderStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                clipping = TextClipping.Overflow
            };
            _placeholderStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);

            _loginLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Overflow
            };
            _loginLabel.normal.textColor = new Color(1f, 0.78f, 0.32f, 1f);

            _buttonBackground = RecoveredTitleLoginSurface.MakeRoundTexture(
                8,
                2,
                new Color(0.117647f, 0.098039f, 0.07451f, 1f),
                new Color(0.72f, 0.58f, 0.3f, 1f),
                new Color(0.588235f, 0.470588f, 0.258824f, 1f));
            _regButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(8, 8, 8, 8)
            };
            RecoveredTitleLoginSurface.SetAllStyleBackgrounds(_regButtonStyle, _buttonBackground);
            _regButtonStyle.normal.textColor = new Color(1f, 0.82f, 0.4f, 1f);
            _regButtonStyle.focused.textColor = _regButtonStyle.normal.textColor;
            _regButtonStyle.hover.textColor = Color.white;
            _regButtonStyle.active.textColor = Color.white;

            _panelBackground = RecoveredTitleLoginSurface.MakeRoundTexture(
                14,
                2,
                new Color(0f, 0f, 0f, 0.78f),
                new Color(0.72f, 0.58f, 0.3f, 1f),
                null);
            _panelStyle = new GUIStyle
            {
                border = new RectOffset(14, 14, 14, 14)
            };
            _panelStyle.normal.background = _panelBackground;

            _inputBackground = RecoveredTitleLoginSurface.MakeRoundTexture(
                6,
                0,
                new Color(0.16f, 0.15f, 0.13f, 1f),
                Color.clear,
                null);
            _inputStyle = new GUIStyle
            {
                border = new RectOffset(6, 6, 6, 6)
            };
            _inputStyle.normal.background = _inputBackground;

            _hintStyle = new GUIStyle(_center)
            {
                fontSize = 13,
                wordWrap = false,
                clipping = TextClipping.Overflow
            };

            _disclaimerStyle = new GUIStyle(_loginLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = false
            };
            _disclaimerStyle.normal.textColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        }

        // OnGUI (06000ec7) performs these writes after style initialization on every
        // GUI event. They are not part of the once-only style-allocation guard.
        GUI.skin.settings.selectionColor = new Color(0.55f, 0.45f, 0.25f, 0.45f);
        GUI.skin.settings.cursorColor = Color.white;
    }

}
}
