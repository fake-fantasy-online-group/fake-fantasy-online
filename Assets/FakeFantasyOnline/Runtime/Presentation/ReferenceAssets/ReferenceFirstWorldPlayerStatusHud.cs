using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Immutable CharData subset written by entry and the locked status packet handlers.
/// </summary>
public sealed class ReferenceFirstWorldPlayerStatusSnapshot
{
    private ReferenceFirstWorldPlayerStatusSnapshot(
        string displayName,
        WorldPlayerStatusState status,
        long karma,
        int pkState)
    {
        DisplayName = displayName ?? string.Empty;
        Level = status.Level;
        Experience = status.Experience;
        ExperienceToNext = status.ExperienceToNext;
        Hp = status.HitPoints;
        MaxHp = status.MaximumHitPoints;
        Mp = status.MagicPoints;
        MaxMp = status.MaximumMagicPoints;
        AttributeCount = status.Attributes.Count;

        // Stamina has a distinct packet owner outside the locked attribute branch.
        Stamina = 0;
        MaxStamina = 0;
        Nianli = status.Nianli;
        MaxNianli = status.MaximumNianli;
        Naili = status.Naili;
        MaxNaili = status.MaximumNaili;
        Karma = karma;
        PkState = pkState;

        PetModel = status.Pet.Model;
        PetDisplayName = status.Pet.DisplayName;
        PetLevel = status.Pet.Level;
        PetHp = status.Pet.HitPoints;
        PetMaxHp = status.Pet.MaximumHitPoints;
        PetMp = status.Pet.MagicPoints;
        PetMaxMp = status.Pet.MaximumMagicPoints;
        HasAttributeSnapshot = status.HasAttributeSnapshot;
        HasPetSnapshot = status.HasPetSnapshot;
    }

    public string DisplayName { get; }
    public int Level { get; }
    public int Experience { get; }
    public int ExperienceToNext { get; }
    public int Hp { get; }
    public int MaxHp { get; }
    public int Mp { get; }
    public int MaxMp { get; }
    public int AttributeCount { get; }
    public int Stamina { get; }
    public int MaxStamina { get; }
    public int Nianli { get; }
    public int MaxNianli { get; }
    public int Naili { get; }
    public int MaxNaili { get; }
    public long Karma { get; }
    public int PkState { get; }
    public int PetModel { get; }
    public string PetDisplayName { get; }
    public int PetLevel { get; }
    public int PetHp { get; }
    public int PetMaxHp { get; }
    public int PetMp { get; }
    public int PetMaxMp { get; }
    public bool HasAttributeSnapshot { get; }
    public bool HasPetSnapshot { get; }

    public static ReferenceFirstWorldPlayerStatusSnapshot From(WorldEntryRecord entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        return new ReferenceFirstWorldPlayerStatusSnapshot(
            entry.DisplayName,
            WorldPlayerStatusState.FromEntry(entry),
            0L,
            0);
    }

    public ReferenceFirstWorldPlayerStatusSnapshot WithPlayerStatus(
        WorldPlayerStatusState status)
    {
        if (status == null)
        {
            throw new ArgumentNullException(nameof(status));
        }

        return new ReferenceFirstWorldPlayerStatusSnapshot(
            DisplayName,
            status,
            Karma,
            PkState);
    }

    public ReferenceFirstWorldPlayerStatusSnapshot WithSelfPkState(
        WorldPlayerStatusState status,
        WorldSelfPkState pkState)
    {
        if (status == null)
        {
            throw new ArgumentNullException(nameof(status));
        }

        if (pkState == null)
        {
            throw new ArgumentNullException(nameof(pkState));
        }

        return new ReferenceFirstWorldPlayerStatusSnapshot(
            DisplayName,
            status,
            pkState.Karma,
            pkState.State);
    }
}

/// <summary>
/// Source translation of the initial player/pet status cluster and experience branch in
/// GameHud.OnGUI.
/// </summary>
/// <remarks>
/// Runtime drawing reads the original CharData and GameHud state. Snapshot is retained for
/// observations; resource preparation and pet-head cache publication occur in the GUI body.
/// </remarks>
public sealed class ReferenceFirstWorldPlayerStatusHud : IDisposable
{
    public const string UiScalePreferenceKey = "ui_scale";
    public const string EmptyPetIconPath = "res/peticon/M000.gsa";
    public const string EmptyPetLabel = "暂时没有宠物";
    public const string ExperienceTooltipFormat =
        "经验 {0:N0} / {1:N0}  ({2:0.0}%)  还需 {3:N0}";
    public const bool ShowExperienceByDefault = true;

    private static readonly Color LabelGold =
        new Color(0.9764706f, 0.7960784f, 0.1686275f, 1f);

    private static readonly Color ExperienceTooltipColor =
        new Color(0.92156863f, 0.88235295f, 0.70588237f, 1f);

    private readonly ReferenceFirstWorldHudResources resources;
    private readonly ReferenceWorldBitmapFont font;
    private readonly ReferenceResourceLoader resourceLoader;
    private bool disposed;
    private GameHud hudOwner;
    internal void BindHudOwnerForIndependentRuntime(GameHud owner) => hudOwner = owner;

    private ReferenceFirstWorldPlayerStatusHud(
        ReferenceFirstWorldHudResources resources,
        ReferenceWorldBitmapFont font,
        ReferenceResourceLoader resourceLoader,
        ReferenceFirstWorldPlayerStatusSnapshot snapshot,
        Texture2D petHeadSource,
        string petHeadPath)
    {
        this.resources = resources;
        this.font = font;
        this.resourceLoader = resourceLoader;
        Snapshot = snapshot;
        PetHeadSource = petHeadSource;
        PetHeadPath = petHeadPath;
        Ready = true;
    }

    public bool Ready { get; private set; }
    public ReferenceFirstWorldPlayerStatusSnapshot Snapshot { get; private set; }
    public Texture2D PetHeadSource { get; private set; }
    public Texture2D RoundedPetHead => resources.OwnerForIndependentRuntime._roundDst;
    public string PetHeadPath { get; private set; }

    public static bool TryCreate(
        ReferenceResourceLoader resourceLoader,
        ReferenceFirstWorldHudResources resources,
        WorldEntryRecord entry,
        out ReferenceFirstWorldPlayerStatusHud owner)
    {
        owner = null;
        if (resourceLoader == null || resources == null || !resources.Ready || entry == null)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(resourceLoader, out font) || font == null)
        {
            return false;
        }

        ReferenceFirstWorldPlayerStatusSnapshot snapshot =
            ReferenceFirstWorldPlayerStatusSnapshot.From(entry);
        string petHeadPath = PetIconPath(snapshot.PetModel);
        var candidate = new ReferenceFirstWorldPlayerStatusHud(
            resources,
            font,
            resourceLoader,
            snapshot,
            null,
            petHeadPath);
        owner = candidate;
        return true;
    }

    /// <summary>
    /// Applies one immutable stat/pet observation. Native field reads and pet-icon
    /// preparation belong to the GUI body, independently of this observation.
    /// </summary>
    public bool ApplyAuthoritativeStatus(WorldPlayerStatusState status)
    {
        if (!Ready || disposed || status == null)
        {
            return false;
        }

        // This is an observer only. Native draw reads CharData and owns lazy pet art;
        // an unavailable portrait must not hold back an unrelated stat projection.
        Snapshot = Snapshot.WithPlayerStatus(status);
        PetHeadPath = PetIconPath(status.Pet.Model);
        return true;
    }

    public bool ApplyAuthoritativeSelfPkState(
        WorldPlayerStatusState status,
        WorldSelfPkState pkState)
    {
        if (!Ready || disposed || status == null || pkState == null)
        {
            return false;
        }

        Snapshot = Snapshot.WithSelfPkState(status, pkState);
        return true;
    }

    /// <summary>
    /// Draws the recovered cluster in original screen coordinates.
    /// </summary>
    public bool Draw(float elapsedSeconds)
    {
        bool ignored;
        return Draw(elapsedSeconds, out ignored);
    }

    public bool Draw(float elapsedSeconds, out bool petWindowToggle)
        => DrawSlices(elapsedSeconds, true, true, out petWindowToggle);

    internal bool DrawAvatarForIndependentRuntime(float elapsedSeconds, int screenHeight)
        => DrawSlices(elapsedSeconds, true, false, out _, null, screenHeight);

    internal bool DrawExperienceForIndependentRuntime(float elapsedSeconds, int screenWidth, int screenHeight)
        => DrawSlices(elapsedSeconds, false, true, out _, screenWidth, screenHeight);

    private bool DrawSlices(float elapsedSeconds, bool drawAvatar, bool drawExperience,
        out bool petWindowToggle, int? viewportWidth = null, int? viewportHeight = null)
    {
        petWindowToggle = false;
        if (!Ready || disposed || (viewportHeight ?? Screen.height) < 1 ||
            float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
        {
            return false;
        }

        float scale = hudOwner != null ? hudOwner.s : UIScale.Factor;
        // These are stages of one native OnGUI call: preserve incoming GUI state and
        // each reached leaf's own normal/exceptional restoration behavior.
        // The manual HUD entry already ran the native Load/scale/EnsureStyles prefix.
        if (drawAvatar && (hudOwner == null || hudOwner.showAvatar))
            DrawCluster(scale, elapsedSeconds, out petWindowToggle);
        if (drawExperience && (hudOwner == null || hudOwner.showExp))
            DrawExperience(scale, viewportWidth, viewportHeight);

        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // Rounded pet art follows the original GameHud receiver across borrower lifetimes.
        Ready = false;
        disposed = true;
    }

    public static float ProgressFraction(int current, int maximum)
    {
        if (maximum < 1)
        {
            return 1f;
        }

        return Mathf.Clamp01((float)current / maximum);
    }

    public static int ProgressFrame(int current, int maximum)
    {
        if (maximum < 1)
        {
            return 2;
        }

        return Mathf.Clamp((int)(((float)current * 3f) / maximum), 0, 2);
    }

    public static float ExperienceFraction(int current, int experienceToNext)
    {
        int safeMaximum = Mathf.Max(experienceToNext, 1);
        return Mathf.Clamp01((float)current / safeMaximum);
    }

    public static int ExperienceFrame(int current, int experienceToNext)
    {
        // 06000414 multiplies the clamped floating fraction, not the integer total.
        int frame = (int)(ExperienceFraction(current, experienceToNext) * 3f);
        return Mathf.Clamp(frame, 0, 2);
    }

    public static int ExperienceRemaining(int current, int experienceToNext)
    {
        int safeMaximum = Mathf.Max(experienceToNext, 1);
        return Mathf.Max(unchecked(safeMaximum - current), 0);
    }

    public static Rect ExperienceBackgroundRect(
        int screenWidth,
        int screenHeight,
        float scale)
    {
        return new Rect(
            0f,
            screenHeight - 6f * scale,
            screenWidth,
            6f * scale);
    }

    public static Rect ExperienceHoverRect(
        int screenWidth,
        int screenHeight,
        float scale)
    {
        return new Rect(
            0f,
            screenHeight - 14f * scale,
            screenWidth,
            14f * scale);
    }

    public static Rect ExperienceTooltipRect(
        float mouseX,
        float barY,
        int screenWidth,
        float scale)
    {
        float width = 260f * scale;
        return new Rect(
            Mathf.Clamp(mouseX - 130f * scale, 0f, screenWidth - width),
            barY - 14f * scale - 3f * scale,
            width,
            14f * scale);
    }

    public static string ExperienceTooltip(int current, int experienceToNext)
    {
        int safeMaximum = Mathf.Max(experienceToNext, 1);
        if (current > safeMaximum)
        {
            return string.Format(
                "经验 {0:N0}  (卡级积攒中, 升一级需 {1:N0})  解除卡级后一次性结算",
                current, safeMaximum);
        }
        float fraction = ExperienceFraction(current, experienceToNext);
        return string.Format(
            CultureInfo.CurrentCulture,
            ExperienceTooltipFormat,
            current,
            safeMaximum,
            fraction * 100f,
            ExperienceRemaining(current, experienceToNext));
    }

    private void DrawExperience(float scale, int? viewportWidth, int? viewportHeight)
    {
        Texture2D background = resources.ExperienceBackground;
        if (!ShowExperienceByDefault || !background)
        {
            return;
        }

        int screenWidth = viewportWidth ?? Screen.width;
        int screenHeight = viewportHeight ?? Screen.height;
        float backgroundScale = hudOwner != null ? hudOwner.s : scale;
        Rect full = ExperienceBackgroundRect(screenWidth, screenHeight, backgroundScale);
        GUI.DrawTexture(full, background);

        float fraction = ExperienceFraction(
            CharData.exp,
            CharData.expToNext);
        int frame = ExperienceFrame(
            CharData.exp,
            CharData.expToNext);
        Texture2D fill = resources.ExperienceFillFrames[frame];
        if (fraction > 0f && fill)
        {
            GUI.DrawTextureWithTexCoords(
                new Rect(full.x, full.y, full.width * fraction, full.height),
                fill,
                new Rect(0f, 0f, fraction, 1f));
        }

        float hoverScale = hudOwner != null ? hudOwner.s : scale;
        if (!font.Ready)
        {
            return;
        }

        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        Vector2 mouse = currentEvent.mousePosition;
        Rect hover = ExperienceHoverRect(screenWidth, screenHeight, hoverScale);
        if (!hover.Contains(mouse))
        {
            return;
        }

        string tooltipText = ExperienceTooltip(CharData.exp, CharData.expToNext);
        float tooltipScale = hudOwner != null ? hudOwner.s : scale;
        float tooltipWidth = 260f * tooltipScale;
        float tooltipHeight = 14f * tooltipScale;
        float tooltipMouseX = Event.current.mousePosition.x;
        float tooltipX = Mathf.Clamp(tooltipMouseX - tooltipWidth * 0.5f, 0f, screenWidth - tooltipWidth);
        float spacingScale = hudOwner != null ? hudOwner.s : scale;
        Rect tooltip = new Rect(tooltipX, full.y - tooltipHeight - 3f * spacingScale,
            tooltipWidth, tooltipHeight);
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72000003f);
        GUI.DrawTexture(tooltip, Texture2D.whiteTexture);
        GUI.color = previousColor;
        font.CenterOutline(
            tooltip,
            tooltipText,
            ExperienceTooltipColor,
            hudOwner != null ? hudOwner.s : scale);
    }

    private void DrawCluster(float scale, float elapsedSeconds, out bool petWindowToggle)
    {
        petWindowToggle = false;
        // Native T (06000456): keep pixel coordinates and read the owner scale at the call.
        Texture2D statusBackground = resources.StatusBarBackground;
        TexScale.Draw(0f, 0f, statusBackground, 0f, 0f,
            hudOwner != null ? hudOwner.s : scale);
        if (resources.SocketFill != null)
        {
            float socketScale = hudOwner != null ? hudOwner.s : scale;
            GUI.DrawTexture(ScaleRect(20f, 10f, 56f, 56f, socketScale), resources.SocketFill);
        }
        resources.SyncHeadPortraitVersion(resourceLoader);
        if (resources.HeadPortrait != null)
        {
            float portraitScale = hudOwner != null ? hudOwner.s : scale;
            GUI.DrawTexture(ScaleRect(26f, 15f, 45f, 45f, portraitScale), resources.HeadPortrait);
        }
        // 06000414: the avatar button follows the portrait and precedes both rings.
        float avatarButtonScale = hudOwner != null ? hudOwner.s : scale;
        if (GUI.Button(ScaleRect(26f, 15f, 45f, 45f, avatarButtonScale), GUIContent.none, GUIStyle.none))
            EquipDlg.Toggle();

        if (resources.NailiRing != null)
        {
            Texture2D ring = resources.NailiRing;
            float positionScale = hudOwner != null ? hudOwner.s : scale;
            float width = GsaTextureScale.Width(ring);
            float height = GsaTextureScale.Height(resources.NailiRing);
            float sizeScale = hudOwner != null ? hudOwner.s : scale;
            Rect full = new Rect(34f * positionScale, 8f * positionScale,
                width * sizeScale, height * sizeScale);
            resources.OwnerForIndependentRuntime.DrawRing(ring, full, ProgressFraction(CharData.naili, CharData.maxNaili),
                new Vector2(14.5f, 29.5f), 1);
        }
        if (resources.LianliRing != null)
        {
            Texture2D ring = resources.LianliRing;
            float positionScale = hudOwner != null ? hudOwner.s : scale;
            float width = GsaTextureScale.Width(ring);
            float height = GsaTextureScale.Height(resources.LianliRing);
            float sizeScale = hudOwner != null ? hudOwner.s : scale;
            Rect full = new Rect(19f * positionScale, 11f * positionScale,
                width * sizeScale, height * sizeScale);
            resources.OwnerForIndependentRuntime.DrawRing(ring, full, ProgressFraction(CharData.nianli, CharData.maxNianli),
                new Vector2(29.5f, 26.5f), 2);
        }

        // 06000414 HP slice: ARM 42a3b0-42a904, x86 43fbc0-4400c8 (delayed
        // fall at 44119c-4411f2). Keep the marker on the native HUD receiver and read
        // current CharData, including the second deltaTime read after the delay expires.
        int hpFrame = CharData.maxHp < 1 ? 2 : Mathf.Clamp(
            Mathf.FloorToInt((float)CharData.hp * 3f / CharData.maxHp), 0, 2);
        Texture2D hpCover = hudOwner.hpFill3 != null && hudOwner.hpFill3[hpFrame] != null
            ? hudOwner.hpFill3[hpFrame] : hudOwner.hpFill;
        float hpScale = hudOwner.s;
        if (Event.current.type == EventType.Repaint)
        {
            if (hudOwner.hpAimVal < 0f) hudOwner.hpAimVal = CharData.hp;
            if (hudOwner.hpAimVal <= CharData.hp)
            {
                hudOwner.hpAimVal = CharData.hp;
                hudOwner.hpAimWaitT = 0f;
            }
            else
            {
                hudOwner.hpAimWaitT += Time.deltaTime;
                if (hudOwner.hpAimWaitT > 0.3f)
                {
                    float previousAim = hudOwner.hpAimVal;
                    int currentHp = CharData.hp;
                    int maximumHp = CharData.maxHp;
                    hudOwner.hpAimVal = Mathf.Max(currentHp,
                        previousAim + Time.deltaTime * maximumHp * -1.2f);
                }
            }
        }

        float hpFraction = CharData.maxHp < 1 ? 1f : Mathf.Clamp01((float)CharData.hp / CharData.maxHp);
        float aimFraction = CharData.maxHp < 1 ? 1f : Mathf.Clamp01(hudOwner.hpAimVal / CharData.maxHp);
        DrawProgress(
            75f,
            25f,
            96f,
            12f,
            CharData.hp,
            CharData.maxHp,
            resources.HpBackground,
            hpCover,
            hpScale);
        if (aimFraction > hpFraction + 0.002f)
        {
            float left = 75f * hpScale + 96f * hpScale * hpFraction;
            float right = 75f * hpScale + 96f * hpScale * aimFraction;
            Rect trail = new Rect(left, 25f * hpScale, right - left, 12f * hpScale);
            if (hudOwner.hpAimer != null)
            {
                GUI.DrawTexture(trail, hudOwner.hpAimer);
            }
            else
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.DrawTexture(trail, Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        float mpScale = hudOwner.s;
        DrawProgress(
            75f,
            36f,
            96f,
            12f,
            CharData.mp,
            CharData.maxMp,
            resources.MpBackground,
            resources.MpFill,
            mpScale);

        // Original exclusive hover chain, after both bars and before the name effect.
        // The ring regions use the logical GSA dimensions, even when their fill is empty.
        Vector2 statusPointer = Event.current.mousePosition;
        if (ScaleRect(75f, 25f, 96f, 12f, hpScale).Contains(statusPointer))
        {
            UITip.Show(string.Format("气血 {0}/{1}", CharData.hp, CharData.maxHp), hudOwner.s);
        }
        else if (ScaleRect(75f, 36f, 96f, 12f, mpScale).Contains(statusPointer))
        {
            UITip.Show(string.Format("魔法 {0}/{1}", CharData.mp, CharData.maxMp), hudOwner.s);
        }
        else
        {
            bool nailiHovered = false;
            if (hudOwner.nailiRing != null)
            {
                float positionScale = hudOwner.s;
                float width = GsaTextureScale.Width(hudOwner.nailiRing);
                float height = GsaTextureScale.Height(hudOwner.nailiRing);
                float sizeScale = hudOwner.s;
                nailiHovered = new Rect(34f * positionScale, 8f * positionScale,
                    width * sizeScale, height * sizeScale).Contains(statusPointer);
                if (nailiHovered)
                {
                    UITip.Show(string.Format("耐力 {0}/{1}(打工消耗, 每日重置)",
                        CharData.naili, CharData.maxNaili), hudOwner.s);
                }
            }
            if (!nailiHovered && hudOwner.lianliRing != null)
            {
                float positionScale = hudOwner.s;
                float width = GsaTextureScale.Width(hudOwner.lianliRing);
                float height = GsaTextureScale.Height(hudOwner.lianliRing);
                float sizeScale = hudOwner.s;
                if (new Rect(19f * positionScale, 11f * positionScale,
                    width * sizeScale, height * sizeScale).Contains(statusPointer))
                {
                    UITip.Show(string.Format("念力 {0}/{1}(加工/打孔/镶嵌/世界喊话消耗, 在线恢复)",
                        CharData.nianli, CharData.maxNianli), hudOwner.s);
                }
            }
        }

        // 06000414: double-experience green pulse, otherwise a grey pulse. The native
        // clock is Time.time, and Color.Lerp preserves its clamped component arithmetic.
        if (hudOwner.nameFxWhite != null)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            Color previousColor = GUI.color;
            GUI.color = CharData.doubleExp
                ? Color.Lerp(new Color(0.12f, 0.55f, 0.12f, 1f), new Color(0.3f, 1f, 0.3f, 1f), pulse)
                : Color.Lerp(new Color(0.42f, 0.42f, 0.42f, 1f), new Color(0.78f, 0.78f, 0.78f, 1f), pulse);
            Texture2D effect = hudOwner.nameFxWhite;
            float positionScale = hudOwner.s;
            float width = effect.width;
            float height = hudOwner.nameFxWhite.height;
            float sizeScale = hudOwner.s;
            GUI.DrawTexture(new Rect(76f * positionScale, 7f * positionScale,
                width * sizeScale, height * sizeScale),
                hudOwner.nameFxWhite);
            GUI.color = previousColor;
        }

        // GameHud.OnGUI 06000414 probes through Measure before Ready so the
        // original bitmap owner can retry loading before choosing its fallback.
        font.Measure(".", hudOwner.s);
        bool nameFontReady = font.Ready;
        float nameScale = hudOwner.s;
        Rect nameRect = ScaleRect(80f, 11f, 84f, 12f, nameScale);
        if (nameFontReady)
        {
            font.CenterOutline(
                nameRect,
                CharData.name,
                LabelGold,
                nameScale);
            float levelScale = hudOwner.s;
            font.CenterOutline(
                new Rect(0f, 43f * levelScale, 20f * levelScale, 16f * levelScale),
                CharData.level.ToString(CultureInfo.InvariantCulture),
                LabelGold,
                hudOwner.s);
        }
        else
        {
            // The world owner binds the native GameHud before the first Draw.
            hudOwner.DrawNameFallbackForIndependentRuntime(
                nameRect,
                CharData.name);
            float levelScale = hudOwner.s;
            hudOwner.DrawLevelFallbackForIndependentRuntime(
                new Rect(0f, 43f * levelScale, 20f * levelScale, 16f * levelScale),
                CharData.level.ToString(CultureInfo.InvariantCulture));
        }

        float karmaScale = hudOwner.s;
        Rect karmaRect = ScaleRect(4f, 104f, 72f, 14f, karmaScale);
        Color karmaColor = CharData.pkState == 2
            ? new Color(1f, 0.25f, 0.25f, 1f)
            : CharData.pkState == 1
                ? new Color(0.66f, 0.66f, 0.66f, 1f)
                : Color.white;
        string karmaText = "善恶 " + CharData.karma.ToString(CultureInfo.InvariantCulture);
        if (font.Ready)
        {
            font.DrawOutline(
                karmaRect.x,
                karmaRect.y,
                karmaText,
                karmaColor,
                hudOwner.s);
        }
        else
        {
            // The native karma branch copies the skin label each draw; it does
            // not use the outlined name/level styles or substitute their font.
            GUIStyle karmaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(11f * hudOwner.s)
            };
            karmaStyle.normal.textColor = karmaColor;
            GUI.Label(karmaRect, karmaText, karmaStyle);
        }

        // Native 0414 keeps the same captured status pointer through the karma label.
        if (karmaRect.Contains(statusPointer))
        {
            string[] parts = new string[9];
            parts[0] = "善恶值 ";
            parts[1] = CharData.karma.ToString();
            parts[2] = "(";
            parts[3] = CharData.pkState == 2 ? "红名" : CharData.pkState == 1 ? "灰名" : "白名";
            parts[4] = ")\n";
            parts[5] = CharData.pkMapFree != 0
                ? "★本图为【自由PK区】: 不分阵营也不论PK模式, 任何人可互相攻击; 击杀不计善恶、不会红名\n"
                : "";
            parts[6] = "杀害白名玩家: 善恶值-90, 低于0变红名\n灰名: 主动攻击白名后60秒, 期间可被无惩罚击杀\n红名惩罚: 死亡时非绑定物品每件20%掉落(最多3件), 且人人可无惩罚击杀\n洗白: 红名在线每2分钟善恶+1, 回到0恢复白名\n";
            parts[7] = CharData.pkProtectLv.ToString();
            parts[8] = "级以下受新手保护, 不参与野外PK";
            UITip.Show(string.Concat(parts), hudOwner.s);
        }

        Texture2D petBar = resources.PetBar;
        float petBarScale = hudOwner != null ? hudOwner.s : scale;
        TexScale.Draw(48f * petBarScale, 61f * petBarScale, petBar, 0f, 0f,
            hudOwner != null ? hudOwner.s : scale);
        // Native Px/button reads s again after T, before the pet vitals/name/head reads.
        float petButtonScale = hudOwner != null ? hudOwner.s : scale;
        petWindowToggle = GUI.Button(
            ScaleRect(48f, 61f, 123f, 41f, petButtonScale), GUIContent.none, GUIStyle.none);
        if (petWindowToggle) Win05.Toggle(Win05.PetDialogName);
        bool hasPet = CharData.petModel > 0;
        if (hasPet)
        {
            int petHpFrame = ProgressFrame(CharData.petCurHp, CharData.petMaxHp);
            Texture2D petHpCover = hudOwner.petHpFill3 != null && hudOwner.petHpFill3[petHpFrame] != null
                ? hudOwner.petHpFill3[petHpFrame] : hudOwner.petHpFill;
            float petHpScale = hudOwner.s;
            DrawProgress(
                81f,
                86f,
                90f,
                9f,
                CharData.petCurHp,
                CharData.petMaxHp,
                null,
                petHpCover,
                petHpScale);
            float petMpScale = hudOwner.s;
            DrawProgress(
                80f,
                95f,
                90f,
                9f,
                CharData.petCurSp,
                CharData.petMaxSp,
                null,
                resources.PetMpFill,
                petMpScale);
            Vector2 petPointer = Event.current.mousePosition;
            if (ScaleRect(81f, 86f, 90f, 9f, petHpScale).Contains(petPointer))
            {
                UITip.Show(string.Format("气血 {0}/{1}", CharData.petCurHp, CharData.petMaxHp), hudOwner.s);
            }
            else if (ScaleRect(80f, 95f, 90f, 9f, petMpScale).Contains(petPointer))
            {
                UITip.Show(string.Format("魔法 {0}/{1}", CharData.petCurSp, CharData.petMaxSp), hudOwner.s);
            }
            if (font.Ready)
            {
                float petNameScale = hudOwner.s;
                font.CenterOutline(
                    ScaleRect(92f, 71f, 72f, 12f, petNameScale),
                    CharData.petName,
                    LabelGold,
                    petNameScale);
            }
        }
        else if (font.Ready)
        {
            float petNameScale = hudOwner.s;
            font.CenterOutline(
                ScaleRect(92f, 71f, 72f, 12f, petNameScale),
                EmptyPetLabel,
                new Color(0.7f, 0.7f, 0.7f, 1f),
                petNameScale);
        }

        // The original avatar branch owns head-cache publication at this point.
        PetHeadSource = hudOwner.ResolvePetHeadForIndependentRuntime(hasPet);
        float petHeadScale = hudOwner.s;
        resources.OwnerForIndependentRuntime.DrawPetHead(
            ScaleRect(51f, 68f, 28f, 28f, petHeadScale), PetHeadSource, petHeadScale);
    }

    /// <summary>
    /// GameHud.BuildRingMask 06000421 (1.5.8 arm64 0x43d75c / x86_64 0x455400).
    /// It discovers the ring's occupied angular
    /// arc from alpha greater than 79, removes the largest discontinuity, then clears pixels
    /// beyond the requested fraction in the same top-origin angle space as the original.
    /// </summary>
    public static Texture2D BuildRingMask(
        float centerX,
        float centerY,
        float fraction,
        Texture2D source)
    {
        if (!source.isReadable)
        {
            return source;
        }

        int width = source.width;
        int height = source.height;
        Color32[] pixels = source.GetPixels32();
        List<float> angles = new List<float>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];
                if (pixel.a > 79)
                {
                    angles.Add(Mathf.Atan2(
                        height - 1 - y - centerY,
                        x - centerX));
                }
            }
        }

        if (angles.Count <= 3)
        {
            return source;
        }

        angles.Sort();
        float twoPi = Mathf.PI * 2f;
        float largestGap = angles[0] + twoPi - angles[angles.Count - 1];
        int gapIndex = angles.Count - 1;
        for (int index = 0; index < angles.Count - 1; index++)
        {
            float gap = angles[index + 1] - angles[index];
            if (gap > largestGap)
            {
                largestGap = gap;
                gapIndex = index;
            }
        }

        float startAngle = angles[(gapIndex + 1) % angles.Count];
        float occupiedArc = twoPi - largestGap;
        float endAngle = startAngle + occupiedArc;
        // The original Yof local function (06000464) retains both float operations.
        float startY = centerY + Mathf.Sin(startAngle) * 20f;
        float endY = centerY + Mathf.Sin(endAngle) * 20f;
        bool forward = startY >= endY;
        Color32[] maskPixels = new Color32[height * width];
        Color32 transparent = new Color32(0, 0, 0, 0);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                Color32 pixel = pixels[pixelIndex];
                if (pixel.a > 79)
                {
                    float angle = Mathf.Atan2(
                        height - 1 - y - centerY,
                        x - centerX);
                    // Both original architectures call fmodf twice, with a float add between.
                    float normalized = ((angle - startAngle) % twoPi + twoPi) % twoPi / occupiedArc;
                    float progress = forward ? normalized : 1f - normalized;
                    if (fraction < progress)
                    {
                        pixel = transparent;
                    }
                }

                maskPixels[pixelIndex] = pixel;
            }
        }

        Texture2D mask = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false);
        mask.filterMode = FilterMode.Bilinear;
        mask.wrapMode = TextureWrapMode.Clamp;
        mask.SetPixels32(maskPixels);
        mask.Apply();
        return mask;
    }

    /// <summary>
    /// GameHud.ProgBar (0x0600041f, arm64 0x432594 / x86_64 0x448ec0): the fraction stays 1 when
    /// max &lt; 1 and is otherwise clamped to (0,1] with nothing drawn when v/max is not positive;
    /// both textures then get the same width-scaled rect, the same texCoords (0, 0, fraction, 1)
    /// and alphaBlend false, layer first and cover second, and a null texture only cancels its own
    /// draw. The four callers are the four official OnGUI sites: HP (75,25,96,12) at arm64 0x42a7a4
    /// with hpBg/hpFill3, MP (75,36,96,12) at 0x42a964 with mpBg/mpFill, and pet HP (81,86,90,9) at
    /// 0x42bccc and pet MP (80,95,90,9) at 0x42bd20 with a null layer.
    /// </summary>
    private static void DrawProgress(
        float x,
        float y,
        float width,
        float height,
        int current,
        int maximum,
        Texture2D layer,
        Texture2D cover,
        float scale)
    {
        float fraction = ProgressFraction(current, maximum);
        if (fraction <= 0f)
        {
            return;
        }

        Rect full = ScaleRect(x, y, width, height, scale);
        Rect clipped = new Rect(full.x, full.y, full.width * fraction, full.height);
        Rect texCoords = new Rect(0f, 0f, fraction, 1f);
        if (layer)
        {
            GUI.DrawTextureWithTexCoords(clipped, layer, texCoords, false);
        }

        if (cover)
        {
            GUI.DrawTextureWithTexCoords(clipped, cover, texCoords, false);
        }
    }

    private static Rect ScaleRect(
        float x,
        float y,
        float width,
        float height,
        float scale)
    {
        return new Rect(x * scale, y * scale, width * scale, height * scale);
    }

    private static string PetIconPath(int model)
    {
        return model > 0
            ? string.Format(
                CultureInfo.InvariantCulture,
                "res/peticon/M{0}.gsa",
                model)
            : EmptyPetIconPath;
    }

}

}
