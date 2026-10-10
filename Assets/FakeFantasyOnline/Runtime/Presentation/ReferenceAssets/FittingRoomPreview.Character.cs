using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class FittingRoomPreview
{
    public void ShowCharacter(int gender, int head, int hair, PlayerAvatar.Appear ap,
        IEnumerable<string> suitAuras, IEnumerable<string> soulFx)
    {
        EnsureCam();
        mode = 0;
        cam.enabled = true;
        if (petGo != null) petGo.SetActive(false);
        EnsureActor();
        actorRoot.SetActive(true);
        if (gender != lastG || head != lastHead || hair != lastHair || !ApEq(ap, lastAp) || avatar == null)
        {
            PlayerAvatar old = avatar;
            avatar = PlayerAvatar.Build(gender, head, hair, ap, ppu);
            if (old != null && old != avatar) old.Dispose();
            lastG = gender; lastHead = head; lastHair = hair; lastAp = ap;
            FrameActor();
        }
        ApplyStand();
        if (avatar != null && avatar.headPx > 0) actorFx.bodyHeightU = avatar.headPx / ppu;
        var auras = new Dictionary<string, int>();
        if (suitAuras != null)
            foreach (string name in suitAuras)
                if (!string.IsNullOrEmpty(name)) auras[name] = 2;
        actorFx.SyncAuras(auras);
        var souls = new List<string>();
        if (soulFx != null)
            foreach (string name in soulFx)
                if (!string.IsNullOrEmpty(name)) souls.Add(name);
        actorFx.SyncSoulFx(souls.Count > 0 ? souls : null);
    }

    private void EnsureActor()
    {
        if (actorRoot != null) return;
        actorRoot = new GameObject("PreviewActor");
        actorRoot.transform.SetParent(transform, false);
        actorRoot.transform.position = ORIGIN;
        baseSr = actorRoot.AddComponent<SpriteRenderer>();
        baseSr.sharedMaterial = _defMat;
        baseSr.enabled = false;
        baseSr.sortingOrder = 0;
        dollSr = new SpriteRenderer[14];
        for (int i = 0; i < dollSr.Length; i++)
        {
            var go = new GameObject("L" + i);
            go.transform.SetParent(actorRoot.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = _defMat;
            renderer.enabled = false;
            dollSr[i] = renderer;
        }
        actorFx = actorRoot.AddComponent<ActorEffects>();
        actorFx.playerSr = baseSr;
        actorFx.ppu = ppu;
        actorFx.FacingProvider = () => (CurDir, CurFlip);
    }

    private void ApplyStand()
    {
        if (dollSr == null) return;
        PlayerAvatar.Pose pose = null;
        if (avatar != null && avatar.ok && avatar.stand != null && avatar.stand.Length > 0)
        {
            int dir = Mathf.Clamp(CurDir, 0, avatar.stand.Length - 1);
            if (previewSit && avatar.sit != null && dir < avatar.sit.Length) pose = avatar.sit[dir];
            if (pose == null) pose = avatar.StandAt(dir, Time.time) ?? avatar.stand[0];
        }
        bool flip = CurFlip;
        int count = pose?.layers == null ? 0 : Mathf.Min(pose.layers.Length, dollSr.Length);
        for (int i = 0; i < count; i++)
        {
            PlayerAvatar.LayerInst layer = pose.layers[i];
            SpriteRenderer renderer = dollSr[i];
            if (layer.spr == null) { renderer.enabled = false; continue; }
            renderer.enabled = true;
            renderer.sprite = layer.spr;
            renderer.flipX = flip;
            renderer.transform.localPosition = new Vector3(flip ? -layer.offX : layer.offX, -layer.offY, 0f);
            renderer.sortingOrder = i;
            renderer.sharedMaterial = layer.additive ? GlowMat(avatar == null ? 0 : avatar.GlowModelOf(layer.tag)) : _defMat;
        }
        for (int i = count; i < dollSr.Length; i++) dollSr[i].enabled = false;
    }

    private void FrameActor()
    {
        float height = Mathf.Max(.6f, avatar != null && avatar.headPx > 0 ? avatar.headPx / ppu : 1.8f);
        cam.orthographicSize = height * 1.15f;
        cam.transform.position = ORIGIN + new Vector3(0f, height * .5f, -10f);
    }

    private static bool ApEq(PlayerAvatar.Appear a, PlayerAvatar.Appear b) =>
        a.Body == b.Body && a.Cap == b.Cap && a.Backpack == b.Backpack &&
        a.WeaponR == b.WeaponR && a.WeaponL == b.WeaponL && a.Face == b.Face;
}
}
