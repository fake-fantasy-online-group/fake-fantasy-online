using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private bool HideThisMount => GameSettings.HideOtherPets;

    private void ComposeRemoteMount(bool moving)
    {
        bool localPassenger = riderSeat >= 1 && rideAnchorId == NetClient.SelfEntityId;
        long stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        bool hideArt = !localPassenger && (HideThisMount || !MountArtWanted());
        if (LoadTrace.On)
        {
            LoadTrace.AddSeg(LoadTrace.Seg.骑_可见判定, stamp);
            LoadTrace.NoteSegCall(LoadTrace.Seg.骑_可见判定);
        }
        if (!hideArt)
        {
            if (loadedMount != mountModel)
            {
                MountAvatar.Release(mountData);
                mountData = MountAvatar.Acquire(mountModel, ppu, false);
                loadedMount = mountModel;
                mountFrame = 0;
                mountTimer = 0f;
            }
        }
        else
        {
            if (loadedMount != -1)
            {
                MountAvatar.Release(mountData);
                loadedMount = -1;
            }
            mountData = null;
        }
        EnsureMountLayers();
        int baseSO = SortFor(worldY) + 1;
        if (mountData != null) baseSO += Mathf.Min(mountData.belowAnchor, MountAvatar.SortBoostCapPx);
        if (mountData == null || !mountData.ok)
        {
            if (srMount) srMount.enabled = false;
            if (srMountTop) srMountTop.enabled = false;
            if (srMountFx) srMountFx.enabled = false;
            if (dollRoot) dollRoot.localPosition = Vector3.zero;
            ridingRaiseY = 0f;
            ridingNameOffX = 0f;
            UpdatePickBox(0f, 0f);
            if (statusFx) statusFx.mountOffsetU = Vector2.zero;
            if (mountRing) { UnityEngine.Object.Destroy(mountRing.gameObject); mountRing = null; }
            stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
            ApplyRiderPose(baseSO);
            if (LoadTrace.On)
            {
                LoadTrace.AddSeg(LoadTrace.Seg.骑_骑手姿势, stamp);
                LoadTrace.NoteSegCall(LoadTrace.Seg.骑_骑手姿势);
            }
            return;
        }

        int dir;
        bool mountFlip;
        if (mountData.dirs > 2) { dir = curDir; mountFlip = flip; }
        else
        {
            switch (lastOct)
            {
                case 0: case 1: case 2: dir = 0; mountFlip = true; break;
                case 3: case 4: dir = 0; mountFlip = false; break;
                case 5: case 6: dir = 1; mountFlip = false; break;
                default: dir = 1; mountFlip = true; break;
            }
        }
        mountData.Touch(moving && mountData.walk != null, dir);
        Sprite[,] grid = moving && mountData.walk != null ? mountData.walk : mountData.stand ?? mountData.walk;
        Vector2[,] saddle = mountData.Saddle(moving && mountData.walk != null, riderSeat);
        Sprite[,] top = moving && mountData.walkTop != null ? mountData.walkTop : mountData.standTop ?? mountData.walkTop;
        Vector2[,] topOffsets = moving && mountData.walkTop != null ? mountData.walkTopOff : mountData.standTopOff;
        float interval = moving && mountData.walk != null ? mountData.walkInterval : mountData.standInterval;
        mountTimer += Time.deltaTime;
        if (interval > 0f && mountTimer >= interval) { mountTimer = 0f; mountFrame++; }
        int seat = riderSeat;
        float cx = 0f;
        int baseFrame = 0;
        if (seat != 0)
        {
            if (srMount) srMount.enabled = false;
            if (srMountTop) srMountTop.enabled = false;
            if (srMountFx) srMountFx.enabled = false;
            if (mountRing) { UnityEngine.Object.Destroy(mountRing.gameObject); mountRing = null; }
            if (grid != null)
            {
                grid.GetLength(0);
                int frames = grid.GetLength(1);
                baseFrame = frames > 0 ? mountFrame % frames : 0;
            }
        }
        else if (srMount && grid != null)
        {
            int dd = Mathf.Clamp(dir, 0, grid.GetLength(0) - 1);
            int frames = grid.GetLength(1);
            baseFrame = frames > 0 ? mountFrame % frames : 0;
            Sprite sprite = grid[dd, baseFrame];
            if (sprite) srMount.sprite = sprite;
            srMount.enabled = srMount.sprite;
            srMount.flipX = mountFlip;
            srMount.sortingOrder = baseSO;
            srMount.transform.localPosition = Vector3.zero;
            stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
            cx = MountRingCx(grid, dd);
            if (LoadTrace.On)
            {
                LoadTrace.AddSeg(LoadTrace.Seg.骑_光圈X, stamp);
                LoadTrace.NoteSegCall(LoadTrace.Seg.骑_光圈X);
            }
            if (mountFlip) cx = -cx;
        }

        if (mountTianmo && CharData.ShowMountRing)
        {
            if (!mountRing && !mountRingFailed && seat == 0 && srMount)
            {
                try
                {
                    mountRing = EffectPlayer.PlaySustained(transform.position, "TFX304", ppu, null, 0f);
                    if (mountRing)
                    {
                        mountRing.transform.SetParent(transform, true);
                        mountRing.attachSort = srMount;
                        mountRing.attachUnder = true;
                        mountRing.transform.localScale = Vector3.one;
                    }
                    else mountRingFailed = true;
                }
                catch (Exception ex)
                {
                    mountRingFailed = true;
                    Debug.LogWarning("[RemoteMountRing] 创建失败, 跳过: " + ex.Message);
                }
            }
        }
        else if (mountRing) { UnityEngine.Object.Destroy(mountRing.gameObject); mountRing = null; }
        if (mountRing) mountRing.transform.localPosition = new Vector3(cx, 0f, 0f);

        float saddleX = 0f, saddleY = 0f;
        if (saddle != null && dir < saddle.GetLength(0))
        {
            int frames = saddle.GetLength(1);
            if (frames > 0)
            {
                for (int f = 0; f < frames; f++) saddleX += saddle[dir, f].x;
                saddleX /= frames;
                saddleY = saddle[dir, Mathf.Clamp(baseFrame, 0, frames - 1)].y;
            }
        }
        if (mountFlip) saddleX = -saddleX;
        ridingRaiseY = -saddleY / ppu;
        ridingNameOffX = cx;
        UpdatePickBox(ridingRaiseY, saddleX / ppu);
        if (dollRoot) dollRoot.localPosition = new Vector3(saddleX / ppu, ridingRaiseY, 0f);
        if (statusFx) statusFx.mountOffsetU = new Vector2(saddleX / ppu, ridingRaiseY);

        if (srMountTop && seat == 0)
        {
            int dd = top != null ? Mathf.Clamp(dir, 0, top.GetLength(0) - 1) : 0;
            int frames = top != null ? top.GetLength(1) : 0;
            int f = frames > 0 ? mountFrame % frames : 0;
            if (top != null && f < frames && top[dd, f])
            {
                srMountTop.enabled = true;
                srMountTop.sprite = top[dd, f];
                srMountTop.flipX = mountFlip;
                srMountTop.sortingOrder = baseSO + 30;
                srMountTop.sharedMaterial = mountData.topAdditive ? RemoteGlowMat() : body.sharedMaterial;
                Vector2 offset = topOffsets != null && dd < topOffsets.GetLength(0) && f < topOffsets.GetLength(1)
                    ? topOffsets[dd, f] : Vector2.zero;
                srMountTop.transform.localPosition = new Vector3((mountFlip ? -offset.x : offset.x) / ppu, -offset.y / ppu, 0f);
            }
            else srMountTop.enabled = false;
        }

        if (srMountFx && GameSettings.HideOtherPetGlow) srMountFx.enabled = false;
        else if (srMountFx && seat == 0)
        {
            Sprite[,] fx = moving && mountData.walkFx != null ? mountData.walkFx : mountData.standFx ?? mountData.walkFx;
            Vector2[,] offsets = moving && mountData.walkFx != null ? mountData.walkFxOff : mountData.standFxOff;
            int dd = fx != null ? Mathf.Clamp(dir, 0, fx.GetLength(0) - 1) : 0;
            int frames = fx != null ? fx.GetLength(1) : 0;
            int f = frames > 0 ? mountFrame % frames : 0;
            if (fx != null && f < frames && fx[dd, f])
            {
                srMountFx.enabled = true;
                srMountFx.sprite = fx[dd, f];
                srMountFx.flipX = mountFlip;
                srMountFx.sortingOrder = baseSO + 31;
                srMountFx.sharedMaterial = EffectPlayer.LayerAddMat();
                Vector2 offset = offsets != null && dd < offsets.GetLength(0) && f < offsets.GetLength(1)
                    ? offsets[dd, f] : Vector2.zero;
                srMountFx.transform.localPosition = new Vector3((mountFlip ? -offset.x : offset.x) / ppu, -offset.y / ppu, 0f);
            }
            else srMountFx.enabled = false;
        }
        stamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        ApplyRiderPose(baseSO + (seat != 0 ? 2 : 1));
        if (LoadTrace.On)
        {
            LoadTrace.AddSeg(LoadTrace.Seg.骑_骑手姿势, stamp);
            LoadTrace.NoteSegCall(LoadTrace.Seg.骑_骑手姿势);
        }
    }
}

}
