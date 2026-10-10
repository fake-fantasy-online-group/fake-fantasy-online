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
            using (IAddressRequest request = createAddressRequest())
            {
                request.Timeout = 10;
                yield return request.Send();
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
                            yield break;
                        }
                        AddressError = "服务器地址无效(dispatch.servers 为空)";
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

            _warningSink(string.Format("[Net] {0} (第{1}次, 5s后重试)", AddressError, attempt));
            _errs.Enqueue(AddressError + " (重试中…)");
            yield return addressRetryWait(5f);
        }
    }
}
}
