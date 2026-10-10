using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Official 0x06001051. Resource-loader caches remain the texture owners.
    private Texture2D PetTex(int model)
    {
        if (model < 1 || ResLoader.I == null) return null;
        Texture2D texture = ResLoader.I.LoadGsa(string.Format("res/pet/M{0}.gsa", model));
        if (texture == null)
        {
            ResLoader.ObjAnim animation = ResLoader.I.LoadNpcAnim("npc" + model.ToString(), 100f);
            if (animation.frames != null && animation.frames.Length > 0 && animation.frames[0] != null)
                texture = animation.frames[0].texture;
        }
        return texture;
    }
}
}
