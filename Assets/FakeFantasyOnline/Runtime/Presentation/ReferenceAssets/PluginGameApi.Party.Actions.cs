using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private void AddPartyActions(List<GameActionDefinition> definitions)
    {
        var session = Session;
        var player = ActionFrame?.Player;
        var party = session?.PartyState;
        bool ready = session != null && session.State == ClientSessionState.InWorld && player != null && party != null;
        int selfIndex = ready ? PartySelfIndex(party, player.EntityId, CharData.charId) : -1;
        bool member = selfIndex >= 0;
        bool empty = ready && party.Members.Count == 0;
        bool leader = member && party.LeaderEntityId != 0 && party.LeaderEntityId == party.Members[selfIndex].EntityId;
        const string reason = "Observe party in an active world; membership or leadership prerequisites are not satisfied.";
        AddPartyDefinition(definitions, "party.create", "Create an ordinary party through the native gateway. Name follows the native 16-character input limit; empty is allowed by the native UI.", empty, reason, "name:string");
        AddPartyDefinition(definitions, "party.invite", "Invite an observed live remote player through the native party command. Invitation acceptance requires the other player.", member, reason, "entityId:string", "expectedName:string");
        AddPartyDefinition(definitions, "party.join", "Request to join an observed live remote player's party. Approval remains a native server/player decision.", empty, reason, "entityId:string", "expectedName:string");
        AddPartyDefinition(definitions, "party.leave", "Request to leave the current party as a nonleader.", member && !leader, reason);
        AddPartyDefinition(definitions, "party.dismiss", "Request to dismiss the current party as its observed leader.", leader, reason);
        AddPartyDefinition(definitions, "party.kick", "Request removal of an observed other party member as leader.", leader, reason, "entityId:string", "expectedName:string");
        AddPartyDefinition(definitions, "party.set_leader", "Request leadership transfer to an observed online other party member.", leader, reason, "entityId:string", "expectedName:string");
    }

    private static void AddPartyDefinition(List<GameActionDefinition> definitions, string name, string description, bool available, string reason, params string[] fields)
    {
        var all = new List<string> { "expectedPartyRevision:string" };
        all.AddRange(fields);
        var definition = ActionDefinition(name, description + " Sent confirms only the native gateway write, not a changed party; observe party afterward.", available, reason, all.ToArray());
        // Explicit bounds are discoverable by Lua and external clients; dispatch enforces them too.
        var properties = new Dictionary<string, object>();
        foreach (var pair in (IReadOnlyDictionary<string, object>)definition.Arguments["properties"])
        {
            int maximum = pair.Key == "name" ? 16 : pair.Key == "expectedPartyRevision" ? 19 : pair.Key == "entityId" ? 64 : 256;
            properties.Add(pair.Key, new Dictionary<string, object> { { "type", "string" }, { "maxLength", maximum }, { "minLength", pair.Key == "name" ? 0 : 1 } });
        }
        definitions.Add(new GameActionDefinition(name, definition.Description,
            new Dictionary<string, object> { { "type", "object" }, { "properties", properties },
                { "required", definition.Arguments["required"] }, { "additionalProperties", false } }, available, available ? null : reason));
    }

    private void DispatchParty(PluginAction entry)
    {
        var args = entry.Request.Arguments;
        var session = Session;
        string expected = PartyString(args, "expectedPartyRevision", 19, false);
        long expectedRevision;
        if (!long.TryParse(expected, NumberStyles.None, CultureInfo.InvariantCulture, out expectedRevision) || expectedRevision < 0 ||
            expected != expectedRevision.ToString(CultureInfo.InvariantCulture))
            throw new ArgumentException("expectedPartyRevision must be the exact nonnegative decimal string from the party snapshot.");
        if (expectedRevision != session.PartyStateVersion)
            throw new ActionFailure("party.stale", "The native party changed. Observe party again before deciding.");
        // Native roots are immutable but may be republished by the receive owner.
        // Revalidate roles here, not only in the earlier discovery availability check.
        var party = session.PartyState;
        if (party == null) throw new ActionFailure("party.stale", "The native party snapshot is unavailable.");
        var frame = ActionFrame;
        int self = frame.Player.EntityId;
        long selfCharacterId = CharData.charId;
        int selfIndex = PartySelfIndex(party, self, selfCharacterId);
        bool member = selfIndex >= 0, leader = member && party.LeaderEntityId != 0 && party.LeaderEntityId == party.Members[selfIndex].EntityId;
        bool createOrJoin = entry.Request.Name == "party.create" || entry.Request.Name == "party.join";
        bool leaderOnly = entry.Request.Name == "party.dismiss" || entry.Request.Name == "party.kick" || entry.Request.Name == "party.set_leader";
        if ((createOrJoin && party.Members.Count != 0) || (!createOrJoin && !member) || (leaderOnly && !leader) || (entry.Request.Name == "party.leave" && leader))
            throw new ActionFailure("party.stale", "The native party membership or leadership changed before dispatch.");
        Action ensureCurrent = () =>
        {
            if (!ReferenceEquals(Session, session) || !ReferenceEquals(ActionFrame, frame) ||
                session.PartyStateVersion != expectedRevision || !ReferenceEquals(session.PartyState, party) ||
                CharData.charId != selfCharacterId || ActionFrame?.Player?.EntityId != self)
                throw new ActionFailure("party.stale", "The native party changed before dispatch. Observe again.");
        };
        ensureCurrent();
        switch (entry.Request.Name)
        {
            case "party.create":
                string name = PartyString(args, "name", 16, true);
                foreach (char c in name) if (char.IsControl(c)) throw new ArgumentException("Party names cannot contain control characters.");
                // A null invite list invokes only the existing ordinary create operation.
                ensureCurrent();
                ActionSend(entry, token => session.CreatePartyAndInviteAsync(name.Trim(), null, token)); return;
            case "party.leave": ensureCurrent(); ActionSend(entry, token => session.RequestPartyLeaveAsync(token)); return;
            case "party.dismiss": ensureCurrent(); ActionSend(entry, token => session.RequestPartyDismissAsync(token)); return;
        }
        string kind;
        int id = ActionEntity(PartyString(args, "entityId", 64, false), out kind);
        if (kind != "player" || id <= 0 || id == ActionFrame.Player.EntityId)
            throw new ActionFailure("party.target_invalid", "The target must be an observed other player in this scene.");
        string expectedName = PartyString(args, "expectedName", 256, false);
        if (entry.Request.Name == "party.kick" || entry.Request.Name == "party.set_leader")
        {
            WorldPartyMemberState target = null;
            for (int index = 0; index < party.Members.Count; index++)
                if (party.Members[index].EntityId == id)
                {
                    if (PartyIsSelf(party, index, self, selfCharacterId))
                        throw new ActionFailure("party.target_invalid", "A party operation cannot target the local character.");
                    target = party.Members[index]; break;
                }
            if (target == null || !string.Equals(target.DisplayName, expectedName, StringComparison.Ordinal))
                throw new ActionFailure("party.target_stale", "The observed member identity no longer matches the native party.");
            if (entry.Request.Name == "party.set_leader" && !target.Online)
                throw new ActionFailure("party.target_offline", "Leadership transfer requires an online party member.");
            ensureCurrent();
            ActionSend(entry, token => entry.Request.Name == "party.kick" ? session.RequestPartyKickAsync(id, token) : session.RequestPartyLeaderAsync(id, token));
            return;
        }
        ReferenceWorldRemotePlayerActor remote;
        if (party.ContainsEntity(id) || !ActionFrame.RemoteActors.TryGetValue(id, out remote) || remote.SourceIdentityForIndependentRuntime == null ||
            !string.Equals(remote.SourceIdentityForIndependentRuntime.SelName, expectedName, StringComparison.Ordinal))
            throw new ActionFailure("party.target_stale", "The target must match a current native remote player outside this party.");
        ensureCurrent();
        ActionSend(entry, token => entry.Request.Name == "party.invite" ? session.RequestPartyInviteAsync(id, expectedName, token) : session.RequestPartyJoinAsync(id, expectedName, token));
    }

    // Match the native social UI: persistent identity wins over recycled scene IDs.
    private static bool PartyIsSelf(WorldPartyState state, int index, int selfEntityId, long selfCharacterId)
    {
        if (index < 0 || index >= state.Members.Count) return false;
        if (index < state.CharacterIds.Count && state.CharacterIds[index] != 0 && selfCharacterId > 0)
            return state.CharacterIds[index] == selfCharacterId;
        return selfEntityId != 0 && state.Members[index].EntityId == selfEntityId;
    }

    private static int PartySelfIndex(WorldPartyState state, int selfEntityId, long selfCharacterId)
    {
        for (int index = 0; index < state.Members.Count; index++)
            if (PartyIsSelf(state, index, selfEntityId, selfCharacterId)) return index;
        return -1;
    }

    private static string PartyString(IReadOnlyDictionary<string, object> args, string key, int maximum, bool empty)
    {
        string text = ActionString(args, key);
        if (text.Length > maximum || (!empty && text.Length == 0)) throw new ArgumentException(key + " exceeds its discovered string bounds.");
        return text;
    }
}
}
