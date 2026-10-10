using System.Collections;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer renderer-population and named-play owners.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    public static void SpawnOn(int target, string effName)
    {
        Transform follow = target == 1 && !CharData.riding
            ? CombatNumber.PetTransform : CombatNumber.PlayerTransform;
        if (follow == null) return;
        string key = target.ToString() + ":" + effName;
        EffectPlayer old;
        if (livePulse.TryGetValue(key, out old) && old) Destroy(old.gameObject);
        EffectPlayer player = Play(follow.position, effName, CombatNumber.Ppu, 1, -1, false, false);
        if (player) player.followTarget = follow;
        livePulse[key] = player;
    }

    public static EffectPlayer PlayById(Vector3 worldPos, int id, float ppu)
    {
        for (int index = 0; index < tfxSuffix.Length; index++)
        {
            string name = string.Format("TFX{0:D3}_{1}", id, tfxSuffix[index]);
            if (LoadTemplate(name) != null)
            {
                return Play(worldPos, name, ppu, 1, -1, false, false);
            }
        }

        return null;
    }

    public static EffectPlayer PlaySustained(
        Vector3 worldPos,
        string effName,
        float ppu,
        Transform follow,
        float durSec)
    {
        if (LoadTemplate(effName) == null)
        {
            return null;
        }

        EffectPlayer player = Play(worldPos, effName, ppu, 1, -1, false, false);
        if (!player)
        {
            return null;
        }

        player.followTarget = follow;
        player.loopMode = 1;
        player.selfDestroy = false;
        if (durSec > 0f)
        {
            player.StartCoroutine(player.StopAfterSec(durSec));
        }
        return player;
    }

    private IEnumerator StopAfterSec(float sec)
    {
        yield return new WaitForSeconds(sec);
        Destroy(gameObject);
    }

    public static EffectPlayer PlayNamed(
        Vector3 worldPos,
        string effName,
        float ppu,
        int facingSign = 1,
        int dir = -1,
        bool flip = false,
        bool followFacing = false)
    {
        if (LoadTemplate(effName) != null)
        {
            return Play(worldPos, effName, ppu, facingSign, dir, flip, followFacing);
        }

        return null;
    }

    public static EffectPlayer Play(
        Vector3 worldPos,
        string effName,
        float ppu,
        int facingSign = 1,
        int dir = -1,
        bool flip = false,
        bool followFacing = false)
    {
        Template template = LoadTemplate(effName);
        if (template == null || template.layers.Count == 0)
        {
            return null;
        }

        EffectPlayer player = Rent(effName);
        player.transform.position = worldPos;
        player.tpl = template;
        player.ppu = ppu;
        player.spawnPos = worldPos;
        player.facingSign = facingSign >> 31 | 1;
        player.followFacing = followFacing;

        float scale = NextScale;
        NextScale = 1f;
        player.transform.localScale = scale > 0f
            ? new Vector3(scale, scale, 1f)
            : Vector3.one;

        int count = template.layers.Count;
        player.rends = new SpriteRenderer[count];
        player.frames = new Sprite[count][];
        player.frameDur = new float[count];
        player.layerMultiDir = new bool[count];
        if (followFacing)
        {
            player.dirFrames = new Sprite[count][][];
        }

        for (int index = 0; index < count; index++)
        {
            Layer layer = template.layers[index];
            SpriteRenderer renderer = player.NextLayer(index);
            bool multiDir = false;
            Sprite[] layerFrames;

            if (!followFacing)
            {
                if (dir < 0)
                {
                    layerFrames = LoadMagicFrames(layer.sprite, 0f);
                }
                else
                {
                    layerFrames = LoadMagicFramesDir(layer.sprite, dir, out multiDir);
                    if (multiDir)
                    {
                        renderer.flipX = flip;
                    }
                }
            }
            else
            {
                Sprite[][] directions = LoadMagicFramesAllDirs(layer.sprite, out bool allMultiDir);
                if (!allMultiDir)
                {
                    layerFrames = LoadMagicFrames(layer.sprite, 0f);
                }
                else
                {
                    player.dirFrames[index] = directions;
                    multiDir = true;
                    layerFrames = directions[4];
                }
            }

            player.layerMultiDir[index] = multiDir;
            player.frames[index] = layerFrames;
            player.frameDur[index] = Mathf.Max(layer.speed, 1f);
            renderer.sprite = layerFrames != null && layerFrames.Length > 0
                ? layerFrames[0]
                : null;
            renderer.sortingOrder = index + 31000;

            bool additive = layer.blendMode == 1 || layer.blendMode == 2;
            if (layer.blendMode == 0)
            {
                additive = IsAdditiveIntensityMap(layer.sprite, layerFrames);
            }

            // Official 1.5.8 assigns through Renderer.set_sharedMaterial (0x1025894 arm64); the
            // Renderer.set_material setter is never called anywhere in the official EffectPlayer body.
            renderer.sharedMaterial = additive
                ? AddMat()
                : layer.blendMode == 3
                    ? MaxMat()
                    : AlphaMat();

            if (renderer.sprite)
            {
                if (player.mpb == null)
                {
                    player.mpb = new MaterialPropertyBlock();
                }

                renderer.GetPropertyBlock(player.mpb);
                player.mpb.SetTexture("_MainTex", renderer.sprite.texture);
                renderer.SetPropertyBlock(player.mpb);
            }

            player.rends[index] = renderer;
        }

        return player;
    }
}

}
