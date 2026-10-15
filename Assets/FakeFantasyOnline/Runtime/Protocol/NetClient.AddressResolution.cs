using System;
using System.Collections;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Unity supplies these operations at composition time. Protocol owns all dispatch
    // interpretation, publication, timeout/retry decisions and error-queue writes.
    public interface IAddressRequest : IDisposable
    {
        int Timeout { set; }
        object Send();
        int Result { get; }
        string Error { get; }
        string Text { get; }
    }

    // The original static Servers field is owned by the Presentation NetClient,
    // together with its remaining original static initializers. These bindings move
    // the same array reference across the assembly boundary without a mirrored root.
    private Func<ClientConfig.Srv[]> readServers;
    private Action<ClientConfig.Srv[]> publishServers;
    internal ClientConfig.Srv[] ServersForIndependentRuntime => readServers();

    private Func<IAddressRequest> createAddressRequest;
    private Func<string, ClientConfig.Dispatch> parseAddressDispatch;
    private Func<float, object> addressRetryWait;

    // Resolver and Update both run on the host thread. Only the latest displayed retry
    // is retained; queued entries carry their own local kind, without a growing history.
    private string displayedAddressRetryNotice;
    private Action<string> clearAddressRetryNotice;

    public void BindAddressRetryNoticeClearForIndependentRuntime(Action<string> clearNotice) =>
        clearAddressRetryNotice = clearNotice;

    private void ClearResolvedAddressRetryNotice()
    {
        string obsolete = displayedAddressRetryNotice;
        displayedAddressRetryNotice = null;
        if (obsolete != null) clearAddressRetryNotice?.Invoke(obsolete);
    }

    public void BindAddressResolutionForIndependentRuntime(
        Func<IAddressRequest> requestFactory,
        Func<string, ClientConfig.Dispatch> parseDispatch,
        Func<float, object> scaledWait,
        Func<ClientConfig.Srv[]> serverReader,
        Action<ClientConfig.Srv[]> serverPublisher)
    {
        createAddressRequest = requestFactory;
        parseAddressDispatch = parseDispatch;
        addressRetryWait = scaledWait;
        readServers = serverReader;
        publishServers = serverPublisher;
    }

    // Official 1.5.8 060006ba, lowered by 060008c2/060008c3. Only the Unity operations
    // and the already-configured private DispatchUri are supplied by the composition root.
    public IEnumerator ResolveServerAddress()
    {
        int attempt = 0;
        while (!AddressReady)
        {
            attempt = unchecked(attempt + 1);
            AddressError = null;
            IAddressRequest request = null;
            object operation = null;
            bool started = false;
            try
            {
                request = createAddressRequest();
                request.Timeout = 10;
                operation = request.Send();
                started = true;
            }
            catch (Exception exception)
            {
                // Unity can reject SendWebRequest synchronously (for example when
                // the player forbids HTTP). Publish that failure through the same
                // visible error/retry path instead of abandoning the coroutine
                // with AddressReady=false and AddressError=null forever.
                AddressError = "获取服务器地址失败: " + exception.Message;
            }
            using (request)
            {
                if (started)
                {
                    yield return operation;
                    if (request.Result == 1)
                    {
                        // Only response text, parsing, validation and publication belong to this
                        // catch. Request result/error access and disposal must escape unchanged.
                        try
                        {
                            ClientConfig.Dispatch dispatch = parseAddressDispatch(request.Text);
                            if (dispatch != null && dispatch.servers != null &&
                                dispatch.servers.Length > 0 &&
                                !string.IsNullOrWhiteSpace(dispatch.servers[0].host) &&
                                dispatch.servers[0].port > 0)
                            {
                                publishServers(dispatch.servers);
                                host = dispatch.servers[0].host;
                                port = dispatch.servers[0].port;
                                AddressReady = true;
                            }
                            else AddressError = "服务器地址无效(dispatch.servers 为空)";
                        }
                        catch (Exception exception)
                        {
                            AddressError = "解析服务器地址失败: " + exception.Message;
                        }
                    }
                    else
                    {
                        AddressError = "获取服务器地址失败: " + request.Error;
                    }
                }
            }

            if (AddressReady)
            {
                // UI callbacks are outside the parse/publication catch and run only after
                // disposing the request. A UI exception must not become a dispatch failure.
                ClearResolvedAddressRetryNotice();
                yield break;
            }

            _warningSink(string.Format("[Net] {0} (第{1}次, 5s后重试)", AddressError, attempt));
            string retryNotice = AddressError + " (重试中…)";
            _errs.Enqueue(new QueuedNetworkError(retryNotice, isAddressRetry: true));
            yield return addressRetryWait(5f);
        }
    }
}
}
