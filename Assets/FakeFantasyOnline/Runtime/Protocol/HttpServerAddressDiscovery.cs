using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{

public sealed class HttpServerAddressDiscovery : IServerAddressDiscovery, IDisposable
{
    private readonly PrivateServerOptions _options;
    private readonly HttpClient _client;

    public HttpServerAddressDiscovery(PrivateServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _client = new HttpClient
        {
            Timeout = options.ResponseTimeout
        };
    }

    public async Task<Result<IReadOnlyList<ServerDirectoryEntry>>> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using (HttpResponseMessage response = await _client.GetAsync(
                       _options.DispatchUri,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                        "dispatch.http.failure",
                        "The dispatch service returned an unsuccessful status.");
                }

                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(DispatchDocument));
                DispatchDocument document = serializer.ReadObject(
                    await response.Content.ReadAsStreamAsync()) as DispatchDocument;

                if (document?.Servers == null || document.Servers.Count == 0)
                {
                    return Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                        "dispatch.servers.empty",
                        "The dispatch response did not contain a game server.");
                }

                List<ServerDirectoryEntry> entries = new List<ServerDirectoryEntry>();
                foreach (DispatchServer server in document.Servers)
                {
                    if (server != null &&
                        !string.IsNullOrWhiteSpace(server.Host) &&
                        server.Port >= 1 &&
                        server.Port <= 65535)
                    {
                        entries.Add(
                            new ServerDirectoryEntry(
                                server.Name,
                                new ServerEndpoint(server.Host, server.Port),
                                server.Zone,
                                server.IsPlayerVersusPlayer,
                                server.BusyLevel));
                    }
                }

                if (entries.Count == 0)
                {
                    return Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                        "dispatch.server.invalid",
                        "The dispatch response did not contain a valid game endpoint.");
                }

                return Result<IReadOnlyList<ServerDirectoryEntry>>.Success(
                    new ReadOnlyCollection<ServerDirectoryEntry>(entries));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            return Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                "dispatch.timeout",
                "The dispatch request timed out.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is System.IO.IOException ||
            exception is SerializationException ||
            exception is FormatException)
        {
            return Result<IReadOnlyList<ServerDirectoryEntry>>.Failure(
                "dispatch.response.invalid",
                "The dispatch response could not be read.");
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

[DataContract]
internal sealed class DispatchDocument
{
    [DataMember(Name = "servers")]
    public List<DispatchServer> Servers { get; set; }
}

[DataContract]
internal sealed class DispatchServer
{
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "host")]
    public string Host { get; set; }

    [DataMember(Name = "port")]
    public int Port { get; set; }

    [DataMember(Name = "zone")]
    public string Zone { get; set; }

    [DataMember(Name = "pvp")]
    public bool IsPlayerVersusPlayer { get; set; }

    [DataMember(Name = "busy")]
    public int BusyLevel { get; set; }
}
}
