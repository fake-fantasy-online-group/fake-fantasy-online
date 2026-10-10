using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.PluginHost
{

public sealed class PluginExecutionLimits
{
    public PluginExecutionLimits(TimeSpan callbackTimeout, int consecutiveFailureLimit, int storageQuotaBytes)
    {
        if (callbackTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(callbackTimeout));
        }

        if (consecutiveFailureLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveFailureLimit));
        }

        if (storageQuotaBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storageQuotaBytes));
        }

        CallbackTimeout = callbackTimeout;
        ConsecutiveFailureLimit = consecutiveFailureLimit;
        StorageQuotaBytes = storageQuotaBytes;
    }

    public TimeSpan CallbackTimeout { get; }

    public int ConsecutiveFailureLimit { get; }

    public int StorageQuotaBytes { get; }
}

public sealed class PluginLoadRequest
{
    public PluginLoadRequest(PluginDescriptor descriptor, string entrySource, IPluginContext context)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        EntrySource = entrySource ?? throw new ArgumentNullException(nameof(entrySource));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public PluginDescriptor Descriptor { get; }

    public string EntrySource { get; }

    public IPluginContext Context { get; }
}

public interface IPluginRuntime : IDisposable
{
    Result Load(PluginLoadRequest request);

    Task<Result> InvokeAsync(
        string callbackName,
        PluginExecutionLimits limits,
        CancellationToken cancellationToken);
}

public interface IPluginRuntimeFactory
{
    IPluginRuntime CreateIsolatedRuntime(string pluginId);
}

public interface IPluginPackageSource
{
    Result<PluginLoadRequest> LoadApprovedPackage(string pluginId, IPluginContext context);
}
}
