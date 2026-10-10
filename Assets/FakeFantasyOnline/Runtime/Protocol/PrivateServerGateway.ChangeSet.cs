using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IChangeSetSource, IChangeSetCommands
    {
        private IReadOnlyList<WorldChangeSetItem> _changeSet = Array.Empty<WorldChangeSetItem>();
        private int _changeSetVersion;
        public IReadOnlyList<WorldChangeSetItem> ChangeSetSnapshot => Volatile.Read(ref _changeSet);
        public int ChangeSetVersion => Volatile.Read(ref _changeSetVersion);

        // Official Dispatch 0x0600077f, 0x8073: U16 row count, then the exact ChgItem fields.
        // arm64 jump target 0x250950; SetChangeSet call 0x257368. Icons are presentation data.
        private static IReadOnlyList<WorldChangeSetItem> ReadChangeSet(byte[] payload)
        {
            var reader = new NetClient.Buf(payload);
            int count = reader.U16();
            var items = new List<WorldChangeSetItem>(count);
            for (int i = 0; i < count; i++)
                items.Add(new WorldChangeSetItem(reader.I32(), reader.I32(), reader.Str(), reader.Str(),
                    reader.I32(), reader.I32(), reader.I32(), (byte)reader.U8(), (byte)reader.U8()));
            return items.AsReadOnly();
        }

        private void PublishChangeSet(IReadOnlyList<WorldChangeSetItem> items, CancellationToken cancellationToken)
        {
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Volatile.Write(ref _changeSet, items);
                Interlocked.Increment(ref _changeSetVersion);
            }
        }

        public Task<Result> ChangeSetPutAsync(byte bagTab, int bagSlot, int cell, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.ChangeSetPut(bagTab, bagSlot, cell);
                return Task.FromResult(Result.Success());
            }

            return SendChangeSetAsync(() => _netClient.ChangeSetPut(bagTab, bagSlot, cell), cancellationToken);
        }
        public Task<Result> ChangeSetTakeAsync(int cell, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.ChangeSetTake(cell);
                return Task.FromResult(Result.Success());
            }

            return SendChangeSetAsync(() => _netClient.ChangeSetTake(cell), cancellationToken);
        }
        public Task<Result> ChangeSetSwapAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.ChangeSetSwap();
                return Task.FromResult(Result.Success());
            }

            return SendChangeSetAsync(() => _netClient.ChangeSetSwap(), cancellationToken);
        }

        private async Task<Result> SendChangeSetAsync(Action send, CancellationToken cancellationToken)
        {
            try
            {
                await SendAsync(send, cancellationToken).ConfigureAwait(false);
                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) when (IsWireFailure(exception))
            { return Result.Failure("change.set.send.failure", "The equipment change set request could not be sent."); }
        }
    }
}
