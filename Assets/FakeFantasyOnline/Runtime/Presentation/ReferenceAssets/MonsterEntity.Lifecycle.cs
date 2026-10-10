using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity Update/LateUpdate/death lifecycle owner.
/// </summary>
public partial class MonsterEntity
{
    private const float DIE_FADE_SEC = 0.35f;
    private const float FlashDur = 0.12f;

    public int petOwnerId;
    private bool IsMyPet => friendly && petOwnerId == NetClient.SelfEntityId;
    private EffectPlayer petRing;
    private bool petRingFailed;
    private float flashTimer;
    private int dieFrame;
    private float dieTimer;
    private float dieFadeT;
    private BoxCollider2D col;
    private Sprite _lastHitSprite;
    private SpriteRenderer glowSr;
    private Sprite _glowLastBase;
    private Sprite _glowCur;
    private bool _artDerived;
#if UNITY_EDITOR
    private IMonsterEntityLifecycleAdapter lifecycleAdapter;
#endif

    private void Update()
    {
        if (_artDerived)
        {
            SyncShadowSize();
        }
        else
        {
            EnsureArtDerived();
        }

        ArtHint();
        TickStatus();
        TickStatusIcons();
        UpdateNameTag();
        PlaceStatusIcons();
        UpdateHpBar();
        UpdateHitBox();

        if (dying)
        {
            StepDieFrame();
            return;
        }

        if (prepFrame >= 0 && prepFrames != null)
        {
            AdvancePath();
            StepPrepFrame();
            return;
        }

        if (atkFrame >= 0 && atkFrames != null)
        {
            AdvancePath();
            StepAtkFrame();
            return;
        }

        if (flashTimer > 0f && body)
        {
            flashTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(flashTimer / FlashDur);
            float channel = 1f - 0.3f * t;
            body.color = new Color(1f, channel, channel, 1f);
            if (flashTimer <= 0f)
            {
                body.color = Color.white;
            }
        }

        if (friendly && body)
        {
            bool shown = CharData.petShow && CharData.petShowKnown && !HiddenByF11;
            if (shown && !petShownPrev && pathX != null && pathX.Length > 0)
            {
                worldX = pathX[pathX.Length - 1];
                worldY = pathY[pathY.Length - 1];
                pathIdx = pathX.Length;
                moving = false;
                transform.position = new Vector3(
                    ((float)System.Math.Round(worldX) - worldW * 0.5f) / ppu,
                    (worldH * 0.5f - (float)System.Math.Round(worldY)) / ppu, 0f);
                body.sortingOrder = Mathf.Clamp(unchecked((int)worldY), 0, 30000);
            }
            petShownPrev = shown;
            if (body.enabled != shown) body.enabled = shown;
            if (col)
            {
                bool pick = shown && !IsMyPet;
                if (col.enabled != pick) col.enabled = pick;
            }
            if ((!shown || IsMyPet) && ReferenceEquals(TargetSelect.Current, this)) TargetSelect.Clear();

            bool ringWanted = shown && petOwnerId == NetClient.SelfEntityId &&
                !CharData.petRidable && CharData.petTianmo && CharData.petShowTianmo && CharData.ShowMountRing;
            if (ringWanted)
            {
                if (!petRing && !petRingFailed)
                {
                    try
                    {
                        petRing = EffectPlayer.PlaySustained(transform.position, "TFX304", ppu, null, 0f);
                        if (petRing)
                        {
                            petRing.transform.SetParent(transform, true);
                            petRing.attachSort = body;
                            petRing.attachUnder = true;
                        }
                        else petRingFailed = true;
                    }
                    catch (System.Exception) { petRingFailed = true; }
                }
            }
            else if (petRing)
            {
                Destroy(petRing.gameObject);
                petRing = null;
            }
            if (!shown) return;
        }

        if (!moving)
        {
            if (body)
            {
                UpdateFidget();
            }
            return;
        }

        AdvancePath();
        if (walkFrames != null && walkFrames.GetLength(1) > 0)
        {
            StepWalkFrame();
        }
        else
        {
            body.sprite = StandSprite(curDir);
        }

        body.flipX = flip;
    }

    private void LateUpdate()
    {
        if (!body)
        {
            return;
        }

        Sprite baseSprite = body.sprite;
        if (!ReferenceEquals(baseSprite, _glowLastBase) || _glowVerSeen != GlowVer)
        {
            _glowLastBase = baseSprite;
            _glowVerSeen = GlowVer;
            if (baseSprite && !_glowOf.TryGetValue(baseSprite, out _glowCur))
            {
                _glowCur = null;
            }
            else if (!baseSprite)
            {
                _glowCur = null;
            }

            if (_glowCur && !glowSr)
            {
                GameObject glowObject = new GameObject("glow");
                glowObject.transform.SetParent(transform, false);
                glowSr = glowObject.AddComponent<SpriteRenderer>();
                glowSr.sharedMaterial = EffectPlayer.NpcGlowLayerMat();
            }

            if (glowSr)
            {
                glowSr.sprite = _glowCur;
            }
        }

        if (!glowSr)
        {
            return;
        }

        bool allowed = true;
        if (friendly)
        {
            GameSettings.EnsureLoaded();
            allowed = IsMyPet
                ? !GameSettings.HideMyPetGlow
                : !GameSettings.HideOtherPetGlow;
        }

        bool wanted = _glowCur && body.enabled;
        wanted = allowed & wanted;
        if (glowSr.enabled != wanted)
        {
            glowSr.enabled = wanted;
        }

        if (!wanted)
        {
            return;
        }

        glowSr.sortingOrder = body.sortingOrder + 1;
        glowSr.flipX = body.flipX;
        glowSr.color = Color.white;
    }

    public bool BeginDie()
    {
        if (dying)
        {
            return true;
        }

        if (!dieTried && dieAct > 0)
        {
            dieTried = true;
            dieFrames = LoadGrid(dieAct);
            TintLazy(dieAct, dieFrames);
        }

        if (dieFrames == null || dieFrames.GetLength(1) < 1)
        {
            dying = true;
            return false;
        }

        dying = true;
        dieFrame = 0;
        dieTimer = 0f;
        moving = false;
        if (body)
        {
            body.color = Color.white;
        }
        if (col)
        {
            col.enabled = false;
        }
        TargetSelect.OnGone(this);
        return true;
    }

    // Official 1.5.8 StepDieFrame (06000654), arm64 0x4918d4 (856 bytes) / x86_64 0x4a8f60
    // (848 bytes), read from the frozen persistent project into
    // Build/ReferenceReverseGhidra-1.5.8/queries/wb127-stepdieframe-{arm64,x86_64}. Entry gate is
    // UnityEngine.Object.op_Equality on body (arm64 0x491920 / x86_64 FUN_01038840), so a
    // destroyed renderer returns before any renderer write; every later body test is a raw
    // pointer test whose throw arm is unreachable behind that gate. Field identity from the
    // frozen 1.5.8 metadata index: body 0x78, id 0x30, curDir 0xa0, flip 0xa8, dieFrames 0xc8,
    // dieIntervalMs 0xd0, dieFrame 0xd8, dieTimer 0xdc, dieFadeT 0xe0. The grid arm requires
    // dieFrames != null && GetLength(1) > 0, clamps the direction with
    // Mathf.Clamp(curDir, 0, GetLength(0) - 1) and advances only while dieFrame < GetLength(1) - 1
    // with Time.deltaTime * 1000f against dieIntervalMs. The element test that triggers RowAny is
    // a RAW reference test on both architectures (arm64 export line 102 `lVar10 == 0`, x86_64
    // export line 94 `lVar8 == 0`, no UnityEngine.Object call) and only the gate that precedes the
    // renderer writes is UnityEngine.Object.op_Inequality (arm64 lines 109/115, x86_64 line 107
    // FUN_0103dde0), so `??` reproduces the raw test and a destroyed (fake-null) frame keeps its
    // slot and reaches the Unity gate instead of being replaced. The fade arm adds
    // Time.deltaTime (not *1000f) to dieFadeT, reads body.color, writes alpha
    // Mathf.Clamp01(1f - dieFadeT / 0.35f) and calls CombatWorld.Remove(id, 0) once
    // dieFadeT >= 0.35f; x86_64 prints the arithmetic as dieFadeT / -0.35f + 1f clamped to [0,1]
    // with the end scalar 0.35f (arm64 keeps the same get_color -> set_color order and the same
    // 0x3eb33333 end scalar but prints the Color argument as 0 and exposes no float ops). This
    // restores the compiled null semantics; it is not a claim about the official C# spelling.
    private void StepDieFrame()
    {
        if (body == null)
        {
            return;
        }

        if (dieFrames != null && dieFrames.GetLength(1) > 0)
        {
            int direction = Mathf.Clamp(curDir, 0, dieFrames.GetLength(0) - 1);
            int finalFrame = dieFrames.GetLength(1) - 1;
            if (dieFrame < finalFrame)
            {
                dieTimer += Time.deltaTime * 1000f;
                while (dieTimer >= dieIntervalMs && dieFrame < finalFrame)
                {
                    dieTimer -= dieIntervalMs;
                    dieFrame++;
                }
            }

            Sprite sprite = dieFrames[direction, dieFrame] ??
                RowAny(dieFrames, direction);
            if (sprite != null)
            {
                body.sprite = sprite;
                body.flipX = flip;
            }

            if (dieFrame < finalFrame)
            {
                return;
            }
        }

        dieFadeT += Time.deltaTime;
        Color color = body.color;
        color.a = Mathf.Clamp01(1f - dieFadeT / DIE_FADE_SEC);
        body.color = color;
        if (dieFadeT >= DIE_FADE_SEC)
        {
            CombatWorld.Remove(id, 0);
        }
    }

    public bool IsDying
    {
        get { return dying; }
    }

    public void SetHp(int value)
    {
        if (value < hp && !dying)
        {
            flashTimer = FlashDur;
        }
        hp = Mathf.Max(0, value);
    }

    private void UpdateHitBox()
    {
        if (!col)
        {
            return;
        }

        Sprite sprite = body ? body.sprite : null;
        if (sprite == _lastHitSprite)
        {
            return;
        }

        _lastHitSprite = sprite;
        if (!sprite)
        {
            col.size = new Vector2(0.6f, 1f);
            col.offset = new Vector2(0f, 0.5f);
            return;
        }

        Bounds bounds = sprite.bounds;
        float minimum = 64f / ppu;
        col.size = new Vector2(
            Mathf.Max(bounds.size.x * 2f, minimum),
            Mathf.Max(bounds.size.y * 2f, minimum));
        col.offset = new Vector2(0f, bounds.center.y);
    }
}

#if UNITY_EDITOR
internal interface IMonsterEntityLifecycleAdapter
{
    void UpdateNameTag(MonsterEntity owner);
}
#endif

internal static partial class CharData
{
    private const int FabaoPart = 11;
    private const int DragonFxPart = 12;
    private const string EquipFxPreferencePrefix = "equipfx_";
    private const string FabaoFxPreferencePrefix = "equipfxfb_";
    private const string DragonFxPreferencePrefix = "equipfxdragon_";

    internal static bool petRidable;
    internal static bool petTianmo;
    internal static bool petShowKnown;
    private static bool showMountRingLoaded;
    private const string ShowMountRingPreferencePrefix = "showMountRing_";
    private static string _equipFxChar;
    // CharData.PrefSuffix 0x06000f17 is the official owner of this shape; the equip-FX helpers
    // below were a private copy of it and now forward so the two can never drift apart.
    private static string EquipPreferenceSuffix => PrefSuffix;

    internal static bool ShowMountRing
    {
        get
        {
            if (!showMountRingLoaded && !string.IsNullOrEmpty(preferenceSuffix))
            {
                showMountRing = PlayerPrefs.GetInt(
                    ShowMountRingPreferencePrefix + preferenceSuffix,
                    1) != 0;
                showMountRingLoaded = true;
            }
            return showMountRing;
        }
        set
        {
            showMountRing = value;
            showMountRingLoaded = true;
            if (!string.IsNullOrEmpty(preferenceSuffix))
            {
                PlayerPrefs.SetInt(
                    ShowMountRingPreferencePrefix + preferenceSuffix,
                    value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }

    internal static void BindPreferenceIdentity(
        long? persistentCharacterId,
        string characterId)
    {
        preferenceSuffix = persistentCharacterId.HasValue &&
            persistentCharacterId.Value > 0
            ? persistentCharacterId.Value.ToString(CultureInfo.InvariantCulture)
            : characterId ?? string.Empty;
        legacyPreferenceSuffix = characterId ?? string.Empty;
        showMountRingLoaded = false;
        charId = persistentCharacterId ?? 0;
        name = characterId;
    }

    internal static bool EquipShown(int part, int sub)
    {
        bool shown;
        return !equipShow.TryGetValue(unchecked(part * 10 + sub), out shown) || shown;
    }

    internal static void SetEquipShown(int part, int sub, bool show)
    {
        equipShow[unchecked(part * 10 + sub)] = show;
    }

    internal static void LoadEquipFx()
    {
        if (_equipFxChar == name)
        {
            return;
        }

        _equipFxChar = name;
        equipShow.Clear();
        if (string.IsNullOrEmpty(EquipPreferenceSuffix))
        {
            return;
        }

        if (HasEquipPreference(EquipFxPreferencePrefix))
        {
            int shownMask = GetEquipPreference(EquipFxPreferencePrefix, 0);
            for (int index = 0; index < FxParts.Length; index++)
            {
                int part = FxParts[index];
                if ((shownMask & 1 << (index * 2)) == 0)
                {
                    equipShow[part * 10] = false;
                }
                if ((shownMask & 1 << (index * 2 + 1)) == 0)
                {
                    equipShow[part * 10 + 1] = false;
                }
            }
        }

        if (GetEquipPreference(FabaoFxPreferencePrefix, 1) == 0)
        {
            equipShow[FabaoPart * 10 + 1] = false;
        }
        if (GetEquipPreference(DragonFxPreferencePrefix, 1) == 0)
        {
            equipShow[DragonFxPart * 10 + 1] = false;
        }
    }

    internal static int EquipFxHideMask()
    {
        int mask = 0;
        for (int index = 0; index < FxParts.Length; index++)
        {
            int part = FxParts[index];
            if (!EquipShown(part, 0))
            {
                mask |= 1 << (index * 2);
            }
            if (!EquipShown(part, 1))
            {
                mask |= 1 << (index * 2 + 1);
            }
        }

        if (!EquipShown(FabaoPart, 1))
        {
            mask |= 1 << 16;
        }
        if (!EquipShown(DragonFxPart, 1))
        {
            mask |= 1 << 17;
        }
        return mask;
    }

    internal static void SaveEquipFx()
    {
        if (string.IsNullOrEmpty(EquipPreferenceSuffix))
        {
            return;
        }

        int shownMask = 0;
        for (int index = 0; index < FxParts.Length; index++)
        {
            int part = FxParts[index];
            if (EquipShown(part, 0))
            {
                shownMask |= 1 << (index * 2);
            }
            if (EquipShown(part, 1))
            {
                shownMask |= 1 << (index * 2 + 1);
            }
        }

        SetEquipPreference(EquipFxPreferencePrefix, shownMask);
        SetEquipPreference(
            FabaoFxPreferencePrefix,
            EquipShown(FabaoPart, 1) ? 1 : 0);
        SetEquipPreference(
            DragonFxPreferencePrefix,
            EquipShown(DragonFxPart, 1) ? 1 : 0);
    }

    // Official 0x06000f1a CharData.HasPref, 0x06000f18 CharData.GetPref and 0x06000f19
    // CharData.SetPref are byte-for-byte the bodies these three wrappers used to duplicate, so the
    // equip-FX call sites now go through the recovered official members.
    private static bool HasEquipPreference(string prefix) => HasPref(prefix);

    private static int GetEquipPreference(string prefix, int defaultValue) =>
        GetPref(prefix, defaultValue);

    private static void SetEquipPreference(string prefix, int value) => SetPref(prefix, value);

    internal static void BindPetForIndependentRuntime(
        bool show,
        bool showKnown,
        bool ridable,
        bool tianmo,
        bool showTianmo,
        bool mountRing,
        int model = 0)
    {
        if (!PetSnapshotReceiptOwned)
        {
            petModel = model;
            petShow = show;
            petShowKnown = showKnown;
            petRidable = ridable;
            petTianmo = tianmo;
            petShowTianmo = showTianmo;
        }
        showMountRing = mountRing;
        showMountRingLoaded = true;
    }
}

internal partial class NetClient
{
    internal static int SelfEntityId;
}

}
