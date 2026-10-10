using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeFantasyOnline.PluginApi
{

public interface IPluginCommand
{
    string CommandName { get; }
}

public sealed class SendChatMessageCommand : IPluginCommand
{
    public const string Name = "chat.send";

    public SendChatMessageCommand(string channel, string message)
    {
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public string CommandName => Name;

    public string Channel { get; }

    public string Message { get; }
}

public enum PluginCommandStatus
{
    ServerAccepted,
    ServerRejected,
    TransportFailed
}

public sealed class PluginCommandResult
{
    public PluginCommandResult(PluginCommandStatus status, string reasonCode)
    {
        Status = status;
        ReasonCode = reasonCode ?? string.Empty;
    }

    public PluginCommandStatus Status { get; }

    public string ReasonCode { get; }
}

public interface IPluginCommandGateway
{
    Task<PluginCommandResult> ExecuteAsync(
        IPluginCommand command,
        CancellationToken cancellationToken);
}
}
