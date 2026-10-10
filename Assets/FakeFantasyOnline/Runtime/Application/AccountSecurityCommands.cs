using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IAccountSecurityCommands
{
    Task<Result> ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken);
    Task<Result> ChangeSecurityCodeAsync(string password, string oldCode, string newCode, CancellationToken cancellationToken);
}
}
