using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private Texture2D bg, cbg, box, btnLogin, loginPanel, recoverTitle, createbk;
    private Texture2D loginBg, selBg, maleFrm, femaleFrm, btnRegister, chkOff, chkOn;
    private bool tried05;
    // Existing reviewed Avatar PNG supply; this is still the shared persistent loader.
    private ReferenceResourceLoader avatarResources;

    internal Texture2D LoginBackgroundForIndependentRuntime => loginBg ?? bg;
    internal Texture2D SelectionBackgroundForIndependentRuntime => selBg ?? cbg ?? bg;
    internal Texture2D LoginPanelForIndependentRuntime => box;
    internal Texture2D CheckboxOffForIndependentRuntime => chkOff;
    internal Texture2D CheckboxOnForIndependentRuntime => chkOn;
    internal Texture2D CompleteButtonForIndependentRuntime => btnComplete;
    internal Texture2D ReturnButtonForIndependentRuntime => btnReturn;

    // The real component is already attached when Unity calls Awake. The existing
    // same-host view publishes the page adapters, then this invocation owns the
    // closed resource prefix and PlayLogin -> SetupNet tail on the same stack.
    private void Awake()
    {
        Action completeAwake = GetComponent<ClientRootView>()
            .PublishTitleOwnerForIndependentRuntime(this);
        bg = L("titleuibk");
        cbg = L("freegame_titleuibk2");
        box = L("loginbk");
        loginPanel = L("loginbk");
        recoverTitle = L("recovertitle");
        btnLogin = L("loginbtn");
        btnQuit = L("quitbtn");
        btnComplete = L("completebtn");
        btnReturn = L("returnbtn");
        btnCreate = L("createbtn");
        btnEnter = L("enterbtn");
        zonepanel = L("zonepanel");
        actorbk = L("actorselbk");
        createbk = L("creatwinbk");
        previewfrm = L("previewlabel");
        proppanel = L("proppanel2");
        namelbl = L("namelabel");
        hairbtnTex = L("hairbtn");
        loginBg = L("crossserver1");
        selBg = L("extitleuibk2");
        male = L("malecheck");
        maleSel = L("malecheck_sel");
        maleFrm = L("malecheck_frm");
        female = L("femalecheck");
        femaleSel = L("femalecheck_sel");
        femaleFrm = L("femalecheck_frm");
        for (int i = 0; i < 6; i++)
        {
            typebtn[i] = L("typebtn" + i);
            typebtnSel[i] = L("typebtn" + i + "_selg");
        }
        for (int i = 1; i <= 8; i++)
        {
            colorbtn[i] = L("colorbtn" + i);
            colorbtnSel[i] = L("colorbtn" + i + "_sel");
        }
        for (int i = 0; i < 5; i++)
            classBody[i] = LoadAvatarForIndependentRuntime("class" + i);
        for (int race = 0; race < 6; race++)
            for (int gender = 0; gender < 2; gender++)
                classpic[race, gender] = LoadAvatarForIndependentRuntime("classpic" + race + "_" + gender);
        // Preserve the closed Awake nude-before-hair passes and existing PNG names.
        for (int gender = 0; gender < 2; gender++)
            for (int head = 1; head <= 8; head++)
                for (int hair = 1; hair <= 2; hair++)
                    nudeAv[gender, head, hair] = LoadAvatarForIndependentRuntime(
                        "nude" + gender + "_" + head + "_" + hair);
        for (int gender = 0; gender < 2; gender++)
            for (int head = 1; head <= 8; head++)
                for (int hair = 1; hair <= 2; hair++)
                    hairHead[gender, head, hair] = LoadAvatarForIndependentRuntime(
                        "hairhead" + gender + "_" + head + "_" + hair);
        completeAwake();
    }

    // Preserve the already admitted resource representation for Avatar inputs.
    // It does not synthesize an asset or fall back to an unrelated image.
    private Texture2D LoadAvatarForIndependentRuntime(string name)
    {
        if (avatarResources == null)
            avatarResources = ReferenceResourceLoader.OpenStreamingAssets();
        return avatarResources.LoadReviewedPng("creation-textures/" + name + ".png");
    }

    // 06000ec6: unavailable loader/package does not consume the attempt. Once admitted,
    // latch before the first read; null results retain legacy fields, and exceptions
    // retain both the reached publication prefix and the consumed attempt.
    internal void TryLoad05Login()
    {
        if (tried05 || ResLoader.I == null || ResLoader.I.Pkg == null) return;
        tried05 = true;
        Texture2D t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/titleuibk.gsa");
        if (t)
        {
            bg = t;
            loginBg = t;
            selBg = t;
        }
        t = ResLoader.I.LoadGsa("res/uires/titleui/loginbk.gsa");
        if (t) box = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/loginbtn.gsa", 0);
        if (t) btnLogin = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/candidatebtn.gsa", 0);
        if (t) btnRegister = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/quitbtn.gsa", 0);
        if (t) btnQuit = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/zonepanel.gsa");
        if (t) zonepanel = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/returnbtn.gsa", 0);
        if (t) btnReturn = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/affirmbtn.gsa", 0);
        if (t) btnAffirm = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/autobtn.gsa", 0);
        if (t) btnAuto = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/infolabel.gsa");
        if (t) infoBar = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/common/checkbox.gsa", 0);
        if (t) chkOff = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/common/checkbox.gsa", 1);
        if (t) chkOn = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/worldbtn.gsa", 0);
        if (t) worldBtn[0] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/pvebtn.gsa", 0);
        if (t) pveBtn[0] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/pvpbtn.gsa", 0);
        if (t) pvpBtn[0] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/worldbtn.gsa", 1);
        if (t) worldBtn[1] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/pvebtn.gsa", 1);
        if (t) pveBtn[1] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/pvpbtn.gsa", 1);
        if (t) pvpBtn[1] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busylabel.gsa", 0);
        if (t) busyBar[0] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busylabel.gsa", 1);
        if (t) busyBar[1] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busylabel.gsa", 2);
        if (t) busyBar[2] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busylabel.gsa", 3);
        if (t) busyBar[3] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busyworld.gsa", 0);
        if (t) busyWorld[0] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busyworld.gsa", 1);
        if (t) busyWorld[1] = t;
        t = ResLoader.I.LoadGsaFrameA("res/uires/titleui/busyworld.gsa", 2);
        if (t) busyWorld[2] = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/actorselbk.gsa");
        if (t) actorbk = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/createbtn.gsa", 0);
        if (t) btnCreate = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/deletebtn.gsa", 0);
        if (t) btnDelete = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/enterbtn.gsa", 0);
        if (t) btnEnter = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/completebtn.gsa", 0);
        if (t) btnComplete = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/namelabel.gsa");
        if (t) namelbl = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/previewlabel.gsa");
        if (t) previewfrm = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/proppanel2.gsa");
        if (t) proppanel = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/proppanel1.gsa");
        if (t) selProp = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/actorcover.gsa");
        if (t) actorcover = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/genderlabel.gsa");
        if (t) genderLbl = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/mantypelabel.gsa");
        if (t) typeLbl = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/figurelabel.gsa");
        if (t) figureLbl = t;
        t = ResLoader.I.LoadGsa("res/uires/titleui/hairlabel.gsa");
        if (t) hairLbl = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/hairbtn.gsa", 0);
        if (t) hairbtnTex = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/malecheck.gsa", 1);
        if (t) male = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/malecheck.gsa", 0);
        if (t) maleSel = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/femalecheck.gsa", 1);
        if (t) female = t;
        t = ResLoader.I.LoadGsaFrame("res/uires/titleui/femalecheck.gsa", 0);
        if (t) femaleSel = t;
        for (int i = 0; i < 5; i++)
        {
            t = ResLoader.I.LoadGsaFrame(string.Format("res/uires/titleui/typebtn{0}.gsa", i), 1);
            if (t) typebtn[i] = t;
            t = ResLoader.I.LoadGsaFrame(string.Format("res/uires/titleui/typebtn{0}.gsa", i), 0);
            if (t) typebtnSel[i] = t;
        }
        for (int i = 1; i <= 8; i++)
        {
            t = ResLoader.I.LoadGsaFrame(string.Format("res/uires/titleui/colorbtn{0}.gsa", i), 0);
            if (t) colorbtn[i] = t;
            t = ResLoader.I.LoadGsaFrame(string.Format("res/uires/titleui/colorbtn{0}.gsa", i), 1);
            if (t) colorbtnSel[i] = t;
        }
    }
}
}
