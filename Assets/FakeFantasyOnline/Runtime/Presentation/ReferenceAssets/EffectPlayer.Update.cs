using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer per-frame owner.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    private void Update()
    {
        if (tpl == null || tpl.layers == null || rends == null)
        {
            Destroy(gameObject);
            return;
        }

        t += Time.deltaTime * 1000f;

        if (followTarget)
        {
            transform.position = followTarget.position;
        }

        if (moveTarget || moveTargetPos.HasValue)
        {
            if (moveDur > 0f)
            {
                moveT += Time.deltaTime * 1000f;
                float progress = Mathf.Clamp01(moveT / moveDur);
                Vector3 destination = moveTarget
                    ? moveTarget.position
                    : moveTargetPos.Value;
                transform.position = Vector3.Lerp(spawnPos, destination, progress);
            }
        }

        for (int index = 0; index < tpl.layers.Count; index++)
        {
            Layer layer = tpl.layers[index];
            SpriteRenderer renderer = rends[index];
            if (!renderer)
            {
                continue;
            }

            if (layer.kfs.Count == 0)
            {
                continue;
            }

            KF last = layer.kfs[layer.kfs.Count - 1];
            if (selfDestroy && (loopMode & -3) != 1 && t > last.t)
            {
                if (renderer.enabled)
                {
                    renderer.enabled = false;
                }

                continue;
            }

            if (!renderer.enabled)
            {
                renderer.enabled = true;
            }

            if (attachSort)
            {
                renderer.sortingOrder = (uniqueBeamC ? 0 : index) + attachSort.sortingOrder +
                    LayerPlaneOffset(layer);
            }

            if (followFacing && dirFrames != null && dirFrames[index] != null)
            {
                int direction = (uint)facingDir < 4u ? facingDir : 4;
                Sprite[] directionFrames = dirFrames[index][direction];
                if (directionFrames != null && directionFrames.Length > 0)
                {
                    int frame = directionFrames.Length < 2
                        ? 0
                        : (int)(t / frameDur[index]) % directionFrames.Length;
                    renderer.sprite = directionFrames[frame];
                    renderer.flipX = facingFlip;
                }
            }
            else
            {
                Sprite[] layerFrames = frames[index];
                if (layerFrames != null && layerFrames.Length >= 2)
                {
                    int frame = (int)(t / frameDur[index]);
                    frame = layerMultiDir != null && layerMultiDir[index]
                        ? Mathf.Min(frame, layerFrames.Length - 1)
                        : frame % layerFrames.Length;
                    renderer.sprite = layerFrames[frame];
                }
            }

            if (renderer.sprite)
            {
                if (mpb == null)
                {
                    mpb = new MaterialPropertyBlock();
                }

                renderer.GetPropertyBlock(mpb);
                mpb.SetTexture("_MainTex", renderer.sprite.texture);
                renderer.SetPropertyBlock(mpb);
            }

            KF keyframe = Sample(layer, t);
            if (layer.sx != null)
            {
                keyframe.pos.x = layer.sx.Eval(t);
            }

            if (layer.sy != null)
            {
                keyframe.pos.y = layer.sy.Eval(t);
            }

            if (layer.sz != null)
            {
                keyframe.pos.z = layer.sz.Eval(t);
            }

            renderer.transform.localPosition = new Vector3(
                (keyframe.pos.x + layer.offX) / ppu,
                (keyframe.pos.z - keyframe.pos.y * 0.5f - layer.offY) / ppu,
                0f);
            renderer.transform.localScale = new Vector3(
                keyframe.scale.x,
                keyframe.scale.y,
                1f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, keyframe.rot);
            renderer.color = keyframe.rgb;
        }

        if (loopMode == 3)
        {
            float cycle = tpl.lifeMs + loopDelay;
            if (t >= cycle)
            {
                t -= cycle;
            }
        }
        else if (loopMode == 1 && t >= tpl.lifeMs)
        {
            t -= tpl.lifeMs;
        }

        if (!(selfDestroy && t >= tpl.lifeMs))
        {
            return;
        }

        if (!followTarget && !transform.parent)
        {
            ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }
}

}
