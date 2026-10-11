using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static partial class GameActionAdapterTests
{
    private static void RunPartyTests()
    {
        var api = Setup();
        var session = api.TestSession;
        CharData.charId = 0;
        var definitions = new Dictionary<string, GameActionDefinition>();
        foreach (var d in api.GetActions()) if (d.Name.StartsWith("party.")) definitions.Add(d.Name, d);
        Check(definitions.Count == 7 && definitions["party.create"].Available && definitions["party.join"].Available && !definitions["party.dismiss"].Available, "party catalog exposes ordinary actions with native membership availability");
        var properties = (IReadOnlyDictionary<string, object>)definitions["party.create"].Arguments["properties"];
        Check(Convert.ToInt32(((IReadOnlyDictionary<string, object>)properties["name"])["maxLength"]) == 16, "party name schema mirrors native UI bound");
        Check(Act(api, "party.create", Map("name", "solo")).Code == "arguments.invalid", "party revision is required");
        Check(Act(api, "party.create", Map("name", "solo", "expectedPartyRevision", "0")).Code == "party.stale", "stale party snapshot never sends create");
        foreach (var invalid in new[] { "01", "-1", "1.0", "9223372036854775808", " 1" })
            Check(Act(api, "party.create", Map("name", "solo", "expectedPartyRevision", invalid)).Code == "arguments.invalid", "party revision rejects noncanonical or overflowing string");
        Check(Act(api, "party.create", Map("name", new string('x', 17), "expectedPartyRevision", "1")).Code == "arguments.invalid", "oversized name rejected");
        Check(Act(api, "party.create", Map("name", "x\ny", "expectedPartyRevision", "1")).Code == "arguments.invalid", "control character rejected");
        Check(session.Sends == 0, "invalid party input never writes");
        var created = Act(api, "party.create", Map("name", " solo ", "expectedPartyRevision", "1"), "party-create");
        Check(created.State == GameActionState.Sent && !(bool)created.Data["serverOutcomeKnown"] && session.LastPartyAction == "create" && session.LastPartyName == "solo", "create uses native gateway and reports write only");
        Check(Act(api, "party.create", Map("name", " solo ", "expectedPartyRevision", "1"), "party-create").ActionId == created.ActionId && session.Sends == 1, "party request replay does not resend");
        var frame = GameBootstrap.I.Window.Frame;
        frame.RemoteActors[2] = new ReferenceWorldRemotePlayerActor { SourceIdentityForIndependentRuntime = new RemotePlayer { SelName = "Other" } };
        Check(Act(api, "party.join", Map("entityId", "player:0:2", "expectedName", "Other", "expectedPartyRevision", "1")).Code == "entity.stale", "party target scene guard");
        Check(Act(api, "party.join", Map("entityId", "player:1:2", "expectedName", "Changed", "expectedPartyRevision", "1")).Code == "party.target_stale", "party target name guard");
        var joined = Act(api, "party.join", Map("entityId", "player:1:2", "expectedName", "Other", "expectedPartyRevision", "1"));
        Check(joined.State == GameActionState.Sent && session.LastPartyAction == "join" && session.LastPartyTarget == 2, "join uses native observed target");
        session.PartyState.Members.Add(new WorldPartyMemberState { EntityId = 1, DisplayName = "Self" });
        session.PartyState.LeaderEntityId = 1; session.PartyStateVersion = 2;
        Check(Act(api, "party.create", Map("name", "solo", "expectedPartyRevision", "2")).Code == "action.unavailable", "cannot create over existing party");
        Check(Act(api, "party.invite", Map("entityId", "player:1:2", "expectedName", "Other", "expectedPartyRevision", "2")).State == GameActionState.Sent && session.LastPartyAction == "invite", "invite through native command");
        session.PartyState.Members.Add(new WorldPartyMemberState { EntityId = 2, DisplayName = "Other", Online = false });
        session.PartyStateVersion = 3;
        Check(Act(api, "party.set_leader", Map("entityId", "player:1:2", "expectedName", "Other", "expectedPartyRevision", "3")).Code == "party.target_offline", "offline member cannot become leader");
        Check(Act(api, "party.kick", Map("entityId", "player:1:1", "expectedName", "Self", "expectedPartyRevision", "3")).Code == "party.target_invalid", "self kick rejected");
        Check(Act(api, "party.kick", Map("entityId", "player:1:2", "expectedName", "Other", "expectedPartyRevision", "3")).State == GameActionState.Sent && session.LastPartyAction == "kick", "offline member kick uses native command");
        session.PartyState.Members[1].Online = true;
        Check(Act(api, "party.set_leader", Map("entityId", "player:1:2", "expectedName", "Other", "expectedPartyRevision", "3")).State == GameActionState.Sent && session.LastPartyAction == "leader", "leader transfer uses native command");
        Check(Act(api, "party.dismiss", Map("expectedPartyRevision", "3")).State == GameActionState.Sent && session.LastPartyAction == "dismiss", "dismiss uses native command");
        Check(Act(api, "party.leave", Map("expectedPartyRevision", "3")).Code == "action.unavailable", "leader uses dismiss, not leave, matching native menu");
        CharData.charId = 99;
        session.PartyState.CharacterIds.Add(77);
        session.PartyState.CharacterIds.Add(88);
        Check(!Definition(api, "party.dismiss").Available, "recycled self entity with different persistent identity is not leader");
        session.PartyState.CharacterIds[0] = 99;
        session.PartyState.Members[0].EntityId = 9;
        session.PartyState.LeaderEntityId = 9;
        Check(Definition(api, "party.dismiss").Available, "persistent self identity wins over changed scene ID");
        Check(Act(api, "party.kick", Map("entityId", "player:1:9", "expectedName", "Self", "expectedPartyRevision", "3")).Code == "party.target_invalid", "cannot kick persistent self at old entity ID");
        session.PartyState.Members[0].EntityId = 0;
        session.PartyState.LeaderEntityId = 0;
        Check(!Definition(api, "party.dismiss").Available && Definition(api, "party.leave").Available,
            "zero leader sentinel cannot make persistent self row leader");
        CharData.charId = 0;
        session.PartyState.Members[0].EntityId = 1;
        session.PartyState.LeaderEntityId = 2;
        Check(Act(api, "party.dismiss", Map("expectedPartyRevision", "3")).Code == "action.unavailable", "nonleader dismissal blocked");
        Check(Act(api, "party.leave", Map("expectedPartyRevision", "3")).State == GameActionState.Sent && session.LastPartyAction == "leave", "member leave uses native command");
        api = Setup(); session = api.TestSession;
        int versionReads = 0;
        session.PartyVersionReader = () =>
        {
            if (++versionReads == 2) session.PartyState = new WorldPartyState();
            return 1;
        };
        Check(Act(api, "party.create", Map("name", "", "expectedPartyRevision", "1")).Code == "party.stale" && session.Sends == 0,
            "party root replacement before version increment is rejected");
        session.PartyVersionReader = () => ++versionReads >= 4 ? 2 : 1;
        Check(Act(api, "party.create", Map("name", "", "expectedPartyRevision", "1")).Code == "party.stale" && session.Sends == 0,
            "party revision changing during dispatch is rejected");
        api = Setup(); session = api.TestSession;
        session.PartyState.Members.Add(new WorldPartyMemberState { EntityId = 1, DisplayName = "Self" });
        session.PartyState.LeaderEntityId = 1;
        session.PartyVersionReader = () =>
        {
            session.PartyState = new WorldPartyState();
            return 1;
        };
        Check(Act(api, "party.dismiss", Map("expectedPartyRevision", "1")).Code == "party.stale" && session.Sends == 0,
            "party role is revalidated after earlier availability check");
        api = Setup(); session = api.TestSession;
        var pending = new TaskCompletionSource<Result>(); session.Next = pending.Task;
        created = Act(api, "party.create", Map("name", "", "expectedPartyRevision", "1"));
        Check(created.State == GameActionState.Queued, "party awaits native task");
        api.TestScene = 2;
        Check(api.GetAction(created.ActionId).State == GameActionState.Unknown, "party scene interruption remains uncertain");
        pending.SetResult(Result.Success());
        Check(api.GetAction(created.ActionId).State == GameActionState.Sent, "late party write observed truthfully");
    }
}
