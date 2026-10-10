using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    private static string ActionPose(int actionIndex)
    {
        switch (actionIndex)
        {
            case 1: return "run";
            case 2: return "sit";
            case 3: return "die";
            case 4: return "attack1";
            case 5: return "attack2";
            case 6: return "attack3";
            case 7: return "magic";
            case 8: return "prepare1";
            case 9: return "prepare2";
            case 10: return "prepare3";
            case 11: return "hurt";
            case 12: return "ride";
            default: return "stand";
        }
    }

    public Pose[,] AttackPose(int variant, out int fpd)
    {
        variant = Mathf.Clamp(variant, 0, 2);
        if (_atkBuilt[variant])
        {
            fpd = _atkFpd[variant];
            return _atk[variant];
        }
        _atkBuilt[variant] = true;
        _atkFpd[variant] = 1;
        _atk[variant] = null;
        if (stand == null || _headFr == null)
        {
            fpd = 1;
            return null;
        }
        byte[] body = ReadPkg(string.Format("avatar/npc/npc{0}{1:D2}.gsn", _bodyModel, variant + 5));
        if (body != null)
        {
            int n = System.Math.Max(1, DimgDecoder.FramesPerDir(body));
            string bodyKey = string.Format("B{0}a{1}", _bodyModel, variant);
            Dictionary<int, List<Layer>> frames = FramesOf(body, bodyKey, _headFr, _headKey,
                n, PartsFor(variant + 5), 1, "attack" + (variant + 1));
            if (GlowPartsFor(variant + 5).Count > 0)
                MergeGlow(frames, FramesOf(body, bodyKey, _headFr, _headKey, n,
                    GlowPartsFor(variant + 5), 1, "attack" + (variant + 1), -1, true));
            _atk[variant] = new Pose[5, n];
            for (int d = 0; d < 5; d++)
                for (int f = 0; f < n; f++)
                {
                    List<Layer> layers;
                    if (frames.TryGetValue(Key(d, f), out layers)) _atk[variant][d, f] = MakePose(layers, _ppu);
                }
            _atkFpd[variant] = n;
        }
        else
        {
            int n;
            Pose[,] fallback = PoseFrames(0, out n);
            if (fallback != null && n > 1)
            {
                _atk[variant] = fallback;
                _atkFpd[variant] = n;
            }
        }
        fpd = _atkFpd[variant];
        return _atk[variant];
    }

    public Pose[,] MagicPose(out int fpd)
    {
        if (_magBuilt)
        {
            fpd = _magFpd;
            return _mag;
        }
        _magBuilt = true;
        _magFpd = 1;
        _mag = null;
        if (stand == null || _headFr == null)
        {
            fpd = 1;
            return null;
        }
        byte[] body = ReadPkg(string.Format("avatar/npc/npc{0}08.gsn", _bodyModel));
        if (body != null)
        {
            int n = System.Math.Max(1, DimgDecoder.FramesPerDir(body));
            string bodyKey = string.Format("B{0}m", _bodyModel);
            Dictionary<int, List<Layer>> frames = FramesOf(body, bodyKey, _headFr, _headKey,
                n, PartsFor(8), 1, "magic");
            if (GlowPartsFor(8).Count > 0)
                MergeGlow(frames, FramesOf(body, bodyKey, _headFr, _headKey, n,
                    GlowPartsFor(8), 1, "magic", -1, true));
            _mag = new Pose[5, n];
            for (int d = 0; d < 5; d++)
                for (int f = 0; f < n; f++)
                {
                    List<Layer> layers;
                    if (frames.TryGetValue(Key(d, f), out layers)) _mag[d, f] = MakePose(layers, _ppu);
                }
            _magFpd = n;
        }
        fpd = _magFpd;
        return _mag;
    }

    public Pose[] PoseAt(int actionIndex)
    {
        if (actionIndex < 1) return stand;
        if (stand == null) return null;
        if (actionIndex == 12 && ride != null) return ride;
        if (actionIndex == 2 && sit != null) return sit;
        if (_poseCache == null || _headFr == null) return stand;
        Pose[] result;
        if (_poseCache.TryGetValue(actionIndex, out result)) return result;
        byte[] body = ReadPkg(string.Format("avatar/npc/npc{0}{1:D2}.gsn", _bodyModel, actionIndex + 1));
        result = new Pose[5];
        if (body != null)
        {
            int stride = DimgDecoder.FramesPerDir(body);
            string bodyKey = string.Format("B{0}p{1}", _bodyModel, actionIndex);
            Dictionary<int, List<Layer>> frames = FramesOf(body, bodyKey, _headFr, _headKey,
                1, PartsFor(), 3, ActionPose(actionIndex), stride);
            if (GlowPartsFor(actionIndex + 1).Count > 0)
                MergeGlow(frames, FramesOf(body, string.Format("B{0}p{1}", _bodyModel, actionIndex),
                    _headFr, _headKey, 1, GlowPartsFor(actionIndex + 1), 3, ActionPose(actionIndex), stride, true));
            for (int d = 0; d < 5; d++)
            {
                List<Layer> layers;
                if (frames.TryGetValue(Key(d, 0), out layers)) result[d] = MakePose(layers, _ppu);
            }
        }
        if (result[0] == null) result = stand;
        _poseCache[actionIndex] = result;
        return result;
    }

    public Pose[,] PoseFrames(int actionIndex, out int fpd)
    {
        fpd = 1;
        if (stand == null || _headFr == null) return null;
        if (_poseFramesCache == null) _poseFramesCache = new Dictionary<int, Pose[,]>();
        if (_poseFpdCache == null) _poseFpdCache = new Dictionary<int, int>();
        Pose[,] result;
        if (_poseFramesCache.TryGetValue(actionIndex, out result))
        {
            fpd = _poseFpdCache[actionIndex];
            return result;
        }
        byte[] body = ReadPkg(string.Format("avatar/npc/npc{0}{1:D2}.gsn", _bodyModel, actionIndex + 1));
        if (body == null && actionIndex == 3)
            body = ReadPkg(string.Format("avatar/npc/npc{0}{1:D2}.gsn", _fallbackBody, actionIndex + 1));
        if (body == null)
        {
            _poseFramesCache[actionIndex] = null;
            _poseFpdCache[actionIndex] = 1;
            return null;
        }
        int n = DimgDecoder.FramesPerDir(body);
        Dictionary<int, List<Layer>> frames = FramesOf(body, string.Format("B{0}f{1}", _bodyModel, actionIndex),
            _headFr, _headKey, n, PartsFor(), 1, ActionPose(actionIndex));
        if (GlowPartsFor(actionIndex + 1).Count > 0)
            MergeGlow(frames, FramesOf(body, string.Format("B{0}f{1}", _bodyModel, actionIndex),
                _headFr, _headKey, n, GlowPartsFor(actionIndex + 1), 1, ActionPose(actionIndex), -1, true));
        result = new Pose[5, n];
        for (int d = 0; d < 5; d++)
            for (int f = 0; f < n; f++)
            {
                List<Layer> layers;
                if (frames.TryGetValue(Key(d, f), out layers)) result[d, f] = MakePose(layers, _ppu);
            }
        fpd = n;
        _poseFramesCache[actionIndex] = result;
        _poseFpdCache[actionIndex] = n;
        return result;
    }
}

}
