using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private PluginTitleSnapshot observedTitleState;
    private static string TitleIdentity(string value) => !string.IsNullOrEmpty(value) && value.Length <= 64 ? value : null;

    private void TrackTitleObservation()
    {
        observedTitleState = TitleBridge?.ReadState();
        TrackObservation("title", Token(observedTitleState?.InstanceId, observedTitleState?.Revision,
            Session?.State, Session?.Connected, ObservationFrame?.Built));
    }

    private Dictionary<string, object> ObserveTitle(ObservationArguments args)
    {
        var state = observedTitleState;
        var session = Session;
        bool worldEntered = ObservationWorldReady(session);
        if (state == null)
            return Values("available", true, "nativeTitleAvailable", false,
                "page", worldEntered ? "InWorld" : "Unavailable", "sessionState", session?.State.ToString(),
                "connected", session != null && session.Connected, "worldEntered", worldEntered,
                "loading", session != null && session.State == ClientSessionState.EnteringWorld,
                "credentialInput", "native_ui_or_sensitive_login", "credentialsExposed", false,
                "servers", new object[0], "characters", new object[0]);
        var servers = new List<object>();
        for (int i = args.Offset; i < state.Servers.Length && servers.Count < args.Limit; i++)
        {
            var row = state.Servers[i];
            servers.Add(Values("serverId", "server:" + row.DirectoryIndex.ToString(CultureInfo.InvariantCulture),
                "worldIndex", row.WorldIndex, "lineIndex", row.LineIndex,
                "worldName", BoundText(row.WorldName, 256), "name", BoundText(row.Name, 256),
                "busyLevel", row.BusyLevel, "statusLevel", row.StatusLevel, "maintenance", row.BusyLevel > 3,
                "selected", row.Selected, "pvp", row.IsPlayerVersusPlayer));
        }
        var characters = new List<object>();
        for (int i = args.Offset; i < state.Characters.Length && characters.Count < args.Limit; i++)
        {
            var row = state.Characters[i];
            characters.Add(Values("characterId", TitleIdentity(row.CharacterId), "selectable", TitleIdentity(row.CharacterId) != null, "name", BoundText(row.DisplayName, 256),
                "level", row.Level, "race", row.Race, "gender", row.Gender, "selected", row.Selected));
        }
        int serverNext = args.Offset + servers.Count, characterNext = args.Offset + characters.Count;
        return Values("available", true, "nativeTitleAvailable", true, "page", state.Page,
            "expectedTitleInstanceId", state.InstanceId, "expectedTitleRevision", state.Revision.ToString(CultureInfo.InvariantCulture),
            "sessionState", session?.State.ToString(), "connected", session != null && session.Connected,
            "worldEntered", worldEntered, "loginLoading", state.LoginLoading, "enteringWorld", state.Entering,
            "loading", state.LoginLoading || state.Entering, "updateRequired", state.NeedsUpdate, "modalOpen", state.ModalOpen,
            "credentialInput", "native_ui_or_sensitive_login", "credentialsReady", state.CredentialsReady, "credentialsExposed", false,
            // Message content is intentionally fixed text. Even network errors may echo
            // credentials; expose exact numeric native results and a local code instead.
            "hasNativeMessage", !string.IsNullOrEmpty(state.SafeMessage),
            "resultSequence", state.ResultSequence.ToString(CultureInfo.InvariantCulture), "resultCode", SafeTitleCode(state.ResultCode),
            "nativeResultCode", state.HasNativeResultCode ? (object)state.NativeResultCode : null,
            "characterIdentityKind", "native_name", "selectedCharacterId", TitleIdentity(state.SelectedCharacterId),
            "servers", servers, "characters", characters, "offset", args.Offset, "limit", args.Limit,
            "serverTotal", state.ServerTotalCount, "characterTotal", state.CharacterTotalCount,
            "serversTruncated", state.ServersTruncated || serverNext < state.Servers.Length,
            "charactersTruncated", state.CharactersTruncated || characterNext < state.Characters.Length,
            "serverNextOffset", serverNext < state.Servers.Length ? (object)serverNext : null,
            "characterNextOffset", characterNext < state.Characters.Length ? (object)characterNext : null);
    }
}
}
