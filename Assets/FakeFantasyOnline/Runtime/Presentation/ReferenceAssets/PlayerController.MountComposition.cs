using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private int riderSoBase = 1;

    private void ComposeMount(bool moving, int oct)
    {
        if (loadedMount != CharData.mountModel)
        {
            MountAvatar.Release(mountData);
            mountData = MountAvatar.Acquire(CharData.mountModel, ppu, true);
            loadedMount = CharData.mountModel;
            mountFrame = 0;
            mountTimer = 0f;
        }
        EnsureMount();
        if (mountData == null || !mountData.ok)
        {
            if (CharData.mountModel > 0 && _mountDiagWarned.Add(CharData.mountModel))
            {
                string prefix = string.Format("[骑乘] ★坐骑没画出来 model={0} —— ", CharData.mountModel);
                string reason = mountData == null ? "Acquire 返回 null(没拿到数据)" : "数据拿到了但 ok=false(几何没解出来)";
                Debug.LogWarning(string.Concat(prefix, reason, "。玩家看到的就是'骑上去坐骑不见了'。"));
            }
            if (srMount) srMount.enabled = false;
            if (srMountTop) srMountTop.enabled = false;
            if (srMountFx) srMountFx.enabled = false;
            if (mountRing)
            {
                Destroy(mountRing.gameObject);
                mountRing = null;
            }
            if (dollRoot) dollRoot.localPosition = Vector3.zero;
            if (actorFx) actorFx.mountOffsetU = Vector2.zero;
            CharData.mountBoundTopPx = 0;
            CharData.mountNameOffXU = 0f;
            return;
        }
        if (_mountDiagWarned.Count >= 0 && mountData.stand != null &&
            !_mountPixWarned.Contains(CharData.mountModel))
        {
            Sprite[,] stand = mountData.stand;
            int upperD = stand.GetUpperBound(0), upperF = stand.GetUpperBound(1);
            bool any = false;
            for (int d = stand.GetLowerBound(0); d <= upperD && !any; d++)
                for (int f = stand.GetLowerBound(1); f <= upperF; f++)
                    if (stand[d, f]) { any = true; break; }
            if (!any && !mountData.Warming)
            {
                _mountPixWarned.Add(CharData.mountModel);
                Debug.LogWarning(string.Concat(string.Format("[骑乘] ★坐骑几何有、像素一帧都没有 model={0} ",
                    CharData.mountModel), "(补帧已结束却整套站立网格全空) —— 后台解码全失败或帧被淘汰, 表现同样是'骑上去看不见'。"));
            }
        }
        if (CharData.rideAnchorId != 0) moving = rideAnchorMoving;
        if (CharData.mountTianmo && CharData.ShowMountRing && CharData.rideAnchorId == 0)
        {
            if (!mountRing && !mountRingFailed && srMount)
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
                    Debug.LogWarning("[MountRing] 创建失败, 跳过: " + ex.Message);
                }
            }
        }
        else if (mountRing)
        {
            Destroy(mountRing.gameObject);
            mountRing = null;
        }
        CharData.mountBoundTopPx = mountData.boundCy;
        int md;
        bool mf;
        if (mountData.dirs > 2) { md = curDir; mf = flip; }
        else
        {
            switch (oct)
            {
                case 0: case 1: case 2: md = 0; mf = true; break;
                case 3: case 4: md = 0; mf = false; break;
                case 5: case 6: md = 1; mf = false; break;
                default: md = 1; mf = true; break;
            }
        }
        mountData.Touch(moving && mountData.walk != null, md);
        Sprite[,] grid = moving && mountData.walk != null ? mountData.walk : mountData.stand ?? mountData.walk;
        Vector2[,] saddle = mountData.Saddle(moving && mountData.walk != null, CharData.riderSeat);
        Sprite[,] top = moving && mountData.walkTop != null ? mountData.walkTop : mountData.standTop ?? mountData.walkTop;
        Vector2[,] topOff = moving && mountData.walkTop != null ? mountData.walkTopOff : mountData.standTopOff;
        float interval = moving && mountData.walk != null ? mountData.walkInterval : mountData.standInterval;
        mountTimer += Time.deltaTime;
        if (mountTimer >= interval) { mountTimer = 0f; mountFrame = unchecked(mountFrame + 1); }
        int so = unchecked(Mathf.Clamp(unchecked((int)worldY), 0, 30000) + 1 +
            Mathf.Min(mountData == null ? 0 : mountData.belowAnchor, 64));
        int anchor = CharData.rideAnchorId;
        riderSoBase = unchecked(so + (anchor != 0 ? 1 : 0));
        if (anchor != 0)
        {
            if (srMount) srMount.enabled = false;
            if (srMountTop) srMountTop.enabled = false;
            if (srMountFx) srMountFx.enabled = false;
            if (mountRing) { Destroy(mountRing.gameObject); mountRing = null; }
        }
        if (srMount && grid != null)
        {
            int dd = Mathf.Clamp(md, 0, grid.GetLength(0) - 1);
            int n = grid.GetLength(1);
            int ff = n > 0 ? mountFrame % n : 0;
            Sprite sprite = grid[dd, ff];
            if (sprite) srMount.sprite = sprite;
            srMount.enabled = anchor == 0 && srMount.sprite;
            srMount.flipX = mf;
            srMount.sortingOrder = so;
            srMount.transform.localPosition = Vector3.zero;
            if (anchor != 0) srMount.enabled = false;
            float cx = MountRingCx(grid, dd);
            if (mf) cx = -cx;
            CharData.mountNameOffXU = cx;
            if (mountRing && srMount.sprite) mountRing.transform.localPosition = new Vector3(cx, 0f, 0f);
            float sx = 0f, sy = 0f;
            if (saddle != null && dd < saddle.GetLength(0))
            {
                n = saddle.GetLength(1);
                if (n > 0)
                {
                    for (int f = 0; f < n; f++) sx += saddle[dd, f].x;
                    sx /= n;
                    sy = saddle[dd, Mathf.Clamp(ff, 0, n - 1)].y;
                }
            }
            if (mf) sx = -sx;
            CharData.mountSaddleX = sx;
            CharData.mountSaddleY = sy;
        }
        bool topLive = srMountTop;
        if (anchor == 0 && topLive)
        {
            int dd = top != null ? Mathf.Clamp(md, 0, top.GetLength(0) - 1) : 0;
            int n = top != null ? top.GetLength(1) : 0;
            int ff = n > 0 ? mountFrame % n : 0;
            if (top != null && ff < n && top[dd, ff])
            {
                srMountTop.enabled = true;
                srMountTop.sprite = top[dd, ff];
                srMountTop.flipX = mf;
                srMountTop.sortingOrder = unchecked(so + 30);
                srMountTop.sharedMaterial = mountData.topAdditive ? DollGlowMat() : sr.sharedMaterial;
                Vector2 off = topOff != null && dd < topOff.GetLength(0) && ff < topOff.GetLength(1)
                    ? topOff[dd, ff] : Vector2.zero;
                srMountTop.transform.localPosition = new Vector3((mf ? -off.x : off.x) / ppu, -off.y / ppu, 0f);
            }
            else srMountTop.enabled = false;
        }
        if (srMountFx && GameSettings.HideMyPetGlow) { srMountFx.enabled = false; return; }
        bool fxLive = srMountFx;
        if (anchor != 0 || !fxLive) return;
        Sprite[,] fx = moving && mountData.walkFx != null ? mountData.walkFx : mountData.standFx ?? mountData.walkFx;
        Vector2[,] fxOff = moving && mountData.walkFx != null ? mountData.walkFxOff : mountData.standFxOff;
        if (fx == null) { srMountFx.enabled = false; return; }
        int fd = Mathf.Clamp(md, 0, fx.GetLength(0) - 1);
        int fn = fx.GetLength(1);
        int fi = fn > 0 ? mountFrame % fn : 0;
        if (fi >= fn || !fx[fd, fi]) { srMountFx.enabled = false; return; }
        srMountFx.enabled = true;
        srMountFx.sprite = fx[fd, fi];
        srMountFx.flipX = mf;
        srMountFx.sortingOrder = unchecked(so + 31);
        srMountFx.sharedMaterial = EffectPlayer.LayerAddMat();
        Vector2 fxOffset = fxOff != null && fd < fxOff.GetLength(0) && fi < fxOff.GetLength(1)
            ? fxOff[fd, fi] : Vector2.zero;
        srMountFx.transform.localPosition = new Vector3((mf ? -fxOffset.x : fxOffset.x) / ppu, -fxOffset.y / ppu, 0f);
    }
}

internal static partial class CharData
{
}

}
