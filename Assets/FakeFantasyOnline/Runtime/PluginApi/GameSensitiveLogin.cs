namespace FakeFantasyOnline.PluginApi
{
    /// <summary>Ephemeral trusted-local login. Never route credentials through Execute or retain them.</summary>
    public interface IGameSensitiveLogin
    {
        GameSensitiveLoginResult SubmitLogin(string account, string password,
            string expectedTitleInstanceId, string expectedTitleRevision);
    }

    public sealed class GameSensitiveLoginResult
    {
        public GameSensitiveLoginResult(bool submitted, string code) { Submitted = submitted; Code = code; }
        public bool Submitted { get; }
        public string Code { get; }
    }
}
