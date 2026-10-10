using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05 : MonoBehaviour
{
    [Serializable] public class SkLevel { public int lv,lreq,mp,cast,cd,money; public string desc; }
    [Serializable] public class SkInfo
    {
        public int id,icon,sys,type,maxlv,pre; public string name,desc; public SkLevel[] levels;
        public int castkind,tgt,shape,radius,rwidth,dist,rectl,rectw,canSelf,canFriendly,canEnemy;
    }
    [Serializable] private class SkDB { public SkInfo[] skills; }
    [Serializable] private class SkSlot { public int id,x,y; }
    [Serializable] private class SkSwitch
    {
        public string tab,tree; public int sw,groupid,tabX,tabY,containX,containY,containW,containH,treeW,treeH;
        public SkSlot[] slots;
    }
    [Serializable] private class SkLayout { public int winW,winH; public string bk; public int[] pointlabel; public SkSwitch[] switches; }
    private class Def { public string gsa; public float w,h,cx,cy; public string title; public bool nohelp,src24,iron,g2021; }

    // The unchanged nineteen-entry Def table is ordered in Win05.Initialization.cs.

    // Official Win05.OnGUI 06001042, arm64 :102-116 / x86_64 :109-124. The native body reads
    // `cur` (+0x28), returns when it is null, then does
    // `if (!Defs.TryGetValue(cur, out def)) { cur = null; write-barrier; return; }` - an unknown
    // window name clears the current window instead of drawing anything, and nothing else in the
    // body runs that frame. The dictionary is the whole whitelist, so the nineteen official keys
    // above are kept as-is: a shorter list would close a native window official keeps open.
    private bool TryGetCurrentDefinition(out Def def)
    {
        if (cur == null)
        {
            def = null;
            return false;
        }

        if (Defs.TryGetValue(cur, out def))
        {
            return true;
        }

        cur = null;
        return false;
    }


}
}
