using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class PetSkillIconUI
{
    private static readonly Dictionary<int, Texture2D> _cache = new Dictionary<int, Texture2D>();

    public static Texture2D Get(int skillId, int iconId)
    {
        if (_cache.TryGetValue(skillId, out Texture2D cached)) return cached;
        Texture2D texture = null;
        if (ResLoader.I != null && iconId > 0)
        {
            byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", iconId));
            if (data != null)
            {
                DimgDecoder.Result decoded = DimgDecoder.Decode(data, 0);
                if (decoded.ok)
                {
                    texture = decoded.tex;
                    TexScale.Register(texture, decoded.scale);
                }
            }
        }
        if (texture == null && ResLoader.I != null)
        {
            ResLoader.ObjAnim animation = ResLoader.I.LoadNpcAnim("npc" + skillId.ToString(), 100f);
            if (animation.frames != null && animation.frames.Length > 0 && animation.frames[0] != null)
                texture = animation.frames[0].texture;
        }
        _cache[skillId] = texture;
        return texture;
    }
}
}
