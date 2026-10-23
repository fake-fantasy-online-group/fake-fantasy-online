using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official SkillCaster effect-routing owner.
/// </summary>
public sealed partial class SkillCaster : MonoBehaviour
{
    private enum Op
    {
        Wait,
        PlayNamed,
        PlayId,
        MoveTo,
        Sound,
    }

    private struct Step
    {
        public Op op;
        public float num;
        public string name;
        public bool whoTarget;
    }

    private const float ATK_PRE_TIME = 500f;
    private const string ScriptPath = "res/skillevent.txt";

    private static Dictionary<string, List<Step>> table;

    private List<Step> steps;
    private Transform caster;
    private Transform target;
    private float ppu;
    private Vector3? groundPos;
    private bool atkToTarget;
    private bool releaseOnly;
    private bool impactOnly;
    private Vector3? releaseOrigin;

    internal static int ParsedScriptCountForAudit
    {
        get
        {
            EnsureParsed();
            return table == null ? 0 : table.Count;
        }
    }

    public static void Cast(
        int skillId,
        Transform caster,
        Transform target,
        float ppu,
        Vector3? groundPos = null,
        bool playerSupportCast = false)
    {
        EnsureParsed();
        if (table != null && table.ContainsKey("Skill" + skillId))
        {
            CastKey("Skill" + skillId, caster, target, ppu, groundPos);
            return;
        }

        CastData(skillId, caster, target, ppu, groundPos, playerSupportCast);
    }

    private static void CastData(
        int skillId,
        Transform caster,
        Transform target,
        float ppu,
        Vector3? groundPos = null,
        bool playerSupportCast = false)
    {
        SkillEffectDB.Entry entry = SkillEffectDB.Get(skillId);
        if (entry == null || !caster)
        {
            return;
        }

        Transform resolvedTarget = target ? target : caster;
        bool attackOnTarget = playerSupportCast && resolvedTarget != caster &&
            entry.hit.Count == 0 && entry.fly.Count == 0 && !groundPos.HasValue;
        GameObject owner = new GameObject("SkillFxData:" + skillId);
        SkillCaster runner = owner.AddComponent<SkillCaster>();
        runner.caster = caster;
        runner.target = resolvedTarget;
        runner.ppu = ppu;
        runner.groundPos = groundPos;
        runner.atkToTarget = attackOnTarget;
        runner.StartCoroutine(runner.RunData(entry));
    }

    private IEnumerator RunData(SkillEffectDB.Entry entry)
    {
        (int cDir, bool cFlip) = CasterFacing(caster);
        int maximumFlyDelay = 0;
        if (!impactOnly) foreach (SkillEffectDB.Ph phase in entry.fly)
        {
            maximumFlyDelay = Math.Max(maximumFlyDelay, phase.delay);
            if (caster == null && !releaseOrigin.HasValue)
            {
                continue;
            }
            EffectPlayer effect = EffectPlayer.PlayNamed(
                releaseOrigin ?? caster.position,
                EffectName(phase),
                ppu,
                1,
                cDir,
                cFlip,
                false);
            if (!effect)
            {
                continue;
            }

            float duration = Mathf.Max(phase.delay, 120);
            if (groundPos.HasValue)
            {
                effect.MoveToPos(groundPos.Value, duration);
            }
            else if (target && target != caster)
            {
                effect.MoveTo(target, duration);
            }
        }

        bool groundAttackAtTarget = groundPos.HasValue && entry.hit.Count == 0;
        List<(int delay, bool whoTarget, int index, int effect)> phases =
            new List<(int, bool, int, int)>();
        if (!impactOnly) foreach (SkillEffectDB.Ph phase in entry.atk)
        {
            phases.Add((phase.delay, groundAttackAtTarget || atkToTarget, phase.index, phase.effect));
        }
        if (!releaseOnly) foreach (SkillEffectDB.Ph phase in entry.hit)
        {
            phases.Add((
                maximumFlyDelay + phase.delay,
                true,
                phase.index,
                phase.effect));
        }

        phases.Sort((left, right) => left.delay.CompareTo(right.delay));
        float elapsed = 0f;
        foreach ((int delay, bool whoTarget, int phaseIndex, int effectId) in phases)
        {
            float wait = delay - elapsed;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait / 1000f);
                elapsed = delay;
            }

            string effectName = EffectName(phaseIndex, effectId);
            if (whoTarget)
            {
                if (groundPos.HasValue)
                {
                    EffectPlayer.PlayNamed(
                        groundPos.Value,
                        effectName,
                        ppu,
                        1,
                        -1,
                        false,
                        false);
                }
                else
                {
                    PlayOn(target, effectName, ppu, -1, false);
                }
            }
            else
            {
                if (releaseOrigin.HasValue)
                    EffectPlayer.PlayNamed(releaseOrigin.Value, effectName, ppu, 1, cDir, cFlip, false);
                else PlayOn(caster, effectName, ppu, cDir, cFlip);
            }
        }

        Destroy(gameObject);
    }

    public static void PlayOn(
        Transform who,
        string name,
        float ppu,
        int dir = -1,
        bool flip = false)
    {
        if (!who)
        {
            return;
        }

        ActorEffects actorEffects = who.GetComponent<ActorEffects>();
        if (actorEffects)
        {
            actorEffects.PlayCast(name, 0, dir, flip);
            return;
        }

        EffectPlayer.PlayNamed(who.position, name, ppu, 1, dir, flip, false);
    }

    private static (int, bool) CasterFacing(Transform value)
    {
        if (!value)
        {
            return (-1, false);
        }

        PlayerController player = value.GetComponent<PlayerController>();
        if (player)
        {
            return (player.CurDir, player.Flip);
        }

        MonsterEntity monster = value.GetComponent<MonsterEntity>();
        return monster ? (monster.CurDir, monster.Flip) : (-1, false);
    }

    public static void CastKey(
        string key,
        Transform caster,
        Transform target,
        float ppu,
        Vector3? groundPos = null)
    {
        EnsureParsed();
        List<Step> parsed;
        if (table == null || !table.TryGetValue(key, out parsed) || parsed.Count == 0)
        {
            return;
        }

        GameObject owner = new GameObject("SkillFx:" + key);
        SkillCaster runner = owner.AddComponent<SkillCaster>();
        runner.steps = parsed;
        runner.caster = caster;
        runner.target = target ? target : caster;
        runner.ppu = ppu;
        runner.groundPos = groundPos;
        runner.StartCoroutine(runner.Run());
    }

    private IEnumerator Run()
    {
        EffectPlayer last = null;
        foreach (Step step in steps)
        {
            switch (step.op)
            {
                case Op.Wait:
                    if (step.num > 0f)
                        yield return new WaitForSeconds(step.num / 1000f);
                    break;
                case Op.Sound:
                    SoundManager.Play((int)step.num);
                    break;
                case Op.PlayNamed:
                    if (step.whoTarget && groundPos.HasValue)
                    {
                        last = EffectPlayer.PlayNamed(groundPos.Value, step.name, ppu,
                            1, -1, false, false);
                    }
                    else
                    {
                        Transform who = step.whoTarget ? target : caster;
                        if (who)
                            last = EffectPlayer.Play(who.position, step.name, ppu,
                                1, -1, false, false);
                    }
                    break;
                case Op.PlayId:
                    if (step.whoTarget && groundPos.HasValue)
                    {
                        last = EffectPlayer.PlayById(groundPos.Value, (int)step.num, ppu);
                    }
                    else
                    {
                        Transform who = step.whoTarget ? target : caster;
                        if (who)
                            last = EffectPlayer.PlayById(who.position, (int)step.num, ppu);
                    }
                    break;
                case Op.MoveTo:
                    if (last != null && groundPos.HasValue)
                        last.MoveToPos(groundPos.Value, step.num);
                    else if (last != null && target != null)
                        last.MoveTo(target, step.num);
                    break;
            }
        }
        Destroy(gameObject);
    }

    // Official 0x06000cdb body (arm64 0x31bbbc/424 B, x86_64 0x32ed40/400 B), branch for branch:
    //   - `table` is created with the parameterless Dictionary ctor. Both architectures call the
    //     ctor with only the instance and the generic MethodInfo (arm64 0x31bc5c bl 0x6b9324 with
    //     x1 = usage 3859 only; x86_64 0x32edd9 call 0x6cf6f0 with rdi/rdsi, rdx untouched), and
    //     no StringComparer.Ordinal is ever read - the method resolves exactly six metadata slots
    //     (3859 ctor, 545 Dictionary<string,List<Step>>, 2095 UnityEngine.Object, 2298 ResLoader,
    //     2405 SkillCaster, 15699 "res/skillevent.txt"), none of which is StringComparer.
    //   - the bytes come from `ResLoader.I.Pkg.Read(ScriptPath)` directly. ResLoader.I is the
    //     ResLoader static field at offset 0 and Pkg is its instance field at +0x20
    //     (`ResLoader.<Pkg>k__BackingField`, PkgArchive), so the callee is the instance
    //     PkgArchive.Read(String) 0x06000bf4 - there is no ResLoader.ReadRes edge, hence no
    //     res24/Pkg24 fallback and no StatReads/StatMisses side effect here.
    //   - `ResLoader.I` is skipped when Object.op_Inequality(I, null) is false; the Pkg null test
    //     also skips, but the receiver of the instance Read call throws NullReferenceException if
    //     the second read yields null (arm64 0x31bd24 / x86_64 0x32ee8e -> noreturn throw helper).
    //   - the only guard on the bytes is `bytes != null` (arm64 0x31bd3c / x86_64 0x32eea7); there
    //     is no Length == 0 test before Gbk.GetString, which itself returns string.Empty for an
    //     empty array, and ParseAll already returns on an empty string.
    //   - the decoded text is Gbk.GetString(Byte[]) 0x06000471 (arm64 0x31bd44 -> 0x446834,
    //     x86_64 0x32eeae -> 0x45e800), then ParseAll is tail-called (arm64 0x31bd50 b 0x31c814,
    //     x86_64 0x32eeba jmp 0x32f8b0). No System.Text.Encoding / code page anywhere in the body.
    //   - no LSDA landing pad and no __cxa_* call on either architecture: no try/catch/finally,
    //     and after any skipped read `table` stays non-null, so nothing is retried.
    private static void EnsureParsed()
    {
        if (table != null)
        {
            return;
        }

        table = new Dictionary<string, List<Step>>();
        if (ResLoader.I == null)
        {
            return;
        }

        if (ResLoader.I.Pkg == null)
        {
            return;
        }

        byte[] bytes = ResLoader.I.Pkg.Read(ScriptPath);
        if (bytes == null)
        {
            return;
        }

        ParseAll(Gbk.GetString(bytes));
    }

    // Official 0x06000cdc body (arm64 0x31c814/1712 B, x86_64 0x32f8b0/1824 B), branch for branch:
    //   - the text is split with `Split(Char, StringSplitOptions)` on '\n' only (arm64 0x31c94c
    //     `mov w1,#0xa` / 0x31c950 `mov w2,#0` / x86_64 `mov edx,0xa` `mov r8d,0`), so there is no
    //     Replace("\r", ...) and no StringSplitOptions.RemoveEmptyEntries. The only guard on the
    //     receiver is the implicit null check (arm64 0x31c948 cbz -> 0x31cec0, x86_64
    //     `test rdi,rdi; je`): a null text throws NullReferenceException, it is not skipped.
    //   - every line is Trim()-ed first (arm64 0x31c9d8 / x86_64 0x32fa84, the parameterless
    //     overload), then searched with IndexOf(String) "function " (StringLiteral 14941, 9 chars,
    //     which is why the name starts at found + 9: arm64 0x31c930 `add w1,w23,#9`). The header is
    //     recognised anywhere in the line, not only at index 0, and it only takes effect when
    //     IndexOf('(') is > 0 (arm64 0x31c928 `cmp w0,#1; b.lt`, x86_64 `cmp esi,1; jl`); otherwise
    //     currentKey / current / variable are left untouched.
    //   - "end" and "--" are StartsWith(String) tests (StringLiteral 14855 / 10937) and an empty
    //     line is skipped through the String.Length test (arm64 0x31c9e4 / x86_64 0x32fac6); there
    //     is no mid-line comment truncation.
    //   - on "end" the finished list is published only when it is not empty (arm64 0x31c9b0
    //     `ldr w8,[x20+0x18]; cmp w8,#1; b.lt`, x86_64 0x32fa94) and through the dictionary indexer
    //     setter (MethodRef[43117]; Cpp2IL lifts the same shape as
    //     Dictionary`2::set_Item), so a repeated function name overwrites instead of throwing.
    //     Only currentKey is cleared afterwards (arm64 0x31c9b8 / x86_64 0x32fa4b).
    //   - the command tests are Contains(String) in this order: GetAtkPreTime (12195), WaitTick
    //     (14037), PlayEffect (13174), EffectMoveTo (11981), PlaySound (13176). GetAtkPreTime sets
    //     `variable` to 500 - ParseF(text after the first '-' whose index is > 0) and, when the same
    //     line also contains "WaitTick", emits the Wait step itself; the "t" argument test is
    //     Contains(StringLiteral 16223 "t"), not an equality test.
    //   - numbers always go through Digits(String) + the two-argument
    //     Single.TryParse(String, Single&) (arm64 0xecccc8 / x86_64 0xeca6f0, three arguments only),
    //     i.e. there is no NumberStyles / CultureInfo argument and no ParseF call: 0x06000cde has no
    //     recorded caller in the atlas.
    //   - Step is written as op (+0x00), num (+0x04), name (+0x08, the only field with a write
    //     barrier), whoTarget (+0x10); EffectMoveTo keeps name null and whoTarget false, and the
    //     quoted PlayEffect argument is trimmed with a three element char array {'\'', '"', ' '}
    //     (newarr 3 at arm64 0x31cc70 `mov w1,#3` + InitializeArray of the field
    //     2985A56178646E7200FF88969C8181D39C7873ECBC5716D98D448B5D273A34BF, whose managed blob is
    //     27 00 22 00 20 00) before EndsWith(String) ".eff" drops the last four characters.
    private static void ParseAll(string text)
    {
        string[] lines = text.Split('\n');
        string currentKey = null;
        List<Step> current = null;
        float variable = 0f;
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            int function = line.IndexOf("function ");
            if (function >= 0)
            {
                int open = line.IndexOf('(');
                if (open > 0)
                {
                    currentKey = line.Substring(function + 9, open - (function + 9)).Trim();
                    current = new List<Step>();
                    variable = 0f;
                }
                continue;
            }

            if (currentKey == null)
            {
                continue;
            }

            if (line.StartsWith("end"))
            {
                if (current.Count > 0)
                {
                    table[currentKey] = current;
                }
                currentKey = null;
                continue;
            }

            if (line.StartsWith("--") || line.Length == 0)
            {
                continue;
            }

            if (line.Contains("GetAtkPreTime"))
            {
                float offset = 0f;
                int minus = line.IndexOf('-');
                if (minus > 0)
                {
                    float.TryParse(Digits(line.Substring(minus + 1)), out offset);
                }
                variable = ATK_PRE_TIME - offset;
                if (line.Contains("WaitTick"))
                {
                    current.Add(new Step
                    {
                        op = Op.Wait,
                        num = variable,
                    });
                }
                continue;
            }

            if (line.Contains("WaitTick"))
            {
                string argument = ArgN(line, 0);
                float wait = variable;
                if (!argument.Contains("t"))
                {
                    float.TryParse(Digits(argument), out wait);
                }
                current.Add(new Step
                {
                    op = Op.Wait,
                    num = wait,
                });
                continue;
            }

            if (line.Contains("PlayEffect"))
            {
                string effect = ArgN(line, 0);
                string who = ArgN(line, 1);
                bool whoTarget = who.Contains("attackee");
                int quote = effect.IndexOf('\'');
                char marker = quote < 0 ? '"' : '\'';
                if (effect.IndexOf(marker) < 0)
                {
                    float id = 0f;
                    float.TryParse(Digits(effect), out id);
                    current.Add(new Step
                    {
                        op = Op.PlayId,
                        num = id,
                        whoTarget = whoTarget,
                    });
                    continue;
                }

                string name = effect.Trim('\'', '"', ' ');
                if (name.EndsWith(".eff"))
                {
                    name = name.Substring(0, name.Length - 4);
                }
                current.Add(new Step
                {
                    op = Op.PlayNamed,
                    name = name,
                    whoTarget = whoTarget,
                });
                continue;
            }

            if (line.Contains("EffectMoveTo"))
            {
                float duration = 0f;
                float.TryParse(Digits(ArgN(line, 2)), out duration);
                current.Add(new Step
                {
                    op = Op.MoveTo,
                    num = duration,
                });
                continue;
            }

            if (line.Contains("PlaySound"))
            {
                float sound = 0f;
                float.TryParse(Digits(ArgN(line, 0)), out sound);
                current.Add(new Step
                {
                    op = Op.Sound,
                    num = sound,
                });
            }
        }
    }

    // Official 0x06000cdd body (arm64 0x31d020/240 B, x86_64 0x330130): the argument list is the
    // text between the first '(' and the last ')' split on ','; a missing or inverted bracket pair
    // returns StringLiteral 10335 ("") and an index past the end returns "" as well, while a
    // negative index runs into the unsigned bounds check (arm64 0x31d0d4 -> 0x177a80).
    private static string ArgN(string line, int index)
    {
        int open = line.IndexOf('(');
        int close = line.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return string.Empty;
        }

        string[] arguments = line.Substring(open + 1, close - open - 1).Split(',');
        return index < arguments.Length ? arguments[index].Trim() : string.Empty;
    }

    // Official 0x06000cde body (arm64 0x31d110/48 B, x86_64 0x330210): one zeroed local, Digits and
    // the two-argument Single.TryParse, so a failing parse yields 0. The atlas records no caller
    // for this token - ParseAll performs the same two calls inline.
    private static float ParseF(string value)
    {
        float result = 0f;
        float.TryParse(Digits(value), out result);
        return result;
    }

    // Official 0x06000cdf body (arm64 0x31cec4/348 B, x86_64 0x32ffd0/352 B): the StringBuilder is
    // allocated before the string is touched, a null argument throws NullReferenceException, and
    // only Char.IsDigit or '.' is kept - '-' and '+' are dropped.
    private static string Digits(string value)
    {
        StringBuilder result = new StringBuilder();
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (char.IsDigit(current) || current == '.')
            {
                result.Append(current);
            }
        }
        return result.ToString();
    }

    private static string EffectName(SkillEffectDB.Ph phase)
    {
        return EffectName(phase.index, phase.effect);
    }

    private static string EffectName(int index, int effect)
    {
        return string.Format("TFX{0:D3}_{1}", effect, index);
    }


}

}
