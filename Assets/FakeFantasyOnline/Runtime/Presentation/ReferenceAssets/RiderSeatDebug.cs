using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 rider-seat debug owner (06000c88-06000c8a). Both message handlers are
// compiled empty in this build, so the recovered bodies are empty methods; only the
// declared instance sentinel is real state.
public class RiderSeatDebug : MonoBehaviour
{
    private int savedModel = -1;

    // Official 06000c88: the native body is empty (4 bytes, bare return).
    public static void Boot()
    {
    }

    // Official 06000c89: the native body is empty (4 bytes, bare return).
    private void Update()
    {
    }
}

}
