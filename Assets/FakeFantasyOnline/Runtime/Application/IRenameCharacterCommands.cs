using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IRenameCharacterCommands
{
    Task<Result> RenameCharacterAsync(string name, CancellationToken cancellationToken);
}

public interface IRenameCharacterReceiveContext
{
    void ReceiveRenameCharacterResult(int code, string message, string name);
}
}
