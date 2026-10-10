using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    private static int PartFrameIndex(int bind, int d, int pfpd, int bf, int wf,
        int wingSplit, bool partsFrameAdvance, bool wingFrameAdvance)
    {
        if (pfpd < 2) return d;
        int phase;
        if (bind == 4)
            phase = wingFrameAdvance ? (wingSplit > 1 ? wf : bf) % pfpd : 0;
        else
            phase = !partsFrameAdvance && (bind == 5 || bind == 6) ? 0 : bf % pfpd;
        return d * pfpd + phase;
    }

    private static void WarnMissingFrame(string keyp, int pidx, int pfpd, int d)
    {
        if (string.IsNullOrEmpty(keyp) || _warnedFrames.ContainsKey(keyp)) return;
        _warnedFrames[keyp] = true;
        Debug.LogWarning(string.Format(
            "[Avatar] 部件缺帧: {0} 取不到 pidx={1}(方向d={2}, 每向{3}帧) —— 该层不画。多为美术实际帧数少于文件头声明(全库约1.87%)。",
            keyp, pidx, d, pfpd));
    }

    private static Dictionary<int, List<Layer>> FramesOf(byte[] bodyBytes, string bodyKey,
        DimgDecoder.Raw[] headFr, string headKey, int nfpd,
        List<(int, string, byte[], byte[], byte[], byte[])> parts, int action, string q21pose,
        int bodyStride = -1, bool glowOnly = false, bool partsFrameAdvance = true,
        int wingSplit = 1, bool wingFrameAdvance = true)
    {
        int poseIdx = AvatarDepth.PoseIdx(q21pose);
        int split = System.Math.Max(1, wingSplit);
        int stride = bodyStride > 0 ? bodyStride : wingSplit > 1 ? nfpd / split : nfpd;
        Dictionary<int, List<Layer>> result = new Dictionary<int, List<Layer>>();
        for (int d = 0; d < 5; d++)
        {
            DimgDecoder.Raw head = headFr[d];
            for (int f = 0; f < nfpd; f++)
            {
                int bf = wingSplit > 1 ? f / split : f;
                int wf = wingSplit > 1 ? f % split : f;
                int bidx = d * stride + (stride > 1 ? bf % stride : 0);
                string bkey = string.Format("{0}_{1}", bodyKey, bidx);
                DimgDecoder.Raw body = DimgDecoder.DecodeRaw(bodyBytes, bidx, !glowOnly && !HasSprite(bkey));
                if (!body.ok) continue;
                int ax = body.hasBind ? body.fcx : body.boundCx - body.nLeft;
                int ay = body.hasBind ? body.fcy : body.boundCy - body.nTop;
                List<(int tag, Layer layer)> tagged = new List<(int, Layer)>();
                if (!glowOnly)
                {
                    tagged.Add((0, new Layer
                    {
                        ox = -ax, oy = -ay, w = body.w, h = body.h,
                        px = body.px, px16 = body.px16, ax = ax, ay = ay,
                        tag = 0, key = bkey, scale = body.scale
                    }));
                    if (head.ok && body.hasBind)
                    {
                        tagged.Add((3, new Layer
                        {
                            ox = body.bind0x - head.fcx - ax, oy = body.bind0y - head.fcy - ay,
                            w = head.w, h = head.h, px = head.px, ax = head.fcx, ay = head.fcy,
                            tag = 3, key = string.Format("{0}_{1}", headKey, d), scale = head.scale
                        }));
                    }
                }
                foreach ((int bind, string keyp, byte[] a, byte[] b, byte[] c, byte[] unused) in parts)
                {
                    byte[] selected = action == 2 ? (SitPartArt == 1 ? a : SitPartArt == 3 ? c : b)
                        : action == 3 ? c : a;
                    selected = selected ?? a;
                    string suffix = selected == a ? "" : selected == b ? "s2" : selected == c ? "s3" : "s13";
                    int pfpd = DimgDecoder.FramesPerDir(selected);
                    int pidx = PartFrameIndex(bind, d, pfpd, bf, wf, wingSplit, partsFrameAdvance, wingFrameAdvance);
                    string pkey = string.Format("{0}{1}_{2}", keyp, suffix, pidx);
                    DimgDecoder.Raw part = DimgDecoder.DecodeRaw(selected, pidx, !HasSprite(pkey));
                    if (!part.ok && pfpd > 1)
                    {
                        int first = d * pfpd;
                        if (first != pidx)
                        {
                            string firstKey = string.Format("{0}{1}_{2}", keyp, suffix, first);
                            DimgDecoder.Raw fallback = DimgDecoder.DecodeRaw(selected, first, !HasSprite(firstKey));
                            if (fallback.ok)
                            {
                                part = fallback;
                                pkey = firstKey;
                                pidx = first;
                            }
                        }
                    }
                    if (!part.ok)
                    {
                        WarnMissingFrame(keyp, pidx, pfpd, d);
                        continue;
                    }
                    if (glowOnly && (part.dwFlags & 256) == 0) continue;
                    int bi = bind >= 4 && bind <= 6 ? (bind - 3) * 2 : 0;
                    if (body.binds == null || body.binds.Length < bi + 2) continue;
                    tagged.Add((bind, new Layer
                    {
                        ox = body.binds[bi] - part.fcx - ax,
                        oy = body.binds[bi + 1] - part.fcy - ay,
                        w = part.w, h = part.h, px = part.px, px16 = part.px16,
                        ax = part.fcx, ay = part.fcy, tag = bind, additive = glowOnly,
                        key = pkey, scale = part.scale
                    }));
                }
                int df = q21pose == "stand" ? 0 : f;
                int oct = FRAME_TO_OCT[Mathf.Clamp(d, 0, 4)];
                tagged.Sort((x, y) =>
                {
                    int xDepth = AvatarDepth.DepthArt(x.tag, poseIdx, d, df, 1);
                    int yDepth = AvatarDepth.DepthArt(y.tag, poseIdx, d, df, 1);
                    return xDepth != yDepth ? xDepth.CompareTo(yDepth) :
                        AvatarDepth.BaseRank(x.tag, oct).CompareTo(AvatarDepth.BaseRank(y.tag, oct));
                });
                List<Layer> layers = new List<Layer>(tagged.Count);
                foreach ((int tag, Layer layer) in tagged) layers.Add(layer);
                result[Key(d, f)] = layers;
            }
        }
        return result;
    }
}

}
