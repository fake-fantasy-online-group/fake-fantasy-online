using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// The one Presentation implementation of <see cref="IWorldReceiveOwner"/>. It owns nothing:
/// every member writes the official static owner that Dispatch case 0x8044 / 0x804c writes,
/// so the recovered fields stay the single source for both the receive path and the UI.
/// </summary>
/// <remarks>
/// The official cases write these statics on the receive thread while the case body runs, with
/// no world-frame gate and no dependency on the world window being built. Protocol cannot name
/// <c>CombatWorld</c> or <c>CharData</c>, so the composition root binds this adapter and the
/// gateway calls it at the same point of the same packet; the per-frame UI projection then only
/// reads the owner.
/// </remarks>
public sealed partial class ReferenceWorldReceiveOwner : IWorldReceiveOwner, ISkillBookStreamReceiveOwner
{
    /// <summary>Official 0x800d tail call; attachment and replacement stay with SpawnOn.</summary>
    public void ReceiveSpawnOnEffect(int target, string effectName)
    {
        EffectPlayer.SpawnOn(target, effectName);
    }

    // Original 0x8003: clear +0xc8/+0xcc/+0xd0 before Buf.I32, then store its
    // return at NetClient +0x7c. The protocol cursor stays between these two calls.
    public void BeginSelfData() => NetClient.BeginSelfDataForIndependentRuntime();

    public void ReceiveSelfDataIdentity(int entityId) => NetClient.SelfEntityId = entityId;

    public void ReceiveEntrySettings(int? petViewMask, bool? smartCast)
    {
        if (petViewMask.HasValue && petViewMask.Value >= 0)
            GameSettings.ApplyPetViewMask(petViewMask.Value);
        if (smartCast.HasValue)
        {
            GameSettings.EnsureLoaded();
            if (GameSettings.SmartCast != smartCast.Value)
            {
                GameSettings.SmartCast = smartCast.Value;
                GameSettings.Save();
            }
        }
    }

    /// <summary>
    /// Official 0x8044: arm64 <c>ldr w8,[x0,#0xb8]; str w19,[x8,#0x8]</c> at 0x25419c /
    /// x86_64 <c>movq 0xb8(%rdi),%rax; movl %ebx,0x8(%rax)</c> at 0x26341b, with the class
    /// pointer taken from metadata usage ordinal 1463 (TypeInfo:CombatWorld) and no condition
    /// between the Buf.I32 call and the store. <c>CombatWorld.LocalFaction</c> is field token
    /// 67109276, System.Int32, static, atlas offset 8.
    /// </summary>
    public void ReceiveLocalFaction(WorldLocalFactionState state)
    {
        CombatWorld.LocalFaction = state == null ? 0 : state.Faction;
    }

    public void ClearOwnedTitles()
    {
        CharData.ClearOwnedTitlesForIndependentRuntime();
    }

    public void AddOwnedTitle(string title)
    {
        CharData.AddOwnedTitleForIndependentRuntime(title);
    }

    public void SetCurrentTitle(string title)
    {
        CharData.SetCurrentTitleForIndependentRuntime(title);
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd) entry store of
    /// <c>CharData.lastLogin</c> (+0x28): arm64 export lines 152-159 and x86_64 lines 162-169 of
    /// the frozen dual-architecture body (wbb-072-0x06000efd-{arm64,x86_64}-20260930/06000efd.c),
    /// which stands immediately after the seven-scalar block and immediately before the official
    /// <c>CharData.title</c> store. One host action, one store, no argument read from the packet.
    /// </summary>
    /// <remarks>
    /// In the product this store is awaited immediately before the official
    /// <see cref="SetCurrentTitle"/> step, so the two entry stores keep the official order
    /// (<c>lastLogin</c> then <c>title</c>); the earlier arrangement had <c>title</c> run before
    /// this one. No step between those two positions reads or writes either static - the named
    /// consumers run from the per-frame UI pass after the entry has finished - so only the
    /// relative order changed, not what those consumers observe.
    /// </remarks>
    public void ReceiveEntryLastLogin()
    {
        CharData.SetEntryLastLoginForIndependentRuntime();
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd) identity stores: arm64 export lines 102 and
    /// 109 and x86_64 lines 108 and 114 of the frozen dual-architecture body
    /// (wbb-072-0x06000efd-{arm64,x86_64}-20260930/06000efd.c), the two stores that stand
    /// between the case's own preamble and the <c>CharData.attrs</c> (+0xb0) store.
    /// <c>CharData.name</c> (+0x0) takes <c>EnterInfo.name</c> and <c>CharData.charId</c>
    /// (+0x8, System.Int64) takes <c>EnterInfo.charId</c> - the Int64 at atlas offset 128 of
    /// <c>NetClient+EnterInfo</c>, which case 0x8003 fills from the I64 tail of the self-data
    /// payload (arm64 <c>0x250948 bl #0x274eac</c> behind the <c>Remaining &gt;= 8</c> gate at
    /// 0x250934-0x25093c, landing at <c>0x2585c8 str x0,[sp,#0x1050]</c>; a payload without the
    /// tail takes <c>0x2585c4 mov x0,#0</c> into the same slot) and not from the network entity
    /// id, which the same case stores outside this struct at <c>0x250720 str w0,[x8,#0x7c]</c>.
    /// Both stores are plain writes with no filter, clamp or default between the read and the
    /// slot, so a zero id and the server's own name are stored exactly as they arrived.
    /// </summary>
    /// <remarks>
    /// These two static slots are also written later by the world build -
    /// <c>ReferenceFirstWorldInitialFrame</c> stores <c>CharData.charId</c> from the same
    /// <c>WorldEntryRecord.PersistentCharacterId</c> and <c>GameBootstrap.ConfigureWorld</c>
    /// stores <c>CharData.name</c> from the same <c>WorldEntryRecord.DisplayName</c> - so those
    /// later stores repeat the values this entry step already published and are left in place.
    /// What this step adds is the official timing: both identity fields are published before
    /// <see cref="LoadEntryNameMode"/> derives its <c>namemode_</c> key, so the preference is
    /// read under the persistent id the server sent instead of the display name the selection
    /// mirror left behind.
    /// </remarks>
    public void ReceiveEntryIdentity(string name, long characterId)
    {
        CharData.name = name;
        CharData.charId = characterId;
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd) call to <c>CharData.LoadNameMode()</c>
    /// (0x06000f2b): arm64 export line 110 / x86_64 line 116, between the <c>CharData.charId</c>
    /// (+0x8) store and the <c>CharData.attrs</c> (+0xb0) store. The callee is the already
    /// recovered carrier in CharData.NameMode.cs: empty suffix returns without caching, an
    /// unchanged suffix short-circuits before the preference read, otherwise the suffix is
    /// cached and <c>GetPref("namemode_", 0)</c> is clamped into <c>CharData.nameMode</c>.
    /// </summary>
    public void LoadEntryNameMode()
    {
        CharData.LoadNameMode();
    }

    /// <summary>
    /// Official 0x804b: the case body reads the whole packet and then tail-calls
    /// <c>NpcChatter.SetServerChatter</c> (0x0600090f), which replaces the five process-wide
    /// chatter collections and overwrites the three timings only for positive values. The
    /// projection is the recovered signature - same argument list, same
    /// <c>enabled != 0</c> narrowing - and it is the only writer, so the tables are replaced
    /// once per received packet instead of once per frame.
    /// </summary>
    /// <remarks>
    /// The official tail call has no result; the projection's row guard is a product-only null
    /// check on a list the decoder always fills with non-null rows, so nothing is branched on
    /// here. The write itself does not depend on the world being built: <c>SetServerChatter</c>
    /// touches process-wide statics only, and the caller guarantees the host thread because
    /// <c>NpcChatter.EnsureData</c> loads through <c>GameTables</c> and
    /// <c>Font.GetOSInstalledFontNames</c>, both of which are host-only Unity entry points.
    /// </remarks>
    public void ReceiveNpcChatter(WorldNpcChatterState state)
    {
        ReferenceNpcGreetingCatalog.ApplyServerChatterNativeProjection(state);
    }

    /// <summary>
    /// Official 0x8028: the case body reads the whole packet and then tail-calls
    /// <c>TaskClient.SetOffers</c> (0x06000e5e), which stores the NPC name, clears the offer list,
    /// refills it and fires <c>OnOffersChanged</c>. The official owner is the
    /// <c>TaskClient</c> static itself, so this port writes that static and nothing else; the
    /// dialog reaches the same replacement through the single <c>OnOffersChanged</c> slot that
    /// <c>DialogUI.Show</c> (0x06000211) assigns, exactly as <c>DialogUI.OnTaskOffers</c>
    /// (0x06000212) does.
    /// </summary>
    /// <remarks>
    /// The write does not depend on the world being built or on the dialog being open:
    /// <c>SetOffers</c> touches process-wide statics only, and the callback is a null-conditional
    /// invoke, so a packet that arrives while no dialog exists still replaces the offers. A null
    /// state is the product-only guard for a caller that already decoded a packet; the official
    /// case has no such branch because it always holds the list it just read.
    /// </remarks>
    public void ReceiveNpcTaskOffers(WorldNpcTaskOffersState state)
    {
        if (state == null)
        {
            return;
        }

        TaskClient.SetWorldOffers(state.NpcName, state.Offers);
    }

    /// <summary>
    /// Official 0x8027: the case body reads both counts and every row and then tail-calls
    /// <c>TaskClient.SetState</c> (0x06000e5d, arm64 case entry 0x260a08 tail
    /// <c>bl 0x36e7d8</c> / x86_64 tail <c>call 0x380e60</c>). The official owner is the
    /// <c>TaskClient</c> statics themselves, so this port performs that one replacement and
    /// nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The row conversion is part of the official case, not of a later projection: the case
    /// strips the three rich-code strings as they are read, stores the raw ready byte and stamps
    /// each row with <c>Time.realtimeSinceStartup</c> at construction, so the rows a later frame
    /// reads are the ones that were built at receipt. A null state is the product-only guard for
    /// a caller that already decoded a packet; the official case has no such branch because it
    /// always holds the lists it just read.
    /// </para>
    /// <para>
    /// Nothing here compares the incoming snapshot with a remembered one. The official case has
    /// no equality test, so two replies that decode to the same rows - and two empty replies -
    /// still perform two whole replacements and fire <c>OnStateChanged</c> twice, and the per-row
    /// stamp makes a second application of an already-applied snapshot observable as a later
    /// stamp. That is why the row conversion and the one <c>SetState</c> call stay together at
    /// this receive point and no frame or build re-applies them.
    /// </para>
    /// </remarks>
    public void ReceiveTaskLog(WorldTaskLogState state)
    {
        if (state == null)
        {
            return;
        }

        List<TaskClient.ActiveTask> active =
            new List<TaskClient.ActiveTask>(state.Active.Count);
        foreach (WorldActiveTaskState row in state.Active)
        {
            active.Add(new TaskClient.ActiveTask
            {
                id = row.TaskId, remainSec = row.RemainingSeconds, step = row.Step, total = row.Total,
                text = RichCode.Strip(row.Text),
                progress = RichCode.Strip(row.Progress),
                to = RichCode.Strip(row.TurnInNpc),
                ready = row.ReadyByte, recvTime = UnityEngine.Time.realtimeSinceStartup,
            });
        }

        TaskClient.SetState(active, new List<int>(state.Done));
    }

    /// <summary>
    /// Official 0x8017: the case builds one new <c>List&lt;CharData.BuffView&gt;</c> with the row
    /// count as its capacity, reads every row through one <c>NetClient.Buf</c> - I32 skillId
    /// (+0x10), I32 iconId (+0x14), I32 remainSec (+0x18), Str name (+0x20), Str desc (+0x28),
    /// U8 good as a bool (+0x34), U8 posMode sign-extended with <c>sxtb</c>/<c>movsbl</c> (+0x38)
    /// and <c>Time.realtimeSinceStartup</c> into recvTime (+0x30), read once per row inside the
    /// loop - and then stores that list into the <c>CharData</c> buff static (arm64
    /// <c>str x20,[x0,#0xc0]!</c> at 0x258880 / x86_64 <c>movq %rsi,0xc0(%rdi)</c> at 0x2653ad,
    /// both followed by the write barrier), the two row aggregates and the double-exp tail.
    /// </summary>
    /// <remarks>
    /// The four members below are those steps in their native order and they are driven from
    /// inside one host action by the reader that owns the single <c>NetClient.Buf</c>
    /// (<c>PrivateServerWorldStateDecoder.ReadBuffStateInto</c>), so the static never becomes
    /// observable with a half-filled list.
    /// </remarks>
    private List<CharData.BuffView> buffRows;

    /// <summary>
    /// Official 0x8017 step 1. The count is the wire value, so the list is built with the wire
    /// capacity and an empty packet still replaces the static with an empty list.
    /// </summary>
    public void BeginBuffRows(int count)
    {
        buffRows = new List<CharData.BuffView>(count);
    }

    public void AddBuffRow(
        int skillId,
        int iconId,
        int remainingSeconds,
        string name,
        string description,
        bool good,
        int positionMode,
        float receiveTime)
    {
        buffRows.Add(
            new CharData.BuffView
            {
                skillId = skillId,
                iconId = ResolveBuffIcon(skillId, name, iconId),
                remainSec = remainingSeconds,
                name = name,
                desc = description,
                good = good,
                posMode = positionMode,
                recvTime = receiveTime,
            });
    }

    public void CompleteBuffRows(bool invisible, byte immobile)
    {
        CharData.buffs = buffRows;
        CharData.invisible = invisible;
        CharData.immobile = immobile;
    }

    public void CompleteBuffTail(bool doubleExperience)
    {
        CharData.doubleExp = doubleExperience;
    }

    /// <summary>
    /// Official 0x8007 steps 5 and 6, and the one array store of the 0x8003 enter callback.
    /// Both arrays are stored into the <c>CharData</c> statics inside the case body, in the
    /// official order: the second array into <c>CharData.attrsTotal</c> (+0xb8) first (arm64
    /// 0x250c50 / x86_64 0x25f42b) and only then the first array into <c>CharData.attrs</c>
    /// (+0xb0) (arm64 0x250c7c / x86_64 0x25f467). This is the only writer of those two statics:
    /// the per-frame local-movement branch used to re-bind them and therefore skipped every
    /// death, mount-anchor and work frame, which no official frame does.
    /// </summary>
    /// <remarks>
    /// A null argument is "no store", not "store null": 0x8003 never writes +0xb8 and 0x8007
    /// skips that store when its second count is zero, so an absent totals array leaves the
    /// previous one in place and every official reader keeps falling back to <c>CharData.attrs</c>
    /// until a packet really publishes totals. Both arguments are copied element by element, the
    /// way the official case allocates one <c>new int[count]</c> per array; nothing is padded,
    /// defaulted or length-adjusted here, and the write does not depend on the world frame being
    /// built because the official case has no such gate.
    /// </remarks>
    public void ReceivePlayerAttributes(
        IReadOnlyList<int> attributes,
        IReadOnlyList<int> attributeTotals)
    {
        if (attributeTotals != null)
        {
            CharData.attrsTotal = CopyAttributeArray(attributeTotals);
        }

        if (attributes != null)
        {
            CharData.attrs = CopyAttributeArray(attributes);
        }
    }

    /// <summary>
    /// Official 0x8007 steps 8..14 and the enter callback's step 8, in the official index order
    /// and with the official offsets: level (+0x20) = element 0, exp (+0x68) = 1, expToNext
    /// (+0x6c) = 2, hp (+0x58) = 3, maxHp (+0x5c) = 4, mp (+0x60) = 5 and maxMp (+0x64) = 6.
    /// The offsets are the frozen 1.5.8 atlas offsets of the seven fields and the index order is
    /// the store order of both architectures, so the write is positional and not a named
    /// projection: element 1 goes to +0x68 because both case bodies store it there.
    /// </summary>
    private static void StoreSevenAttributeFields(IReadOnlyList<int> attributes)
    {
        CharData.level = attributes[0];
        CharData.exp = attributes[1];
        CharData.expToNext = attributes[2];
        CharData.hp = attributes[3];
        CharData.maxHp = attributes[4];
        CharData.mp = attributes[5];
        CharData.maxMp = attributes[6];
    }

    public void ReceivePlayerAttributeFields(IReadOnlyList<int> attributes)
    {
        // Official 0x8007 arm64 0x250c8c / 0x250c90 / 0x250c94 / 0x250c98 (x86_64 0x25f47c):
        // a null first array or one with fewer than seven elements performs none of the seven
        // stores, so the values an earlier packet or the enter callback left behind stay.
        if (attributes == null || attributes.Count < 7)
        {
            return;
        }

        StoreSevenAttributeFields(attributes);
    }

    public void ReceiveEnterAttributeFields(IReadOnlyList<int> attributes)
    {
        // Official DoEnterWorld arm64 0x391cf4 cbz x21 and 0x391d04 cbz x8 (x86_64 0x3a5095 /
        // 0x3a50aa): a null or empty attrs array performs no store at all, so the level the
        // selection mirror wrote from the roster stays.
        if (attributes == null || attributes.Count == 0)
        {
            return;
        }

        // arm64 0x391d0c-0x391d28 (x86_64 0x3a50b4-0x3a50d5): a length of one to six stores
        // element 0 into level only and then leaves the block, so the other six scalars keep
        // their previous values; no zero, default or roster value is written over them.
        if (attributes.Count < 7)
        {
            CharData.level = attributes[0];
            return;
        }

        StoreSevenAttributeFields(attributes);
    }

    /// <summary>
    /// <see cref="IWorldReceiveOwner.ReadEntryRetainedScalars"/>: the current content of the same
    /// seven statics <see cref="StoreSevenAttributeFields"/> writes, in the same order, and no more.
    /// The read adds no owner state and performs no store, so it can answer at any time the enter
    /// gate asks without changing what those statics hold.
    /// </summary>
    /// <remarks>
    /// <c>CharData.level</c> is read here for the same reason the six are: the enter callback's
    /// skipped level store leaves the static alone, and the static is what the selection mirror
    /// TitleFlow.MirrorSlotToCharData (0x06000efb) wrote from the roster unless a startup 0x8007
    /// with at least seven elements - the only other writer, arm64 0x250c8c / x86_64 0x25f47c -
    /// already overwrote it. Returning the static therefore covers both orders, while a roster
    /// value returned here would be wrong for the second one.
    /// </remarks>
    public WorldEntryRetainedScalars ReadEntryRetainedScalars()
    {
        return new WorldEntryRetainedScalars(
            CharData.level,
            CharData.exp,
            CharData.expToNext,
            CharData.hp,
            CharData.maxHp,
            CharData.mp,
            CharData.maxMp);
    }

    /// <summary>
    /// Official 0x8007 step 7: element 23 of the FIRST array goes into
    /// <c>InventoryDlg.MaxWeight</c>, the static System.Int32 at +0x24 that
    /// <c>InventoryDlg.OnGUI</c> reads as the weight denominator (arm64 <c>0x259f94 str w22,
    /// [x9,#0x24]</c> / x86_64 <c>0x268904 movl %r12d,0x24(%rcx)</c>, both reached through
    /// metadata usage ordinal 1929 <c>TypeInfo:InventoryDlg</c>). The value is the int the wire
    /// carried, stored as it arrived: no clamp, no default and no fallback to the 100 field
    /// initializer of <c>InventoryDlg</c>, because the caller only reaches this member when the
    /// packet's first array really holds element 23.
    /// </summary>
    public void ReceiveInventoryWeightLimit(int maximumWeight)
    {
        InventoryDlg.MaxWeight = maximumWeight;
    }

    /// <summary>
    /// Official 0x8006 / 0x8069 step: the packet's own Money (+0x10) and CaiYu (+0x18) - the
    /// first two i64 header reads - then Weight (+0x20) and MaxWeight (+0x24), stored in that
    /// order as they arrived. Values are written exactly as the wire carried them - a
    /// server-sent 0 or negative value is a real value and is not replaced by a field
    /// initializer of <c>InventoryDlg</c>, and no cross-scene reset happens here because the
    /// official statics are never cleared when a world is left. Nothing compares the incoming
    /// value with the retained mirror: these four slots are written by the packet that carried
    /// them and never re-projected from a later state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The packet's third i64 is the fifth and last store of the same case body and does not
    /// touch <c>InventoryDlg</c>: it writes <c>CharData.honor</c> (+0x70), after the four slots
    /// above and after the <c>CharData</c> class-init check, so it stays last here as well.
    /// The long is stored whole - never truncated to int, never defaulted - and a
    /// <c>WorldInventoryState.Empty</c> identity is not a packet, so this member is only
    /// reached when a 0x8006 or 0x8069 really arrived.
    /// </para>
    /// </remarks>
    public void ReceiveInventoryHeader(
        long money, long caiYu, int weight, int maximumWeight, long honor)
    {
        InventoryDlg.Money = money;
        InventoryDlg.CaiYu = caiYu;
        InventoryDlg.Weight = weight;
        InventoryDlg.MaxWeight = maximumWeight;
        CharData.honor = honor;
    }

    /// <summary>
    /// Official 0x8071: the one host action that performs the four <c>CharData</c> stores of the
    /// case body in their native order, including the host clock read that stands after the third
    /// store. The three wire values are written exactly as they arrived - a zero charge count, a
    /// zero or negative ttl and a zero skill id are all real values and none of them is replaced
    /// by a field initializer - and two packets that arrive in one frame both run the whole
    /// action, so the later packet's values and its later clock are the ones that survive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here clears <c>counterRecvTime</c> and nothing resets the three counts: the 0x8003
    /// SelfData prefix reached through <see cref="ReceiveSelfDataIdentity"/> is the only official
    /// clearer and it leaves +0xd4 alone on both architectures.
    /// </para>
    /// <para>
    /// This member is a writer and not a display step: the two official readers
    /// <c>CharData.CounterChargesNow</c> (0x06000f1b) and <c>CharData.CounterRemainSec</c>
    /// (0x06000f1c) keep their recovered bodies and no icon, counter or HUD element is drawn or
    /// refreshed from here.
    /// </para>
    /// </remarks>
    public void ReceiveCounterWindow(int charges, int ttlMilliseconds, int skillId)
    {
        CharData.ReceiveCounterWindowForIndependentRuntime(charges, ttlMilliseconds, skillId);
    }

    /// <summary>
    /// Official 0x805c, the whole case body in its native order: the six unconditional reads land in
    /// the seven <c>CharData</c> raid slots and the clock is taken after the sixth store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here clears the slots and nothing resets them: a register-tracked scan of the arm64
    /// il2cpp section for stores against a <c>CharData</c> statics base finds case 0x805c as the
    /// only writer of +0x91..+0xa8, and the 0x8003 SelfData prefix - the official clearer of the
    /// sibling counter slots - writes +0xc8 and +0xd0 only.
    /// </para>
    /// <para>
    /// This member is a writer and not a display step: the official reader
    /// <c>GameHud.DrawRaidTimer</c> (0x0600044a) keeps its recovered body and no countdown, timer
    /// or HUD element is refreshed from here.
    /// </para>
    /// </remarks>
    public void ReceiveRaidState(
        bool active,
        int wave,
        int total,
        int nextSec,
        int remainSec,
        int alive)
    {
        CharData.ReceiveRaidStateForIndependentRuntime(active, wave, total, nextSec, remainSec, alive);
    }

    /// <summary>
    /// Historical Editor audit setup for an already-decoded state. No product receipt calls
    /// this snapshot adapter; the raw receipt uses IHotbarReceiveOwner between wire reads.
    /// </summary>
    public void ReceiveHotbar(WorldHotbarState state)
    {
        if (state == null)
        {
            return;
        }

        IReadOnlyList<WorldHotbarSlotState> slots = state.Slots;
        int[] ids = new int[slots.Count];
        int[] kinds = new int[slots.Count];
        int[] icons = new int[slots.Count];
        for (int index = 0; index < slots.Count; index++)
        {
            WorldHotbarSlotState slot = slots[index];
            ids[index] = slot.Id;
            kinds[index] = (int)slot.Kind;
            icons[index] = slot.IconId;
            if (kinds[index] == 1 && ids[index] != 0 && icons[index] >= 1)
            {
                CharData.petSkillIcons[ids[index]] = icons[index];
            }
        }

        GameHud.LoadHotbar(ids, kinds, state.Expanded, icons);
    }

    public void ReceiveSkillBook(ISkillBookReader reader, out WorldSkillBookObservation observation) =>
        CharData.ReceiveSkillsForIndependentRuntime(reader, out observation);

    public void ReceiveNewbieSkillBookTip(WorldSkillBookObservation observation)
    {
        if (observation.HasValue && observation.SkillCount > 0) NewbieTipBox.Push(7);
    }

    /// <summary>Apply reached skill-root stores and capture their actual retained fields.</summary>
    public WorldSkillBookState ReceiveSkillBook(WorldSkillBookReceipt receipt) =>
        CharData.ReceiveSkillsForIndependentRuntime(receipt);

    /// <summary>
    /// Official 0x8015 tail: the one <c>NewbieTipBox.Push(7)</c> the case performs when the skill
    /// dictionary it just stored holds at least one row (arm64 0x253478-0x2534e0 / x86_64
    /// 0x2625b8-0x262608). The push is the official side effect in its official place - after the
    /// row loop of this very packet and before the next packet is read - so it runs once per
    /// received 0x8015 and never from a later snapshot replay. <c>Push</c> owns the whole rest of
    /// the chain: it is idempotent per id through <c>EnsureShown</c>, and it is the only caller of
    /// the 0x1076 TipSeen send, so no second tip path and no second send is introduced here.
    /// </summary>
    public void ReceiveNewbieSkillBookTip(WorldSkillBookState state)
    {
        if (state == null || state.Skills.Count <= 0)
        {
            return;
        }

        NewbieTipBox.Push(7);
    }

    /// <summary>
    /// Official 0x800b tail: the two pushes the case performs over the skill list it just stored -
    /// 51 when one row has <c>level &gt; 0</c> (arm64 0x25e7b0 / x86_64 0x26d4f3) and then 52 when
    /// one row has <c>id == 13002</c> and <c>level &gt;= 1</c> (arm64 0x25e838 / x86_64 0x26d586),
    /// each search stopping at its first match and 51 always running first. Both pushes are the
    /// official side effect in its official place - after every pet field store of this very packet
    /// and before the next packet is read - so they run once per received 0x800b and never from a
    /// later snapshot replay. <c>Push</c> itself reads no pet field: it only touches the role-scoped
    /// seen set and the 0x1076 send, so performing this step while the retained pet snapshot is
    /// still unpublished cannot change which id is pushed - this packet's own decoded rows decide,
    /// and the pet mirror stays on the existing publication path. <c>Push</c> owns the whole rest of the chain: it is idempotent per id
    /// through <c>EnsureShown</c> and it is the only caller of the 0x1076 TipSeen send, so no second
    /// tip path, no second send and no external dedup is introduced here.
    /// </summary>
    public void ReceiveNewbiePetSkillsTip(WorldPlayerPetStatusState pet)
    {
        if (pet == null)
        {
            return;
        }

        foreach (WorldPlayerPetSkillState skill in pet.Skills)
        {
            if (skill.Level > 0)
            {
                NewbieTipBox.Push(51);
                break;
            }
        }

        foreach (WorldPlayerPetSkillState skill in pet.Skills)
        {
            if (skill.Id == 13002 && skill.Level >= 1)
            {
                NewbieTipBox.Push(52);
                break;
            }
        }
    }

    /// <summary>
    /// Official 0x8054: the one <c>NewbieTipBox.SeedServer</c> tail call the case performs with the
    /// list it just filled from this packet (arm64 0x25517c / x86_64 0x264683), including the
    /// zero-count case. The seed is the official side effect in its official place - after this
    /// packet's id loop and before the next packet is read - so it runs once per received 0x8054 and
    /// never from a later replay. <c>SeedServer</c> owns the whole rest of the chain: it resolves
    /// the role-scoped seen set through <c>EnsureShown</c> and writes <c>PlayerPrefs</c> only when
    /// an id was new, so no default-seen set, no queue and no second persistence path is added here.
    /// </summary>
    public void ReceiveNewbieTipSeed(List<int> ids)
    {
        NewbieTipBox.SeedServer(ids);
    }

    /// <summary>
    /// Official 0x8001: the one store of <c>Environment.TickCount - (int)sentTick</c> into the
    /// <c>NetClient.RttMs</c> static (System.Int32, atlas static offset 16), arm64 0x24be64
    /// <c>str w19,[x8,#0x10]</c> / x86_64 0x2596fa <c>movl %ebx,0x10(%rax)</c>, reached only
    /// after the 0..59999 gate of the same case. No clamp, filter or default is applied here -
    /// the value is stored exactly as Protocol computed it, and the -1 the class constructor
    /// wrote survives until a reply really arrives.
    /// </summary>
    public void ReceiveRoundTripLatency(int latencyMs)
    {
        NetClient.RttMs = latencyMs;
    }

    /// <summary>
    /// Official 0x805b: the one store of <c>Buf.U16()</c> into the <c>NetClient.OnlineCount</c>
    /// static (System.Int32, atlas static offset 20), arm64 0x24cd10 <c>str w0,[x8,#0x14]</c> /
    /// x86_64 0x25a70d <c>movl %eax,0x14(%rcx)</c>. No clamp, filter, dedup or default is applied
    /// here - the 16-bit wire value is stored exactly as Protocol read it and the -1 of the class
    /// constructor survives until a reply really arrives, which is what keeps
    /// <c>TitleFlow.LevelOf</c> on its busy fallback.
    /// </summary>
    public void ReceiveOnlineCount(int onlineCount)
    {
        NetClient.OnlineCount = onlineCount;
    }

    private static int[] CopyAttributeArray(IReadOnlyList<int> values)
    {
        int[] copy = new int[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return copy;
    }
}

}
