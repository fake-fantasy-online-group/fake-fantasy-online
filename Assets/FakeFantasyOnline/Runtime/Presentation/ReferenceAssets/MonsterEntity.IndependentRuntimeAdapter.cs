using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Decision-free independent-runtime access to recovered MonsterEntity state.
/// This adapter deliberately lives outside the official recovered core.
/// </summary>
public partial class MonsterEntity
{
#if UNITY_EDITOR
    internal void ConfigureForIndependentRuntime(
        int modelId,
        string monsterName,
        float pixelsPerUnit)
    {
        model = modelId;
        mname = monsterName;
        ppu = pixelsPerUnit;
    }

    internal void ConfigureLifecycleForIndependentRuntime(
        IMonsterEntityLifecycleAdapter adapter,
        int entityId,
        int ownerEntityId,
        int monsterLevel,
        int hitPoints,
        int maximumHitPoints,
        float x,
        float y,
        float width,
        float height,
        bool isFriendly,
        SpriteRenderer bodyRenderer,
        BoxCollider2D collider)
    {
        lifecycleAdapter = adapter;
        id = entityId;
        petOwnerId = ownerEntityId;
        level = monsterLevel;
        hp = hitPoints;
        maxHp = maximumHitPoints;
        worldX = x;
        worldY = y;
        worldW = width;
        worldH = height;
        friendly = isFriendly;
        body = bodyRenderer;
        col = collider;
        _lastHitSprite = null;
        if (col && (!body || !body.sprite))
        {
            // Official Init reaches UpdateHitBox with its null-sprite fallback already projected.
            // Independent async construction can begin before a body frame exists, so seed the
            // same cached geometry without changing Collider.enabled.
            col.size = new Vector2(0.6f, 1f);
            col.offset = new Vector2(0f, 0.5f);
        }
        enabled = false;
    }

#endif

    internal void UpdateHitBoxForIndependentRuntime()
    {
        UpdateHitBox();
    }

    // Explicit diagnostic input only. Live components receive Unity's OnMouseDown themselves,
    // independently of PlayerController.Update and optional world-view admission.
    internal void OnMouseDownForDiagnostic() => OnMouseDown();

    internal bool BeginDieForIndependentRuntime()
    {
        return BeginDie();
    }

    internal void SetHpForIndependentRuntime(int value)
    {
        SetHp(value);
    }

    internal void ClearPathForIndependentRuntime()
    {
        pathX = null;
        pathY = null;
        pathIdx = 0;
        moving = false;
        catchUp = 1f;
    }

    internal SpriteRenderer BodyForIndependentRuntime => body;
    internal SpriteRenderer GlowRendererForIndependentRuntime => glowSr;
    internal EffectPlayer EliteRingForIndependentRuntime => eliteRing;
    internal BoxCollider2D ColliderForIndependentRuntime => col;
    internal bool DyingForIndependentRuntime => dying;
    internal int DeathFrameForIndependentRuntime => dieFrame;
    internal float DeathTimerForIndependentRuntime => dieTimer;
    internal float DeathFadeForIndependentRuntime => dieFadeT;
    internal float FlashTimerForIndependentRuntime => flashTimer;
    internal bool UnityLiveForIndependentRuntime => this;

    internal void LoadAnimForIndependentRuntime(int modelId)
    {
        LoadAnim(modelId);
    }

    internal Sprite[,] StandFramesForIndependentRuntime => standFrames;
    internal Sprite[,] WalkFramesForIndependentRuntime => walkFrames;
    internal int StandActionForIndependentRuntime => standAct;
    internal int WalkActionForIndependentRuntime => walkAct;
    internal int AttackActionForIndependentRuntime => atkAct;
    internal int PrepareActionForIndependentRuntime => prepAct;
    internal int DeathActionForIndependentRuntime => dieAct;
    internal float StandIntervalForIndependentRuntime => standIntervalMs;
    internal float WalkIntervalForIndependentRuntime => walkIntervalMs;
    internal float AttackIntervalForIndependentRuntime => atkIntervalMs;
    internal float PrepareIntervalForIndependentRuntime => prepIntervalMs;
    internal float DeathIntervalForIndependentRuntime => dieIntervalMs;

    internal Sprite StandSpriteForIndependentRuntime(int direction, int frame)
    {
        standFrame = frame;
        return StandSprite(direction);
    }

    internal Sprite[,] PlayAttackForIndependentRuntime()
    {
        PlayAttack();
        return atkFrames;
    }

    internal Sprite[,] PlayPrepareForIndependentRuntime()
    {
        PlayPrepare();
        return prepFrames;
    }

    // Read-only mirror of the two grids an official owner can start without this adapter:
    // CombatWorld.MonsterPlayAttack and CombatWorld.MonsterPlayPrepare call PlayAttack and
    // PlayPrepare directly, so the receive-side actor reads the decoded rows here instead of
    // starting the animation a second time.
    internal Sprite[,] AttackFramesForIndependentRuntime => atkFrames;
    internal Sprite[,] PrepareFramesForIndependentRuntime => prepFrames;

    internal int AttackFrameForIndependentRuntime => atkFrame;
    internal float AttackTimerForIndependentRuntime => atkTimer;
    internal int PrepareFrameForIndependentRuntime => prepFrame;
    internal float PrepareTimerForIndependentRuntime => prepTimer;
    internal float PrepareHeldForIndependentRuntime => prepHeldMs;

    internal void ReleaseHeldGridsForIndependentRuntime()
    {
        ReleaseHeldGrids();
    }

    internal static Sprite GlowForIndependentRuntime(Sprite baseSprite)
    {
        Sprite glow;
        return baseSprite != null && _glowOf.TryGetValue(baseSprite, out glow)
            ? glow
            : null;
    }

    internal void ArtHintForIndependentRuntime(bool isMoving, int direction)
    {
        moving = isMoving;
        curDir = direction;
        ArtHint();
    }

    internal static bool TryGetHeadTopForIndependentRuntime(
        int modelId,
        out float headTop)
    {
        return _headTopCache.TryGetValue(modelId, out headTop);
    }

    internal Sprite[,] EnsureDeathForIndependentRuntime()
    {
        EnsureDie();
        return dieFrames;
    }

    internal void ConfigureMovementForIndependentRuntime(
        float x,
        float y,
        float width,
        float height,
        bool isFriendly,
        SpriteRenderer bodyRenderer)
    {
        worldX = x;
        worldY = y;
        worldW = width;
        worldH = height;
        friendly = isFriendly;
        body = bodyRenderer;
    }

    internal void SetPathForIndependentRuntime(int[] xs, int[] ys, int speed)
    {
        SetPath(xs, ys, speed);
    }

    internal void AdvancePathForIndependentRuntime()
    {
        AdvancePath();
    }

    internal float WorldXForIndependentRuntime => worldX;
    internal float WorldYForIndependentRuntime => worldY;
    internal float MoveSpeedForIndependentRuntime => moveSpeed;
    internal float CatchUpForIndependentRuntime => catchUp;
    internal int PathIndexForIndependentRuntime => pathIdx;
    internal int PathCountForIndependentRuntime => pathX == null ? 0 : pathX.Length;
    internal bool MovingForIndependentRuntime => moving;
    internal int DirectionForIndependentRuntime => curDir;
    internal bool FlipForIndependentRuntime => flip;

    public int CurDir => curDir;

    public bool Flip => flip;

    internal void ConfigureFidgetForIndependentRuntime(
        Sprite[,] standing,
        Sprite[,] hand,
        int direction,
        bool isFlipped,
        int frame)
    {
        standFrames = standing;
        handFrames = hand;
        curDir = direction;
        flip = isFlipped;
        fidgetFrame = frame;
        fidgetFrameTimer = 0f;
    }

    internal void UpdateFidgetForIndependentRuntime()
    {
        UpdateFidget();
    }

    internal int FidgetFrameForIndependentRuntime => fidgetFrame;

    internal float DeathAlphaForIndependentRuntime => body == null
        ? 0f
        : body.color.a;
}

}
