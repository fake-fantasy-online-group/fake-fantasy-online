using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity action-selection, grid-ownership and visible frame cone.
/// </summary>
public partial class MonsterEntity
{
    private SpriteRenderer body;
    private Sprite[,] walkFrames;
    private Sprite[,] handFrames;
    private Sprite[,] standFrames;
    private int curFrame;
    private bool flip;
    private float walkFrameTimer;
    private float standFrameTimer;
    private float standIntervalMs;
    private int standFrame;
    private Sprite[,] dieFrames;
    private bool dying;
    private Sprite[,] atkFrames;
    private float atkTimer;
    public bool IsCaster { get; private set; }
    private Sprite[,] prepFrames;
    private float prepTimer;
    private float prepHeldMs;
    private int handAct;
    private int dieAct;
    private int atkAct;
    private int prepAct;
    private bool handTried;
    private bool dieTried;
    private bool atkTried;
    private bool prepTried;
    private bool colorlessMask;

    private void LoadAnim(int m)
    {
        walkAct = NpcActions.Find(m, "Walk");
        standAct = NpcActions.Find(m, "Stand");
        if (walkAct > 0)
        {
            walkFrames = LoadGrid(walkAct);
            int interval = NpcActions.Interval(m, "Walk");
            if (interval > 0)
            {
                walkIntervalMs = interval;
            }

            if (standAct < 1)
            {
                // Official 1.5.8 LoadAnim (06000620) has no Stand action here: it adopts the
                // walk grid and re-reads the Walk interval (arm64 0x489a7c -> 0x489aa0,
                // x86_64 0x4a109b -> 0x4a1118).
                standFrames = walkFrames;
                interval = NpcActions.Interval(m, "Walk");
            }
            else
            {
                standFrames = LoadGrid(standAct);
                interval = NpcActions.Interval(m, "Stand");
            }

            if (interval > 0)
            {
                standIntervalMs = interval;
            }
        }
        else if (standAct > 0)
        {
            standFrames = LoadGrid(standAct);
            int interval = NpcActions.Interval(m, "Stand");
            if (interval > 0)
            {
                standIntervalMs = interval;
            }
        }

        handAct = NpcActions.Find(m, "Hand");
        if (handAct > 0)
        {
            int interval = NpcActions.Interval(m, "Hand");
            if (interval > 0)
            {
                handFrameSec = interval / 1000f;
            }
        }

        dieAct = NpcActions.Find(m, "Die");
        if (dieAct < 1)
        {
            dieAct = NpcActions.Find(m, "die");
        }
        if (dieAct > 0)
        {
            int interval = NpcActions.Interval(m, "Die");
            if (interval < 1)
            {
                interval = NpcActions.Interval(m, "die");
            }
            if (interval > 0)
            {
                dieIntervalMs = interval;
            }
        }

        string attackName = NpcActions.FindNameContaining(m, "magic");
        if (attackName != null)
        {
            IsCaster = true;
        }
        else
        {
            string[] names =
            {
                "attack1",
                "Attack",
                "AttackFree",
                "AttackSword",
                "Attackfree",
            };
            for (int index = 0; index < names.Length; index++)
            {
                if (NpcActions.Find(m, names[index]) > 0)
                {
                    attackName = names[index];
                    break;
                }
            }
        }

        if (attackName != null)
        {
            atkAct = NpcActions.Find(m, attackName);
            int interval = NpcActions.Interval(m, attackName);
            if (interval > 0)
            {
                atkIntervalMs = interval;
            }
        }

        if (IsCaster)
        {
            string prepareName = "PrepareMagic";
            if (NpcActions.Find(m, prepareName) < 1)
            {
                prepareName = NpcActions.FindNameContaining(m, "pare");
            }

            if (prepareName != null)
            {
                // Official 1.5.8 LoadAnim (06000620) writes prepAct (0x16c) only after the Find
                // result is positive: iVar2 = Find(m, prepareName); if (0 < iVar2) then 0x16c =
                // iVar2 and Interval is read. A non-positive result leaves the field untouched
                // (store target arm64 0x489ec0, x86_64 0x4a14f1 - both inside the 0 < iVar2 arm).
                int candidate = NpcActions.Find(m, prepareName);
                if (candidate > 0)
                {
                    prepAct = candidate;
                    int interval = NpcActions.Interval(m, prepareName);
                    if (interval > 0)
                    {
                        prepIntervalMs = interval;
                    }
                }
            }
        }

        // Official 1.5.8 LoadAnim (06000620) tail: when neither grid exists it shows the first
        // frame of the model's default action instead of nothing (arm64 0x489f10,
        // x86_64 0x4a1557). Only a Unity-live Sprite is adopted, into a fresh Sprite[1,1] whose
        // [0,0] slot is the whole grid - the sprite is never written into walkFrames.
        if (standFrames == null && walkFrames == null)
        {
            Sprite first = LoadGsnFirst(m);
            if (first != null)
            {
                standFrames = new Sprite[1, 1];
                standFrames[0, 0] = first;
            }
        }
    }

    private void EnsureHand()
    {
        if (!handTried && handAct > 0)
        {
            handTried = true;
            handFrames = LoadGrid(handAct);
            TintLazy(handAct, handFrames);
        }
    }

    private void EnsureDie()
    {
        if (!dieTried && dieAct > 0)
        {
            dieTried = true;
            dieFrames = LoadGrid(dieAct);
            TintLazy(dieAct, dieFrames);
        }
    }

    private void EnsureAtk()
    {
        if (!atkTried && atkAct > 0)
        {
            atkTried = true;
            atkFrames = LoadGrid(atkAct);
            TintLazy(atkAct, atkFrames);
        }
    }

    private void EnsurePrep()
    {
        if (!prepTried && prepAct > 0)
        {
            prepTried = true;
            prepFrames = LoadGrid(prepAct);
            TintLazy(prepAct, prepFrames);
        }
    }

    private void TintLazy(int actId, Sprite[,] grid)
    {
        if (grid == null || !colorlessMask)
        {
            return;
        }

        if (_recoloredAct.Add(actId))
        {
            RecolorGrid(grid, new System.Collections.Generic.HashSet<Texture2D>());
        }
    }

    // Official 1.5.8 StandSprite (0600063a), arm64 0x489ff8 / x86_64 0x4a1620: the fallback chain
    // (current frame -> frame 0 -> RowAny), the walk first frame and both AnyDecoded roots are
    // selected by RAW reference null tests (arm64 `lVar9 == 0` / x86_64 `lVar5 == 0`, no
    // UnityEngine.Object call), while only the two return gates call
    // UnityEngine.Object.op_Inequality. The source therefore reproduces raw-reference semantics
    // with ??: a destroyed (fake-null) sprite keeps its slot in the chain and is rejected by the
    // later Unity gate instead of being replaced by frame 0. This restores the compiled null
    // semantics; it is not a claim that the official C# source text used ?? or any other spelling.
    private Sprite StandSprite(int dir)
    {
        if (standFrames != null && standFrames.GetLength(0) > 0)
        {
            int direction = Mathf.Clamp(dir, 0, standFrames.GetLength(0) - 1);
            int frame = Mathf.Clamp(standFrame, 0, standFrames.GetLength(1) - 1);
            Sprite sprite = standFrames[direction, frame] ??
                standFrames[direction, 0] ??
                RowAny(standFrames, direction);
            if (sprite != null)
            {
                return sprite;
            }
        }

        if (walkFrames != null && walkFrames.GetLength(0) > 0)
        {
            int direction = Mathf.Clamp(dir, 0, walkFrames.GetLength(0) - 1);
            Sprite sprite = walkFrames[direction, 0] ?? RowAny(walkFrames, direction);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return AnyDecoded(standFrames) ?? AnyDecoded(walkFrames);
    }

    private static Sprite RowAny(Sprite[,] grid, int direction)
    {
        if (grid == null || direction < 0 || direction >= grid.GetLength(0))
        {
            return null;
        }

        int frameCount = grid.GetLength(1);
        for (int frame = 0; frame < frameCount; frame++)
        {
            Sprite sprite = grid[direction, frame];
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private Sprite AnyDecoded(Sprite[,] grid)
    {
        if (grid == null)
        {
            return null;
        }

        int directionCount = grid.GetLength(0);
        int frameCount = grid.GetLength(1);
        if (_anyHintD >= 0 && _anyHintD < directionCount &&
            _anyHintF >= 0 && _anyHintF < frameCount)
        {
            Sprite hinted = grid[_anyHintD, _anyHintF];
            if (hinted != null)
            {
                return hinted;
            }
        }

        for (int direction = 0; direction < directionCount; direction++)
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                Sprite sprite = grid[direction, frame];
                if (sprite != null)
                {
                    _anyHintD = direction;
                    _anyHintF = frame;
                    return sprite;
                }
            }
        }

        return null;
    }

    private void StepStandFrame()
    {
        if (standFrames == null)
        {
            return;
        }

        int frameCount = standFrames.GetLength(1);
        if (frameCount < 2 || standIntervalMs <= 0f)
        {
            standFrame = 0;
            return;
        }

        standFrameTimer += Time.deltaTime * 1000f;
        if (standFrameTimer >= standIntervalMs)
        {
            standFrameTimer = Mathf.Min(
                standFrameTimer - standIntervalMs,
                standIntervalMs);
            standFrame = (standFrame + 1) % frameCount;
        }
    }

    // Official 1.5.8 StepWalkFrame (06000644), arm64 0x492a50 / x86_64 0x4aa150: only the two
    // fallback steps are RAW reference null tests (arm64 `lVar8 == 0` at export line 74 and
    // `lVar8 != 0` at line 82; x86_64 `lVar5 == 0` at line 65 and `lVar5 != 0` at line 70, no
    // UnityEngine.Object call), while the single gate that precedes the renderer write is
    // UnityEngine.Object.op_Inequality. The source therefore reproduces the raw-reference
    // semantics with ?? (which compiles to a raw reference test): a destroyed (fake-null) sprite
    // keeps its slot and reaches the gate instead of being replaced by RowAny. StandSprite is
    // called only when RowAny's raw result is zero, and it receives the unclamped curDir field
    // (arm64 line 84 / x86_64 line 72 read 0xa0 directly); when the gate rejects the final value
    // the body returns without writing and without any further call. This restores the compiled
    // null semantics; it is not a claim that the official C# source text used ?? or any other
    // spelling.
    private void StepWalkFrame()
    {
        walkFrameTimer += Time.deltaTime * 1000f;
        if (walkFrameTimer >= walkIntervalMs)
        {
            walkFrameTimer = walkFrameTimer - walkIntervalMs <= walkIntervalMs
                ? walkFrameTimer - walkIntervalMs
                : walkIntervalMs;
            curFrame++;
        }

        int direction = Mathf.Clamp(curDir, 0, walkFrames.GetLength(0) - 1);
        curFrame %= walkFrames.GetLength(1);
        Sprite sprite = walkFrames[direction, curFrame] ??
            RowAny(walkFrames, direction) ??
            StandSprite(curDir);
        if (sprite != null)
        {
            body.sprite = sprite;
        }
    }

    public void PlayAttack()
    {
        if (dying)
        {
            return;
        }

        if (!atkTried && atkAct > 0)
        {
            atkTried = true;
            atkFrames = LoadGrid(atkAct);
            TintLazy(atkAct, atkFrames);
        }

        if (atkFrames != null && atkFrames.GetLength(1) > 0)
        {
            prepFrame = -1;
            atkFrame = 0;
            atkTimer = 0f;
        }
    }

    public void PlayPrepare()
    {
        if (dying)
        {
            return;
        }

        if (!prepTried && prepAct > 0)
        {
            prepTried = true;
            prepFrames = LoadGrid(prepAct);
            TintLazy(prepAct, prepFrames);
        }

        if (prepFrames != null && prepFrames.GetLength(1) > 0)
        {
            prepFrame = 0;
            prepTimer = 0f;
            prepHeldMs = 0f;
            atkFrame = -1;
        }
    }

    // Official 1.5.8 StepPrepFrame (06000647), arm64 0x492110 (712 bytes) / x86_64 0x4a9800
    // (704 bytes). Two compiled facts drive this body: (1) the element test that triggers RowAny is
    // a RAW reference test on both architectures (arm64 line 97 `lVar8 == 0`, x86_64 line 88, no
    // UnityEngine.Object call) while the gate after the fallback is
    // UnityEngine.Object.op_Inequality (arm64 line 105, x86_64 line 101), so `??` reproduces the
    // raw test and a destroyed (fake-null) slot keeps its value and reaches the gate instead of
    // being replaced; (2) the prepare loop stops at the last frame index (GetLength(1) - 1) and
    // then holds that frame for prepHeldMs (0x1b4) with a 3000f limit before falling back, unlike
    // StepAtkFrame below. The end arm writes prepFrame = -1 and prepHeldMs = 0f, reads body, calls
    // StandSprite with the unclamped curDir field (0xa0) and writes sprite then flipX with no gate
    // on the result, so a null StandSprite result is written. Restoring the compiled null semantics
    // here is not a claim about the official C# source spelling.
    private void StepPrepFrame()
    {
        if (body == null)
        {
            prepFrame = -1;
            return;
        }

        int direction = Mathf.Clamp(curDir, 0, prepFrames.GetLength(0) - 1);
        int finalFrame = prepFrames.GetLength(1) - 1;
        prepTimer += Time.deltaTime * 1000f;
        while (prepTimer >= prepIntervalMs && prepFrame < finalFrame)
        {
            prepTimer -= prepIntervalMs;
            prepFrame++;
        }

        if (prepFrame >= finalFrame)
        {
            prepHeldMs += Time.deltaTime * 1000f;
            if (prepHeldMs >= 3000f)
            {
                prepFrame = -1;
                prepHeldMs = 0f;
                body.sprite = StandSprite(curDir);
                body.flipX = flip;
                return;
            }
        }
        else
        {
            prepHeldMs = 0f;
        }

        Sprite sprite = prepFrames[direction, prepFrame] ??
            RowAny(prepFrames, direction);
        if (sprite != null)
        {
            body.sprite = sprite;
            body.flipX = flip;
        }
    }

    // Official 1.5.8 StepAtkFrame (06000648), arm64 0x4923d8 (648 bytes) / x86_64 0x4a9ac0
    // (608 bytes). Same raw/null split as StepPrepFrame: the element test before RowAny is a raw
    // reference test (arm64 line 101, x86_64 line 81) and the gate after the fallback is
    // UnityEngine.Object.op_Inequality (arm64 line 108, x86_64 line 101), so `??` is the compiled
    // equivalent. The end rules are deliberately not unified with the prepare member: the loop
    // bound is GetLength(1) itself, not GetLength(1) - 1 (arm64 lines 69-72, x86_64 lines 60-63),
    // there is no hold timer (prepHeldMs 0x1b4 is never read here) and the end arm fires on the
    // very pass that pushed atkFrame to frameCount: it writes atkFrame = -1, reads body, calls
    // StandSprite with the unclamped curDir field (0xa0) and writes sprite then flipX with no gate
    // on the result.
    private void StepAtkFrame()
    {
        if (body == null)
        {
            atkFrame = -1;
            return;
        }

        int direction = Mathf.Clamp(curDir, 0, atkFrames.GetLength(0) - 1);
        int frameCount = atkFrames.GetLength(1);
        atkTimer += Time.deltaTime * 1000f;
        while (atkTimer >= atkIntervalMs && atkFrame < frameCount)
        {
            atkTimer -= atkIntervalMs;
            atkFrame++;
        }

        if (atkFrame >= frameCount)
        {
            atkFrame = -1;
            body.sprite = StandSprite(curDir);
            body.flipX = flip;
            return;
        }

        Sprite sprite = atkFrames[direction, atkFrame] ??
            RowAny(atkFrames, direction);
        if (sprite != null)
        {
            body.sprite = sprite;
            body.flipX = flip;
        }
    }

}

}
