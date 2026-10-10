using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
// The production receipt owner consumes the existing Buf before optional observation rows.
public interface ICombatStatusReceiveOwner
{
    int SelfEntityId { get; }
    WorldEntityStatusEffect ReceiveEntityStatusEffect(ICombatEntityReader reader, out int entityId);
    IReadOnlyList<WorldEntityStatusIcon> ReceiveEntityStatusIcons(ICombatEntityReader reader, out int entityId);
}
}
