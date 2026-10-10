using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface ICharacterSettingsCommands
{
    Task<Result> SetCharacterPkModeAsync(int mode, CancellationToken cancellationToken);
    Task<Result> SetCharacterGlowModeAsync(int mode, CancellationToken cancellationToken);
    Task<Result> SetCharacterSmartCastAsync(bool enabled, CancellationToken cancellationToken);
    Task<Result> SetPetPickQualityAsync(byte quality, CancellationToken cancellationToken);
}
}
