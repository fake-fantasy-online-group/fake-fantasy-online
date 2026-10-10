using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IHelpTopicsStateSource
{
    private WorldHelpTopicsState _helpTopicsState = WorldHelpTopicsState.Empty;
    private long _helpTopicsStateVersion;

    public WorldHelpTopicsState HelpTopicsState => Volatile.Read(ref _helpTopicsState);
    public long HelpTopicsStateVersion => Interlocked.Read(ref _helpTopicsStateVersion);

    private static WorldHelpTopicsState ReadHelpTopicsState(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int count = reader.U8();
        var topics = new List<WorldHelpTopic>(count);
        for (int i = 0; i < count; i++)
            topics.Add(new WorldHelpTopic(reader.Str(), reader.Str()));
        return new WorldHelpTopicsState(topics);
    }

    private void PublishHelpTopicsState(WorldHelpTopicsState state, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _helpTopicsState, state);
            Interlocked.Increment(ref _helpTopicsStateVersion);
        }
    }
}
}
