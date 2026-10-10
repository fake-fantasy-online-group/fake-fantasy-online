using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.PluginHost
{

public sealed class PluginDescriptor
{
    public PluginDescriptor(
        string id,
        string name,
        string version,
        string apiVersion,
        string entryPoint,
        IReadOnlyList<string> requestedCapabilities)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        ApiVersion = apiVersion ?? throw new ArgumentNullException(nameof(apiVersion));
        EntryPoint = entryPoint ?? throw new ArgumentNullException(nameof(entryPoint));
        RequestedCapabilities = new ReadOnlyCollection<string>(
            new List<string>(requestedCapabilities ?? throw new ArgumentNullException(nameof(requestedCapabilities))));
    }

    public string Id { get; }

    public string Name { get; }

    public string Version { get; }

    public string ApiVersion { get; }

    public string EntryPoint { get; }

    public IReadOnlyList<string> RequestedCapabilities { get; }
}

public sealed class PluginAdmissionResult
{
    private PluginAdmissionResult(bool allowed, string failureCode, IReadOnlyList<string> grantedCapabilities)
    {
        Allowed = allowed;
        FailureCode = failureCode;
        GrantedCapabilities = grantedCapabilities;
    }

    public bool Allowed { get; }

    public string FailureCode { get; }

    public IReadOnlyList<string> GrantedCapabilities { get; }

    public static PluginAdmissionResult Allow(IReadOnlyList<string> capabilities)
    {
        return new PluginAdmissionResult(
            true,
            string.Empty,
            new ReadOnlyCollection<string>(new List<string>(capabilities)));
    }

    public static PluginAdmissionResult Deny(string failureCode)
    {
        return new PluginAdmissionResult(false, failureCode, Array.Empty<string>());
    }
}

public sealed class PluginAdmissionPolicy
{
    private readonly HashSet<string> _supportedCapabilities;

    public PluginAdmissionPolicy(IEnumerable<string> supportedCapabilities)
    {
        _supportedCapabilities = new HashSet<string>(
            supportedCapabilities ?? throw new ArgumentNullException(nameof(supportedCapabilities)),
            StringComparer.Ordinal);
    }

    public PluginAdmissionResult Evaluate(
        PluginDescriptor descriptor,
        IReadOnlyCollection<string> approvedCapabilities)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (approvedCapabilities == null)
        {
            throw new ArgumentNullException(nameof(approvedCapabilities));
        }

        if (!string.Equals(descriptor.ApiVersion, PluginApiVersion.Current, StringComparison.Ordinal))
        {
            return PluginAdmissionResult.Deny("plugin.api_version.unsupported");
        }

        HashSet<string> approved = new HashSet<string>(approvedCapabilities, StringComparer.Ordinal);
        foreach (string capability in descriptor.RequestedCapabilities)
        {
            if (!_supportedCapabilities.Contains(capability))
            {
                return PluginAdmissionResult.Deny("plugin.capability.unsupported");
            }

            if (!approved.Contains(capability))
            {
                return PluginAdmissionResult.Deny("plugin.capability.denied");
            }
        }

        return PluginAdmissionResult.Allow(descriptor.RequestedCapabilities);
    }
}
}
