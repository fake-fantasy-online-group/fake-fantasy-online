using System.IO;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Observation only: this already-owned packet never drives Win05 again. Deferring
    // its immutable projection keeps DTO allocations off the native receipt path.
    private byte[] _lifeSkillBookObservationPayload;

    private WorldLifeSkillBookState ReadLifeSkillBookState()
    {
        lock (_worldStateSync)
        {
            if (_lifeSkillBookObservationPayload != null)
            {
                if (!PrivateServerSkillBookCodec.TryDecodeLifeSkills(
                        _lifeSkillBookObservationPayload, out WorldLifeSkillBookState state))
                    throw new InvalidDataException("The received life-skill observation could not be decoded.");
                Volatile.Write(ref _lifeSkillBookState, state);
                _lifeSkillBookObservationPayload = null;
            }
            return Volatile.Read(ref _lifeSkillBookState);
        }
    }
}
}
