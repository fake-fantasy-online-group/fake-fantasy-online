using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official TargetRing owner (06000C9F..06000CA3).
/// </summary>
public class TargetRing : MonoBehaviour
{
    public static TargetRing I;

    private SpriteRenderer sr;
    private static Sprite[] greenFrames;
    private static Sprite[] redFrames;
    private Sprite[] frames;
    private ISelectable bound;

    private const float FrameMs = 100f;

    public static TargetRing Ensure()
    {
        if (!I)
        {
            GameObject root = new GameObject("TargetRing");
            I = root.AddComponent<TargetRing>();
        }

        return I;
    }

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -4500;
        sr.enabled = false;
    }

    private static Sprite[] Load(int id)
    {
        return EffectPlayer.GetMagicFrames(id);
    }

    public void Bind(ISelectable s)
    {
        bound = s;
        if (s != null)
        {
            if (s.SelFriendly)
            {
                if (greenFrames == null)
                {
                    greenFrames = Load(9001);
                }

                frames = greenFrames;
            }
            else
            {
                if (redFrames == null)
                {
                    redFrames = Load(9002);
                }

                frames = redFrames;
            }

            if (frames != null && frames.Length > 0)
            {
                sr.enabled = true;
                transform.localScale = Vector3.one;
                return;
            }
        }

        sr.enabled = false;
    }

    private void LateUpdate()
    {
        if (bound != null && bound.SelTransform &&
            frames != null && frames.Length > 0)
        {
            transform.position = bound.SelFeetPos;
            int index = (int)(Time.time * 1000f / FrameMs) % frames.Length;
            if (frames[index])
            {
                sr.sprite = frames[index];
            }

            return;
        }

        if (sr.enabled)
        {
            sr.enabled = false;
        }

        bound = null;
        TargetSelect.Current = null;
    }
}

}
