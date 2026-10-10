using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    internal INetClientInstanceBinding InstanceBindingForIndependentRuntime { get; private set; }
    internal INetClientInstanceHost InstanceHostForIndependentRuntime { get; private set; }
    private Action pumpForIndependentRuntime;

    internal void BindInstanceForIndependentRuntime(
        INetClientInstanceBinding binding, INetClientInstanceHost host, Action pump)
    {
        if (InstanceBindingForIndependentRuntime != null)
            throw new InvalidOperationException("The NetClient instance is already bound.");
        if (binding == null) throw new ArgumentNullException(nameof(binding));
        if (host == null) throw new ArgumentNullException(nameof(host));
        if (pump == null) throw new ArgumentNullException(nameof(pump));

        InstanceBindingForIndependentRuntime = binding;
        InstanceHostForIndependentRuntime = host;
        pumpForIndependentRuntime = pump;
        // These capabilities always address this owner's slots, even after I changes.
        // Allocate them during host composition, outside SetupNet's eight ordered stores.
        clearErrorReceipt = () => binding.BindNativeNetworkErrorForIndependentRuntime(null);
        clearLoginResultReceipt = () => binding.BindLoginResultReceipt(null);
        clearRosterReceipt = () => binding.BindCharacterRosterReceipt(null);
        clearCreateResultReceipt = () => binding.BindCreateResultReceipt(null);
        clearEnterWorldReceipt = () => binding.BindWorldEntryReceipt(null, null);
        clearAccountResultsReceipt = () => binding.BindAccountResults(null, null);
    }

    // The original component is the sole frame driver once its graph is bound.
    // This invokes the existing recovered Protocol Update, with no second queue or drain.
    private void Update()
    {
        pumpForIndependentRuntime?.Invoke();
    }
}
}
