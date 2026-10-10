using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class NoticeBoard
{
    private static bool _fetching;
    private static bool _tried;
    private static string endpointForIndependentRuntime;

    // Bootstrap supplies the deployment address. This does not reset native state.
    public static void ConfigureEndpointForIndependentRuntime(string url)
    {
        endpointForIndependentRuntime = url;
    }

    // Official 1.5.8 0x060008e6.
    public static void Ensure(MonoBehaviour host)
    {
        if (_tried || host == null)
        {
            return;
        }
        _tried = true;
        if (!Loaded)
        {
            TextAsset asset = Resources.Load<TextAsset>("changelog");
            if (asset != null)
            {
                PublishJson(asset.text);
            }
        }
        if (!_fetching)
        {
            host.StartCoroutine(Fetch());
        }
    }

    // Official 1.5.8 0x060008e7 / iterator 0x060008f0. Only the endpoint is injected.
    private static IEnumerator Fetch()
    {
        _fetching = true;
        using (UnityWebRequest request = UnityWebRequest.Get(endpointForIndependentRuntime))
        {
            request.timeout = 6;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                PublishJson(request.downloadHandler.text);
            }
        }
        // The original reset follows the using block, not its finally path.
        _fetching = false;
    }
}
}
