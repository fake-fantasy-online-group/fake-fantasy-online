using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerAvatar
{
    // Official PlayerAvatar.ZRank (0x06000998) and PlayerAvatar.LayerDepth (0x0600099a), restored
    // from the frozen dual-architecture 1.5.8 exports. Neither has a recorded caller in the official
    // call graph, so they are restored as official members and left unwired, the same way the
    // already-restored caller-less official members are kept.
    //
    // arm64 0x06000998 reads the translated bind out of TAG2LAYER only when the incoming bind is a
    // valid index, then clamps the direction into 0..4 the same way PlayerAvatar.Frames already does
    // (arm64 lowers it to dir & ~(dir >> 31) followed by dir = 4 when 3 < dir; x86_64 lowers it to
    // dir = 0 when dir < 0 followed by dir = 4 when 4 <= dir), maps it through FRAME_TO_OCT into
    // LAYERORDER and returns the position of the bind in that row, falling back to 99 when absent.
    public static int ZRank(int bind, int dir)
    {
        if (bind >= 0 && bind < TAG2LAYER.Length)
        {
            bind = TAG2LAYER[bind];
        }

        int[] order = LAYERORDER[FRAME_TO_OCT[Mathf.Clamp(dir, 0, 4)]];
        int index = Array.IndexOf(order, bind);
        return index < 0 ? 99 : index;
    }

    // Official guard order is d, then action, then tag, then the _depth null test, and every out of
    // range index falls through to the runtime's own range failure, which the direct indexing below
    // reproduces. The frame index is clamped to 7 only from above: arm64 lowers it to f = 7 when
    // 6 < f and x86_64 to f = 7 when 7 <= f, so a negative frame still fails on the range check.
    public static int LayerDepth(int tag, int action, int d, int f)
    {
        if (d > 4 || action > 2 || tag > 6 || _depth == null)
        {
            return 0;
        }

        return _depth[tag][action][d][f > 6 ? 7 : f];
    }
}

}
