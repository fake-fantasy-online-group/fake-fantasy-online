using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IMailCommands
    {
        Task<Result> OpenMailAsync(CancellationToken ct);
        Task<Result> ActionAsync(byte action, long mailId, CancellationToken ct);
        Task<Result> SendAsync(string to, string title, string body, long money, long caiyu, byte type,
            List<(byte, int, int)> attachments, CancellationToken ct);
    }
}
