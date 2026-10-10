using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 click-marker effect: a ring that grows from 0.2 to 1.2 scale while it
// fades out, then destroys its own GameObject.
public class ClickRingLife : MonoBehaviour
{
    private const float Life = 0.7f;

    private SpriteRenderer rsr;
    private float t;

    // 06000a29: keep the renderer, shrink the ring to a fifth of its size and clear the tint.
    public void Init(SpriteRenderer r)
    {
        rsr = r;
        transform.localScale = Vector3.one * 0.2f;
        rsr.color = new Color(1f, 1f, 1f, 1f);
    }

    // 06000a2a: grow the ring, fade it, then destroy the GameObject once it has lived long enough.
    private void Update()
    {
        t += Time.deltaTime;
        transform.localScale = Vector3.one * (t / Life + 0.2f);
        if (rsr != null)
            rsr.color = new Color(1f, 1f, 1f, 1f - t / Life);
        if (t >= Life)
            Destroy(gameObject);
    }
}

}
