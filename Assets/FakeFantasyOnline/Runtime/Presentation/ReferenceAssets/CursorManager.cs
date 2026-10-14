using System.IO;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered CursorManager normal/cast ownership used by SkillTargeting.
/// </summary>
public partial class CursorManager : MonoBehaviour
{
    public static CursorManager I;

    private static readonly Vector2 normalHot = new Vector2(10f, 2f);
    private static readonly Vector2 avatarHot = new Vector2(4f, 4f);
    private static readonly Vector2 repairHot = new Vector2(6f, 6f);
    private static readonly Vector2 chatHot = new Vector2(4f, 4f);
    private static readonly Vector2 fetchHot = new Vector2(14f, 10f);
    private static readonly Vector2 atkHot = new Vector2(6f, 6f);
    private static readonly Vector2 castHot = new Vector2(8f, 6f);

    private Texture2D normalCur;
    private Texture2D castCur;
    private bool castMode;
    public int cursorScale = 1;
    private Texture2D chatCur, fetchCur, atkHCur, atkMCur, atkLCur, repairCur, avatarCur, fishCur, mineCur;
    private bool avatarMode, repairMode, npcHover, fetchHover, gatherTried;
    private int gatherHover;
    private int attackCmp = -2;
    private Texture2D[] chatFrames, animFrames;
    private Vector2 animHot;
    private int animLastFrame = -1;

    internal bool CastModeForAudit => castMode;

    private string referenceRootForIndependentRuntime;

    internal static CursorManager EnsureForIndependentRuntime(string referenceRoot)
    {
        if (I != null) return I;
        var root = new GameObject("CursorManager");
        root.SetActive(false);
        CursorManager owner = root.AddComponent<CursorManager>();
        owner.referenceRootForIndependentRuntime = referenceRoot;
        root.SetActive(true);
        return owner;
    }

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
        normalCur = Build("Normal");
        chatCur = Build("Chat");
        fetchCur = Build("Fetch");
        castCur = Build("Cast");
        atkHCur = Build("AttackH");
        atkMCur = Build("AttackM");
        atkLCur = Build("AttackL");
        Set(normalCur);
    }

    // Product-only overload: it only supplies the reference asset root for the running runtime and
    // carries no official token of its own. Awake and every other caller use the single-argument form.
    private Texture2D Build(string name) => Build(
        referenceRootForIndependentRuntime ?? Path.Combine(ResourceFileRoot.Path, "ReferenceClient"), name);

    // Official 0x060001c0, instance UnityEngine.Texture2D CursorManager.Build(System.String). A cursor
    // png is located under <referenceRoot>/Cursor, decoded, magnified by the cursor scale and returned
    // as an owned RGBA32 texture.
    private Texture2D Build(string referenceRoot, string name)
    {
        string path = Path.Combine(referenceRoot, "Cursor", name + ".png");
        if (!File.Exists(path))
        {
            return null;
        }

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(source, File.ReadAllBytes(path));

        int scale = Mathf.Max(1, cursorScale);
        int width = source.width;
        int height = source.height;
        Color32[] sourcePixels = source.GetPixels32();
        Texture2D result = new Texture2D(unchecked(width * scale), unchecked(height * scale), TextureFormat.RGBA32, false);
        Color32[] output = new Color32[unchecked(width * scale * height * scale)];
        for (int y = 0; y < unchecked(height * scale); y++)
            for (int x = 0; x < unchecked(width * scale); x++)
                output[unchecked(y * width * scale + x)] = sourcePixels[unchecked((y / scale) * width + x / scale)];
        result.SetPixels32(output);
        result.filterMode = FilterMode.Point;
        result.Apply(false, false);
        Destroy(source);
        return result;
    }

    private static bool Valid(Texture2D texture) => texture != null && texture.format == TextureFormat.RGBA32 && texture.mipmapCount == 1 && texture.isReadable;

    private void Set(Texture2D texture)
    {
        if (!Valid(texture)) return;
        Vector2 hot;
        if (texture == chatCur) hot = chatHot;
        else if (texture == fetchCur) hot = fetchHot;
        else if (texture == castCur) hot = castHot;
        else if (texture == avatarCur) hot = avatarHot;
        else if (texture == repairCur) hot = repairHot;
        else if (texture == atkHCur || texture == atkMCur || texture == atkLCur) hot = atkHot;
        else hot = normalHot;
        Cursor.SetCursor(texture, hot * Mathf.Max(1, cursorScale), CursorMode.Auto);
    }

    private void Refresh()
    {
        if (npcHover && !avatarMode && !repairMode && !castMode && gatherHover == 0 && attackCmp <= -2 && !fetchHover && chatFrames != null && chatFrames.Length >= 2)
        { animFrames = chatFrames; animHot = chatHot; animLastFrame = -1; return; }
        animFrames = null;
        if (avatarMode) Set(avatarCur);
        else if (repairMode) Set(repairCur);
        else if (castMode) Set(castCur);
        else if (gatherHover == 1) Set(fishCur);
        else if (gatherHover == 2) Set(mineCur);
        else if (attackCmp > -2) Set(attackCmp > 0 ? atkHCur : attackCmp == -1 ? atkLCur : atkMCur);
        else if (fetchHover) Set(fetchCur);
        else Set(npcHover ? chatCur : normalCur);
    }

    public void SetGatherHover(int kind)
    {
        if (gatherHover == kind) return;
        if (kind != 0 && !gatherTried)
        {
            gatherTried = true;
            ResLoader loader = ResLoader.I;
            if (loader != null)
            {
                fishCur = ToCursorTex(loader.LoadGsaFrame("res/uires/cursor/Fish.gsa", 0));
                mineCur = ToCursorTex(loader.LoadGsaFrame("res/uires/cursor/Mine.gsa", 0));
            }
        }
        gatherHover = kind;
        Refresh();
    }

    // Official 0x060001c7, static UnityEngine.Texture2D CursorManager.ToCursorTex(UnityEngine.Texture2D).
    // arm64 0x358804/512 and x86_64 0x36a970/416 both return null through UnityEngine.Object.op_Equality
    // when the source is null, then read GetPixels32 before the width and the height, allocate a
    // Texture2D(width, height, RGBA32, false), copy those pixels with the one-argument SetPixels32, set
    // FilterMode.Point and Apply(false, false). The body is wrapped in a real try/catch: Both slices own
    // an LSDA (arm64 0x12f27a0, x86_64 0x12ed698) whose call-site records protect every call of that
    // sequence individually, and the landing pads (arm64 0x35897c, x86_64 0x36aa95) select C++ clause 1,
    // call __cxa_begin_catch, test the thrown object against the runtime default slot +0x10, i.e. the
    // System.Object of a bare catch, then __cxa_end_catch and return null. Every conversion failure is
    // therefore swallowed exactly once, at this member, and the borrowed source stays owned by its loader.
    private static Texture2D ToCursorTex(Texture2D source)
    {
        if (source == null) return null;
        try
        {
            Color32[] pixels = source.GetPixels32();
            int w = source.width, h = source.height;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.filterMode = FilterMode.Point;
            texture.Apply(false, false);
            return texture;
        }
        catch { return null; }
    }

    public void SetCastMode(bool on)
    {
        if (castMode == on)
        {
            return;
        }

        castMode = on;
        Refresh();
    }

}

}
