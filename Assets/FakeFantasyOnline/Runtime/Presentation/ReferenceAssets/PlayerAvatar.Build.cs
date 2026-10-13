using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    public static PlayerAvatar Build(int gender, int head, int hairColor, Appear ap, float ppu)
    {
        ap.WeaponL = ReferenceAvatarWeaponPresentation.LeftModel(ap.WeaponR, ap.WeaponL);
        PlayerAvatar avatar = new PlayerAvatar();
        if (_liveAvatars.Count >= _avatarPurgeAt)
        {
            for (int i = _liveAvatars.Count - 1; i >= 0; i--)
                if (!_liveAvatars[i].IsAlive) _liveAvatars.RemoveAt(i);
            _avatarPurgeAt = Math.Max(64, _liveAvatars.Count * 2);
        }
        _liveAvatars.Add(new WeakReference(avatar));
        avatar._ppu = ppu;
        LoadWeaponDepth();
        AvatarDepth.EnsureLoaded();
        bool female = gender != 1;
        string sexCode = gender == 1 ? "01" : "51";
        head = Mathf.Clamp(head, 1, 8);
        hairColor = Mathf.Clamp(hairColor, 1, HairRGB.Length);
        int fallbackBody = gender == 1 ? 141 : 142;
        int bodyModel = ap.Body > 0 ? ap.Body : fallbackBody;
        byte[] standBytes = ReadPkg(string.Format("avatar/npc/npc{0}01.gsn", bodyModel)) ??
            ReadPkg(string.Format("avatar/npc/npc{0}01.gsn", fallbackBody));
        byte[] walkBytes = ReadPkg(string.Format("avatar/npc/npc{0}02.gsn", bodyModel)) ??
            ReadPkg(string.Format("avatar/npc/npc{0}02.gsn", fallbackBody));
        byte[] sitBytes = ReadPkg(string.Format("avatar/npc/npc{0}03.gsn", bodyModel)) ??
            ReadPkg(string.Format("avatar/npc/npc{0}03.gsn", fallbackBody));
        byte[] rideBytes = ReadPkg(string.Format("avatar/npc/npc{0}13.gsn", bodyModel)) ??
            ReadPkg(string.Format("avatar/npc/npc{0}13.gsn", fallbackBody));
        byte[] headBytes = ReadPkg(string.Format("avatar/head/head{0:D2}{1}.gsn", head, sexCode));
        if (standBytes == null || walkBytes == null || headBytes == null) return avatar;
        DimgDecoder.Raw[] heads = new DimgDecoder.Raw[5];
        for (int d = 0; d < 5; d++)
        {
            DimgDecoder.Raw raw = DimgDecoder.DecodeRaw(headBytes, d);
            if (raw.px == null && raw.px16 != null)
            {
                raw.px = DimgDecoder.Expand16(raw.px16, raw.w, raw.h);
                raw.px16 = null;
            }
            if (raw.ok) Recolor(raw.px, HairRGB[hairColor - 1]);
            heads[d] = raw;
        }
        avatar._bodyModel = bodyModel;
        avatar._fallbackBody = fallbackBody;
        avatar._female = female;
        avatar._headFr = heads;
        avatar._ap = ap;
        avatar._headKey = string.Format("H{0}_{1}_{2}", head, hairColor, sexCode);
        avatar._poseCache = new Dictionary<int, Pose[]>();
        List<(int, string, byte[], byte[], byte[], byte[])> parts = avatar.PartsFor();
        int glowCount = avatar.GlowPartsFor().Count;
        string key = string.Format("B{0}", bodyModel);
        bool partsAdvance = SuitFxDB.IsStandAnimWeaponModel(ap.WeaponR) || SuitFxDB.IsStandAnimWeaponModel(ap.WeaponL);
        int bodyFpd = 1;
        if (partsAdvance)
        {
            foreach (var part in avatar.GlowPartsFor())
                bodyFpd = Math.Max(bodyFpd, Math.Min(16, DimgDecoder.FramesPerDir(part.Item3)));
            foreach (var part in parts)
                if (part.Item1 == 5 || part.Item1 == 6)
                    bodyFpd = Math.Max(bodyFpd, Math.Min(16, DimgDecoder.FramesPerDir(part.Item3)));
        }
        bodyFpd = Math.Max(bodyFpd, Math.Min(16, DimgDecoder.FramesPerDir(standBytes)));
        foreach (var part in parts)
            if (part.Item1 == 1 || part.Item1 == 2)
                bodyFpd = Math.Max(bodyFpd, Math.Min(16, DimgDecoder.FramesPerDir(part.Item3)));
        bool wingAdvance = SuitFxDB.IsStandAnimBackpackModel(ap.Backpack);
        int wingFpd = 1;
        if (wingAdvance)
            foreach (var part in parts)
                if (part.Item1 == 4)
                    wingFpd = Math.Max(wingFpd, Math.Min(16, DimgDecoder.FramesPerDir(part.Item3)));
        avatar.standBodyFpd = bodyFpd;
        avatar.standWingFpd = wingFpd;
        avatar.bodyBoundW = DimgDecoder.BoundWidthOf(standBytes);
        int standCount = bodyFpd * wingFpd;
        Dictionary<int, List<Layer>> standing = FramesOf(standBytes, key + "s", heads, avatar._headKey,
            standCount, parts, 0, "stand", DimgDecoder.FramesPerDir(standBytes), false, partsAdvance, wingFpd, wingAdvance);
        Dictionary<int, List<Layer>> walking = FramesOf(walkBytes, key + "w", heads, avatar._headKey, 8, parts, 1, "run");
        Dictionary<int, List<Layer>> sitting = sitBytes != null ?
            FramesOf(sitBytes, key + "t", heads, avatar._headKey, 1, parts, 2, "sit", DimgDecoder.FramesPerDir(sitBytes)) :
            new Dictionary<int, List<Layer>>();
        Dictionary<int, List<Layer>> riding = rideBytes != null ?
            FramesOf(rideBytes, key + "r", heads, avatar._headKey, 1, parts, 3, "ride", DimgDecoder.FramesPerDir(rideBytes)) :
            new Dictionary<int, List<Layer>>();
        if (glowCount > 0)
        {
            MergeGlow(standing, FramesOf(standBytes, key + "s", heads, avatar._headKey,
                standCount, avatar.GlowPartsFor(), 0, "stand", DimgDecoder.FramesPerDir(standBytes),
                true, partsAdvance, wingFpd, wingAdvance));
            MergeGlow(walking, FramesOf(walkBytes, key + "w", heads, avatar._headKey, 8,
                avatar.GlowPartsFor(), 1, "run", -1, true));
            if (sitBytes != null)
                MergeGlow(sitting, FramesOf(sitBytes, key + "t", heads, avatar._headKey, 1,
                    avatar.GlowPartsFor(), 2, "sit", DimgDecoder.FramesPerDir(sitBytes), true));
            if (rideBytes != null)
                MergeGlow(riding, FramesOf(rideBytes, key + "r", heads, avatar._headKey, 1,
                    avatar.GlowPartsFor(), 3, "ride", DimgDecoder.FramesPerDir(rideBytes), true));
        }
        avatar.stand = new Pose[5];
        avatar.standAnim = new Pose[5][];
        avatar.walk = new Pose[5, 8];
        avatar.sit = new Pose[5];
        avatar.ride = new Pose[5];
        for (int d = 0; d < 5; d++)
        {
            Pose[] directionFrames = new Pose[standCount];
            List<Layer> layers;
            for (int f = 0; f < standCount; f++)
                if (standing.TryGetValue(Key(d, f), out layers)) directionFrames[f] = MakePose(layers, ppu);
            avatar.standAnim[d] = directionFrames;
            if (standing.TryGetValue(Key(d, 0), out layers)) avatar.stand[d] = directionFrames[0] ?? MakePose(layers, ppu);
            if (sitting.TryGetValue(Key(d, 0), out layers)) avatar.sit[d] = MakePose(layers, ppu);
            if (riding.TryGetValue(Key(d, 0), out layers)) avatar.ride[d] = MakePose(layers, ppu);
            for (int f = 0; f < 8; f++)
                if (walking.TryGetValue(Key(d, f), out layers)) avatar.walk[d, f] = MakePose(layers, ppu);
        }
        if (avatar.sit[0] == null && avatar.sit[1] == null && avatar.sit[2] == null &&
            avatar.sit[3] == null && avatar.sit[4] == null && bodyModel != fallbackBody)
        {
            Debug.LogWarning(string.Format("[Avatar] 身体 model {0} 的坐姿(03)一帧都没烤出来 → 退用默认身体 {1} 的坐姿", bodyModel, fallbackBody) +
                "(sitBytes=" + (sitBytes != null ? sitBytes.Length.ToString() + "字节" : "null") + ")。摆摊/坐下才会用到这个姿势。");
            byte[] fallback = ReadPkg(string.Format("avatar/npc/npc{0}03.gsn", fallbackBody));
            if (fallback != null)
            {
                Dictionary<int, List<Layer>> replacement = FramesOf(fallback, string.Format("{0}03t", fallbackBody),
                    heads, avatar._headKey, 1, parts, 2, "sit", DimgDecoder.FramesPerDir(fallback));
                for (int d = 0; d < 5; d++)
                {
                    List<Layer> layers;
                    if (replacement.TryGetValue(Key(d, 0), out layers)) avatar.sit[d] = MakePose(layers, ppu);
                }
            }
        }
        avatar.ok = avatar.stand[0] != null;
        byte[] capStand = null;
        foreach (var part in parts) if (part.Item1 == 1) capStand = part.Item3;
        int capStride = Math.Max(1, capStand != null ? DimgDecoder.FramesPerDir(capStand) : 0);
        int headTop = 0;
        int capTop = 0;
        for (int d = 0; d < 5; d++)
        {
            DimgDecoder.Raw body = DimgDecoder.DecodeRaw(standBytes, d, false);
            if (!body.ok) continue;
            if (!body.hasBind)
            {
                headTop = Math.Max(headTop, body.boundCy - body.nTop);
                continue;
            }
            int dy = body.fcy - body.bind0y;
            headTop = Math.Max(headTop, dy + (heads[d].ok ? heads[d].fcy : 0));
            if (capStand != null)
            {
                DimgDecoder.Raw cap = DimgDecoder.DecodeRaw(capStand, d * capStride, false);
                if (cap.ok) capTop = Math.Max(capTop, dy + cap.fcy);
            }
        }
        avatar.headPx = headTop;
        avatar.nameTopPx = Math.Max(headTop, capTop);
        int sitTop = 0;
        if (sitBytes != null)
        {
            int sitStride = Math.Max(1, DimgDecoder.FramesPerDir(sitBytes));
            byte[] capSit = null;
            foreach (var part in parts) if (part.Item1 == 1) capSit = part.Item5;
            capStride = Math.Max(1, capSit != null ? DimgDecoder.FramesPerDir(capSit) : 0);
            for (int d = 0; d < 5; d++)
            {
                DimgDecoder.Raw body = DimgDecoder.DecodeRaw(sitBytes, d * sitStride, false);
                if (!body.ok || !body.hasBind) continue;
                int dy = body.fcy - body.bind0y;
                if (heads[d].ok) sitTop = Math.Max(sitTop, dy + heads[d].fcy);
                if (capSit != null)
                {
                    DimgDecoder.Raw cap = DimgDecoder.DecodeRaw(capSit, d * capStride, false);
                    if (!cap.ok) cap = DimgDecoder.DecodeRaw(capSit, d, false);
                    if (cap.ok) sitTop = Math.Max(sitTop, dy + cap.fcy);
                }
            }
        }
        avatar.sitTopPx = sitTop == 0 ? avatar.nameTopPx : sitTop;
        return avatar;
    }

    private static void Recolor(Color32[] px, Color32 target)
    {
        float h, s, v;
        Color.RGBToHSV(new Color(target.r / 255f, target.g / 255f, target.b / 255f, 1f), out h, out s, out v);
        float saturation = Mathf.Min(1f, s + 0.1f);
        float blend = Mathf.Clamp01(1f + -3f * s);
        for (int i = 0; i < px.Length; i++)
        {
            Color32 original = px[i];
            if (original.a < 30 || IsSkin(original.r, original.g, original.b)) continue;
            float sourceH, sourceS, sourceV;
            Color.RGBToHSV(new Color(original.r / 255f, original.g / 255f, original.b / 255f, 1f),
                out sourceH, out sourceS, out sourceV);
            float mapped = Mathf.Clamp01(v + (sourceV + -0.5f) * 0.45f);
            float finalV = sourceV + (mapped - sourceV) * blend;
            Color color = Color.HSVToRGB(h, saturation, finalV, true);
            px[i] = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), original.a);
        }
    }

    private static bool IsSkin(int r, int g, int b) =>
        r > 150 && r >= g && g >= b && r - b < 110 && b > 70 && g - b < 70;
}

}
