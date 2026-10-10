using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Original 1.5.8 resource slots. hb24Bk and cooldownFrames are declared by their
    // already-recovered consumers; this complete Load writes those same fields.
    private bool loaded;
    // 06000454 instance defaults are ordered in GameHud.Initialization.cs.
    internal Texture2D statusbarbk;
    internal Texture2D hpBg;
    internal Texture2D hpFill;
    internal Texture2D mpBg;
    internal Texture2D mpFill;
    internal Texture2D headPortrait;
    internal Texture2D petbar;
    internal Texture2D petHpFill;
    internal Texture2D petMpFill;
    private Texture2D petHead;
    private Texture2D petClawHead;
    internal Texture2D nameFxGray;
    internal Texture2D nameFxWhite;
    internal Texture2D socketFill;
    internal Texture2D tbarMonster;
    internal Texture2D tbarPlayer;
    internal Texture2D tbarHpFill;
    internal Texture2D accelbarbk;
    internal Texture2D acceltext;
    internal Texture2D togglebtn;
    internal Texture2D accelPlus;
    internal Texture2D accelMinus;
    internal Texture2D accelHelp;
    internal Texture2D accelEffect;
    internal Texture2D accelShowBtn;
    internal Texture2D hb24Text;
    internal Texture2D hb24Text2;
    internal Texture2D hb24Block;
    internal Texture2D hb24Effect;
    internal Texture2D[] hb24Ctrl;
    internal Texture2D skBar;
    internal Texture2D skToggle;
    internal Texture2D skWorldBtn;
    internal Texture2D skPlus;
    internal Texture2D skMinus;
    internal Texture2D skFrame;
    internal Texture2D skMarkSelf;
    internal Texture2D skMarkMate;
    internal Texture2D hpAimer;
    // hpAimVal starts at -1 in Initialization; 06000414 advances both slots on Repaint.
    internal float hpAimWaitT;
    internal Texture2D lianliRing;
    internal Texture2D nailiRing;
    internal Texture2D exp05Bk;
    internal Texture2D ctrlBarBk;
    internal Texture2D[] hpFill3;
    internal Texture2D[] petHpFill3;
    internal Texture2D[] exp05Fill;
    internal Texture2D[] tbarHpFill3;
    internal Texture2D[] ctrlTex;
    internal Texture2D[] rack21;
    internal Texture2D BottomBackgroundForIndependentRuntime => hb24Bk;
    internal Texture2D[] CooldownFramesForIndependentRuntime => cooldownFrames;
    internal string HeadPortraitPathForIndependentRuntime { get; private set; }

    // 06000410, complete body. No guard inside Load: direct invocation repeats the
    // sequence. Ordinary GUI entry below owns the one-shot loaded test.
    private void Load()
    {
        loaded = true;
        ResLoader R = ResLoader.I;
        if (R == null) return;

        statusbarbk = R.LoadUI("ui/statusbar/statusbarbk.png");
        hpBg = R.LoadUI("ui/statusbar/hplayer.png");
        hpFill = R.LoadUI("ui/statusbar/hpbar.png");
        hpFill3 = new Texture2D[3];
        hpFill3[0] = R.LoadGsaFrameA("res/uires/statusbar/hpbar.gsa", 0);
        hpFill3[1] = R.LoadGsaFrameA("res/uires/statusbar/hpbar.gsa", 1);
        hpFill3[2] = R.LoadGsaFrameA("res/uires/statusbar/hpbar.gsa", 2);
        hpAimer = R.LoadGsa("res/uires/common/aimer.gsa");
        lianliRing = R.LoadGsa("res/effect/lianli.gsa");
        nailiRing = R.LoadGsa("res/effect/naili.gsa");
        mpBg = R.LoadUI("ui/statusbar/mplayer.png");
        mpFill = R.LoadUI("ui/statusbar/mpbar.png");
        petbar = R.LoadUI("ui/petdlg/petbar.png");
        petHpFill = R.LoadUI("ui/petdlg/pethp.png");
        petHpFill3 = new Texture2D[3];
        petHpFill3[0] = R.LoadGsaFrameA("res/uires/petdlg/pethp.gsa", 0);
        petHpFill3[1] = R.LoadGsaFrameA("res/uires/petdlg/pethp.gsa", 1);
        petHpFill3[2] = R.LoadGsaFrameA("res/uires/petdlg/pethp.gsa", 2);
        petMpFill = R.LoadUI("ui/petdlg/petmp.png");
        petHeadModel = -1;
        nameFxGray = R.LoadUI("ui/statusbar/namelabeleffectgray.png");
        nameFxWhite = WhiteCopy(nameFxGray);
        socketFill = BuildDisc(56, new Color32(24, 19, 16, 255));
        exp05Bk = R.LoadGsaFrameA("res/uires/statusbar/expbarbk.gsa", 0);
        exp05Fill = new Texture2D[3];
        exp05Fill[0] = R.LoadGsaFrameA("res/uires/statusbar/expbar.gsa", 0);
        exp05Fill[1] = R.LoadGsaFrameA("res/uires/statusbar/expbar.gsa", 1);
        exp05Fill[2] = R.LoadGsaFrameA("res/uires/statusbar/expbar.gsa", 2);
        skBar = R.LoadGsa("res/uires/sketchmap/sketchbar.gsa");
        skToggle = R.LoadGsaFrame("res/uires/sketchmap/togglebtn.gsa", 0);
        skWorldBtn = R.LoadGsaFrame("res/uires/sketchmap/worldbtn.gsa", 0);
        skPlus = R.LoadGsaFrame("res/uires/sketchmap/plusbtn.gsa", 0);
        skMinus = R.LoadGsaFrame("res/uires/sketchmap/minusbtn.gsa", 0);
        skFrame = R.LoadGsa("res/effect/sketchmap.gsa");
        skMarkSelf = R.LoadGsa("res/buddy/sketch_wanjia.gsa");
        skMarkMate = R.LoadGsa("res/buddy/sketch_duiyou.gsa");
        tbarPlayer = R.LoadGsaFrameA("res/uires/gameui/targetbar.gsa", 0);
        tbarMonster = R.LoadGsaFrameA("res/uires/gameui/targetbar.gsa", 1);
        tbarHpFill = R.LoadGsa("res/uires/gameui/targethp.gsa");
        tbarHpFill3 = new Texture2D[3];
        tbarHpFill3[0] = R.LoadGsaFrameA("res/uires/gameui/targethp.gsa", 0);
        tbarHpFill3[1] = R.LoadGsaFrameA("res/uires/gameui/targethp.gsa", 1);
        tbarHpFill3[2] = R.LoadGsaFrameA("res/uires/gameui/targethp.gsa", 2);
        GameSettings.EnsureLoaded();
        accelbarbk = R.LoadUI("ui/accelbar/accelbarbk.png");
        acceltext = R.LoadUI("ui/accelbar/acceltext.png");
        togglebtn = R.LoadUI("ui/accelbar/togglebtn.png");
        accelPlus = R.LoadUI("ui/accelbar/plusbtn.png");
        accelMinus = R.LoadUI("ui/accelbar/minusbtn.png");
        accelHelp = R.LoadUI("ui/accelbar/helpicon.png");
        accelShowBtn = R.LoadUI("ui/accelbar/accelbtn.png");
        accelEffect = R.LoadUI("ui/accelbar/accelbareffect.png");
        hb24Bk = R.LoadUI("ui/accelbar24/accelbarbk.png");
        hb24Text = R.LoadUI("ui/accelbar24/acceltext.png");
        hb24Text2 = R.LoadUI("ui/accelbar24/accelbar_text2.png");
        hb24Block = R.LoadUI("ui/accelbar24/singleblock.png");
        hb24Effect = R.LoadUI("ui/accelbar24/accelbareffect.png");
        hb24Ctrl = new Texture2D[unchecked(CtrlBtns.Length * 3)];
        for (int i = 0; i < CtrlBtns.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Texture2D[] destination = hb24Ctrl;
                int index = unchecked(i * 3 + j);
                string[] parts = new string[5];
                parts[0] = "ui/accelbar24/ctrl/";
                parts[1] = CtrlBtns[i].Item1;
                parts[2] = "_f";
                parts[3] = j.ToString();
                parts[4] = ".png";
                destination[index] = R.LoadUI(string.Concat(parts));
            }
        }
        cooldownFrames = new Texture2D[37];
        for (int i = 0; i < 37; i++)
            cooldownFrames[i] = R.LoadGsaFrame("res/effect/cooldown.gsa", i);
        ctrlBarBk = R.LoadUI("ui/ctrlbar/ctrlbarbk.png");
        ctrlTex = new Texture2D[unchecked(CtrlBtns.Length * 3)];
        for (int i = 0; i < CtrlBtns.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Texture2D[] destination = ctrlTex;
                int index = unchecked(i * 3 + j);
                string[] parts = new string[5];
                parts[0] = "ui/ctrlbar/";
                parts[1] = CtrlBtns[i].Item1;
                parts[2] = "_f";
                parts[3] = j.ToString();
                parts[4] = ".png";
                destination[index] = R.LoadUI(string.Concat(parts));
            }
        }
        rack21 = new Texture2D[3];
        rack21[0] = R.LoadGsaFrame2021First("res/uires/ctrlbar/rackbtn.gsa", 0);
        rack21[1] = R.LoadGsaFrame2021First("res/uires/ctrlbar/rackbtn.gsa", 1);
        rack21[2] = R.LoadGsaFrame2021First("res/uires/ctrlbar/rackbtn.gsa", 2);
        if (ChatUI.I == null)
            new GameObject("ChatUI").AddComponent<ChatUI>();
        LoadHeadPortrait();
    }

    // 06000414 avatar tail: retain the branch selected before pet vitals/name drawing.
    // Publish the model marker before loading; a failed positive-model load is not retried
    // until the model changes, while the empty-pet art retries whenever its texture is null.
    internal Texture2D ResolvePetHeadForIndependentRuntime(bool hasPet)
    {
        if (hasPet)
        {
            if (petHeadModel != CharData.petModel)
            {
                petHeadModel = CharData.petModel;
                if (CharData.petModel > 0 && ResLoader.I != null)
                {
                    ResLoader loader = ResLoader.I;
                    string path = string.Format("res/peticon/M{0}.gsa", CharData.petModel);
                    petHead = loader.LoadGsa(path);
                }
                else
                {
                    petHead = null;
                }
            }
            return petHead;
        }

        petHeadModel = 0;
        petHead = null;
        if (petClawHead == null && ResLoader.I != null)
            petClawHead = ResLoader.I.LoadGsa("res/peticon/M000.gsa");
        return petClawHead;
    }

    // Only 06000414's proven entry, driven once by the existing manual OnGUI host.
    // No font, event, world-liveness or texture-completeness gate belongs before Load.
    internal void DrawResourcePrefixForIndependentRuntime(out int screenWidth, out int screenHeight)
    {
        GUI.depth = 50;
        if (!loaded) Load();
        s = uiScale > 0f ? uiScale : UIScale.Factor;
        // OnGUI keeps these invocation-local values through its later drawing stages.
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        EnsureStyles();
    }

    // 06000411's already-established portrait algorithm, now on its real receiver.
    // Initial Load reads CharData here; the appearance-version draw edge uses the same body.
    private void LoadHeadPortrait()
    {
        ResLoader R = ResLoader.I;
        if (R == null) return;
        HeadPortraitPathForIndependentRuntime = ReferenceFirstWorldHudResources.BuildHeadPortraitPath(
            CharData.gender, CharData.head, CharData.hair);
        headPortrait = R.LoadGsa(HeadPortraitPathForIndependentRuntime);
        headPortVer = CharData.apVersion;
    }

    internal void SyncHeadPortraitForIndependentRuntime()
    {
        if (headPortVer != CharData.apVersion) LoadHeadPortrait();
    }

    private static Texture2D WhiteCopy(Texture2D source)
    {
        if (!source)
        {
            return null;
        }

        Color32[] pixels = source.GetPixels32();
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new Color32(255, 255, 255, pixels[index].a);
        }

        Texture2D texture = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false,
            false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D BuildDisc(int size, Color32 color)
    {
        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) / 2f;
        float radius = size / 2f - 0.5f;
        byte red = color.r;
        byte green = color.g;
        byte blue = color.b;
        for (int y = 0; y < size; y++)
        {
            float dy = y - center;
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float coverage = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                byte alpha = (byte)(int)(coverage * 255f);
                pixels[y * size + x] = new Color32(red, green, blue, alpha);
            }
        }

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false,
            false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    internal void ReleaseGeneratedHudTexturesForIndependentRuntime()
    {
        DestroyOwnedTexture(nameFxWhite);
        DestroyOwnedTexture(socketFill);
        nameFxWhite = null;
        socketFill = null;
    }

    private static void DestroyOwnedTexture(Texture2D texture)
    {
        if (!texture)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(texture);
    }
}
}
