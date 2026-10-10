using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Direct source recovery of the official 1.3.4 PlayerNameTag owner.
/// </summary>
/// <remarks>
/// The official field/method owner and its LateUpdate/Rebuild/PlaceSharp/UpdateHpBar branch order
/// are retained. Configure and the Apply* methods are behavior-free independent-runtime adapters:
/// they only project the already authoritative CharData, SocialClient, StallConfigDlg and status
/// values into the recovered owner while the corresponding official global owners are restored.
/// </remarks>
public class PlayerNameTag : MonoBehaviour
{
    public float nameHeight = 1f;

    private const int SCALE = 1;

    private GameObject tagGo;
    private SpriteRenderer tagSr;
    private static Material mat;
    private ReferenceWorldLocalPlayerDoll _pc;
    private string lastName;
    private string lastTitle;
    private bool lastReady;
    private float lastH = -1f;
    private bool lastShowTitle = true;
    private bool lastWorking;
    private int lastWorkSec = -1;
    private string lastTeam = string.Empty;
    private bool lastStalling;
    private byte lastPk;
    private bool lastLeader;
    private static Camera sCam;
    private readonly HeadHpBarView hpBarView = new HeadHpBarView();

    // Independent-runtime data adapters. These hold official global-owner values only; all
    // presentation decisions remain in the recovered methods below.
    private ReferenceWorldRemoteFeedbackAssets feedbackAssets;
    private string charName = string.Empty;
    private string charTitle = string.Empty;
    private bool charShowTitle;
    private byte charPkState;
    private int charHp;
    private int charMaxHp;
    private bool charRiding;
    private int charMountBoundTopPx;
    private float charMountNameOffXU;
    private bool localStalling;
    private bool selfInParty;
    private string teamName = string.Empty;
    private int partyLeaderId;
    private int selfEntityId;
    private bool working;
    private int workSec;
    private string workNamePrefix;
    private bool configured;

    private ReferenceWorldLocalPlayerDoll PC
    {
        get { return _pc; }
    }

    internal GameObject TagObject => tagGo;
    internal SpriteRenderer TagRenderer => tagSr;
    internal HeadHpBarView HpBarView => hpBarView;

    internal bool Configure(
        ReferenceWorldLocalPlayerDoll pc,
        ReferenceWorldRemoteFeedbackAssets assets,
        string displayName,
        string title,
        int entityId,
        int hitPoints,
        int maximumHitPoints,
        bool nativeStartup = false)
    {
        // Font availability is owned by LateUpdate/Rebuild, including BakeNameTag's null
        // result. Binding a non-null font adapter must not turn that local rendering state
        // into failure of ReferenceFirstWorldInitialFrame.BuildForViewport.
        // Direct native startup has no SelfData receipt; its actual self id can remain zero.
        if (pc == null || !pc.Built || pc.Root == null || assets == null ||
            assets.Font == null ||
            string.IsNullOrEmpty(displayName) || (entityId == 0 && !nativeStartup))
        {
            return false;
        }

        _pc = pc;
        feedbackAssets = assets;
        charName = displayName;
        charTitle = title ?? string.Empty;
        selfEntityId = entityId;
        charHp = hitPoints;
        charMaxHp = maximumHitPoints;
        configured = true;
        return true;
    }

    internal bool ApplyAuthoritativeStatus(
        int hitPoints,
        int maximumHitPoints,
        byte? pkState)
    {
        if (!configured)
        {
            return false;
        }

        // Original UpdateHpBar admits signed CharData.maxHp. A nonpositive value
        // only hides the bar at its draw-time gate; it does not reject the world
        // through this status-observation adapter.
        charHp = hitPoints;
        charMaxHp = maximumHitPoints;
        if (pkState.HasValue)
        {
            charPkState = pkState.Value;
        }

        return true;
    }

    internal void ApplyReceivedNameForIndependentRuntime(string value)
    {
        charName = value;
    }

    /// <summary>
    /// Projects the official <c>CharData.title</c> owner (field token 67114201, the +0x30
    /// static Dispatch case 0x804c stores) into the head title. The value is stored exactly as
    /// the server sent it: the official case applies no empty-string filter, so an empty title
    /// is an empty title and <see cref="Rebuild"/> renders nothing for it.
    /// </summary>
    internal void ApplyReceivedTitleForIndependentRuntime(string value)
    {
        charTitle = value;
    }

    internal bool ApplyCharacterIdentity(
        string displayName,
        string title,
        bool showTitle)
    {
        if (!configured || displayName == null || title == null)
        {
            return false;
        }

        charName = displayName;
        charTitle = title;
        charShowTitle = showTitle;
        return true;
    }

    internal bool ApplySocialState(
        bool inParty,
        string currentTeamName,
        int leaderEntityId,
        bool stalling)
    {
        if (!configured || currentTeamName == null)
        {
            return false;
        }

        selfInParty = inParty;
        teamName = currentTeamName;
        partyLeaderId = leaderEntityId;
        localStalling = stalling;
        return true;
    }

    internal void BindNativeRidingForIndependentRuntime()
    {
        charRiding = CharData.riding;
        charMountBoundTopPx = CharData.mountBoundTopPx;
        charMountNameOffXU = CharData.mountNameOffXU;
    }

    internal bool ApplyRidingState(
        bool riding,
        int mountBoundTopPixels,
        float mountNameOffsetWorldUnits)
    {
        if (!configured || mountBoundTopPixels < 0 ||
            float.IsNaN(mountNameOffsetWorldUnits) ||
            float.IsInfinity(mountNameOffsetWorldUnits))
        {
            return false;
        }

        charRiding = riding;
        charMountBoundTopPx = mountBoundTopPixels;
        charMountNameOffXU = mountNameOffsetWorldUnits;
        return true;
    }

    internal bool ApplyWorkingState(
        bool value,
        int elapsedSeconds,
        string namePrefix)
    {
        if (!configured || elapsedSeconds < 0)
        {
            return false;
        }

        working = value;
        workSec = elapsedSeconds;
        workNamePrefix = namePrefix;
        return true;
    }

    private void LateUpdate()
    {
        if (!configured)
        {
            return;
        }

        if (string.IsNullOrEmpty(charName))
        {
            if (tagGo)
            {
                tagGo.SetActive(false);
            }

            hpBarView.Tick(transform, false, 0, 0, 0f, 0f);
            return;
        }

        PlayerController workPc = PC == null ? null : PC.SourceControllerForIndependentRuntime;
        bool currentWorking = workPc != null && workPc.Working;
        int currentWorkSec = currentWorking ? unchecked((int)workPc.WorkElapsed) : 0;
        bool stalling = localStalling;
        string currentTeam = selfInParty ? teamName : string.Empty;
        // Official PlayerNameTag.LateUpdate (0x06000a33, arm64 0x2c9580) tests the owner itself
        // against this cached copy - arm64 0x2c9980 ldrb w8,[x8,#0x248] / 0x2c9984
        // ldrb w9,[x19,#0x58] / 0x2c9988 cmp w8,w9 / b.ne to Rebuild - and Rebuild
        // (0x06000a37, arm64 0x2c9ea8 / 0x2c9eb0) stores the owner byte into the copy. Reading
        // the owner here keeps one source: the baked tag shows a title exactly when
        // CharData.showTitle says so, whose official initial value is true (CharData..cctor,
        // arm64 0x396174), and the change test below then does what the official test does.
        charShowTitle = CharData.showTitle;

        if (!tagSr || !tagSr.sprite ||
            !string.Equals(charName, lastName, StringComparison.Ordinal) ||
            !string.Equals(charTitle, lastTitle, StringComparison.Ordinal) ||
            feedbackAssets.Font.Ready != lastReady || charShowTitle != lastShowTitle ||
            currentWorking != lastWorking ||
            (currentWorking && currentWorkSec != lastWorkSec) ||
            !string.Equals(currentTeam, lastTeam, StringComparison.Ordinal) ||
            stalling != lastStalling || charPkState != lastPk ||
            IsPartyLeader() != lastLeader)
        {
            Rebuild();
        }

        lastStalling = stalling;
        lastWorking = currentWorking;
        lastWorkSec = currentWorkSec;
        if (!tagGo)
        {
            return;
        }

        tagGo.SetActive(true);
        // Official 0x06000a33 arm64 0x2c9ab0 tests the owner for liveness before reading
        // PlayerController.ppu at +0xd8 and falls back to the literal 100.0f only when that test
        // fails, so this placement segment divides by the live owner value instead of a constant.
        // The trailing native null-check at 0x2c9ab8 sits after the liveness test and is therefore
        // unreachable; the one test above carries the observable semantics.
        workPc = PC == null ? null : PC.SourceControllerForIndependentRuntime;
        float ppu = workPc != null ? workPc.ppu : 100f;
        float localY = nameHeight;
        float offX = 0f;
        if (charRiding && charMountBoundTopPx > 0)
        {
            localY = charMountBoundTopPx / ppu;
            offX = charMountNameOffXU;
        }
        else if (workPc != null && workPc.SittingForIndependentRuntime &&
                 workPc.SitNameTopPx > 0)
        {
            // Official LateUpdate evaluates the getter twice - the 0x06000a18 call at
            // arm64 0x2c9bd4 feeds the gate and the one at 0x2c9be8 feeds the height - so the
            // two reads are carried as two reads rather than folded into a local.
            localY = unchecked(workPc.SitNameTopPx + 3) / ppu;
        }

        PlaceSharp(tagGo.transform, localY + 0.04f, ppu, offX);
        UpdateHpBar(localY + 0.04f, offX, ppu, currentWorking, stalling);
    }

    // Original 1.5.8 shared placement; the remaining local-name owner is recovered separately.
    public static void PlaceSharp(
        Transform tag,
        float localY,
        float ppu,
        float offX = 0f)
    {
        if (!sCam || !sCam.isActiveAndEnabled)
        {
            sCam = Camera.main ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        }

        if (ppu > 0f && offX != 0f)
        {
            offX = Mathf.Round(offX * ppu) / ppu;
        }

        Vector3 ownerPosition = tag.parent ? tag.parent.position : Vector3.zero;
        tag.position = ownerPosition + new Vector3(offX, localY, 0f);

        if (sCam && sCam.orthographic && !(sCam.orthographicSize <= 0f))
        {
            float screenPixelsPerWorldUnit =
                Screen.height / (sCam.orthographicSize * 2f);
            float scale = screenPixelsPerWorldUnit > 0f
                ? ppu * UiScaleFactor() / screenPixelsPerWorldUnit
                : UiScaleFactor();
            tag.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            tag.localScale = Vector3.one * UiScaleFactor();
        }
    }

    private void UpdateHpBar(
        float anchorY,
        float offX,
        float ppu,
        bool currentWorking,
        bool stalling)
    {
        float hpY = anchorY + 29f / ppu;
        bool show = false;
        if (charMaxHp > 0 && charHp > 0 && !currentWorking && !stalling)
        {
            if (tagSr && tagSr.sprite)
            {
                hpY = Mathf.Max(
                    hpY,
                    anchorY + unchecked(tagSr.sprite.texture.height + 2) / ppu);
            }

            show = true;
        }

        hpBarView.Tick(
            transform,
            show,
            charHp,
            charMaxHp,
            hpY,
            offX);
    }

    private bool IsPartyLeader()
    {
        return partyLeaderId != 0 && partyLeaderId == selfEntityId;
    }

    private void Rebuild()
    {
        lastName = charName;
        lastTitle = charTitle;
        lastReady = feedbackAssets.Font.Ready;
        lastShowTitle = charShowTitle;

        string renderedName;
        string title;
        PlayerController workPc = PC == null ? null : PC.SourceControllerForIndependentRuntime;
        if (workPc != null && workPc.Working)
        {
            renderedName = workPc.WorkNamePrefix + CharData.name;
            int sec = unchecked((int)workPc.WorkElapsed);
            title = string.Format(
                "{0:00}:{1:00}",
                sec / 60,
                sec % 60);
        }
        else
        {
            renderedName = charName;
            title = charShowTitle && !localStalling
                ? charTitle ?? string.Empty
                : string.Empty;
        }

        lastTeam = selfInParty ? teamName : string.Empty;
        if (!string.IsNullOrEmpty(lastTeam))
        {
            renderedName = renderedName +
                           ReferenceWorldBitmapFont.TeamSeparator +
                           lastTeam;
        }

        lastPk = charPkState;
        Color nameColor = Color.white;
        if (lastPk == 2)
        {
            nameColor = new Color32(255, 64, 64, 255);
        }
        else if (lastPk == 1)
        {
            nameColor = new Color32(170, 170, 170, 255);
        }

        lastLeader = IsPartyLeader();
        Texture2D texture = feedbackAssets.Font.BakeNameTag(
            renderedName,
            title,
            nameColor,
            Color.white,
            SCALE,
            null,
            null,
            null,
            null,
            lastLeader ? feedbackAssets.LeaderFlag : null);
        if (!texture)
        {
            return;
        }

        workPc = PC == null ? null : PC.SourceControllerForIndependentRuntime;
        float ppu = workPc != null ? workPc.ppu : 100f;
        if (!tagGo)
        {
            tagGo = new GameObject("NameTag");
            tagGo.transform.SetParent(transform, false);
            tagGo.transform.localPosition = new Vector3(0f, nameHeight + 0.04f, 0f);
            lastH = nameHeight;
            tagSr = tagGo.AddComponent<SpriteRenderer>();
            tagSr.sortingOrder = 32000;
            if (!mat)
            {
                Shader shader = Shader.Find("Overlay/NameTag");
                if (shader)
                {
                    mat = new Material(shader);
                }
            }

            if (mat)
            {
                tagSr.sharedMaterial = mat;
            }
        }

        Sprite oldSprite = tagSr.sprite;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0f),
            ppu);
        tagSr.sprite = sprite;
        if (oldSprite)
        {
            Texture2D oldTexture = oldSprite.texture;
            DestroyOwned(oldSprite);
            if (oldTexture)
            {
                DestroyOwned(oldTexture);
            }
        }
    }

    private void OnDestroy()
    {
        if (tagSr && tagSr.sprite)
        {
            Texture2D texture = tagSr.sprite.texture;
            DestroyOwned(tagSr.sprite);
            if (texture)
            {
                DestroyOwned(texture);
            }
        }

        hpBarView.Dispose();
    }

    private static float UiScaleFactor()
    {
        return UIScale.Factor;
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
