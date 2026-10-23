using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

internal readonly struct ProtocolPacket
{
    public ProtocolPacket(ushort opcode, byte[] payload, WorldEntityState entitySpawn = null, bool remoteLifeAtReceive = false,
        WorldSkillBookState skillBook = null, WorldPlayerPetStatusState petStatus = null,
        PrivateServerGateway.PendingChannelChat channelChat = null,
        WorldSkillBookState receivedSkillBook = null,
        WorldPlayerPetStatusState receivedPetStatus = null,
        PrivateServerGateway.DecodedSocialNotice socialNotice = null,
        bool socialNoticeOwnerApplied = false,
        WorldSkillCastState receivedSkillCast = null,
        IReadOnlyList<ServerCharacterRecord> receivedCharacterRoster = null,
        ExceptionDispatchInfo characterRosterFailure = null,
        WorldSkillBookObservation receivedSkillBookObservation = default,
        bool skillBookTipEvaluated = false)
    {
        ReceivedCharacterRoster = receivedCharacterRoster;
        CharacterRosterFailure = characterRosterFailure;
        ReceivedSkillCast = receivedSkillCast;
        EntitySpawn = entitySpawn;
        _skillBook = skillBook;
        ReceivedSkillBookObservation = receivedSkillBookObservation.HasValue
            ? receivedSkillBookObservation : new WorldSkillBookObservation(receivedSkillBook);
        SkillBookTipEvaluated = skillBookTipEvaluated || skillBook != null;
        PetStatus = petStatus;
        ReceivedPetStatus = receivedPetStatus;
        ChannelChat = channelChat;
        SocialNotice = socialNotice;
        SocialNoticeOwnerApplied = socialNoticeOwnerApplied;
        RemoteLifeAtReceive = remoteLifeAtReceive;
        Opcode = opcode;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public ushort Opcode { get; }

    public byte[] Payload { get; }

    public WorldEntityState EntitySpawn { get; }

    // One actual 0x8004 receipt, already decoded and (if bound) delivered to its title owner.
    // Waiters observe these values/errors; they do not decode or replay the callback.
    public IReadOnlyList<ServerCharacterRecord> ReceivedCharacterRoster { get; }
    public ExceptionDispatchInfo CharacterRosterFailure { get; }

    // Single receipt decode, including the shared one-shot native application provenance.
    // Null on 0x800e is its existing malformed decode result, never a request to decode again.
    public WorldSkillCastState ReceivedSkillCast { get; }

    // Skill receipt is read and applied before queuing on both gateway dispatch paths.
    // HasValue is independent of tip provenance and never grants delayed write authority.
    // DTO access remains explicit; native validation/tip consumers borrow the observation.
    public WorldSkillBookState ReceivedSkillBook => ReceivedSkillBookObservation.ToSnapshot();
    public WorldSkillBookObservation ReceivedSkillBookObservation { get; }
    public bool SkillBookTipEvaluated { get; }

    // Full-pet decode/vitals ownership completed on receipt, independently of pet-tip
    // evaluation. Null on a pet packet carries its existing delayed decode-failure boundary.
    public WorldPlayerPetStatusState ReceivedPetStatus { get; }

    // Decoded receive values shared with immediate presentation and later per-packet handling.
    // SkillBookTipEvaluated and non-null PetStatus are supplied only after their matching
    // synchronous tip callback returns, including empty snapshots. SkillBook remains an
    // explicit DTO view of that same provenance; startup reads the bit without projecting.
    private readonly WorldSkillBookState _skillBook;
    public WorldSkillBookState SkillBook => _skillBook ??
        (SkillBookTipEvaluated ? ReceivedSkillBookObservation.ToSnapshot() : null);
    public WorldPlayerPetStatusState PetStatus { get; }
    public PrivateServerGateway.PendingChannelChat ChannelChat { get; }

    // The decoded social value is retained independently from successful owner delivery.
    // Only the synchronous receipt callback can set this provenance; retention never replays it.
    public PrivateServerGateway.DecodedSocialNotice SocialNotice { get; }
    public bool SocialNoticeOwnerApplied { get; }

    // Receive-time remote classification; never part of the wire payload.
    public bool RemoteLifeAtReceive { get; }
}

internal static class ProtocolOpcodes
{
    public const ushort ZoneOverlayState = 0x805f;
    // Official 1.5.8 NetClient.Dispatch jump-table entry 0x8060 (index 0x8060 - 0x8001 = 95 of the
    // same table, arm64 case block 0x24d2cc): the arena draft (选将) payload. A leading zero flag
    // tail-calls ArenaDraftDlg.Close (0x06000044); otherwise the branch ends in
    // ArenaDraftDlg.Show (0x06000043).
    public const ushort ArenaDraft = 0x8060;
    // Official 1.5.8 NetClient.Dispatch jump-table entry 0x8061 (index 0x8061 - 0x8001 = 96 of the
    // table at GameAssembly-arm64 file offset 0x1126fc4, arm64 case block 0x2528d0): the arena
    // death window payload. A leading zero flag tail-calls ArenaDeathDlg.Close (0x06000033);
    // otherwise the branch ends in ArenaDeathDlg.Show (0x06000032).
    public const ushort ArenaDeathState = 0x8061;
    public const ushort ArenaRoster = 0x8062;
    public const ushort LoginRequest = 0x1002;
    public const ushort CreateCharacterRequest = 0x1003;
    public const ushort EnterWorldRequest = 0x1004;
    public const ushort GmCommandRequest = 0x1006;
    public const ushort EquipInventoryItemRequest = 0x1007;
    public const ushort UnequipInventoryItemRequest = 0x1008;
    public const ushort SetCharacterTitleRequest = 0x1005;
    public const ushort AllocateAttributePointsRequest = 0x1009;
    public const ushort SavePositionRequest = 0x100a;
    public const ushort PetCommandRequest = 0x100b;
    public const ushort AttackRequest = 0x100c;
    public const ushort PickGroundItemRequest = 0x1010;
    public const ushort SittingRequest = 0x1011;
    public const ushort UpgradeSkillRequest = 0x1012;
    public const ushort UseSkillRequest = 0x1013;
    public const ushort SaveHotbarRequest = 0x1014;
    public const ushort UseInventoryItemRequest = 0x1015;
    public const ushort SplitInventoryItemRequest = 0x1016;
    public const ushort PlayerMoveRequest = 0x1017;
    public const ushort ReviveRequest = 0x1018;
    public const ushort OpenShopRequest = 0x1019;
    public const ushort BuyShopItemRequest = 0x101a;
    public const ushort SellInventoryItemRequest = 0x101b;
    public const ushort OpenWarehouseRequest = 0x102c;
    public const ushort MoveWarehouseItemRequest = 0x102d;
    public const ushort CaptureInventoryItemRequest = 0x102b;
    public const ushort LearnLifeSkillRequest = 0x1026;
    public const ushort GatherWorkRequest = 0x1025;
    public const ushort BeginWorkRequest = 0x1027;
    public const ushort EndWorkRequest = 0x1028;
    public const ushort NpcTaskOffersRequest = 0x1032;
    public const ushort TaskAcceptRequest = 0x1033;
    public const ushort TaskAbandonRequest = 0x1034;
    public const ushort TaskCompleteRequest = 0x1035;
    public const ushort TradeRequest = 0x1040;
    public const ushort TradeOffer = 0x1041;
    public const ushort TradeLock = 0x1042;
    public const ushort TradeConfirm = 0x1043;
    public const ushort TradeCancel = 0x1044;
    public const ushort PartyInviteRequest = 0x1045;
    public const ushort PartyLeaveRequest = 0x1046;
    public const ushort PartyKickRequest = 0x104d;
    public const ushort PartyLeaderRequest = 0x104e;
    public const ushort PartyCreateRequest = 0x1051;
    public const ushort PartyDismissRequest = 0x1052;
    public const ushort PartyJoinRequest = 0x1053;
    public const ushort FamilyInviteRequest = 0x1047;
    public const ushort FamilyLeaveRequest = 0x1048;
    public const ushort FamilyCreateRequest = 0x1067;
    public const ushort FamilyListRequest = 0x1068;
    public const ushort FamilyApplyRequest = 0x1069;
    public const ushort FamilyManageRequest = 0x106a;
    public const ushort FamilyMailRequest = 0x106b;
    public const ushort FamilyPositionsRequest = 0x106c;
    public const ushort FamilyRefreshRequest = 0x106d;
    public const ushort FamilyPosNameRequest = 0x106e;
    public const ushort FamilyStashRequest = 0x1092;
    public const ushort FamilyStashPermRequest = 0x1093;
    public const ushort StallDeleteRequest = 0x1063;
    public const ushort FriendRequest = 0x104f;
    public const ushort FriendDeleteRequest = 0x1050;
    public const ushort ApprenticeRequest = 0x104b;
    public const ushort DuelChallengeRequest = 0x109c;
    public const ushort DuelListRequest = 0x109d;
    public const ushort DuelBetRequest = 0x109e;
    public const ushort DuelSpectateRequest = 0x109f;
    public const ushort BlockAddRequest = 0x108d;
    public const ushort BlockDeleteRequest = 0x108e;
    public const ushort MailList = 0x801e;
    public const ushort MailNew = 0x801f;
    public const ushort OpenMailRequest = 0x101f;
    public const ushort MailActionRequest = 0x1021;
    public const ushort SendMailRequest = 0x1020;
    public const ushort RideTogetherRequest = 0x107e;
    public const ushort RideInviteRequest = 0x108c;
    public const ushort OpenRackRequest = 0x1059;
    public const ushort BuyRackRequest = 0x105a;
    public const ushort HelpRequest = 0x1077;
    public const ushort RenameCharacterRequest = 0x1079;
    public const ushort WarehouseMoneyRequest = 0x102e;
    public const ushort WarehouseSplitRequest = 0x1030;
    public const ushort WarehouseRearrangeRequest = 0x1031;
    public const ushort RequestDepositRequest = 0x1070;
    public const ushort FittingCatalogRequest = 0x107a;
    public const ushort FittingDescriptionRequest = 0x1098;
    public const ushort RackRefundListRequest = 0x1094;
    public const ushort RackRefundRequest = 0x1095;
    public const ushort ApprenticeListRequest = 0x106f;
    public const ushort SystemRequestResponse = 0x104a;
    public const ushort GossipRequest = 0x104c;
    public const ushort WardrobeStoreRequest = 0x1054;
    public const ushort WardrobeWearRequest = 0x1055;
    public const ushort WardrobeRemoveRequest = 0x1056;
    // Official 1.5.8 NetClient.SendEmote (0x060006da) opcode. arm64 0x267330 `movz w1, #0x1057`
    // and x86_64 0x2767a5 `mov esi, 0x1057` are the only loads of this constant in either frozen
    // slice, and both are the argument of the tail call into NetClient.Send (0x06000772).
    public const ushort EmoteRequest = 0x1057;
    public const ushort ChannelChatRequest = 0x1058;
    public const ushort UseInventoryToolOnRequest = 0x105b;
    public const ushort DeleteCharacterRequest = 0x105f;
    public const ushort InspectPlayerRequest = 0x1073;
    public const ushort SortInventoryRequest = 0x1078;
    public const ushort WardrobeMoveRequest = 0x107c;
    public const ushort OpenTransportRequest = 0x1071;
    public const ushort OpenTeleportRequest = OpenTransportRequest;
    public const ushort ChooseTransportRequest = 0x1072;
    public const ushort ServerStatusRequest = 0x107d;
    public const ushort QueryTargetStatusRequest = 0x1081;
    public const ushort EquipmentEffectsMaskRequest = 0x1082;
    // Official 1.5.8 NetClient.ArenaRevive (0x06000746, arm64 0x26f224 / x86_64 0x27e780) passes
    // this opcode to NetClient.Send (0x06000772) with the writer 0x06000837: one Buf.U8, 0 to
    // revive and 1 to surrender.
    public const ushort ArenaReviveRequest = 0x1084;
    // Official 1.5.8 NetClient.ArenaPick (0x06000744, arm64 0x26f050 / x86_64 0x27e580) passes this
    // opcode to NetClient.Send (0x06000772) with the writer 0x06000835: one Buf.U8 holding the card
    // index the player clicked.
    public const ushort ArenaPickRequest = 0x1083;
    // Official 1.5.8 NetClient.ArenaReroll (0x06000745, arm64 0x26f118 / x86_64 0x27e650) passes
    // this opcode to NetClient.Send (0x06000772) with the writer 0x060007a9, whose emitted extent is
    // 4 bytes of bare return: the frame is the opcode alone, with no payload.
    public const ushort ArenaRerollRequest = 0x1086;
    public const ushort SetPetViewRequest = 0x1085;
    public const ushort ClientCapabilities = 0x1091;
    // Official 1.5.8 NetClient.Register (0x060006c1, arm64 0x265c44 / x86_64 0x275040): the body
    // allocates the display class, stores acc (+0x10), pw (+0x18), sec (+0x20) and invite (+0x28)
    // in that order and passes 0x105d to NetClient.Send (0x06000772) with <Register>b__0
    // (0x06000880). The frozen writer emits Buf.Str (0x0600078e) four times — acc, pw, sec and
    // `invite ?? ""` (null falls back to usage 10335) — then
    // `SystemInfo.deviceUniqueIdentifier ?? ""` and Buf.U8 (0x0600078a) of VmDetect.Detect. The
    // same six-field order is what the local private server decodes (decRegister: acc, pwd, sec,
    // invite, device, vm) for 0x105d.
    public const ushort RegisterRequest = 0x105d;
    // Official 1.5.8 NetClient.ResetPwd (0x060006c2, arm64 0x265d64 / x86_64 0x275160): the body
    // allocates the display class NetClient+<>c__DisplayClass61_0, stores acc (+0x10), sec
    // (+0x18) and newPw (+0x20) in that order and passes 0x105e to NetClient.Send (0x06000772)
    // with <ResetPwd>b__0 (0x06000882). The frozen writer emits exactly three Buf.Str
    // (0x0600078e) calls — acc, sec, newPw — and stops: unlike Register (0x105d) there is no
    // SystemInfo.deviceUniqueIdentifier and no VmDetect.Detect byte. The local private server
    // decodes 0x105e with decResetPassword (account, security, password) in the same order.
    public const ushort ResetPasswordRequest = 0x105e;
    // Official 1.5.8 NetClient.TheftAppeal (0x060006e4, arm64 0x267ea0 / x86_64 0x2771d0): the
    // body allocates NetClient+<>c__DisplayClass100_0, stores account (+0x10), when (+0x18) and
    // reason (+0x20) in that order and passes 0x1090 to NetClient.Send (0x06000772) with
    // <TheftAppeal>b__0 (0x060007b7). 0x1090 is the literal operand of that call on both frozen
    // architectures, not a numbering inference. The frozen writer emits exactly three Buf.Str
    // (0x0600078e) calls — account, when, reason — and stops, so like ResetPwd (0x105e) and
    // unlike Register (0x105d) it carries no device identifier and no VmDetect byte.
    // No dedicated reply opcode is registered for it on the client side: the atlas NetClient field
    // set has no appeal-result slot and TitleFlow.SetupNet (0x06000ebe) binds only the eight
    // handlers b__96_0..7, so nothing is registered here for a downlink result.
    // Scope of that statement: it is proof about the frozen 1.5.8 client's own registration, not
    // proof that a server cannot answer through a generic notice or error packet. The local private
    // server leaves 0x1090 unimplemented (未接), so no end-to-end claim is made either way.
    public const ushort TheftAppealRequest = 0x1090;

    public const ushort LoginResult = 0x8002;
    public const ushort SelfData = 0x8003;
    public const ushort CharacterList = 0x8004;
    public const ushort CharacterMutationResult = 0x8005;
    public const ushort InventorySnapshot = 0x8006;
    public const ushort AttributeSnapshot = 0x8007;
    public const ushort NpcDialogText = 0x8008;
    // Official 1.5.8 account-creation reply. Identity comes from the frozen NetClient.Dispatch
    // (0x0600077f) arm64 jump table documented in
    // docs/contracts/VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json (case 0x24ff54,
    // body_instructions = 3, bufReads empty), and the consumer is proved by TitleFlow.SetupNet
    // (0x06000ebe): it assigns each NetClient delegate slot an explicit method-def usage, and
    // the slot +0x40 — which the atlas lists as the instance field NetClient.OnRegisterResult —
    // receives usage 09039 = TitleFlow.<SetupNet>b__96_6 (0x06000f06), whose body takes
    // (int code, string msg). The local private server encodes it as I32 code + Str message
    // (protocol.RegisterResult).
    public const ushort RegisterResult = 0x8009;
    // Official 1.5.8 password-recovery reply. Identity comes from the frozen NetClient.Dispatch
    // (0x0600077f) arm64 jump table documented in
    // docs/contracts/VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json (case 0x253084,
    // body_instructions = 33, bufReads = ["Buf.I32", "Buf.Str"]): its first instruction is
    // `ldr x20,[x20,#0x48]`, and 0x48 is the atlas instance field NetClient.OnResetResult
    // (System.Action<System.Int32,System.String>). The consumer is proved by TitleFlow.SetupNet
    // (0x06000ebe, arm64 :99-102), which binds that slot to metadata_usage_09040_kind3 =
    // TitleFlow.<SetupNet>b__96_7 (0x06000f07), whose body takes (int code, string msg) and
    // writes netMsg (+0x250), account (+0x1d8), state (+0x28) and the four recover fields.
    // Capstone on the case shows the shared (int, string) tail: 0x26b00c (guarded four-byte
    // read) into the code argument, then 0x26b088 (guarded length-prefixed UTF-8 read) into the
    // message argument, then the delegate invoke — no get_Remaining gate, so trailing bytes are
    // ignored. The local private server encodes it as I32 code + Str message
    // (protocol.SCResetResult = 0x804f, NewW(SCResetResult).I32(code).Str(message)).
    public const ushort ResetPasswordResult = 0x804f;
    public const ushort PlayerAppearance = 0x800a;
    public const ushort PlayerRename = 0x8056;
    public const ushort RenameCharacterResult = 0x8057;
    public const ushort PetSnapshot = 0x800b;
    // Official 1.5.8 NetClient.Dispatch case 0x806f: ten Buf.I32 (0x06000795) stored into the
    // CharData pet statics. The private server emits it as protocol.SCPetVitals
    // (server/internal/session/snapshot_delta.go).
    public const ushort PetVitals = 0x806f;
    public const ushort CombatNumber = 0x800c;
    // Official 1.5.8 Dispatch: Buf.U8 target, Buf.Str name, then EffectPlayer.SpawnOn.
    public const ushort SpawnOnEffect = 0x800d;
    public const ushort SkillCastState = 0x800e;
    public const ushort EntitySpawn = 0x800f;
    public const ushort EntityRemove = 0x8010;
    public const ushort CombatState = 0x8011;
    public const ushort SelfRidingState = 0x8012;
    public const ushort GroundItemSpawn = 0x8013;
    public const ushort GroundItemRemove = 0x8014;
    public const ushort SkillSnapshot = 0x8015;
    public const ushort HotbarSnapshot = 0x8016;
    public const ushort BuffSnapshot = 0x8017;
    public const ushort EntityMovePath = 0x8018;
    // Official 1.5.8 NetClient.Dispatch owns this case: arm64 block 0x2523a8 and x86_64 block
    // 0x26146a read I32 then Str from the same NetClient.Buf and tail-call
    // CombatWorld.ShowBubble (0x06000139). It is a separate opcode from ChannelChat (0x8039)
    // and never replaces it.
    public const ushort EntityBubble = 0x8019;
    public const ushort PlayerLifeState = 0x801a;
    public const ushort EntityStatusEffect = 0x801b;
    public const ushort ShopItems = 0x801c;
    public const ushort LifeSkillSnapshot = 0x8022;
    public const ushort RemotePlayer = 0x8023;
    public const ushort TaskSnapshot = 0x8027;
    public const ushort NpcTaskOffers = 0x8028;
    public const ushort Emote = 0x8025;
    public const ushort WarehouseSnapshot = 0x8026;
    public const ushort WarehouseDelta = 0x806a;
    public const ushort PartySnapshot = 0x8033;
    public const ushort FamilySnapshot = 0x8034;
    public const ushort FamilyBrowse = 0x8040;
    public const ushort FamilyPositions = 0x8041;
    public const ushort DuelList = 0x8076;
    public const ushort DuelHud = 0x8075;
    // SocialClient.Friends feed (official NetClient.Dispatch 0x8037).
    public const ushort FriendList = 0x8037;
    // SocialClient Master/Peers/Apprentices feed (official NetClient.Dispatch 0x805a).
    public const ushort ApprenticeSnapshot = 0x805a;
    // SocialClient.Blocks feed (official NetClient.Dispatch 0x8068, two-pass).
    public const ushort BlockList = 0x8068;
    public const ushort FamilyStashSnapshot = 0x806b;
    public const ushort HelpTopics = 0x8055;
    public const ushort TradeOpen = 0x8030;
    public const ushort TradeState = 0x8031;
    public const ushort TradeEnd = 0x8032;
    public const ushort SystemRequests = 0x8035;
    public const ushort PrivateChat = 0x8036;
    public const ushort WardrobeSnapshot = 0x8038;
    public const ushort ChannelChat = 0x8039;
    public const ushort Teleport = 0x803a;
    public const ushort StallOwnerUpdate = 0x803d;
    public const ushort StallClosed = 0x803e;
    // Official 0x8043 -> CombatWorld.SetOwner (0x06000132), jump-table case arm64 0x24ca4c /
    // x86_64 0x25a47c: two Buf.I32, no Remaining gate, no trailing-byte check. One opcode of its
    // own and never a tail of another entity packet.
    public const ushort EntityOwner = 0x8043;
    public const ushort LocalFaction = 0x8044;
    public const ushort GroundEffect = 0x80F1;
    public const ushort SkillHitEffect = 0x8045;
    public const ushort TransportList = 0x8049;
    public const ushort NpcSpawn = 0x8046;
    public const ushort NpcRemove = 0x8047;
    public const ushort NpcChatter = 0x804b;
    public const ushort CharacterTitleSnapshot = 0x804c;
    public const ushort InspectedPlayerEquipment = 0x8050;
    public const ushort SelfPkState = 0x8052;
    public const ushort RemotePkState = 0x8053;
    public const ushort RemoteRidingState = 0x8058;
    public const ushort OnlineCount = 0x805b;
    // NetClient.Dispatch (0600077f) case 0x805c, the raid (天神考验) state feed and the only writer
    // of the seven CharData raid slots. Jump-table identity: index 0x805c - 0x8001 = 91 of the 118
    // entry table at arm64 file offset 0x1126fc4, entry 0x732 -> arm64 0x24dae0 (x86_64 0x25b5cd);
    // corroborated by VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json (bufReads [Buf.U8,
    // Buf.I16, Buf.I16, Buf.I32, Buf.I32, Buf.I16]) and by the stores into +0x91, +0x94/+0x98,
    // +0x9c/+0xa0, +0xa4, +0xa8.
    public const ushort RaidState = 0x805c;
    public const ushort TargetStatusSnapshot = 0x805e;
    public const ushort EntityStatusIcons = 0x8064;
    public const ushort AccountSecuritySeed = 0x8065;
    public const ushort InventoryDelta = 0x8069;
    // Official 1.5.8 NetClient.Dispatch (0x0600077f) case 0x8071, the self counter-skill window
    // feed. Jump-table identity (index 0x8071 - 0x8001 = 112 -> arm64 case block 0x24c794) is
    // corroborated by the frozen switch contract
    // docs/contracts/VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json ("0x8071": case
    // 0x24c794, bufReads [Buf.U8, Buf.I32, Buf.get_Remaining, Buf.I32]) and by
    // docs/version-upgrades/1.3.4-to-1.5.8/PROTOCOL_STATIC_DIFF.md (`0x8071 | 3 | U8 I32 I32`).
    // The third I32 sits behind the native gate Buf.Remaining >= 1, not >= 4.
    public const ushort SelfCounterWindow = 0x8071;
}
}
