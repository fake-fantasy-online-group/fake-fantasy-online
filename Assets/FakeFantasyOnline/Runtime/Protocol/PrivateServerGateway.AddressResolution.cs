using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public IServerAddressDiscovery CreateAddressDiscoveryForIndependentRuntime() =>
        new ResolvedAddressDiscovery(_netClient);

    public void BindAddressResolutionForIndependentRuntime(
        Func<NetClient.IAddressRequest> requestFactory,
        Func<string, ClientConfig.Dispatch> parseDispatch,
        Func<float, object> scaledWait,
        Func<ClientConfig.Srv[]> serverReader,
        Action<ClientConfig.Srv[]> serverPublisher) =>
        _netClient.BindAddressResolutionForIndependentRuntime(
            requestFactory, parseDispatch, scaledWait, serverReader, serverPublisher);

    public IEnumerator ResolveServerAddressForIndependentRuntime() => _netClient.ResolveServerAddress();

    // Read-only host admission for the automatic QA entry, never an alternate ready producer.
    public bool HasStartupAddressResultForIndependentRuntime =>
        _netClient.AddressReady || _netClient.AddressError != null;

    public void BindNativeNetworkErrorForIndependentRuntime(Action<string> error) =>
        _netClient.OnError = error;

    public void BindNativeNetworkNoticeForIndependentRuntime(Action<string> notice) =>
        _netClient.OnNotice = notice;

    // This adapter reads the native result; it performs no HTTP request, retry, fallback,
    // filtering or second address selection. The real login route consumes this same array.
    private sealed class ResolvedAddressDiscovery : IServerAddressDiscovery
    {
        private readonly NetClient owner;

        internal ResolvedAddressDiscovery(NetClient owner) { this.owner = owner; }

        public Task<Result<IReadOnlyList<ServerDirectoryEntry>>> DiscoverAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!owner.AddressReady)
            {
                return Task.FromResult(Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                    "dispatch.address.notready", string.IsNullOrEmpty(owner.AddressError)
                        ? "正在获取服务器地址, 请稍候…" : owner.AddressError));
            }

            ClientConfig.Srv[] source = owner.ServersForIndependentRuntime;
            var directory = new ServerDirectoryEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ClientConfig.Srv row = source[i];
                directory[i] = row == null ? null : ServerDirectoryEntry.FromNativeAddressForIndependentRuntime(
                    row.name, row.host, row.port, row.zone, row.pvp, row.busy);
            }
            return Task.FromResult(Result<IReadOnlyList<ServerDirectoryEntry>>.Success(directory));
        }
    }
}
}
