using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Original pose payload. Existing package readers adapt their resource ownership around it.
public partial class PlayerAvatar
{
    public struct LayerInst
    {
        public Sprite spr;
        public float offX;
        public float offY;
        public int tag;
        public bool additive;
    }

    public class Pose
    {
        public LayerInst[] layers;
    }
}

}
