using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned subset of GameSettings consumed by RemotePlayer.UpdateNameTag/UpdateHpBar.
/// </summary>
internal sealed class ReferenceWorldPlayerFeedbackSettings
{
    internal const string ShowPlayerNamesKey = "opt_pname";
    internal const string ShowMonsterNamesKey = "opt_mname";
    internal const string ShowTeamNamesKey = "opt_teamname";
    internal const string HeadHpBarKey = "opt_hpbar";

    public ReferenceWorldPlayerFeedbackSettings(
        bool showPlayerNames,
        bool showMonsterNames,
        bool showTeamNames,
        bool headHpBarOn)
    {
        ShowPlayerNames = showPlayerNames;
        ShowMonsterNames = showMonsterNames;
        ShowTeamNames = showTeamNames;
        HeadHpBarOn = headHpBarOn;
    }

    public bool ShowPlayerNames { get; }
    public bool ShowMonsterNames { get; }
    public bool ShowTeamNames { get; }
    public bool HeadHpBarOn { get; }

    public static ReferenceWorldPlayerFeedbackSettings Load()
    {
        // GameSettings..cctor defaults all name flags to true and the HP flag to false.
        return new ReferenceWorldPlayerFeedbackSettings(
            PlayerPrefs.GetInt(ShowPlayerNamesKey, 1) != 0,
            PlayerPrefs.GetInt(ShowMonsterNamesKey, 1) != 0,
            PlayerPrefs.GetInt(ShowTeamNamesKey, 1) != 0,
            PlayerPrefs.GetInt(HeadHpBarKey, 0) != 0);
    }
}

/// <summary>
/// Instance-shaped access retained for existing presentation and Editor consumers. Font data,
/// loading and all text decisions belong to the original static BitmapFont owner.
/// </summary>
internal sealed class ReferenceWorldBitmapFont
{
    public const string AtlasPath = "font/gbk12.png";
    public const string IndexPath = "font/gbk12.idx";
    public const string WidthPath = "font/gbk12.wid";
    public const int CellPixels = 12;
    public const char TeamSeparator = '\uff5c';

    private ReferenceWorldBitmapFont() { }
    public bool Ready => BitmapFont.Ready;

    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceWorldBitmapFont font)
    {
        font = null;
        if (resources == null) return false;
        BitmapFont.EnsureForIndependentRuntime();
        // The adapter remains available while the static font is unready. Each native
        // consumer owns its Ready branch; construction must not turn it into world failure.
        font = new ReferenceWorldBitmapFont();
        return true;
    }

    public int CellOf(char value) => BitmapFont.CellOfForIndependentRuntime(value);
    public float Advance(int cell) => BitmapFont.AdvForIndependentRuntime(cell);
    public float Measure(string value, float scale) => BitmapFont.Measure(value, scale);
    public void Draw(float x, float y, string value, Color color, float scale) =>
        BitmapFont.Draw(x, y, value, color, scale);
    public void DrawOutline(float x, float y, string value, Color fill, float scale) =>
        BitmapFont.DrawOutline(x, y, value, fill, scale);
    public void CenterOutline(Rect bounds, string value, Color fill, float scale) =>
        BitmapFont.CenterOutline(bounds, value, fill, scale);
    public void CenterOutlineBold(Rect bounds, string value, Color fill, float scale) =>
        BitmapFont.CenterOutlineBold(bounds, value, fill, scale);
    public string[] Wrap(string value, float maximumWidth, float scale) =>
        BitmapFont.Wrap(value, maximumWidth, scale).ToArray();
    public string Elide(string value, float maximumWidth, float scale) =>
        BitmapFont.Elide(value, maximumWidth, scale);
    public Texture2D BakeNameTag(string displayName, string title, Color nameColor, Color titleColor,
        int scale, string[] topLines, Color? topColor, string bottomLine, Color? bottomColor, Texture2D nameIcon) =>
        BitmapFont.BakeNameTag(displayName, title, nameColor, titleColor, scale,
            topLines, topColor, bottomLine, bottomColor, nameIcon);
}

/// <summary>
/// Shared immutable resources for all recovered remote-player feedback views.
/// </summary>
internal sealed class ReferenceWorldRemoteFeedbackAssets : IDisposable
{
    public const string LeaderFlagPath = "res/effect/paopao_tleaderflag.gsa";

    private readonly ReferenceResourceLoader resources;
    private ReferenceWorldStallSignAssets stallSign;
    private Material nameTagMaterial;
    private bool disposed;

    private ReferenceWorldRemoteFeedbackAssets(
        ReferenceResourceLoader resources,
        ReferenceWorldBitmapFont font,
        ReferenceWorldHeadHpBarAssets headHpBar,
        ReferenceWorldChatBubbleAssets chatBubble)
    {
        this.resources = resources;
        Font = font;
        HeadHpBar = headHpBar;
        ChatBubble = chatBubble;
    }

    public ReferenceWorldBitmapFont Font { get; }
    public Texture2D LeaderFlag => global::FakeFantasyOnline.Presentation.ReferenceAssets.LeaderFlag.Tex;
    public ReferenceWorldHeadHpBarAssets HeadHpBar { get; }
    // The retained compatibility view is the only borrower of these sprites. Live
    // local and remote signs use StallSign.Tick and its own static texture lifetime.
    public ReferenceWorldStallSignAssets StallSign
    {
        get
        {
            if (!disposed && stallSign == null)
            {
                ReferenceWorldStallSignAssets.TryCreate(resources, out stallSign);
            }

            return stallSign;
        }
    }
    public ReferenceWorldChatBubbleAssets ChatBubble { get; }
    // Only the retained compatibility views borrow this material. Live PlayerNameTag,
    // RemotePlayer, MonsterEntity and StallSign keep their own native lazy owners.
    // In particular, creating the feedback adapters must not require a name shader.
    public Material NameTagMaterial
    {
        get
        {
            if (!disposed && !nameTagMaterial)
            {
                Shader shader = Shader.Find("Overlay/NameTag");
                if (shader)
                {
                    nameTagMaterial = new Material(shader)
                    {
                        name = "RecoveredRemoteNameTagMaterial",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }

            return nameTagMaterial;
        }
    }

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceWorldRemoteFeedbackAssets assets)
    {
        assets = null;
        ReferenceWorldBitmapFont font;
        ReferenceWorldHeadHpBarAssets hp = null;
        ReferenceWorldChatBubbleAssets chatBubble;
        if (!ReferenceWorldBitmapFont.TryCreate(resources, out font) ||
            !ReferenceWorldHeadHpBarAssets.TryCreate(resources, out hp))
        {
            return false;
        }

        if (!ReferenceWorldChatBubbleAssets.TryCreate(resources, out chatBubble))
        {
            hp.Dispose();
            return false;
        }

        assets = new ReferenceWorldRemoteFeedbackAssets(
            resources,
            font,
            hp,
            chatBubble);
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        HeadHpBar.Dispose();
        stallSign?.Dispose();
        stallSign = null;
        DestroyOwned(nameTagMaterial);
        nameTagMaterial = null;
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

/// <summary>
/// Disposable source translation of RemotePlayer.UpdateNameTag.
/// </summary>
internal sealed class ReferenceWorldRemoteNameTagView : IDisposable
{
    public const int SortingOrder = 32000;

    private readonly ReferenceWorldRemoteFeedbackAssets assets;
    private GameObject root;
    private SpriteRenderer renderer;
    private Sprite sprite;
    private Texture2D texture;
    private string lastName;
    private string lastTitle;
    private string lastTeam;
    private byte lastPk;
    private bool lastLeader;
    private bool lastReady;
    private bool disposed;

    public ReferenceWorldRemoteNameTagView(ReferenceWorldRemoteFeedbackAssets assets)
    {
        this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public GameObject Root => root;
    public SpriteRenderer Renderer => renderer;
    public Texture2D Texture => texture;

    public bool TryTick(
        Transform owner,
        bool show,
        string displayName,
        string title,
        string teamName,
        byte pkState,
        bool leader,
        float localY,
        float offX,
        Camera worldCamera,
        float uiScale,
        int screenHeight)
    {
        if (disposed || owner == null)
        {
            return false;
        }

        bool ready = assets.Font.Ready;
        if (!show || string.IsNullOrEmpty(displayName) || !ready)
        {
            if (root && root.activeSelf)
            {
                root.SetActive(false);
            }

            lastReady = ready;
            return true;
        }

        string resolvedTitle = title ?? string.Empty;
        string resolvedTeam = teamName ?? string.Empty;
        string renderedName = string.IsNullOrEmpty(resolvedTeam)
            ? displayName
            : displayName + ReferenceWorldBitmapFont.TeamSeparator + resolvedTeam;
        bool changed = !root || lastName != displayName || lastTitle != resolvedTitle ||
                       lastTeam != resolvedTeam || lastPk != pkState ||
                       lastLeader != leader || lastReady != ready;
        if (changed && !TryRebuild(
                owner,
                renderedName,
                resolvedTitle,
                pkState,
                leader))
        {
            return false;
        }

        lastName = displayName;
        lastTitle = resolvedTitle;
        lastTeam = resolvedTeam;
        lastPk = pkState;
        lastLeader = leader;
        lastReady = ready;
        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        PlaceSharp(root.transform, localY, offX, worldCamera, uiScale, screenHeight);
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        DestroyOwned(root);
        DestroyOwned(sprite);
        DestroyOwned(texture);
        root = null;
        renderer = null;
        sprite = null;
        texture = null;
    }

    private bool TryRebuild(
        Transform owner,
        string renderedName,
        string title,
        byte pkState,
        bool leader)
    {
        Color nameColor = Color.white;
        if (pkState == 1)
        {
            nameColor = new Color32(170, 170, 170, 255);
        }
        else if (pkState == 2)
        {
            nameColor = new Color32(255, 64, 64, 255);
        }

        Texture2D nextTexture = assets.Font.BakeNameTag(
            renderedName,
            title,
            nameColor,
            Color.white,
            1,
            null,
            null,
            null,
            null,
            leader ? assets.LeaderFlag : null);
        if (!nextTexture)
        {
            return false;
        }

        Sprite nextSprite = Sprite.Create(
            nextTexture,
            new Rect(0f, 0f, nextTexture.width, nextTexture.height),
            new Vector2(0.5f, 0f),
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        if (!nextSprite)
        {
            DestroyOwned(nextTexture);
            return false;
        }

        nextSprite.name = "RecoveredRemoteNameTagSprite";
        if (!root)
        {
            root = new GameObject("RecoveredRemoteNameTag");
            root.transform.SetParent(owner, false);
            renderer = root.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortingOrder;
            Material material = assets.NameTagMaterial;
            if (material)
            {
                renderer.sharedMaterial = material;
            }
        }

        Sprite oldSprite = sprite;
        Texture2D oldTexture = texture;
        sprite = nextSprite;
        texture = nextTexture;
        renderer.sprite = sprite;
        DestroyOwned(oldSprite);
        DestroyOwned(oldTexture);
        return true;
    }

    private static void PlaceSharp(
        Transform tag,
        float localY,
        float offX,
        Camera worldCamera,
        float uiScale,
        int screenHeight)
    {
        PlayerNameTag.PlaceSharp(tag, localY, ReferenceWorldLocalPlayerDoll.PixelsPerUnit, offX);
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

}
