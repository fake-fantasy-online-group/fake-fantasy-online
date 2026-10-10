using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 macOS window-settle driver created by WindowCenter.Install.
public class WindowCenterDriver : MonoBehaviour
{
    private int lastW;
    private int lastH;
    private int settle;

    // 06001089: record the current screen size and arm the settle counter.
    private void Start()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        settle = 5;
    }

    // 0600108a: re-arm on a screen-size change, otherwise count the settle frames down.
    private void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH)
        {
            if (settle < 1)
                return;
        }
        else
        {
            lastW = Screen.width;
            lastH = Screen.height;
            settle = 5;
        }
        settle--;
    }
}

}
