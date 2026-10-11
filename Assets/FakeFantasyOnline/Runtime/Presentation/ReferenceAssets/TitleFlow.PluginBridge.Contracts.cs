namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Deliberately separate from the public game API. Credential submission is reachable only
// through the separately authorized sensitive transport, never through an observation/action.
internal interface IPluginTitleBridge
{
    PluginTitleSnapshot ReadState();
    string SubmitLogin();
    string SubmitCredentials(string account, string password);
    string SelectServer(int directoryIndex);
    string ConfirmSelectedServer();
    string SelectCharacter(string characterId);
    string EnterWorld();
}

internal sealed class PluginTitleSnapshot
{
    public string InstanceId;
    public long Revision;
    public string Page;
    public bool LoginLoading, Entering, NeedsUpdate, CredentialsReady, ModalOpen;
    public string SafeMessage;
    public long ResultSequence;
    public string ResultCode;
    public bool HasNativeResultCode;
    public int NativeResultCode;
    public string SelectedCharacterId;
    public int ServerTotalCount, CharacterTotalCount;
    public bool ServersTruncated, CharactersTruncated;
    public PluginTitleServer[] Servers;
    public PluginTitleCharacter[] Characters;
}

internal sealed class PluginTitleServer
{
    public int DirectoryIndex, WorldIndex, LineIndex, BusyLevel, StatusLevel;
    public string WorldName, Name;
    public bool Selected, IsPlayerVersusPlayer;
}

internal sealed class PluginTitleCharacter
{
    public string CharacterId, DisplayName;
    public int Level, Race, Gender;
    public bool Selected;
}

}
