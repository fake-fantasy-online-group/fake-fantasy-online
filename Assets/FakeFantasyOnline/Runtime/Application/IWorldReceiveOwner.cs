using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>
/// Narrow host port for the official receive steps that write a static owner while the
/// official body runs: 0x8044 writes <c>CombatWorld.LocalFaction</c>, 0x804c rewrites
/// <c>CharData.title</c> plus <c>CharData.ownedTitles</c> and the 0x8003 enter callback
/// (TitleFlow.DoEnterWorld, 0x06000efd) stores the enter title into the same
/// <c>CharData.title</c>. Protocol stays Unity-free and
/// never references Presentation, so the real implementation is injected by the composition
/// root and the gateway calls it at the same point the official case body writes, once per
/// received packet.
/// </summary>
/// <remarks>
/// The four members are the official read/write steps in their original order, not a snapshot
/// projection: 0x804c empties the owned list before the count test and appends one entry per
/// loop iteration, so a short packet must leave the entries that were already appended in
/// place instead of rolling the whole packet back. That is why the port takes one append call
/// per entry rather than a completed list.
/// </remarks>
public interface IWorldReceiveOwner
{
    /// <summary>
    /// Official 0x800d forwards its raw unsigned target and effect name to
    /// EffectPlayer.SpawnOn inside the host-thread dispatch, before the next packet.
    /// </summary>
    void ReceiveSpawnOnEffect(int target, string effectName);

    /// <summary>
    /// Official 0x8003 counter clear, before the first payload I32. A failed first read
    /// retains these zero stores and leaves the previous SelfEntityId untouched.
    /// </summary>
    void BeginSelfData();

    /// <summary>Stores that same cursor's first I32 before any later SelfData read.</summary>
    void ReceiveSelfDataIdentity(int entityId);

    /// <summary>
    /// Official 0x8003 settings tail: apply a supplied nonnegative pet-view mask, then
    /// load and conditionally save the supplied smart-cast value, before client caps.
    /// </summary>
    void ReceiveEntrySettings(int? petViewMask, bool? smartCast);

    /// <summary>
    /// Official 0x8044 (arm64 0x25416c / x86_64 0x2633eb) step 2: the single I32 read from the
    /// packet is stored into the int32 static at CombatWorld +0x8. The official case has no
    /// world-frame, map or built gate and no second condition, so the write happens on receipt
    /// and never waits for a later projection.
    /// </summary>
    void ReceiveLocalFaction(WorldLocalFactionState state);

    /// <summary>
    /// Official 0x804c step 3: after the count byte is read and before the count is tested, the
    /// <c>CharData.ownedTitles</c> list is emptied by one zero-argument instance call
    /// (MethodRef[44370]), so the list is emptied even when the count is zero.
    /// </summary>
    void ClearOwnedTitles();

    /// <summary>
    /// Official 0x804c step 4: one Buf.Str read, then the inlined <c>List&lt;string&gt;.Add</c>
    /// on the very same static list. No empty, duplicate or null filter runs.
    /// </summary>
    void AddOwnedTitle(string title);

    /// <summary>
    /// Official 0x804c step 5: only after the loop - and also when the count was zero - is the
    /// string read in step 1 stored into <c>CharData.title</c>. The same member carries the
    /// 0x8003 enter title (TitleFlow.DoEnterWorld, arm64 0x391ef4) so both official writers
    /// feed one owner; the value is stored as it arrives, with no empty-string filter.
    /// </summary>
    void SetCurrentTitle(string title);

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34 / x86_64 0x3a4e70) step after
    /// the <c>EnterInfo.attrs</c> array store and its seven named scalars:
    /// <c>DateTime.Now.ToString("yyyy-MM-dd HH:mm")</c> into <c>CharData.lastLogin</c> (+0x28).
    /// arm64 0x391e7c bl get_Now (0xe9144c), 0x391e90 bl ToString(System.String) (0xe91bac) with
    /// metadata usage ordinal 16594 - exact decode, value <c>yyyy-MM-dd HH:mm</c> - and
    /// 0x391eb0 <c>str x21,[x0,#0x28]!</c>; x86_64 0x3a50f3 call get_Now (0xe90240), 0x3a510c
    /// call ToString(System.String) (0xe90980) and 0x3a5134 <c>mov [rdi+0x28],rax</c>.
    /// </summary>
    /// <remarks>
    /// The stored value is the local wall clock at the moment the callback runs, formatted with
    /// <see cref="System.DateTime.ToString(System.String)"/> - current culture, no invariant
    /// format provider and no UTC conversion - because the official enter packet carries no
    /// login timestamp of its own. Nothing is recomputed per frame: this is one store per entry,
    /// and the character-selection mirror (TitleFlow.MirrorSlotToCharData, 0x06000efb) keeps
    /// writing the roster row's own value on its own slot-change gate.
    /// </remarks>
    void ReceiveEntryLastLogin();

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34 / x86_64 0x3a4e70) identity
    /// stores, in the official order and with the official two-field shape:
    /// <c>CharData.name</c> (+0x0, System.String) = <c>EnterInfo.name</c> and
    /// <c>CharData.charId</c> (+0x8, System.Int64) = <c>EnterInfo.charId</c> - arm64 export
    /// lines 102 and 109 of the frozen dual-architecture body
    /// (wbb-072-0x06000efd-arm64-20260930/06000efd.c, <c>*puVar8 = uVar16</c> with
    /// <c>uVar16 = *param_2</c> and <c>*(undefined8 *)(lVar15 + 8) = param_2[0xe]</c>) and
    /// x86_64 lines 108 and 114 of the matching export (<c>*puVar4 = in_stack_00000008</c>,
    /// <c>*(undefined8 *)(lVar9 + 8) = in_stack_00000078</c>). The four Int32 stores the same
    /// block performs into <c>CharData</c> +0x10..+0x1c (race, gender, head, hair) are not part
    /// of this member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two values are the server's own identity from case 0x8003 and nothing else.
    /// <c>EnterInfo.charId</c> is the Int64 field at atlas offset 128 (field token 67111467) of
    /// <c>NetClient+EnterInfo</c>, whose struct base the case holds at <c>sp + 0xfe0</c>: the
    /// I64 tail is read at arm64 <c>0x250948 bl #0x274eac</c> behind
    /// <c>0x250934 bl #0x26afc0</c> / <c>0x250938 cmp w0,#8</c> / <c>0x25093c b.lt
    /// #0x2585c4</c> and lands in the field at <c>0x2585c8 str x0,[sp,#0x1050]</c>, while a
    /// payload that ends before it takes <c>0x2585c4 mov x0,#0</c> into the same slot. It is
    /// therefore the persistent character id and never the network self entity id: that one is
    /// the case's first I32 and leaves the struct entirely (arm64
    /// <c>0x250720 str w0,[x8,#0x7c]</c> into another type's statics).
    /// </para>
    /// <para>
    /// No filter, clamp, default or null substitution sits between either read and its store on
    /// either architecture, so the name is the string the wire carried and the id is the Int64
    /// the wire carried, zero included. The caller passes exactly the two values this entry
    /// decoded - the server display name and the entry's persistent id, or zero when the 0x8003
    /// tail was absent - and never a roster-predicted name, a previously published id or a
    /// connection-scoped entity id.
    /// </para>
    /// </remarks>
    void ReceiveEntryIdentity(string name, long characterId);

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd) step placed after the <c>CharData.name</c>
    /// (+0x0) and <c>CharData.charId</c> (+0x8) stores and before the <c>CharData.attrs</c>
    /// (+0xb0) store: the zero-argument <c>CharData.LoadNameMode()</c> (0x06000f2b) - arm64
    /// 0x391cd8 <c>bl #0x3958e0</c> / x86_64 0x3a506b <c>call 0x3a8c50</c>.
    /// </summary>
    /// <remarks>
    /// One call per entry, which is also the only official caller of 0x06000f2b in the frozen
    /// index: the preference is read once per process for a suffix that changed, never per
    /// frame. The key the callee derives is <c>namemode_</c> plus <c>CharData.PrefSuffix</c>
    /// (<c>charId &gt; 0 ? charId.ToString() : name</c>), so this step depends on the two
    /// identity stores that precede it in the official body and reads no argument of its own.
    /// <para>
    /// Identity timing in this product, checked on the real paths rather than on this interface
    /// alone: <see cref="ReceiveEntryIdentity"/> runs immediately before this step and is the
    /// only protocol-side writer of the two fields, so the suffix this call derives is the
    /// server's own identity - <c>namemode_&lt;charId&gt;</c> whenever the 0x8003 payload carried
    /// the I64 tail, and <c>namemode_&lt;server name&gt;</c> when it did not - exactly the key
    /// the official body derives, because the official stores both fields in front of the same
    /// call. Before that publication the only writer of <c>CharData.name</c> was the roster
    /// mirror on both entry paths (RecoveredTitleCharacterSurface.MirrorSlotToCharData,
    /// including the QA background entry, which has no slot click and mirrors explicitly) and
    /// <c>CharData.charId</c> still held what the previous world left - <c>0</c>, because
    /// ReferenceFirstWorldInitialFrame.ReleaseOwnedState zeroes it - which is why this step used
    /// to read the name-suffixed key and never the id-suffixed one. The later id-suffixed
    /// identity (ReferenceFirstWorldInitialFrame, which stores <c>charId</c> again and rebinds
    /// the preference identity) belongs to the world build, writes the same value this step
    /// already published and is untouched here.
    /// </remarks>
    void LoadEntryNameMode();

    /// <summary>
    /// Official 0x804b: the case reads its five roots and the whole line list through one
    /// NetClient.Buf and then tail-calls <c>NpcChatter.SetServerChatter(list, enabled, minGap,
    /// maxGap, showSec)</c> (0x0600090f, arm64 bl 0x281454 / x86_64 call 0x28ff40) inside the
    /// case body. The call is one whole-table replacement with no world-frame, map or built gate
    /// and no comparison with the previous packet, so it happens once per received packet, in the
    /// order the packets arrive, and never waits for a later projection.
    /// </summary>
    /// <remarks>
    /// Ordering is the reason this port exists. <c>SetServerChatter</c> overwrites
    /// sMinGap/sMaxGap/sShowSec only when the incoming value is positive, so two packets that
    /// arrive in the same frame are not interchangeable: "positive then zero" keeps the first
    /// packet's timings while "zero then positive" keeps the second one's. Collapsing them into
    /// one per-frame snapshot - or applying the same snapshot a second time from the frame - is
    /// not the official behaviour, so the receive step is the only caller and the whole
    /// replacement runs inside one host action.
    /// </remarks>
    void ReceiveNpcChatter(WorldNpcChatterState state);

    /// <summary>
    /// Official 0x8028: the case reads the NPC name, the row count and every row through one
    /// NetClient.Buf and then tail-calls <c>TaskClient.SetOffers(npc, list)</c> (0x06000e5e,
    /// arm64 0x36ea70 / x86_64 0x3810e0) inside the case body. <c>SetOffers</c> stores the name,
    /// clears the offer list, refills it and only then fires <c>OnOffersChanged</c>, so every
    /// packet is one whole replacement that runs on receipt, in arrival order, and never waits
    /// for a later projection.
    /// </summary>
    /// <remarks>
    /// Ordering is the reason this port exists. Two 0x8028 replies read in the same frame are not
    /// interchangeable even when the last snapshot equals the result: <c>SetOffers</c> fires
    /// <c>OnOffersChanged</c> once per packet, and <c>DialogUI.OnTaskOffers</c> (0x06000212) that
    /// the callback reaches rebuilds the menu each time, so the menu is rebuilt once per packet
    /// and not once per frame. Every rebuild also resets <c>InSubView</c> and drops the current
    /// selection and scroll, which is a per-reply effect with no equivalent in a collapsed
    /// per-frame application of the final snapshot.
    /// </remarks>
    void ReceiveNpcTaskOffers(WorldNpcTaskOffersState state);

    /// <summary>
    /// Official 0x8027: the case reads both counts and every row through one
    /// <c>NetClient.Buf</c> and then tail-calls <c>TaskClient.SetState(active, done)</c>
    /// (0x06000e5d, arm64 <c>bl 0x36e7d8</c> / x86_64 <c>call 0x380e60</c>) inside the case
    /// body. <c>SetState</c> clears <c>Active</c>, refills it, clears <c>Done</c>, adds each
    /// done id and only then fires <c>OnStateChanged</c>, so every packet is one whole
    /// replacement that runs on receipt, in arrival order, and never waits for a later
    /// projection.
    /// </summary>
    /// <remarks>
    /// Ordering is the reason this port exists. Two 0x8027 replies read in the same frame are
    /// not interchangeable even when the last snapshot equals the result: <c>SetState</c> fires
    /// <c>OnStateChanged</c> once per packet and rewrites <c>Active</c> and <c>Done</c> once per
    /// packet, so one collapsed per-frame application of the final snapshot is not the official
    /// behaviour. The same holds across opcodes - a 0x8027 that arrives after a 0x8028 must run
    /// after that 0x8028's <c>SetOffers</c>, because both write the same process-wide
    /// <c>TaskClient</c> statics and the second writer wins.
    /// </remarks>
    void ReceiveTaskLog(WorldTaskLogState state);

    /// <summary>
    /// Official 0x8017 step 1: the case builds one new <c>List&lt;CharData.BuffView&gt;</c> with
    /// the wire row count as its capacity, before the loop and also for an empty packet
    /// (arm64 0x9cb004 / x86_64 0x9cfb00).
    /// </summary>
    void BeginBuffRows(int count);

    /// <summary>
    /// Official 0x8017 step 2: one row that survived the custom-map test is appended to the list
    /// the case is building. The fields arrive exactly as the case read them - the sign-extended
    /// positionMode, the wire booleans and the per-row <c>Time.realtimeSinceStartup</c> stamp
    /// taken inside the loop after the posMode read - so the row carries the moment it was read
    /// and the aura consumer's <c>remainSec - (now - recvTime)</c> countdown starts there.
    /// </summary>
    /// <remarks>
    /// The row stamp is why this step is per row and not a packet-level value. Two 0x8017 packets
    /// in one frame produce two lists with two different stamps instead of one list carrying the
    /// last snapshot, and a row the custom-map test drops never reaches this call while still
    /// having been read and stamped.
    /// </remarks>
    void AddBuffRow(
        int skillId,
        int iconId,
        int remainingSeconds,
        string name,
        string description,
        bool good,
        int positionMode,
        float receiveTime);

    /// <summary>
    /// Official 0x8017 step 3: after the loop the case stores the finished list into the
    /// <c>CharData</c> buff static (arm64 <c>str x20,[x0,#0xc0]!</c> at 0x258880 / x86_64
    /// <c>movq %rsi,0xc0(%rdi)</c> at 0x2653ad, both followed by the write barrier) and then
    /// writes the two row aggregates - <c>invisible</c> and the raw <c>immobile</c> unsigned
    /// maximum - in that order. Every packet, an empty one included, replaces all three instead
    /// of merging them with the previous reply.
    /// </summary>
    void CompleteBuffRows(bool invisible, byte immobile);

    /// <summary>
    /// Official 0x8017 step 4: the last write of the case stores the optional tail byte into
    /// <c>CharData.doubleExp</c> (arm64 0x258e1c / x86_64 0x265411), after the four-value
    /// replacement above. A packet that ends after the last row stores false instead of keeping
    /// the previous value.
    /// </summary>
    void CompleteBuffTail(bool doubleExperience);

    /// <summary>
    /// Official 0x8007 steps 5 and 6, plus the 0x8003 enter callback's one array store: the two
    /// <c>CharData</c> attribute statics are written inside the case body, in this order -
    /// <c>CharData.attrsTotal</c> (+0xb8) first (arm64 0x250c50 <c>str x21,[x0,#0xb8]!</c> /
    /// x86_64 0x25f42b <c>movq %rbx,0xb8(%rdi)</c>), then <c>CharData.attrs</c> (+0xb0) (arm64
    /// 0x250c7c <c>str x20,[x0,#0xb0]!</c> / x86_64 0x25f467 <c>movq %r14,0xb0(%rdi)</c>), each
    /// followed by its write barrier. Both statics are static <c>System.Int32[]</c> (atlas
    /// 176/0xb0 and 184/0xb8, the only Int32[] pair at those offsets).
    /// </summary>
    /// <remarks>
    /// Ordering and the skip are the reason this port exists. Neither store waits for a frame:
    /// the case writes both arrays on receipt, so a death, mount-anchor or work frame - which the
    /// product's local-movement branch never reaches - still leaves the arrays the last packet
    /// published. Two 0x8007 packets in one frame each perform the whole pair of stores and the
    /// second wins; collapsing them into one per-frame application of the last snapshot is not the
    /// official behaviour.
    /// <para>
    /// A null argument means "this packet performs no store into that slot" and never "store
    /// null": official 0x8003 writes no totals at all - its array reaches <c>CharData.attrs</c>
    /// only through TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391ce8 <c>str x21,[x0,#0xb0]!</c>)
    /// - and 0x8007 skips the +0xb8 store when its second count is zero (arm64 0x250bec
    /// <c>cmp w19,#0x1</c> / 0x250bf0 <c>b.lt 0x250c5c</c>, x86_64 0x25f3cf <c>testl %r15d,
    /// %r15d</c> / 0x25f3d2 <c>jle 0x25f441</c>), which leaves the previous totals array in
    /// place. The caller therefore passes the packet's own write intent - that packet's first
    /// array plus its second array only when that second count is at least one, otherwise null -
    /// and not the arrays of the already-applied <see cref="WorldPlayerStatusState"/>: the mirror
    /// deliberately retains the previous totals for the UI when a packet's second array is empty,
    /// so its merged arrays would copy that retained array back into +0xb8, which the official
    /// case never does. During session entry the mirror root may not exist yet; that is not a
    /// reason to hold a store back, because this owner writes the real statics and each startup
    /// packet still performs its own store in arrival order instead of one merged store after
    /// SelfData.
    /// </para>
    /// </remarks>
    void ReceivePlayerAttributes(IReadOnlyList<int> attributes, IReadOnlyList<int> attributeTotals);

    /// <summary>
    /// Official 0x8007 steps 8..14: the seven named scalars the same case body stores from its
    /// FIRST array, right after the two array stores of
    /// <see cref="ReceivePlayerAttributes"/> and before the element-23 store. arm64
    /// <c>0x250c8c cbz x20</c> / <c>0x250c90 ldr x8,[x20,#0x18]</c> / <c>0x250c94 cmp w8,#7</c> /
    /// <c>0x250c98 b.lt 0x250d28</c> gate the whole block on a non-null array of at least seven
    /// elements, and then the stores run in index order: <c>0x250cbc str w22,[x9,#0x20]</c>
    /// level = element 0, <c>0x250ccc str w10,[x9,#0x68]</c> exp = 1, <c>0x250cdc str
    /// w10,[x9,#0x6c]</c> expToNext = 2, <c>0x250cec str w10,[x9,#0x58]</c> hp = 3,
    /// <c>0x250cfc str w10,[x9,#0x5c]</c> maxHp = 4, <c>0x250d0c str w10,[x9,#0x60]</c> mp = 5
    /// and <c>0x250d1c str w10,[x9,#0x64]</c> maxMp = 6. x86_64 is the same block at 0x25f47c
    /// (<c>movl %r15d,0x20(%rcx)</c>) with the same seven offsets and the same
    /// <c>cmpl $0x7</c> gate. Every store is a 32-bit store into the <c>CharData</c> statics.
    /// </summary>
    /// <remarks>
    /// A null array or one shorter than seven elements performs no scalar store at all here -
    /// the official branch leaves all seven values exactly as they were - so this member is a
    /// no-op in that case and never substitutes a default, a mirror value or a zero. The same
    /// seven fields are also written by the 0x8003 enter callback, whose short-array branch has
    /// its own shape; see <see cref="ReceiveEnterAttributeFields"/>.
    /// </remarks>
    void ReceivePlayerAttributeFields(IReadOnlyList<int> attributes);

    /// <summary>Official 0x8007 tail: raw attrs 25..28, only when Count is at least 29.</summary>
    void ReceivePlayerRingFields(IReadOnlyList<int> attributes);

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34 / x86_64 0x3a4e70) steps 7
    /// and 8: the enter callback stores <c>EnterInfo.attrs</c> into <c>CharData.attrs</c>
    /// (+0xb0) and then writes the same seven named scalars from that array, in the same index
    /// order as 0x8007 - level = element 0 (+0x20), exp = 1 (+0x68), expToNext = 2 (+0x6c),
    /// hp = 3 (+0x58), maxHp = 4 (+0x5c), mp = 5 (+0x60), maxMp = 6 (+0x64) - arm64
    /// <c>0x391dcc ldr w24,[x21,#0x20]</c> / <c>0x391dec str w24,[x9,#0x20]</c>, then
    /// <c>0x391df8-0x391dfc</c>, <c>0x391e08-0x391e0c</c>, <c>0x391e18-0x391e1c</c>,
    /// <c>0x391e28-0x391e2c</c>, <c>0x391e38-0x391e3c</c> and <c>0x391e48-0x391e4c</c>;
    /// x86_64 <c>0x3a55a5-0x3a55ca</c> then 0x3a55d7/0x3a55e7/0x3a55f7/0x3a5607/0x3a5617/
    /// 0x3a5627 with the same offsets.
    /// </summary>
    /// <remarks>
    /// The short-array shape is the official one and is not a fallback invented here: arm64
    /// <c>0x391cf4 cbz x21</c> skips the whole block for a null array, <c>0x391d04 cbz x8</c>
    /// skips it for an empty one, and the branch taken for a length of one to six writes
    /// element 0 into level only (<c>0x391d0c ldr w21,[x21,#0x20]</c> /
    /// <c>0x391d28 str w21,[x8,#0x20]</c> / <c>0x391d2c b 0x391e50</c>) - the other six scalars
    /// keep the values they already had. x86_64 0x3a5095-0x3a50d5 is the same shape. The level
    /// gate is narrower than the six: only a null or empty array skips its store, while a length
    /// of one to six still writes element 0, so "short" never means "skip" for level. The value
    /// an absent level store retains is whatever <c>CharData.level</c> holds - the roster value
    /// the selection mirror (TitleFlow.MirrorSlotToCharData, 0x06000efb) wrote, unless a startup
    /// 0x8007 with at least seven elements was dispatched before this entry and already
    /// overwrote it - so no roster value is re-copied here: this member simply performs no store.
    /// </remarks>
    void ReceiveEnterAttributeFields(IReadOnlyList<int> attributes);

    /// <summary>
    /// The read counterpart of <see cref="ReceiveEnterAttributeFields"/>: the seven named scalars of
    /// the same official static block - <c>CharData.level</c> (+0x20), <c>CharData.exp</c> (+0x68),
    /// <c>expToNext</c> (+0x6c), <c>hp</c> (+0x58), <c>maxHp</c> (+0x5c), <c>mp</c> (+0x60) and
    /// <c>maxMp</c> (+0x64) - as they stand at the moment the caller asks.
    /// </summary>
    /// <remarks>
    /// This is not a second authority for those numbers: it reads the very statics the official
    /// writes (0x06000efd and the 0x8007 case of 0x0600077f) and nothing else. The enter gate needs
    /// it because the official skip retains whatever those statics already hold, which is a
    /// property of the static and not of any connection-scoped root - the frozen teardown bodies
    /// (GameBootstrap.TeardownWorldUi 0x060003f3, its local purge 0x060003f7, NetClient.Close
    /// 0x060006be, NetClient.OnDestroy 0x060006bf) write none of these seven offsets on either
    /// architecture - level included, whose only initializer is the 1 CharData..cctor (0x06000f2d)
    /// writes - so their content survives a connection teardown while no published root does.
    /// Reading them once per enter decision keeps the Application snapshot aligned with the
    /// consumers that read the statics directly - for example
    /// <c>PlayerController.SelHp</c>/<c>SelMaxHp</c>, the skill-row mp check and the level the
    /// name-tag colouring and the task-panel gates compare against - instead of letting the two
    /// layers disagree after a reconnect. Level belongs to the same read although its gate is
    /// narrower - only a null or empty enter array skips its store, while a length of one to six
    /// still writes element 0 - because what a skipped store keeps is the static either way, and
    /// the static is also what a startup 0x8007 already overwrote before this entry. One read, no
    /// store, no cache. A port that has no bound implementation never reaches this member - the
    /// caller keeps a null result, the keepers fall back to the zero the six statics start from
    /// and level keeps the roster carrier - so an absent owner changes no consumption path and no
    /// packet ordering.
    /// </remarks>
    WorldEntryRetainedScalars ReadEntryRetainedScalars();

    /// <summary>
    /// Official 0x8007 step 7, the one store of the case that leaves the <c>CharData</c> block:
    /// element 23 of the FIRST array is stored into <c>InventoryDlg.MaxWeight</c> - static
    /// <c>System.Int32</c> at atlas offset 36/0x24, field token 67110650 - through the second
    /// metadata slot <c>arm64 0x148b4e8 / x86_64 0x147f960</c> (usage ordinal 1929,
    /// <c>TypeInfo:InventoryDlg</c>), not through the CharData slot (ordinal 1441, arm64
    /// 0x148a5a8 / x86_64 0x147ea20) the same case uses for every other store. arm64 reads
    /// <c>0x259f6c ldr w22,[x20,#0x7c]</c> - (0x7c - 0x20) / 4 = element 23 - and stores
    /// <c>0x259f94 str w22,[x9,#0x24]</c>; x86_64 reads <c>0x2688d8 movl 0x7c(%r14),%r12d</c>
    /// and stores <c>0x268904 movl %r12d,0x24(%rcx)</c>. The atlas has no CharData field at
    /// 0x24 at all (level is 0x20, lastLogin 0x28), so no CharData slot is involved.
    /// </summary>
    /// <remarks>
    /// The caller performs the length gate and simply does not call this member when the packet
    /// performs no store, which is the same "write intent" shape the two array arguments of
    /// <see cref="ReceivePlayerAttributes"/> already use: the store is reached only when the
    /// FIRST array holds at least 24 elements (arm64 <c>0x250d20 cmp w8,#0x18</c> /
    /// <c>0x250d24 b.ge 0x259f6c</c>; x86_64 <c>0x25f51a cmpl $0x18,%eax</c> /
    /// <c>0x25f51d jge 0x2688d8</c>), so a shorter or empty first array leaves the previous
    /// value in place and no default, clamp or mirror maximum is substituted for it.
    /// <para>
    /// This store is the last of the three Scalar copies' block and runs after both array stores
    /// of <see cref="ReceivePlayerAttributes"/> - <c>CharData.attrsTotal</c> (+0xb8) then
    /// <c>CharData.attrs</c> (+0xb0) - and before the 25..28 copies and the level-up comparison,
    /// all inside the same case body, so it is performed by the same host action as those stores
    /// and not by a later frame. <c>InventoryDlg.MaxWeight</c> also has two other official
    /// writers - the 0x8006 and 0x8069 inventory publications write the wire maximum weight into
    /// the same slot - so this member is one arrival-ordered writer and not the owner of the
    /// field: whichever packet arrives last leaves its value behind, exactly as officially.
    /// </para>
    /// </remarks>
    void ReceiveInventoryWeightLimit(int maximumWeight);

    /// <summary>
    /// Official 0x8006 and 0x8069: the four consecutive <c>InventoryDlg</c> static stores each
    /// case performs from its own five header reads - <c>Money</c> (+0x10, System.Int64, field
    /// token 67110647) and <c>CaiYu</c> (+0x18, System.Int64, 67110648) from the first two i64
    /// reads, then <c>Weight</c> (+0x20) and <c>MaxWeight</c> (+0x24) from the two trailing i32
    /// reads. arm64 0x8006 <c>0x25f10c ldp x10, x11, [sp, #0xa0]</c> /
    /// <c>0x25f110 stp x11, x10, [x8, #0x10]</c> / <c>0x25f114 ldp w10, w11, [sp, #0x90]</c> /
    /// <c>0x25f118 stp w11, w10, [x8, #0x20]</c>; arm64 0x8069 <c>0x2557d0 stp x19, x21,
    /// [x8, #0x10]</c> / <c>0x2557d4 stp w22, w23, [x8, #0x20]</c>; x86_64 0x8006
    /// <c>0x26dfc6</c> / <c>0x26dfd1</c> / <c>0x26dfdb</c> / <c>0x26dfe4</c> and 0x8069
    /// <c>0x264cdd</c> / <c>0x264ce1</c> / <c>0x264ce5</c> / <c>0x264ce9</c>. The i64 helper is
    /// arm64 0x274eac / x86_64 0x284070 and the i32 helper is arm64 0x26b00c / x86_64
    /// 0x27a660; no comparison, clamp or default sits between any read and its store, so a
    /// server-sent zero or negative value is written exactly as it arrived.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Like <see cref="ReceiveInventoryWeightLimit"/>, these four stores belong to the case body:
    /// one store per packet that really arrives, on the packet's own arrival, and never from a
    /// later frame. The official client performs no +0x10 / +0x18 / +0x20 / +0x24 store before
    /// the first inventory packet, so the field initializers survive until a packet really
    /// arrives and a sentinel that stands for "no packet yet" must not be projected into any of
    /// the four slots. A whole-binary scan of every reference to the <c>InventoryDlg</c>
    /// metadata slot (arm64 0x148b4e8 / x86_64 0x147f960) finds exactly two writers of +0x10
    /// and +0x18 - the two blocks above - plus the 0x8007 element-23 store, which writes +0x24
    /// only and never +0x10 or +0x18; the 0x8003 enter root performs no such store at all.
    /// </para>
    /// <para>
    /// The writers are ordered by arrival alone - whichever of 0x8007, 0x8006 and 0x8069
    /// arrives last leaves its value in +0x24, and whichever of 0x8006 and 0x8069 arrives last
    /// leaves its values in +0x10 / +0x18 - and this member is what makes that true for the two
    /// inventory opcodes: values taken from a retained state during a later frame would let a
    /// packet published before a later 0x8007 overwrite that 0x8007 element-23 maximum weight
    /// with the older one, which no official frame does because the official store already
    /// happened at the older packet's own arrival.
    /// </para>
    /// The fifth value of the same header read is the third i64 and leaves the four
    /// <c>InventoryDlg</c> slots alone: it goes to <c>CharData.honor</c> (+0x70, System.Int64,
    /// field token 67114212) through metadata usage ordinal 1441 <c>TypeInfo:CharData</c>
    /// (arm64 0x148a5a8 / x86_64 0x147ea20) - arm64 0x8006 <c>0x25f14c str x10,[x8,#0x70]</c>
    /// and 0x8069 <c>0x2557f8 str x20,[x8,#0x70]</c>; x86_64 <c>0x26e01e</c> and
    /// <c>0x264d1d</c> <c>mov qword ptr [rax+0x70], rdx</c>. Both architectures read the three
    /// i64 with the same helper (arm64 0x274eac / x86_64 0x284070) before the two i32, but the
    /// honor store itself is the <em>last</em> of the five: in each case the four
    /// <c>InventoryDlg</c> stores run first and the class-init check for <c>CharData</c> sits
    /// between them and this store, so an observer that saw the packet's money, cai yu, weight
    /// or maximum weight already published still sees the previous honor until this step. No
    /// comparison, clamp or default sits between any read and its store, so a server-sent zero
    /// or negative honor is written exactly as it arrived.
    /// </remarks>
    void ReceiveInventoryHeader(
        long money, long caiYu, int weight, int maximumWeight, long honor);

    /// <summary>
    /// Official 0x8071: the four <c>CharData</c> stores the case body performs, in their native
    /// order - <c>counterCharges</c> (+0xc8, System.Int32, field token 67114230) from the case's
    /// single <c>Buf.U8</c> (arm64 <c>0x24c7a4 bl 0x26afcc</c> / <c>0x24c7d4 str w0,[x8,#0xc8]</c>,
    /// x86_64 <c>0x25a1ab callq 0x27a630</c> / <c>0x25a1d8 movl %eax,0xc8(%rcx)</c>),
    /// <c>counterTtlMs</c> (+0xcc, 67114231) from the first <c>Buf.I32</c> (arm64
    /// <c>0x24c7e0</c> / <c>0x24c7ec str w0,[x8,#0xcc]</c>, x86_64 <c>0x25a1e3</c> /
    /// <c>0x25a1f2</c>), <c>counterSkillId</c> (+0xd0, 67114232) from the second <c>Buf.I32</c>
    /// or from the literal 0 (arm64 <c>0x24c7f8 bl 0x26afc0</c> = <c>Buf.get_Remaining</c>,
    /// <c>0x24c7fc cmp w0,#0x1</c> / <c>0x24c800 b.lt 0x258604</c>, <c>0x258604 mov w19,#0x0</c>,
    /// <c>0x258620 str w19,[x8,#0xd0]</c>; x86_64 <c>0x25a1fd callq 0x27a620</c>,
    /// <c>0x25a202 testl %eax,%eax</c> / <c>0x25a204 jle 0x25a212</c>, <c>0x25a22d movl
    /// %ebx,0xd0(%rax)</c>), and <c>counterRecvTime</c> (+0xd4, System.Single, 67114233) from
    /// <c>Time.get_realtimeSinceStartup()</c> (arm64 <c>0x258628 bl 0x104dde0</c> /
    /// <c>0x258634 str s0,[x8,#0xd4]</c>; x86_64 <c>0x25a235 callq 0x10402e0</c> /
    /// <c>0x25a244 movss %xmm0,0xd4(%rax)</c>). All four are static and all four sit in the
    /// <c>TypeInfo:CharData</c> metadata usage (arm64 0x148a5a8 / x86_64 0x147ea20).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate is <c>Remaining &gt;= 1</c> and not <c>&gt;= 4</c>: both architectures test the
    /// remaining byte count against 1 before the third read, so a payload that leaves 1 to 3 bytes
    /// takes the read arm and <c>Buf.I32</c> raises on both architectures. No value check is
    /// performed on any of the three wire values - a zero or negative ttl is a real value and is
    /// stored as it arrived, exactly like the four <see cref="ReceiveInventoryHeader"/> slots.
    /// </para>
    /// <para>
    /// The clock is read by this member and not by the caller: the official read sits after the
    /// third store and inside the same case body, so the receive owner takes it at that point
    /// of the packet on the host thread. No cache, FIFO, timer or per-frame restamp is
    /// introduced, and two packets that arrive in one frame are not deduplicated - each one
    /// performs all four stores and the second leaves its own values behind.
    /// </para>
    /// <para>
    /// This is one arrival-ordered writer and not the owner of the zero stores: the 0x8003
    /// SelfData prefix already clears <c>counterCharges</c>, <c>counterTtlMs</c> and
    /// <c>counterSkillId</c> and deliberately leaves <c>counterRecvTime</c> alone, so a 0x8003
    /// that arrives after a 0x8071 clears exactly what the official case clears.
    /// </para>
    /// </remarks>
    void ReceiveCounterWindow(int charges, int ttlMilliseconds, int skillId);

    /// <summary>
    /// Official 0x805c (arm64 0x24dae0 / x86_64 0x25b5cd), the whole case body in its native order:
    /// six unconditional reads - <c>Buf.U8</c>, <c>Buf.I16</c>, <c>Buf.I16</c>, <c>Buf.I32</c>,
    /// <c>Buf.I32</c>, <c>Buf.I16</c> - into the seven <c>CharData</c> raid slots +0x91..+0xa4,
    /// then the <c>Time.get_realtimeSinceStartup</c> stamp into +0xa8.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The clock is read by this member and not by the caller: the official read sits after the
    /// sixth store inside the same case body, so there is no cache, timer or per-frame restamp and
    /// two packets arriving in one frame are not deduplicated - the second leaves its own values.
    /// </para>
    /// <para>
    /// There is no official clearer: unlike the counter slots, the 0x8003 SelfData prefix does not
    /// touch +0x91..+0xa8 on either architecture, so a raid that ends without a further 0x805c
    /// keeps its last state on screen.
    /// </para>
    /// </remarks>
    void ReceiveRaidState(
        bool active,
        int wave,
        int total,
        int nextSec,
        int remainSec,
        int alive);

    /// <summary>
    /// Compatibility entry for the historical decoded-state Editor audit. Product receipt uses
    /// IHotbarReceiveOwner during raw reads, so malformed-prefix stores cannot be replayed here.
    /// </summary>
    void ReceiveHotbar(WorldHotbarState state);

    /// <summary>
    /// Official 0x8015 tail: after the row loop and the optional damage-bonus tail, the case body
    /// reloads the skill dictionary it just stored and calls <c>NewbieTipBox.Push</c> (0x060008d8)
    /// with the literal 7 when that dictionary holds at least one entry - arm64 0x253478
    /// <c>ldr x0,[x8,#0x48]</c>, 0x25348c <c>bl 0x66766c</c> (Dictionary get_Count), 0x253490
    /// <c>cmp w0,#1</c> / <c>b.lt 0x260d68</c> and the tail <c>b 0x27ab58</c> at 0x2534e0;
    /// x86_64 0x2625b8 <c>mov rdi,[rax+0x48]</c>, 0x2625cf <c>call 0x67b830</c>, 0x2625d4
    /// <c>test eax,eax</c> / <c>jle 0x27009b</c> and the tail <c>jmp 0x289990</c> at 0x262608.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The count is the dictionary count, not the wire row count: both architectures call
    /// <c>get_Count</c> on the map the case filled, so duplicate skill ids in one packet collapse
    /// before the test runs and an empty packet performs no push. The case carries no world, map,
    /// scene or in-world gate and no second condition, so a 0x8015 read before SelfData pushes
    /// exactly like one read inside the world.
    /// </para>
    /// <para>
    /// The runtime singleton equality test that both architectures perform next to this call -
    /// arm64 <c>ldur x8,[x29,#-0x88]</c> against <c>*(*(0x1394290))</c>, x86_64
    /// <c>cmp rax,[rbp-0x30]</c> against <c>*(*(rip+0x1127d0d))</c> - is not a tip condition: the
    /// value is loaded once before any opcode is handled (arm64 0x24a770 / x86_64 at the
    /// equivalent prologue slot) on the same runtime page that supplies the frame-size check
    /// helper (arm64 0x1394248, called at 0x24a73c), and the same pair is compared on all 63
    /// arm64 tail-call exits and on the single common arm64 epilogue at 0x260d68 / x86_64
    /// 0x27009b. It is therefore a codegen-level exit check and not a Buf, session or
    /// termination gate, and no product branch is derived from it here.
    /// </para>
    /// <para>
    /// This member is the tip side effect only. The official case also replaces the skill
    /// dictionary; that replacement is now received separately through ISkillBookReceiveOwner
    /// before queuing the packet. Nothing here writes <c>CharData</c> skills or republishes a
    /// delayed snapshot. A null state is the product-only guard for a caller that
    /// already decoded a payload; the official case has no such branch because it holds the map
    /// it just read (a null map reaches the IL2CPP null guard at arm64 0x25c960 / x86_64
    /// 0x26b60d instead).
    /// </para>
    /// </remarks>
    void ReceiveNewbieSkillBookTip(WorldSkillBookState state);

    /// <summary>
    /// Official 0x800b tail: after this packet has stored every pet field, including the skill list
    /// the case itself built, the case body runs two searches over that same list in this order and
    /// pushes at most once each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Push 51 comes first: arm64 0x25e72c <c>ldr x19,[x8,#0xd0]</c> loads the cached
    /// predicate (created at 0x25e758 / 0x25e774 and stored back at 0x25e780 when it is still
    /// null), 0x25e79c <c>bl 0xa2cbd0</c> runs the search over the list the case stored at
    /// 0x256c64, 0x25e7a4 <c>cbz w0,#0x25e7b4</c> skips the push when the search found nothing and
    /// 0x25e7a8 <c>mov w0,#0x33</c> plus 0x25e7b0 <c>bl 0x27ab58</c> push the literal 51; x86_64
    /// 0x26d4b9 stores the same cached predicate, 0x26d4e3 <c>call 0xa326b0</c> runs the search,
    /// 0x26d4e8 <c>test al,al</c> / 0x26d4ea <c>je 0x26d4f8</c> is the same skip and 0x26d4ec
    /// <c>mov edi,0x33</c> plus 0x26d4f3 <c>call 0x289990</c> push it. The predicate is the cached
    /// <c>Predicate&lt;CharData.PetSkill&gt;</c> delegate this case lazily creates when the field is
    /// still null and it tests <c>level &gt; 0</c> (the frozen trigger contract
    /// <c>docs/contracts/VERSION_UPGRADE_1_5_8_NEWBIE_TIP_TRIGGERS_V1.md</c>; the token that
    /// contract names for it, 0x060007b4, resolves to <c>MailDlg.SetMailList</c> in the frozen
    /// 1.5.8 atlas and is not re-used here), so a packet whose skill rows are all level 0 - or whose U8 skill count is zero, in which case the
    /// case stores the list it built at 0x256c64 without entering the row loop at all - performs
    /// no push 51.
    /// </para>
    /// <para>
    /// Push 52 follows in the same block over the same list: arm64 0x25e7cc <c>bl 0xa2cf74</c>
    /// takes the enumerator, 0x25e814 <c>bl 0x740a54</c> advances it, 0x25e818
    /// <c>tbz w0,#0,#0x25e83c</c> leaves the loop when it is exhausted, 0x25e800
    /// <c>mov w19,#0x32ca</c> is the literal id 13002, 0x25e81c <c>ldr w8,[sp,#0xf80]</c> reads the
    /// id at +0x10 and 0x25e820 <c>ldr w9,[sp,#0xf9c]</c> the level at +0x2c of the same element,
    /// 0x25e824 <c>cmp w8,w19</c> / 0x25e828 <c>ccmp w9,#1,#8,eq</c> / 0x25e82c
    /// <c>b.lt 0x25e80c</c> continue on both an unequal id and a level below one, and 0x25e830
    /// <c>mov w0,#0x34</c> plus 0x25e838 <c>bl 0x27ab58</c> push the literal 52 on the first match;
    /// x86_64 0x26d510 <c>call 0xa32a00</c>, 0x26d561 <c>call 0x74f730</c>, 0x26d566
    /// <c>test al,al</c> / 0x26d568 <c>je 0x26d58b</c>, 0x26d56a
    /// <c>cmp dword ptr [rbp-0xb70],0x32ca</c> / 0x26d574 <c>jne 0x26d55b</c>, 0x26d576
    /// <c>cmp dword ptr [rbp-0xb54],0</c> / 0x26d57d <c>jle 0x26d55b</c> and 0x26d57f
    /// <c>mov edi,0x34</c> plus 0x26d586 <c>call 0x289990</c> are the identical search with the
    /// same two stack offsets (+0x10 id, +0x2c level).
    /// </para>
    /// <para>
    /// Both searches stop at their first match and 51 always precedes 52, so a packet carrying one
    /// qualifying row pushes 51 and then 52 out of one case body. The whole tail sits after every
    /// pet field store of this packet and carries no world, map, scene or in-world gate, so a
    /// 0x800b read before SelfData pushes exactly like one read inside the world. On arm64 the tail
    /// is also reachable from the separate 0x804d case (arm64 0x24f930), which reads U16 plus two
    /// I32 and then falls through into the same block; the product has no 0x804d arm and this
    /// member is called for 0x800b only.
    /// </para>
    /// <para>
    /// This member is the tip side effect only. The official case also replaces the pet statics and
    /// the skill list, and that replacement stays on the existing publication path: nothing here
    /// writes <c>CharData</c> pet state, so a startup packet cannot move the observable pet
    /// snapshot earlier than it does today. A null state is the product-only guard for a caller that
    /// already decoded a payload; the official case has no such branch because it holds the list it
    /// just built.
    /// </para>
    /// </remarks>
    void ReceiveNewbiePetSkillsTip(WorldPlayerPetStatusState pet);

    /// <summary>
    /// Official 0x8054 case: the case reads one U16 count, builds a <c>List&lt;int&gt;</c> of that
    /// capacity and fills it with that many signed I32 reads, then tail-calls
    /// <c>NewbieTipBox.SeedServer</c> (0x060008d7, arm64 0x27a93c / x86_64 0x289780) with that
    /// list - arm64 case 0x255070: 0x25507c <c>bl 0x26b690</c> (Buf.U16), 0x2550a0-0x2550ac the
    /// capacity constructor, 0x2550b0 <c>cmp w21,#1</c> / 0x2550b4 <c>b.lt 0x255134</c>, 0x2550ec
    /// <c>bl 0x26b00c</c> (Buf.I32) per row and the tail <c>b 0x27a93c</c> at 0x25517c; x86_64 case
    /// 0x2645b2: 0x2645c0 <c>call 0x27ad70</c>, 0x2645ea the constructor, 0x2645ef
    /// <c>test r14d,r14d</c> / 0x2645f2 <c>jle 0x264659</c>, 0x264629 <c>call 0x27a660</c> per row
    /// and the tail <c>jmp 0x289780</c> at 0x264683.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The seed call is unconditional on the count: both architectures reach the tail from the
    /// <c>count &lt; 1</c> branch as well (arm64 0x2550b4, x86_64 0x2645f2), so a zero-count 0x8054
    /// still performs one <c>SeedServer</c> with the empty list it just built. Nothing filters,
    /// sorts, de-duplicates or range-checks an id in the case and nothing clears a tip queue there:
    /// the list is handed over whole and <c>SeedServer</c> is the only member that touches the
    /// role-scoped <c>PlayerPrefs</c>, first through <c>EnsureShown</c> (0x060008d6) and then
    /// through one <c>SetString</c> plus one <c>Save</c> only when an id was new.
    /// </para>
    /// <para>
    /// The case carries no world, map, scene or in-world gate, so a 0x8054 read before SelfData
    /// seeds exactly like one read inside the world. The runtime singleton equality test next to
    /// the tail (arm64 0x255134-0x255148, x86_64 0x264659-0x264667) is the same codegen-level exit
    /// check the 0x8015 arm carries and not a seed condition.
    /// </para>
    /// <para>
    /// This member is the seed side effect only and performs no tip lookup of its own; a null list
    /// has no official branch because the case always holds the list it just built.
    /// </para>
    /// </remarks>
    void ReceiveNewbieTipSeed(List<int> ids);

    /// <summary>
    /// Official <c>NetClient.Dispatch</c> (0x0600077f) case 0x8001, the reply to the client's own
    /// 0x1001 ping and the only writer of the <c>NetClient.RttMs</c> static (System.Int32, atlas
    /// static offset 16, milliseconds) found in the frozen Dispatch body on either architecture:
    /// the case reads one <c>Buf.I64()</c> (arm64 0x24be24 <c>bl NetClient+Buf.I64</c>, x86_64
    /// 0x2596b8), subtracts it from <c>Environment.TickCount</c> as 32-bit integers and stores the
    /// difference when and only when the unsigned result is at most 59999 - arm64 0x24be38
    /// <c>lsr w8,w19,#5</c> / <c>cmp w8,#0x752</c> / <c>b.hi</c> and x86_64 0x2596cc
    /// <c>cmpl $0xea5f,%ebx</c> / <c>ja</c> are the same unsigned bound, so a negative or
    /// over-large delta leaves the previous value in place.
    /// </summary>
    /// <remarks>
    /// One call per received 0x8001 and never a per-frame projection: the official case writes
    /// the static inside the receive step, so several replies inside one frame stay distinct and
    /// a rejected delta is simply not delivered. The subtraction, the low-32 truncation of the
    /// I64 and the 0..59999 gate all belong to Protocol and are finished before this member runs;
    /// the member only stores the value in the owner that <c>TitleFlow.DrawServer</c>
    /// (0x06000ef4) reads.
    /// </remarks>
    void ReceiveRoundTripLatency(int latencyMs);

    /// <summary>
    /// Official <c>NetClient.Dispatch</c> (0x0600077f) case 0x805b, the reply to the client's own
    /// 0x107d <c>SrvStatReq</c> (0x060006d5) and the only writer of the
    /// <c>NetClient.OnlineCount</c> static (System.Int32, atlas static offset 20): the case calls
    /// <c>Buf.U16()</c> (0x06000793, arm64 0x26b690 / x86_64 0x27ad70) once and stores the 32-bit
    /// result - arm64 0x24cd10 <c>str w0,[x8,#0x14]</c>, x86_64 0x25a70d
    /// <c>movl %eax,0x14(%rcx)</c> - with no length gate, no stage gate and no clamp.
    /// </summary>
    /// <remarks>
    /// One call per received 0x805b and never a projection of an awaited reply: the official
    /// request is fire-and-forget, so the count arrives through the receive step exactly once per
    /// packet and the -1 of the class constructor survives until a reply really arrives. The
    /// reading of the wire belongs to Protocol and is finished before this member runs; the member
    /// only stores the value in the owner that <c>TitleFlow.LevelOf</c> (0x06000ef1) and
    /// <c>TitleFlow.DrawServer</c> (0x06000ef4) read.
    /// </remarks>
    void ReceiveOnlineCount(int onlineCount);
}
/// <summary>Immediate 0x8046/0x8047 delivery to the original typed NPC registry owner.</summary>
public interface INetworkNpcReceiveOwner
{
    void ReceiveNetworkNpcSpawn(WorldNpcState row);
    void ReceiveNetworkNpcRemove(int id);
}

/// <summary>
/// A one-use host handoff for the adapted asynchronous entry. Calling the returned action
/// means the selected entry's BuildWorld attempt returned normally, not that it succeeded.
/// </summary>
public interface IWorldEntryAdmissionSource
{
    System.Action CaptureWorldEntryCompletion(WorldEntryRecord selectedEntry);
}

}
