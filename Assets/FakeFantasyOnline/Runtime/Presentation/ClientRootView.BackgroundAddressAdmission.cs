using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    private Func<bool> hasStartupAddressResult;

    // Composition-only probe of the same native resolver that ordinary login consumes.
    // It changes no address state and is used only to schedule the existing automatic QA entry.
    internal void BindStartupAddressResultForIndependentRuntime(Func<bool> hasResult) =>
        hasStartupAddressResult = hasResult;

    private async Task WaitForBackgroundAddressResult(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Existing independent hosts that supply their own DiscoverAsync retain that flow.
        // The actual product Bootstrap always supplies the native owner probe before Initialize.
        if (hasStartupAddressResult == null || hasStartupAddressResult()) return;

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => completion.TrySetCanceled()))
        {
            Coroutine wait = StartCoroutine(ObserveBackgroundAddressResult(completion));
            try
            {
                await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                if (this != null && wait != null) StopCoroutine(wait);
            }
        }
    }

    private IEnumerator ObserveBackgroundAddressResult(TaskCompletionSource<bool> completion)
    {
        while (!completion.Task.IsCompleted)
        {
            bool hasResult;
            try
            {
                hasResult = hasStartupAddressResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
                yield break;
            }
            if (hasResult)
            {
                completion.TrySetResult(true);
                yield break;
            }
            yield return null;
        }
    }
}
}
