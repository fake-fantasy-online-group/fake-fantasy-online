using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class ResLoader
{
    public ObjAnim LoadNpcAnim(string spriteName, float ppu)
    {
        ObjAnim cached;
        if (npcAnimCache.TryGetValue(spriteName, out cached))
        {
            npcAnimBudget.Touch(spriteName);
            return cached;
        }

        ObjAnim result = default(ObjAnim);
        if (!string.IsNullOrEmpty(spriteName) && Pkg != null)
        {
            byte[] data = Pkg.Read("avatar/npc/" + spriteName + "01.gsn");
            if (data != null)
            {
                DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(data);
                if (anim.ok)
                {
                    int directions = Math.Max(anim.dirs, 1);
                    int frameCount = anim.frames / directions;
                    if (frameCount > 1)
                    {
                        result.frames = new Sprite[frameCount];
                        result.nLeft = new int[frameCount];
                        result.nTop = new int[frameCount];
                        int scale = Math.Max(anim.scale, 1);
                        float basePpu = ppu * scale;
                        for (int index = 0; index < frameCount && index < anim.f.Length; index++)
                        {
                            DimgDecoder.FrameTex frame = anim.f[index];
                            if (frame.tex)
                            {
                                result.frames[index] = Sprite.Create(
                                    frame.tex,
                                    new Rect(0f, 0f, frame.tex.width, frame.tex.height),
                                    new Vector2(0f, 1f),
                                    basePpu);
                            }

                            result.nLeft[index] = frame.nLeft;
                            result.nTop[index] = frame.nTop;
                        }
                    }
                }
            }
        }

        npcAnimCache[spriteName] = result;
        npcAnimBudget.Note(spriteName, TextureBytes(result));
        return result;
    }

    public long OtherArtBytes
    {
        get
        {
            HashSet<Texture2D> textures = new HashSet<Texture2D>();
            CollectObjTextures(value => textures.Add(value));
            CollectUiTextures(value => textures.Add(value));
            long bytes = 0L;
            foreach (Texture2D texture in textures)
            {
                bytes += TextureBytes(texture);
            }

            return bytes;
        }
    }

    public string OtherArtStat()
    {
        return string.Format(
            "物件{0} 静态/{1}动画/{2}轮廓 · UI{3}/{4} · {5:F1}MB",
            objCache.Count,
            objAnimCache.Count,
            contourCache.Count,
            texCache.Count,
            uiCache.Count,
            OtherArtBytes / (1024f * 1024f));
    }

    public ObjSprite LoadObj(int imageId, int outW = 0, int outH = 0)
    {
        if (objCache.TryGetValue(imageId, out ObjSprite cached))
        {
            if (cached.candW == 0 ||
                (Mathf.Abs(cached.candW - outW) <= 6 && Mathf.Abs(cached.candH - outH) <= 6))
                return cached;
            DropObj(imageId);
        }

        ObjSprite result = default;
        string path = ObjPath(imageId);
        if (path != null && Pkg != null && Pkg.Has(path))
        {
            result = Decode(path);
        }
        else if (Pkg != null && MapObjNames.TryGetValue((imageId >> 16) & 0xff, out string objectName) && outW > 0)
        {
            List<string> candidates = Pkg.FindAll("object/" + objectName + "/",
                string.Format("_{0:D4}.gso", imageId & 0xffff));
            int best = int.MaxValue;
            foreach (string candidate in candidates)
            {
                ObjSprite decoded = Decode(candidate);
                if (decoded.tex == null) continue;
                int score = Mathf.Abs(decoded.tex.width / decoded.hdScale - outW) +
                    Mathf.Abs(decoded.tex.height / decoded.hdScale - outH);
                if (score < best && score <= 6)
                {
                    result = decoded;
                    result.candW = outW;
                    result.candH = outH;
                    best = score;
                }
            }
        }
        objCache[imageId] = result;
        return result;
    }

    private ObjSprite Decode(string path)
    {
        ObjSprite result = default(ObjSprite);
        byte[] data = Pkg == null ? null : Pkg.Read(path);
        if (data == null)
        {
            return result;
        }

        DimgDecoder.Result decoded = DimgDecoder.Decode(data, 0);
        if (!decoded.ok)
        {
            return result;
        }

        result.tex = decoded.tex;
        result.nLeft = decoded.nLeft;
        result.nTop = decoded.nTop;
        result.clipBy = decoded.clipBy;
        result.clipWidth = decoded.clipWidth;
        result.clipArr = decoded.clipArr;
        result.frames = decoded.frames;
        result.path = path;
        result.hdScale = Math.Max(decoded.scale, 1);
        return result;
    }

    public Texture2D LoadObjTex(int imageId)
    {
        return LoadObj(imageId).tex;
    }

    public List<ObjSprite> LoadClouds(int cloudType)
    {
        List<ObjSprite> result = new List<ObjSprite>();
        if (Pkg == null)
        {
            return result;
        }

        foreach (string path in Pkg.FindAll(
                     "object/YD/", string.Format("_{0:D4}.gso", cloudType)))
        {
            ObjSprite value = Decode(path);
            if (value.tex)
            {
                result.Add(value);
            }
        }

        return result;
    }

    public ObjAnim LoadObjAnim(int imageId, float ppu)
    {
        if (objAnimCache.TryGetValue(imageId, out ObjAnim cached)) return cached;
        ObjAnim result = default;
        ObjSprite source = LoadObj(imageId);
        if (source.path != null && source.frames > 1 && Pkg != null)
        {
            byte[] data = Pkg.Read(source.path);
            if (data != null)
            {
                DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(data);
                if (animation.ok)
                {
                    int count = animation.frames / Math.Max(animation.dirs, 1);
                    if (count > 1)
                    {
                        result.frames = new Sprite[count];
                        result.nLeft = new int[count];
                        result.nTop = new int[count];
                        int scale = Math.Max(animation.scale, 1);
                        for (int i = 0; i < count && i < animation.f.Length; i++)
                        {
                            DimgDecoder.FrameTex frame = animation.f[i];
                            if (frame.tex != null)
                                result.frames[i] = Sprite.Create(frame.tex,
                                    new Rect(0, 0, frame.tex.width, frame.tex.height),
                                    new Vector2(0, 1), ppu * scale);
                            result.nLeft[i] = frame.nLeft;
                            result.nTop[i] = frame.nTop;
                        }
                    }
                }
            }
        }
        objAnimCache[imageId] = result;
        return result;
    }

    public Texture2D ContourTex(int imageId)
    {
        Texture2D cached;
        if (contourCache.TryGetValue(imageId, out cached))
        {
            return cached;
        }

        Texture2D source = LoadObjTex(imageId);
        if (!source)
        {
            contourCache[imageId] = null;
            return null;
        }

        Texture2D contour = new Texture2D(
            source.width, source.height, TextureFormat.RGBA32, false);
        Color32[] pixels = source.GetPixels32();
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new Color32(255, 255, 255, pixels[index].a);
        }

        contour.SetPixels32(pixels);
        contour.Apply(false, false);
        contour.filterMode = FilterMode.Point;
        contour.wrapMode = TextureWrapMode.Clamp;
        contourCache[imageId] = contour;
        return contour;
    }

    public long ObjSrcBytes(int imageId)
    {
        long bytes = 0L;
        ObjSprite sprite;
        if (objCache.TryGetValue(imageId, out sprite))
        {
            bytes += TextureBytes(sprite.tex);
        }

        ObjAnim anim;
        if (objAnimCache.TryGetValue(imageId, out anim))
        {
            bytes += TextureBytes(anim);
        }

        Texture2D contour;
        if (contourCache.TryGetValue(imageId, out contour))
        {
            bytes += TextureBytes(contour);
        }

        return bytes;
    }

    public long DropObj(int imageId)
    {
        long bytes = ObjSrcBytes(imageId);
        ObjSprite sprite;
        if (objCache.TryGetValue(imageId, out sprite) && sprite.tex)
        {
            Destroy(sprite.tex);
        }
        objCache.Remove(imageId);

        ObjAnim anim;
        if (objAnimCache.TryGetValue(imageId, out anim))
        {
            DestroyAnim(anim);
        }
        objAnimCache.Remove(imageId);

        Texture2D contour;
        if (contourCache.TryGetValue(imageId, out contour) && contour)
        {
            Destroy(contour);
        }
        contourCache.Remove(imageId);
        return bytes;
    }

    public void ClearObjCaches()
    {
        List<int> ids = new List<int>(objCache.Keys);
        foreach (int id in objAnimCache.Keys)
        {
            if (!ids.Contains(id)) ids.Add(id);
        }
        foreach (int id in contourCache.Keys)
        {
            if (!ids.Contains(id)) ids.Add(id);
        }
        foreach (int id in ids) DropObj(id);
        objPathCache.Clear();
    }

    private static long TextureBytes(ObjAnim value)
    {
        long bytes = 0L;
        if (value.frames != null)
        {
            foreach (Sprite sprite in value.frames)
            {
                if (sprite && sprite.texture) bytes += TextureBytes(sprite.texture);
            }
        }
        return bytes;
    }

    private static long TextureBytes(Texture2D value)
    {
        return value ? (long)value.width * value.height * 4L : 0L;
    }

    private static void DestroyAnim(ObjAnim value)
    {
        if (value.frames == null) return;
        foreach (Sprite sprite in value.frames)
        {
            if (!sprite) continue;
            if (sprite.texture) Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}

}
