namespace FakeFantasyOnline.PluginApi
{
    /// <summary>Ephemeral trusted-local login. Never route credentials through Execute or retain them.</summary>
    public interface IGameSensitiveLogin
    {
        GameSensitiveLoginResult SubmitLogin(string account, string password,
            string expectedTitleInstanceId, string expectedTitleRevision);
    }

    /// <summary>Optional live readiness for sensitive login (native title is on the Login page and idle).</summary>
    public interface IGameSensitiveLoginState
    {
        bool SensitiveLoginReady { get; }
    }

    public sealed class GameSensitiveLoginResult
    {
        public GameSensitiveLoginResult(bool submitted, string code) { Submitted = submitted; Code = code; }
        public bool Submitted { get; }
        public string Code { get; }
    }
}
