using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class FittingRoomPreview : MonoBehaviour
{
    public static FittingRoomPreview I;
    private static readonly Vector3 ORIGIN = new Vector3(0f, 2000f, 0f);
    private static readonly int[] DirByOct = { 1, 0, 4, 0, 1, 2, 3, 2 };
    private static readonly bool[] FlipByOct = { true, true, false, false, false, false, false, true };
    public static int GlowOverride = -1;
    private const int PREVIEW_DIR = 4;
    private const float ppu = 100f;
    private int previewOct = 2;
    private Camera cam;
    private RenderTexture rt;
    private GameObject actorRoot;
    private SpriteRenderer[] dollSr;
    private SpriteRenderer baseSr;
    private Material _defMat;
    private PlayerAvatar avatar;
    private ActorEffects actorFx;
    private int lastG = -1, lastHead = -1, lastHair = -1;
    private PlayerAvatar.Appear lastAp;
    private GameObject petGo;
    private SpriteRenderer petSr, petGlowSr;
    private int lastPet = -1, mode = -1;
    private bool previewSit;
    private bool independentReleased;
    private Sprite[,] petGrid, petGlowGrid;
    private int petDirs = 1, petDir;

    public int CurDir => DirByOct[previewOct];
    public bool CurFlip => FlipByOct[previewOct];
    public bool Sitting => previewSit;
    public void SetSit(bool sit) => previewSit = sit;
    public RenderTexture RT { get { EnsureCam(); return rt; } }

    public static FittingRoomPreview Ensure()
    {
        if (I == null)
        {
            var go = new GameObject("FittingRoomPreview");
            I = go.AddComponent<FittingRoomPreview>();
            DontDestroyOnLoad(go);
        }
        return I;
    }

    public void Rotate(int delta)
    {
        if (mode == 1)
        {
            if (petDirs > 0)
            {
                petDir = (petDir + delta + petDirs) % petDirs;
                ApplyPetFrame();
            }
        }
        else previewOct = (previewOct + delta + 8) % 8;
    }

    private static Material GlowMat() => EffectPlayer.GlowLayerMat(GlowOverride >= 0 ? GlowOverride : CharData.glowMode);
    private static Material GlowMat(int glowModel) => EffectPlayer.GlowLayerMat(
        GlowOverride >= 0 ? GlowOverride : CharData.glowMode, SuitFxDB.IsGlowPremulModel(glowModel));

    private void EnsureCam()
    {
        if (cam != null) return;
        rt = new RenderTexture(384, 540, 16) { name = "FittingRT", antiAliasing = 1 };
        rt.Create();
        var go = new GameObject("FittingPreviewCam");
        go.transform.SetParent(transform, false);
        cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.06f, .06f, .09f, 1f);
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        cam.orthographicSize = 1.2f;
        cam.transform.position = ORIGIN + new Vector3(0f, .9f, -10f);
        cam.targetTexture = rt;
        cam.enabled = false;
        go = new GameObject("_matsrc");
        go.transform.SetParent(transform, false);
        SpriteRenderer source = go.AddComponent<SpriteRenderer>();
        source.enabled = false;
        _defMat = source.sharedMaterial;
    }

    // The independent world owns this preview and its generated resources. Package
    // textures and shared effect materials remain with their existing owners.
    internal void ReleaseForIndependentRuntime()
    {
        if (independentReleased) return;
        independentReleased = true;
        Hide();
        enabled = false;
        avatar?.Dispose();
        avatar = null;
        DestroyPetGrids();
        if (cam != null) cam.targetTexture = null;
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
            rt = null;
        }
        if (I == this) I = null;
        Destroy(gameObject);
    }

    public void Hide()
    {
        mode = -1;
        GlowOverride = -1;
        if (cam != null) cam.enabled = false;
        if (actorRoot != null) actorRoot.SetActive(false);
        if (petGo != null) petGo.SetActive(false);
    }
}
}
