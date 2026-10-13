using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IWorldEntryPrefixReceiptSource
{
    // The title GUI is already on the host. Send through the existing native writer
    // without creating a response reader or holding an entry admission before SelfData.
    public void SendTitleEntryForIndependentRuntime(string name) => _netClient.EnterGame(name);

    private Action<WorldEntryRecord> _worldEntryPrefixReceiver;
    private Action _worldEntryPrefix;

    public void BindWorldEntryReceipt(Action<WorldEntryRecord> receive) =>
        BindWorldEntryReceipt(receive, null);

    public void BindWorldEntryReceipt(Action<WorldEntryRecord> receive, Action begin)
    {
        // The two source portions belong to this same installed callback. Clearing or
        // replacing the slot must not leave a different title's prefix attached to it.
        _worldEntryPrefixReceiver = receive;
        _worldEntryPrefix = receive == null ? null : begin;
        _netClient.OnEnterWorld = receive;
    }

    public void ClearTitleEntryNoticeForIndependentRuntime() => _netClient.OnNotice = null;

    public void SendTitleEntryCapsForIndependentRuntime() => _netClient.SendClientCaps();

    private bool TryReceiveWorldEntryWithoutWaiter(ushort opcode, byte[] payload)
    {
        if (opcode != ProtocolOpcodes.SelfData ||
            Volatile.Read(ref _worldEntryAdmission) != null) return false;
        // A selected request already owns its existing response reader. Only the missing
        // no-waiter input enters here, with this actual receipt and no fabricated selection.
        // Do not enqueue/replay SelfData or start a second response reader.
        if (!_host.IsHostThread)
            throw new InvalidOperationException("World-entry receipts must run on the host thread.");
        CancellationTokenSource connection = _connectionLifetime;
        if (connection == null)
            throw new InvalidOperationException("World-entry receipt has no active connection.");
        var response = new EnterWorldResponse(new ProtocolPacket(opcode, payload),
            Array.Empty<PendingRemoteWorldState>(), Array.Empty<WorldGroundItemState>(),
            Array.Empty<PendingChannelChat>(), null, null, null, null, null, null, null,
            default, null, null, false, null, null);
        Task<Result<WorldEntryRecord>> applied = ApplyEnterWorldResponseAsync(
            response, null, null, null, connection.Token, connection);
        // Every await in this body is an IHostExecutor action. That contract runs host calls
        // inline. Observe only an already-completed result; never block the host on a post.
        if (!applied.IsCompleted)
            throw new InvalidOperationException("The host executor did not complete its inline world-entry action.");
        Result<WorldEntryRecord> result = applied.GetAwaiter().GetResult();
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error.Message);
        return true;
    }

    private async Task<Result<WorldEntryRecord>> ApplyEnterWorldResponseAsync(
        EnterWorldResponse response, string requestedName, int? rosterLevel,
        WorldEntryAdmission admission, CancellationToken cancellationToken,
        CancellationTokenSource receiptConnection = null)
    {
        bool receiptWithoutWaiter = admission == null;
        Action<WorldEntryRecord> receive = null;
        ProtocolPacket packet = response.Packet;
        // Official NetClient.Dispatch opcode 0x8003: the switch jump table maps 0x8003 to the
        // arm64 block 0x250698 (F32 at 0x250768 and 0x250778) and to the x86_64 block
        // 0x25ed8d (F32 at 0x25ee87 and 0x25ee9d). Both coordinates stay floats - neither
        // branch contains fcvtzs, cvttss2si or any other float-to-integer conversion, and
        // arm64 stores them with `str s0, [sp, #0x1004]` / `str s0, [sp, #0x1008]` before the
        // frame is handed to the self-data callback - so the reader keeps them as floats.
        // The optional tails are gated on Remaining alone (arm64 0x250938 `cmp w0, #8`,
        // 0x2585d8/0x258e38/0x258e64 `cmp w0, #1`, 0x258e90 `cmp w0, #4`) and a short tail
        // falls back to the native sentinel -1 instead of being rejected
        // (arm64 0x258e24/0x258e50/0x258e7c `mov w0, #-1`). No byte is validated against 1:
        // arm64 0x258f08 only tests `cmp w10, #0` / `cset w21, ne` for the smart-cast byte,
        // and the branch ends with no trailing-byte check.
        int entityId;
        int mapId;
        WorldEntryRecord entry;
        WorldPlayerStatusState playerStatus;
        IReadOnlyList<int> enterAttributes = null;
        // The real receipt owns one Buf. Preserve the reached prefix even if a later
        // read fails: counter clear, first I32, identity store, then the remaining reads.
        NetClient.Buf reader = new NetClient.Buf(packet.Payload);
        _worldReceiveOwner?.BeginSelfData();
        entityId = reader.I32();
        _worldReceiveOwner?.ReceiveSelfDataIdentity(entityId);
        reader.U8(); // compatibility byte
        string name = reader.Str();
        endgameCharacter = "";
        mapId = reader.I32();
        float x = reader.F32();
        float y = reader.F32();
        int race = reader.U8();
        int gender = reader.U8();
        int head = reader.U8();
        int hair = reader.U8();
        IReadOnlyList<int> attributes = ReadAttributes(reader);
        string title = reader.Str();
        CharacterAppearanceRecord appearance = ReadAppearance(reader);
        WorldPetRecord pet = new WorldPetRecord(
            reader.I32(),
            reader.Str(),
            reader.I32());

        long? persistentCharacterId = null;
        int? glowMode = null;
        bool? smartCast = null;
        int? glowUnlock = null;
        int? petViewMask = null;

        if (reader.Remaining >= 8)
        {
            persistentCharacterId = reader.I64();
            if (persistentCharacterId.Value > 0)
                endgameCharacter = persistentCharacterId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (reader.Remaining >= 1)
        {
            glowMode = reader.U8();
        }

        if (reader.Remaining >= 1)
        {
            int wireSmartCast = reader.U8();
            smartCast = wireSmartCast != 0;
        }

        if (reader.Remaining >= 1)
        {
            glowUnlock = reader.U8();
        }

        if (reader.Remaining >= 4)
        {
            petViewMask = reader.I32();
        }

        // Dispatch's settings precede the live callback lookup. The title's entering,
        // Notice and caps effects belong to that callback, not to a null-subscriber packet.
        _worldReceiveOwner?.ReceiveEntrySettings(petViewMask, smartCast);
        receive = _netClient.OnEnterWorld;
        Action beginEntry = ReferenceEquals(receive, _worldEntryPrefixReceiver)
            ? _worldEntryPrefix : null;
        if (receive != null && beginEntry != null)
        {
            // Begin the captured original title body before any entry/root observation
            // allocation or CharData publication. On the real receipt host this is inline;
            // an exception stops this reached prefix and never acknowledges a world build.
            await _host.RunAsync(beginEntry, cancellationToken).ConfigureAwait(false);
        }

        // An unsolicited receipt has no selected roster record. Read the existing native
        // scalar owner once instead of inventing a fallback level or selected identity.
        WorldEntryRetainedScalars receiptRetained = null;
        if (receiptWithoutWaiter)
        {
            receiptRetained = await ReadEntryRetainedScalarsAsync(cancellationToken)
                .ConfigureAwait(false);
            if (receiptRetained == null)
                throw new InvalidOperationException("World-entry receipt requires its native retained-state owner.");
        }
        entry = new WorldEntryRecord(
            entityId,
            receiptWithoutWaiter ? name : requestedName,
            name,
            title,
            race,
            gender,
            head,
            hair,
            mapId,
            x,
            y,
            attributes,
            appearance,
            pet,
            persistentCharacterId,
            glowMode,
            smartCast,
            glowUnlock,
            petViewMask,
            // The roster carrier: the level the selection mirror
            // TitleFlow.MirrorSlotToCharData (0x06000efb) already wrote into CharData.level
            // before this request. The published root uses it only when the official
            // statics cannot be read at all (a null receive port), because the value a
            // skipped level store keeps is the static's own content: the mirror's roster
            // value when the mirror was the last writer, and the value of a startup 0x8007
            // that arrived before this entry when that packet was.
            rosterLevel ?? receiptRetained.Level);
        // The seven stores the enter callback skips retain what the official statics already
        // hold, so the only correct carrier for them is those statics themselves, read once
        // here through the receive port. A previously published root cannot carry them: it
        // belongs to one connection and DisposeConnectionCore discards it, while the frozen
        // teardown bodies (TeardownWorldUi 0x060003f3, its purge 0x060003f7, NetClient.Close
        // 0x060006be, NetClient.OnDestroy 0x060006bf) write none of the seven offsets and
        // CharData..cctor (0x06000f2d) writes only level, so officially they survive every
        // connection teardown. This read precedes this entry's own host steps, so what it
        // sees is the pre-entry content - including anything a pre-SelfData 0x8007 stored -
        // exactly the content the official skip leaves in place. Level is read here too,
        // and not from the roster carrier above, for the same reason: a startup 0x8007 with
        // at least seven elements already overwrote CharData.level, so the level the
        // callback leaves standing is that packet's, not the roster's.
        WorldEntryRetainedScalars retained = receiptWithoutWaiter ? receiptRetained :
            await ReadEntryRetainedScalarsAsync(cancellationToken).ConfigureAwait(false);
        playerStatus = WorldPlayerStatusState.FromEntry(entry, retained);
        // Carry this entry's exact resolved level through the selected-record handoff
        // as well. On an empty attrs array it is the native retained scalar, not a
        // fresh roster projection. Do this before any earlier startup snapshot is
        // merged into playerStatus below; the record is not a snapshot replay.
        entry = entry.WithLevel(playerStatus.Level);
        // The 0x8003 enter callback stores EnterInfo.attrs, the attributes of this very
        // entry, so the array is captured here before any later snapshot is merged into
        // the mirror root; the mirror and the owner keep their own values from now on.
        enterAttributes = playerStatus.Attributes;
        if (receiptWithoutWaiter)
        {
            // Original 0x8003 loads +0x60 after its payload/settings. Null skips the
            // title continuation. The no-admission split above is only the host adapter's
            // anti-duplication rule; the native callback has no request/waiter condition.
            if (receive == null) return Result<WorldEntryRecord>.Success(entry);
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(receiptConnection, _connectionLifetime))
                throw new OperationCanceledException("The receipt connection was retired before entry admission.");
            admission = new WorldEntryAdmission(receiptConnection, cancellationToken)
                { SelfDataDequeued = true };
            if (Interlocked.CompareExchange(ref _worldEntryAdmission, admission, null) != null)
                throw new InvalidOperationException("A world-entry handoff was already admitted.");
        }

        if (response.AttributeSnapshot != null)
        {
            playerStatus = playerStatus.WithAttributeSnapshot(
                response.AttributeSnapshot);
        }

        if (response.PetStatus != null)
        {
            playerStatus = playerStatus.WithPetSnapshot(response.PetStatus);
        }

        BeginWorldScenePublication(mapId, cancellationToken);
        PublishPlayerStatusState(playerStatus, cancellationToken);
        // Official TitleFlow.DoEnterWorld stores CharData.name (+0x0) from EnterInfo.name and
        // CharData.charId (+0x8, System.Int64) from EnterInfo.charId immediately before it
        // calls CharData.LoadNameMode() (0x06000f2b) and the EnterInfo.attrs store
        // (arm64 export lines 102/109 then 110 / x86_64 lines 108/114 then 116, arm64
        // 0x391cd8 bl #0x3958e0 / x86_64 0x3a506b call 0x3a8c50). Both values are this
        // entry's own decode - the server display name and the I64 persistent id of the
        // 0x8003 payload, zero when that tail was absent - so the preference key the next
        // step derives is namemode_<charId> exactly as officially, instead of the
        // namemode_<name> the selection mirror's own name slot used to leave behind. The
        // later world-build stores of the same two slots write the same values and are
        // untouched.
        await ReceiveEntryIdentityOnHostAsync(
                entry.DisplayName,
                entry.PersistentCharacterId.GetValueOrDefault(),
                cancellationToken)
            .ConfigureAwait(false);
        // The zero-argument CharData.LoadNameMode() call itself keeps its official position
        // after those two stores and ahead of this entry's own array store. It is the only
        // official caller of 0x06000f2b and it takes no argument: the preference key comes
        // from the identity the presentation owner just published.
        await LoadEntryNameModeOnHostAsync(cancellationToken).ConfigureAwait(false);
        // Official case 0x8003 writes no attribute array itself; the enter callback
        // TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391ce8 `str x21,[x0,#0xb0]!`) stores
        // EnterInfo.attrs into CharData.attrs (+0xb0) and writes nothing to +0xb8, so the
        // enter root publishes its attributes alone and the totals stay absent until a real
        // 0x8007. A null totals argument is the packet's own write intent - "this packet does
        // not touch +0xb8" - and not a request to clear it: the pre-SelfData 0x8007 packets
        // already stored their totals in arrival order above, and the enter callback must not
        // overwrite them. Any attribute snapshot carried by this reply was stored on its own
        // arrival, so this step performs the one official 0x8003 store and nothing else.
        await ReceivePlayerAttributesOnHostAsync(
                enterAttributes,
                null,
                null,
                false,
                cancellationToken)
            .ConfigureAwait(false);
        // The same callback also writes the seven named scalars, after that array store and
        // from the same enter array: level = element 0, exp = 1, expToNext = 2, hp = 3,
        // maxHp = 4, mp = 5, maxMp = 6 (arm64 0x391dcc-0x391e4c / x86_64 0x3a55a5-0x3a562b).
        // A later 0x8007 overwrites them through the same owner at its own arrival, so no
        // per-frame writer is introduced for the world's first frame.
        await ReceiveEnterAttributeFieldsOnHostAsync(
                enterAttributes,
                cancellationToken)
            .ConfigureAwait(false);
        // The same callback then stores DateTime.Now.ToString("yyyy-MM-dd HH:mm") into
        // CharData.lastLogin (+0x28) - arm64 0x391e7c-0x391eb8 / x86_64 0x3a50f3-0x3a513f -
        // right after the seven-scalar block. Every later writer of that static (the
        // character-selection mirror 0x06000efb) runs at its own slot-change gate, so nothing
        // else has to be republished with it.
        await ReceiveEntryLastLoginOnHostAsync(cancellationToken).ConfigureAwait(false);
        // Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34) then stores the enter
        // title into the very CharData.title static case 0x804c writes: arm64 0x391ebc
        // ldr x8,[x20,#0x8] (EnterInfo.title), 0x391ec4 cmp x8,#0, 0x391ec8 csel x1, x9, x8,
        // eq with x9 read from metadata usage ordinal 10335 (the empty string, decode state
        // exact), then 0x391ef4 str x1,[x0,#0x30]! and the write barrier; x86_64 loads the
        // same two values at 0x3a5144-0x3a5154 and stores at 0x3a517a. The recovered client
        // left that title outside the owner and read it back only through a guard, so the
        // owner had two sources for the same string; the entry title now goes into the same
        // owner, in arrival order and on the host, and later 0x804c packets keep overwriting
        // it exactly as they do officially. The store is the official step that follows the
        // lastLogin store, so it is awaited here instead of keeping the earlier position the
        // WBB-007-R2 segment gave it: nothing between those two positions reads
        // CharData.title - the two readers are ReferenceFirstWorldWindowOwner
        // .SyncCharacterTitleState (:5844, called forced at the world build :1079 and on
        // every later UI pass :1420, projecting CharData.title into frame
        // .ApplyLocalCharacterTitleForIndependentRuntime at :5858) and
        // ReferenceFirstWorldLocalEquipmentDialog.ApplyCharacterTitle (:1165, called only
        // from the dialog's own title clicks :984 / :1077), both of which can only run after
        // this entry has returned - and no other step between them writes it, so
        // moving it restores the official order without changing what those consumers read.
        await ReceiveEntryTitleOnHostAsync(entry.Title, cancellationToken)
            .ConfigureAwait(false);
        // NPC receipts already reached the native row owner, including pre-SelfData rows.
        // BuildWorld's upcoming Clear owns their reset; never replay a session snapshot.
        PublishInitialRemoteWorldStates(
            response.RemoteWorldStates,
            mapId,
            entityId,
            cancellationToken);
        PublishInitialGroundItems(response.GroundItems, cancellationToken);
        if (response.SelfPkState != null)
        {
            PublishSelfPkState(response.SelfPkState, cancellationToken);
        }

        if (response.LocalFactionState != null)
        {
            PublishLocalFactionState(
                response.LocalFactionState,
                cancellationToken);
        }

        // Party receive edges are published in their original startup order with remote state.

        if (response.TaskLogState != null)
        {
            PublishTaskLogState(response.TaskLogState, cancellationToken);
        }

        if (response.NpcChatterState != null)
        {
            PublishNpcChatterState(response.NpcChatterState, cancellationToken);
        }

        if (response.InventoryState != null)
        {
            // The retained startup reader may resume off-host after earlier
            // ConfigureAwait(false) calls. Publish the native/sidecar identity
            // boundary through the same host executor as other entry owners.
            await PublishInventoryStateOnHostAsync(response.InventoryState, cancellationToken).ConfigureAwait(false);
        }

        if (response.WardrobeState != null)
        {
            PublishWardrobeState(response.WardrobeState, cancellationToken);
        }

        if (response.SkillBookState.HasValue)
        {
            // Receipt already published the actual owner. Retain the previous entry
            // cancellation checkpoint without replaying a potentially older snapshot.
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (response.LifeSkillBookState != null)
        {
            PublishLifeSkillBookState(response.LifeSkillBookState, cancellationToken);
        }

        if (response.BuffStatePayload != null)
        {
            PublishBuffPayload(response.BuffStatePayload, response.BuffStateNonEmpty, cancellationToken);
        }

        lock (_worldDrainSync)
        {
            if (!ReferenceEquals(admission, _worldEntryAdmission) ||
                !ReferenceEquals(admission.Connection, _connectionLifetime))
                return Result<WorldEntryRecord>.Failure("world.enter.superseded",
                    "This entry's connection was retired before its host handoff.");
            Volatile.Write(ref _currentWorldEntityId, entityId);
            admission.ReturnedEntry = entry;
            _stage = GatewayStage.InWorld;
            endgameWorldEntered = true;
        }
        // Return the adapted entry to its host while the common inbox remains fenced.
        // The host acknowledges a normally returned BuildWorld attempt, then the same
        // consumer and existing world drain continue. An escaped build never acknowledges.
        // Both selected and no-waiter receipts reach the installed title on this host call.
        // The selected task observes this result only after that same callback returned.
        try
        {
            receive?.Invoke(entry);
        }
        catch
        {
            ReleaseWorldEntryFenceAfterException(admission);
            throw;
        }
        return Result<WorldEntryRecord>.Success(entry);
    }
}
}
