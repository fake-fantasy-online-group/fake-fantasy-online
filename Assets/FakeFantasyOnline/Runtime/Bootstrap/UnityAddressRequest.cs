using FakeFantasyOnline.Protocol;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Bootstrap
{
// Unity capability adapter only. All dispatch/error/retry decisions stay in Protocol.NetClient.
internal sealed class UnityAddressRequest : NetClient.IAddressRequest
{
    private readonly UnityWebRequest request;

    internal UnityAddressRequest(string configuredPrivateDispatchUrl)
    {
        request = UnityWebRequest.Get(configuredPrivateDispatchUrl);
    }

    public int Timeout { set => request.timeout = value; }
    public object Send() => request.SendWebRequest();
    public int Result => (int)request.result;
    public string Error => request.error;
    public string Text => request.downloadHandler.text;
    public void Dispose() => request.Dispose();
}
}
