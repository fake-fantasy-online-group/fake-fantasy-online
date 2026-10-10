using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskClient
{
    // Official 1.5.8 token 06000e62. The official body walks Active through the struct enumerator
    // and compares the first field (id at absolute +0x10); the explicit null check on the walked
    // element is the field access itself.
    public static ActiveTask GetActive(int id)
    {
        foreach (ActiveTask task in Active)
            if (task.id == id) return task;
        return null;
    }

    // Official 1.5.8 token 06000e60. step is at +0x18 and total at +0x1c, so the terminal test is
    // total-1 <= step with a total>0 guard; a missing row is never terminal.
    public static bool IsTerminal(int id)
    {
        ActiveTask task = GetActive(id);
        if (task == null) return false;
        if (task.total < 1) return false;
        return task.total - 1 <= task.step;
    }

    // Official 1.5.8 token 06000e63. The official body only reads List.Count (+0x18) of Active and
    // throws on a null list, which is what the plain property read emits.
    public static int ActiveCount => Active.Count;

    // Official 1.5.8 token 06000e6b. The official signature carries TaskDB.Def but the body never
    // reads it: the predicate is "active row exists, total>0, ready flag set, step past total-1".
    public static bool Completable(int id, TaskDB.Def def)
    {
        ActiveTask task = GetActive(id);
        if (task == null) return false;
        if (task.total <= 0 || task.ready != 1) return false;
        return task.total - 1 <= task.step;
    }

    // Official 1.5.8 token 06000e69. Every eligibility predicate already lives in Acceptable, so
    // the recommendation is "walk every definition, keep the acceptable ones, order them by how
    // close their recommended level is to the player level, then keep at most max rows". The
    // official Sort argument is the hoisted 21_0 lambda, kept here as the same inline comparison.
    public static List<TaskDB.Def> Recommended(int max = 30)
    {
        List<TaskDB.Def> recommended = new List<TaskDB.Def>();
        foreach (TaskDB.Def def in TaskDB.All())
            if (Acceptable(def)) recommended.Add(def);
        recommended.Sort((left, right) =>
        {
            int a = unchecked(CharData.level - left.recomLevel);
            int b = unchecked(CharData.level - right.recomLevel);
            a = a < 0 ? unchecked(-a) : a;
            b = b < 0 ? unchecked(-b) : b;
            return a.CompareTo(b);
        });
        if (recommended.Count > max) recommended.RemoveRange(max, recommended.Count - max);
        return recommended;
    }
}

}
