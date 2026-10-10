namespace FakeFantasyOnline.Application
{

/// <summary>The five values carried by official 1.5.8 downlink 0x8072.</summary>
public sealed class WorldInvitationCodeResult
{
    public WorldInvitationCodeResult(byte result, string code, int lifetimeSeconds,
        int remainingAccounts, string message)
    {
        Result = result;
        Code = code;
        LifetimeSeconds = lifetimeSeconds;
        RemainingAccounts = remainingAccounts;
        Message = message;
    }

    public byte Result { get; }
    public string Code { get; }
    public int LifetimeSeconds { get; }
    public int RemainingAccounts { get; }
    public string Message { get; }
}

}
