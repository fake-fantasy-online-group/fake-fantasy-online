using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private Dictionary<string, object> ObserveParty(ClientSessionCoordinator s, ObservationArguments args)
    {
        // Protocol can publish concurrently with a Unity frame. Pair the immutable root
        // with a stable native version, using the same bounded read as the native UI.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            long before = s.PartyStateVersion;
            WorldPartyState state = s.PartyState;
            long after = s.PartyStateVersion;
            if (before == after)
                return ObservePartySnapshot(state, after, ObservationFrame?.Player?.EntityId ?? 0, CharData.charId, args);
        }
        return Unavailable("party_changing", "Party state changed while sampling; request a new observation.");
    }

    private Dictionary<string, object> ObservePartySnapshot(WorldPartyState state, long nativeVersion,
        int selfEntityId, long selfCharacterId, ObservationArguments args)
    {
        if (state == null) return Unavailable("not_received", "Party state has not been published.");
        int selfIndex = PartySelfIndex(state, selfEntityId, selfCharacterId);
        bool inParty = selfIndex >= 0;
        var rows = new List<object>();
        for (int index = args.Offset; index < state.Members.Count && rows.Count < args.Limit; index++)
        {
            WorldPartyMemberState member = state.Members[index];
            rows.Add(Values("entityId", EntityReference("player", member.EntityId), "nativeId", member.EntityId,
                "name", BoundText(member.DisplayName, 256), "level", member.Level,
                "hp", member.HitPoints, "maxHp", member.MaximumHitPoints, "online", member.Online,
                "isSelf", PartyIsSelf(state, index, selfEntityId, selfCharacterId),
                "isLeader", state.LeaderEntityId != 0 && member.EntityId == state.LeaderEntityId,
                "gender", member.Gender, "hairId", member.HairId, "hairColor", member.HairColor,
                // Native optional compatibility tails are indexed by member position.
                // Preserve absent values and exact persistent IDs rather than guessing.
                "characterId", index < state.CharacterIds.Count ? state.CharacterIds[index].ToString(CultureInfo.InvariantCulture) : null,
                "profession", index < state.Professions.Count ? (object)(int)state.Professions[index] : null));
        }
        int next = args.Offset + rows.Count;
        return Values("available", true, "nativeVersion", nativeVersion,
            "expectedPartyRevision", nativeVersion.ToString(CultureInfo.InvariantCulture),
            "name", BoundText(state.PartyName, 256),
            "leaderEntityId", state.LeaderEntityId == 0 ? null : EntityReference("player", state.LeaderEntityId),
            "leaderNativeId", state.LeaderEntityId,
            "selfEntityId", selfEntityId == 0 ? null : EntityReference("player", selfEntityId),
            "selfNativeId", selfEntityId, "inParty", inParty,
            "isLeader", inParty && state.LeaderEntityId != 0 && state.LeaderEntityId == state.Members[selfIndex].EntityId,
            "members", rows, "offset", args.Offset, "limit", args.Limit, "count", rows.Count,
            "total", state.Members.Count, "truncated", next < state.Members.Count,
            "nextOffset", next < state.Members.Count ? (object)next : null);
    }

}
}
