using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private static Camera _sCam;
    private float _farSec;
    private Sprite[,] ringCxGrid;
    private float[] ringCxAvg;
    private int[] ringCxDirFill;
    private float[] ringCxDirAt;

    private static Camera MainCam
    {
        get
        {
            if (!_sCam || !_sCam.isActiveAndEnabled) _sCam = Camera.main;
            return _sCam;
        }
    }

    private Material RemoteGlowMat() => EffectPlayer.GlowLayerMat(glowMode);

    private bool MountArtWanted()
    {
        Camera camera = MainCam;
        if (!camera || !camera.orthographic)
        {
            _farSec = 0f;
            return true;
        }
        float half = camera.orthographicSize;
        float radius = Mathf.Max(half * camera.aspect, half) * 1.8f;
        Vector3 position = transform.position;
        Vector3 view = camera.transform.position;
        float dx = position.x - view.x;
        float dy = position.y - view.y;
        if (dx * dx + dy * dy > radius * radius) _farSec += Time.deltaTime;
        else _farSec = 0f;
        return _farSec < 6f;
    }

    private void EnsureMountLayers()
    {
        if (srMount || !body) return;
        GameObject root = new GameObject("RMountPet");
        root.transform.SetParent(transform, false);
        srMount = root.AddComponent<SpriteRenderer>();
        srMount.sharedMaterial = body.sharedMaterial;
        root = new GameObject("RMountTop");
        root.transform.SetParent(transform, false);
        srMountTop = root.AddComponent<SpriteRenderer>();
        srMountTop.sharedMaterial = body.sharedMaterial;
        root = new GameObject("RMountFx");
        root.transform.SetParent(transform, false);
        srMountFx = root.AddComponent<SpriteRenderer>();
        srMountFx.sharedMaterial = body.sharedMaterial;
        if (shadow) shadow.mountSrc = srMount;
    }

    private void ApplyRiderPose(int baseSO)
    {
        int action = NpcActions.RideActionIndex(mountModel);
        PlayerAvatar.Pose[] poses = avatar?.PoseAt(action) ?? new[] { StandPose(curDir) };
        PlayerAvatar.Pose pose = poses[Mathf.Clamp(curDir, 0, poses.Length - 1)];
        if (pose != null) ApplyPose(pose, flip, baseSO);
    }

    private float MountRingCx(Sprite[,] grid, int dd)
    {
        if (grid == null) return 0f;
        int dirs = grid.GetLength(0);
        int frames = grid.GetLength(1);
        if (ringCxGrid != grid || ringCxAvg == null || ringCxAvg.Length != dirs)
        {
            ringCxGrid = grid;
            ringCxAvg = new float[dirs];
            ringCxDirFill = new int[dirs];
            ringCxDirAt = new float[dirs];
            for (int i = 0; i < dirs; i++)
            {
                ringCxDirFill[i] = -1;
                ringCxDirAt[i] = -99f;
            }
        }
        if (dd < 0 || dd >= dirs) return 0f;
        int fillVersion = mountData != null ? mountData.fillVersion : 0;
        float now = Time.unscaledTime;
        if (ringCxDirFill[dd] != fillVersion && now - ringCxDirAt[dd] >= 0.25f)
        {
            ringCxDirFill[dd] = fillVersion;
            ringCxDirAt[dd] = now;
            float sum = 0f;
            int count = 0;
            for (int f = 0; f < frames; f++)
            {
                Sprite sprite = grid[dd, f];
                if (!sprite) continue;
                sum += sprite.bounds.center.x;
                count++;
            }
            ringCxAvg[dd] = count > 0 ? sum / count : 0f;
        }
        return ringCxAvg[dd];
    }

    private float HeadAnchorY(out float offX)
    {
        offX = 0f;
        float height = 1f;
        int top = avatar == null ? 0 : _isStallOwner && avatar.sitTopPx > 0
            ? avatar.sitTopPx : avatar.nameTopPx;
        if (top > 0) height = (top + 3) / ppu;
        else if (body && body.sprite) height = body.sprite.bounds.size.y + 0.05f;
        if (riding)
        {
            if (mountData != null && mountData.boundCy > 0)
            {
                height = mountData.boundCy / ppu + 0.04f;
                offX = ridingNameOffX;
            }
            else height += ridingRaiseY;
        }
        return height;
    }

    internal float HeadAnchorForIndependentRuntime(out float offX) => HeadAnchorY(out offX);
    internal bool MountVisibleForIndependentRuntime => srMount && srMount.enabled;
    internal float MountFarSecondsForIndependentRuntime => _farSec;
}

}
