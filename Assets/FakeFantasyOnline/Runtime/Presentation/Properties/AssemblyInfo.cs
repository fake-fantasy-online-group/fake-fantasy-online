using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FakeFantasyOnline.Editor")]
// The composition root binds NetClient.DispatchMsPerFrame (official 0600077a, the receive
// budget of the recovered Update) as a host provider, so Bootstrap needs the same access.
[assembly: InternalsVisibleTo("FakeFantasyOnline.Bootstrap")]
