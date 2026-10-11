using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow : IPluginTitleBridge
{
    private static TitleFlow pluginTitleOwner;
    private readonly string pluginTitleInstanceId = Guid.NewGuid().ToString("N");
    private long pluginTitleRevision;
    private string pluginTitleFingerprint;
    private long pluginTitleResultSequence;
    private string pluginTitleResultCode = string.Empty;
    private bool pluginTitleHasNativeResultCode;
    private int pluginTitleNativeResultCode;

    internal static IPluginTitleBridge PluginBridgeForIndependentRuntime =>
        pluginTitleOwner != null && pluginTitleOwner.isActiveAndEnabled ? pluginTitleOwner : null;

    // These codes are locally generated, never copies of server-supplied messages. A raw
    // message can contain arbitrary server text (including an echoed credential).
    private void PublishPluginTitleResult(string code, int? nativeCode = null)
    {
        pluginTitleResultSequence++;
        pluginTitleRevision++;
        pluginTitleResultCode = code;
        pluginTitleHasNativeResultCode = nativeCode.HasValue;
        pluginTitleNativeResultCode = nativeCode.GetValueOrDefault();
    }

    private bool PluginTitleAvailable => this != null && isActiveAndEnabled &&
        guiHost != null && guiHost._session != null && !guiHost._backgroundSessionCheckRequested;

    public PluginTitleSnapshot ReadState()
    {
        if (!PluginTitleAvailable) return null;
        var servers = new List<PluginTitleServer>();
        int serverTotal = 0, characterTotal = 0;
        if (_page == TitlePage.ServerSelection)
        {
            PrepareServerPageForPluginAndGui();
            // Match the native draw's count-only invalidation and copied/normalized tree.
            if (srvWorlds == null || srvBuiltFor != serverSession.TitleServerCountForIndependentRuntime)
                BuildSrvTree();
            selWorld = Mathf.Clamp(selWorld, 0, Mathf.Max(0, srvWorlds.Count - 1));
            if (srvWorlds.Count > 0)
                selZone = Mathf.Clamp(selZone, 0, Mathf.Max(0, srvLines[selWorld].Count - 1));
            for (int wi = 0; wi < srvWorlds.Count; wi++)
                for (int li = 0; li < srvLines[wi].Count; li++)
                {
                    serverTotal++;
                    if (servers.Count >= 4096) continue;
                    SrvLine line = srvLines[wi][li];
                    servers.Add(new PluginTitleServer {
                        DirectoryIndex = line.directoryIndex, WorldIndex = wi, LineIndex = li,
                        WorldName = srvWorlds[wi], Name = line.name, BusyLevel = line.busy,
                        StatusLevel = LevelOf(line), IsPlayerVersusPlayer = line.pvp,
                        Selected = wi == selWorld && li == selZone });
                }
        }
        var characters = new List<PluginTitleCharacter>();
        if (_page == TitlePage.CharacterSelection)
        {
            BindRosterForIndependentRuntime(guiHost._session.Characters);
            characterTotal = serverChars.Count;
            for (int i = 0; i < Math.Min(serverChars.Count, 4096); i++)
            {
                var row = serverChars[i];
                characters.Add(new PluginTitleCharacter { CharacterId = row.CharacterId,
                    DisplayName = row.DisplayName, Level = row.Level, Race = row.Race,
                    Gender = row.Gender, Selected = i == selSlot });
            }
        }
        var state = new PluginTitleSnapshot {
            InstanceId = pluginTitleInstanceId, Page = _page.ToString(),
            LoginLoading = loggingIn, Entering = entering,
            NeedsUpdate = RecoveredTitleLoginSurface.NeedUpdate,
            CredentialsReady = !string.IsNullOrWhiteSpace(account) && !string.IsNullOrEmpty(password),
            ModalOpen = _noticeOpen || pendingDelete,
            SafeMessage = string.IsNullOrEmpty(netMsg) ? string.Empty : "Native title has a status message.",
            ResultSequence = pluginTitleResultSequence, ResultCode = pluginTitleResultCode,
            HasNativeResultCode = pluginTitleHasNativeResultCode, NativeResultCode = pluginTitleNativeResultCode,
            SelectedCharacterId = _page == TitlePage.CharacterSelection && selSlot >= 0 && selSlot < serverChars.Count
                ? serverChars[selSlot].CharacterId : null,
            ServerTotalCount = serverTotal, CharacterTotalCount = characterTotal,
            ServersTruncated = serverTotal > servers.Count, CharactersTruncated = characterTotal > characters.Count,
            Servers = servers.ToArray(), Characters = characters.ToArray() };
        string fingerprint = Fingerprint(state);
        if (!string.Equals(fingerprint, pluginTitleFingerprint, StringComparison.Ordinal))
        {
            pluginTitleFingerprint = fingerprint;
            pluginTitleRevision++;
        }
        state.Revision = pluginTitleRevision;
        return state;
    }

    private static string Fingerprint(PluginTitleSnapshot state)
    {
        var text = new StringBuilder();
        Action<object> add = value => {
            string field = value == null ? string.Empty : value.ToString();
            text.Append(field.Length).Append(':').Append(field).Append(';');
        };
        add(state.Page); add(state.LoginLoading); add(state.Entering); add(state.NeedsUpdate);
        add(state.CredentialsReady); add(state.ModalOpen); add(state.SafeMessage);
        add(state.ServerTotalCount); add(state.CharacterTotalCount);
        add(state.ResultSequence); add(state.ResultCode); add(state.HasNativeResultCode); add(state.NativeResultCode); add(state.SelectedCharacterId);
        foreach (var row in state.Servers) {
            add(row.DirectoryIndex); add(row.WorldIndex); add(row.LineIndex); add(row.WorldName);
            add(row.Name); add(row.BusyLevel); add(row.StatusLevel); add(row.Selected); add(row.IsPlayerVersusPlayer);
        }
        foreach (var row in state.Characters) {
            add(row.CharacterId); add(row.DisplayName); add(row.Level); add(row.Race); add(row.Gender); add(row.Selected);
        }
        return text.ToString();
    }

    private string GuardPluginTitle(TitlePage required)
    {
        if (!PluginTitleAvailable) return "title_unavailable";
        if (_page != required) return "wrong_title_page";
        if (loggingIn || entering) return "title_busy";
        if (_noticeOpen || pendingDelete) return "native_modal_open";
        return null;
    }

    public string SubmitLogin()
    {
        string error = GuardPluginTitle(TitlePage.Login);
        if (error != null) return error;
        if (RecoveredTitleLoginSurface.NeedUpdate) return "client_update_required";
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(password))
            return "native_credentials_required";
        if (NetClient.I == null) return "title_unavailable";
        BeginLogin(account.Trim(), savePreferences: false);
        return null;
    }

    public string SubmitCredentials(string submittedAccount, string submittedPassword)
    {
        string error = GuardPluginTitle(TitlePage.Login);
        if (error != null) return error;
        if (RecoveredTitleLoginSurface.NeedUpdate) return "client_update_required";
        if (string.IsNullOrWhiteSpace(submittedAccount) || submittedAccount.Length > 20 ||
            string.IsNullOrEmpty(submittedPassword) || submittedPassword.Length > 40)
            return "invalid_credentials";
        if (NetClient.I == null) return "title_unavailable";
        // BeginLogin captures the supplied pair before connection completion. Restore the
        // UI fields even on synchronous failure so a later manual preference save cannot
        // accidentally persist credentials supplied through the sensitive channel.
        string previousAccount = account, previousPassword = password;
        try
        {
            account = submittedAccount;
            password = submittedPassword;
            BeginLogin(account.Trim(), savePreferences: false);
            return null;
        }
        finally
        {
            account = previousAccount;
            password = previousPassword;
        }
    }

    public string SelectServer(int directoryIndex)
    {
        string error = GuardPluginTitle(TitlePage.ServerSelection);
        if (error != null) return error;
        PluginTitleSnapshot state = ReadState();
        PluginTitleServer found = null;
        foreach (var row in state.Servers)
            if (row.DirectoryIndex == directoryIndex) {
                if (found != null) return "ambiguous_server";
                found = row;
            }
        if (found == null) return "server_not_found";
        pluginTitleRevision++;
        selWorld = found.WorldIndex;
        selZone = found.LineIndex;
        return null;
    }

    public string ConfirmSelectedServer()
    {
        string error = GuardPluginTitle(TitlePage.ServerSelection);
        if (error != null) return error;
        ReadState();
        if (srvWorlds.Count == 0 || srvLines[selWorld].Count == 0) return "server_not_found";
        if (srvLines[selWorld][selZone].busy > 3) return "server_maintenance";
        ConfirmServer();
        return _page == TitlePage.CharacterSelection || _page == TitlePage.Login ? null : "server_confirmation_failed";
    }

    public string SelectCharacter(string characterId)
    {
        string error = GuardPluginTitle(TitlePage.CharacterSelection);
        if (error != null) return error;
        ReadState();
        int found = -1;
        for (int i = 0; i < serverChars.Count; i++)
            if (string.Equals(serverChars[i].CharacterId, characterId, StringComparison.Ordinal)) {
                if (found >= 0) return "ambiguous_character";
                found = i;
            }
        if (found < 0) return "character_not_found";
        pluginTitleRevision++;
        selSlot = found;
        MirrorSlotToCharData(selSlot);
        return null;
    }

    public string EnterWorld()
    {
        string error = GuardPluginTitle(TitlePage.CharacterSelection);
        if (error != null) return error;
        ReadState();
        if (selSlot < 0 || selSlot >= serverChars.Count) return "character_not_selected";
        if (NetClient.I == null) return "title_unavailable";
        EnterGame();
        return null;
    }
}
}
