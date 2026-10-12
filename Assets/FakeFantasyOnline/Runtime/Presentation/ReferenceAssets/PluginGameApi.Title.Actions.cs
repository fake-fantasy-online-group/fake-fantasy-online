using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private bool titleSubmissionInProgress;
    private IPluginTitleBridge TitleBridge => TitleFlow.PluginBridgeForIndependentRuntime;

    private static bool ValidateTitleActionEnvelope(GameActionRequest request)
    {
        // The in-process/Lua path bypasses the socket protocol. Reject credential
        // keys for every ordinary action before any request can enter retention.
        foreach (var pair in request.Arguments)
        {
            string key = pair.Key.Replace("_", "").Replace("-", "").ToLowerInvariant();
            if (key.Contains("password") || key.Contains("credential") || key == "passwd" || key == "secret" || key == "token") return false;
        }
        if (!request.Name.StartsWith("title.", StringComparison.Ordinal)) return true;
        foreach (var pair in request.Arguments)
        {
            switch (pair.Key)
            {
                case "expectedTitleInstanceId": case "expectedTitleRevision":
                    if (!(pair.Value is string) || ((string)pair.Value).Length > 64) return false;
                    break;
                case "serverId":
                    if (request.Name != "title.select_server" || !(pair.Value is string) || ((string)pair.Value).Length > 32) return false;
                    break;
                case "characterId":
                    if (request.Name != "title.select_character" || !(pair.Value is string) || ((string)pair.Value).Length > 64) return false;
                    break;
                case "timeoutSeconds":
                    if ((request.Name != "title.submit_login" && request.Name != "title.enter_world") || !ActionIsNumber(pair.Value)) return false;
                    break;
                default: return false;
            }
        }
        return true;
    }

    private void AddTitleActions(List<GameActionDefinition> definitions)
    {
        var state = TitleBridge?.ReadState();
        bool ready = state != null && !state.LoginLoading && !state.Entering && !state.ModalOpen &&
            !titleSubmissionInProgress && Session != null && Session.State != ClientSessionState.InWorld;
        bool login = ready && state.Page == "Login" && !state.NeedsUpdate && state.CredentialsReady;
        bool server = ready && state.Page == "ServerSelection";
        bool character = ready && state.Page == "CharacterSelection";
        const string reason = "The current native title page, loading state, update gate or modal does not permit this action. Observe title.";
        AddTitleDefinition(definitions, "title.submit_login", "Submit credentials already entered in the native UI without saving preferences. No credential arguments. Completion requires a native login result; use the separate sensitive-login transport for ephemeral input.", login, reason, "timeoutSeconds?:number");
        AddTitleDefinition(definitions, "title.select_server", "Select an observed native server row locally. Does not confirm or connect.", server, reason, "serverId:string");
        AddTitleDefinition(definitions, "title.confirm_server", "Confirm the current native server selection. Endpoint switches may require login again; completion reports only the native page transition.", server, reason);
        AddTitleDefinition(definitions, "title.select_character", "Select an observed native character by exact identity. Never creates or deletes a character.", character, reason, "characterId:string");
        AddTitleDefinition(definitions, "title.enter_world", "Invoke native entry for the selected character. Completion requires the matching active built world; timeout never cancels or retries an already submitted entry.", character && !string.IsNullOrEmpty(state.SelectedCharacterId) && state.SelectedCharacterId.Length <= 64, reason, "timeoutSeconds?:number");
    }

    private static void AddTitleDefinition(List<GameActionDefinition> definitions, string name, string description,
        bool available, string reason, params string[] fields)
    {
        var all = new List<string> { "expectedTitleInstanceId:string", "expectedTitleRevision:string" };
        all.AddRange(fields);
        definitions.Add(ActionDefinition(name, description, available, reason, all.ToArray()));
    }

    private PluginTitleSnapshot GuardTitle(IPluginTitleBridge owner, string expectedInstance, string expectedRevision)
    {
        long revisionValue;
        if (string.IsNullOrEmpty(expectedInstance) || expectedInstance.Length > 64 ||
            !long.TryParse(expectedRevision, NumberStyles.None, CultureInfo.InvariantCulture, out revisionValue) || revisionValue < 0 ||
            expectedRevision != revisionValue.ToString(CultureInfo.InvariantCulture))
            throw new ActionFailure("title.invalid_guard", "Use exact title instance and canonical revision strings from a new observation.");
        var state = owner?.ReadState();
        if (state == null || !ReferenceEquals(owner, TitleBridge))
            throw new ActionFailure("title.unavailable", "The native title owner is unavailable.");
        if (state.InstanceId != expectedInstance || state.Revision != revisionValue)
            throw new ActionFailure("title.stale", "The native title changed. Observe again before deciding.");
        if (titleSubmissionInProgress || state.LoginLoading || state.Entering)
            throw new ActionFailure("title.busy", "A native login or world-entry request is already active.");
        if (state.ModalOpen || Session == null || Session.State == ClientSessionState.InWorld)
            throw new ActionFailure("title.unavailable", "The native title action is unavailable.");
        return state;
    }

    private void DispatchTitle(PluginAction entry)
    {
        var args = entry.Request.Arguments;
        var owner = TitleBridge;
        var state = GuardTitle(owner, ActionString(args, "expectedTitleInstanceId"), ActionString(args, "expectedTitleRevision"));
        string error;
        switch (entry.Request.Name)
        {
            case "title.select_server":
                string serverId = ActionString(args, "serverId");
                int directoryIndex;
                if (!serverId.StartsWith("server:", StringComparison.Ordinal) ||
                    !int.TryParse(serverId.Substring(7), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out directoryIndex) ||
                    serverId != "server:" + directoryIndex.ToString(CultureInfo.InvariantCulture))
                    throw new ActionFailure("title.invalid_server", "serverId must be copied from the title observation.");
                int matches = 0;
                foreach (var row in state.Servers) if (row.DirectoryIndex == directoryIndex) matches++;
                if (matches != 1) throw new ActionFailure("title.invalid_server", "The server ID is missing or ambiguous in this snapshot.");
                error = owner.SelectServer(directoryIndex); break;
            case "title.confirm_server": error = owner.ConfirmSelectedServer(); break;
            case "title.select_character":
                string characterId = ActionString(args, "characterId");
                int characterMatches = 0;
                foreach (var row in state.Characters) if (row.CharacterId == characterId) characterMatches++;
                if (characterMatches != 1) throw new ActionFailure("title.invalid_character", "The character ID is missing or ambiguous in this snapshot.");
                error = owner.SelectCharacter(characterId); break;
            case "title.submit_login": case "title.enter_world":
                entry.Deadline = ActionNow + ActionFloat(args, "timeoutSeconds", 30f, 1f, 120f);
                entry.TitleOwner = owner;
                entry.TitleInstance = state.InstanceId;
                entry.TitleResultSequence = state.ResultSequence;
                entry.TitleSubmittedRevision = state.Revision;
                entry.TitleSession = Session;
                entry.TitlePreviousFrame = ActionFrame;
                entry.Target = state.SelectedCharacterId;
                entry.Kind = entry.Request.Name == "title.submit_login" ? "title.login" : "title.enter";
                titleSubmissionInProgress = true;
                try { error = entry.Kind == "title.login" ? owner.SubmitLogin() : owner.EnterWorld(); }
                finally { titleSubmissionInProgress = false; }
                if (error == null)
                {
                    ActionSet(entry, GameActionState.Running, "title.submitted", "The native title operation started. No successful server outcome is asserted; observe its result before retrying.");
                    return;
                }
                break;
            default: throw new ActionFailure("action.unknown", "This title action is not defined.");
        }
        if (error != null) throw new ActionFailure("title." + SafeTitleCode(error), "The native title declined this operation. Observe the current title state.");
        var after = owner.ReadState();
        ActionLocal(entry, "title.local_transition", "The native title selection or page transition completed locally.",
            new Dictionary<string, object> { { "page", after?.Page }, { "requiresLogin", after?.Page == "Login" },
                { "expectedTitleInstanceId", after?.InstanceId }, { "expectedTitleRevision", after?.Revision.ToString(CultureInfo.InvariantCulture) } });
    }

    private void PollTitle(PluginAction entry)
    {
        if (!ActionActive(entry)) return;
        if (!ReferenceEquals(Session, entry.TitleSession))
        {
            ActionSet(entry, GameActionState.Unknown, "title.owner_changed", "The session changed before the native result was established. Observe again; do not automatically retry."); return;
        }
        var currentOwner = TitleBridge;
        var state = currentOwner?.ReadState();
        // Expected teardown makes the old owner absent. A replacement owner instead
        // proves that a new title flow has superseded this request, even for the same name.
        if (currentOwner != null && (!ReferenceEquals(currentOwner, entry.TitleOwner) ||
            (state != null && state.InstanceId != entry.TitleInstance)))
        { ActionSet(entry, GameActionState.Unknown, "title.owner_changed", "A new title flow replaced this request. Observe before retrying."); return; }
        bool inWorld = Session != null && Session.State == ClientSessionState.InWorld;
        if (entry.Kind == "title.enter" && inWorld && Session.Connected && ActionFrame != null && ActionFrame.Built && !ReferenceEquals(ActionFrame, entry.TitlePreviousFrame))
        {
            // The native roster and EnterGame identify characters by name, not the
            // persistent charId learned later inside the world.
            string actualId = CharData.name;
            if (actualId != (string)entry.Target)
            { ActionSet(entry, GameActionState.Unknown, "title.character_changed", "A different character entered the world. Observe the active player."); return; }
            ActionSet(entry, GameActionState.Completed, "title.world_entered", "The requested character is in the active built native world.",
                new Dictionary<string, object> { { "completionScope", "observed_world" }, { "serverOutcomeKnown", true }, { "characterId", actualId } }); return;
        }
        if (entry.Kind == "title.enter" && inWorld && state == null)
        {
            // Native title destruction precedes completed world construction. Do not treat
            // the expected teardown as failure or require the old scene revision.
        }
        else if (!ReferenceEquals(currentOwner, entry.TitleOwner) || state == null || state.InstanceId != entry.TitleInstance)
        { ActionSet(entry, GameActionState.Unknown, "title.owner_changed", "The title owner changed before a definitive result. Observe before retrying."); return; }
        else
        {
            if (entry.Kind == "title.login" && state.ResultSequence > entry.TitleResultSequence && state.ResultCode == "login_succeeded")
            { ActionSet(entry, GameActionState.Completed, "title.login_succeeded", "The native login callback reported success. Server and character selection remain separate.", new Dictionary<string, object> { { "serverOutcomeKnown", true }, { "completionScope", "native_login_result" } }); return; }
            if (state.ResultSequence > entry.TitleResultSequence && IsTitleFailure(state.ResultCode))
            { ActionSet(entry, state.ResultCode.EndsWith("timeout", StringComparison.Ordinal) ? GameActionState.TimedOut : GameActionState.Rejected,
                "title." + SafeTitleCode(state.ResultCode), "The native title reported an error or timeout. No automatic retry was issued."); return; }
            string expectedPage = entry.Kind == "title.login" ? "Login" : "CharacterSelection";
            if (state.Page != expectedPage && !(entry.Kind == "title.login" && state.Page == "ServerSelection"))
            { ActionSet(entry, GameActionState.Unknown, "title.page_changed", "The native page changed before a definitive result. Observe before retrying."); return; }
        }
        if (ActionNow >= entry.Deadline)
            ActionSet(entry, GameActionState.TimedOut, "title.timeout", "The observation deadline expired. The native request may still complete; inspect title/world before any retry.");
    }

    private static bool IsTitleFailure(string code) => code == "login_failed" || code == "connection_failed" ||
        code == "network_error" || code == "login_timeout" || code == "enter_timeout" || code == "enter_failed";

    private static string SafeTitleCode(string code)
    {
        switch (code)
        {
            case "": return string.Empty;
            case "title_unavailable": case "wrong_title_page": case "title_busy": case "native_modal_open":
            case "client_update_required": case "native_credentials_required": case "invalid_credentials":
            case "ambiguous_server": case "server_not_found": case "server_maintenance": case "server_confirmation_failed":
            case "ambiguous_character": case "character_not_found": case "character_not_selected":
            case "login_succeeded": case "login_failed": case "connection_failed": case "network_error":
            case "login_timeout": case "enter_timeout": case "enter_failed": return code;
            default: return "native_result";
        }
    }

    // Dedicated ephemeral transport only. No PluginAction, request ID, event or retained
    // request contains these parameters. Native code does not persist the supplied values.
    public GameSensitiveLoginResult SubmitLogin(string account, string password, string expectedTitleInstanceId, string expectedTitleRevision)
    {
        RequireMainThread();
        try
        {
            if (string.IsNullOrWhiteSpace(account) || account.Length > 20 || string.IsNullOrEmpty(password) || password.Length > 40)
                return new GameSensitiveLoginResult(false, "invalid_credentials");
            var owner = TitleBridge;
            var state = GuardTitle(owner, expectedTitleInstanceId, expectedTitleRevision);
            if (state.Page != "Login" || state.NeedsUpdate) return new GameSensitiveLoginResult(false, "not_ready");
            titleSubmissionInProgress = true;
            string code;
            try { code = owner.SubmitCredentials(account, password); }
            finally { titleSubmissionInProgress = false; }
            return new GameSensitiveLoginResult(code == null, code == null ? "submitted" : "not_ready");
        }
        catch (ActionFailure failure)
        {
            return new GameSensitiveLoginResult(false, failure.Code == "title.stale" ? "title_changed" : failure.Code == "title.busy" ? "busy" : "not_ready");
        }
        catch (Exception) { return new GameSensitiveLoginResult(false, "outcome_unknown"); }
        finally { account = null; password = null; }
    }
}
}
