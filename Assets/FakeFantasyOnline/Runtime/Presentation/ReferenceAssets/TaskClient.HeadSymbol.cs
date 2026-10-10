using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class TaskClient
{
    // Official 1.5.8 token 06000e5f.
    public static bool IsActive(int id)
    {
        foreach (ActiveTask task in Active)
            if (task.id == id) return true;
        return false;
    }

    // Official 1.5.8 token 06000e61.
    public static bool IsDone(int id) { return Done.Contains(id); }

    // Official 1.5.8 token 06000e64.
    public static (int kind, int color) HeadSymbol(string npc)
    {
        if (string.IsNullOrEmpty(npc)) return (-1, 0);
        foreach (ActiveTask task in Active)
        {
            if (task.to != npc || task.ready != 1) continue;
            TaskDB.Def def = TaskDB.Get(task.id);
            return (0, def != null ? ColorBucket(def) : 2);
        }
        IReadOnlyList<int> ids = TaskDB.ByPublisher(npc);
        if (ids != null)
        {
            foreach (int id in ids)
            {
                TaskDB.Def def = TaskDB.Get(id);
                if (Acceptable(def)) return (1, ColorBucket(def));
            }
        }
        return (-1, 0);
    }

    // Official 1.5.8 token 06000e65.
    public static bool Acceptable(TaskDB.Def def)
    {
        if (def == null || def.disabled) return false;
        if (IsActive(def.id)) return false;
        if (!PrereqMet(def)) return false;
        if (def.minLevel > 0 && CharData.level < def.minLevel) return false;
        int race = JobQuestProf(def.id);
        if (race >= 0 && CharData.race != race) return false;
        if (def.repeatable) return true;
        if (IsDone(def.id)) return false;
        return !SameTitleTaken(def);
    }

    // Official 1.5.8 token 06000e66. Despite its name, this is compared with CharData.race.
    private static int JobQuestProf(int taskId)
    {
        switch (taskId)
        {
            case 400: return 3;
            case 401: return 4;
            case 402: return 2;
            case 403: return 0;
            case 404: return 1;
            default: return -1;
        }
    }

    // Official 1.5.8 token 06000e67.
    private static bool SameTitleTaken(TaskDB.Def def)
    {
        if (string.IsNullOrEmpty(def.name)) return false;
        foreach (int id in Done)
        {
            TaskDB.Def other = TaskDB.Get(id);
            if (other != null && other.id != def.id && other.name == def.name) return true;
        }
        foreach (ActiveTask task in Active)
        {
            TaskDB.Def other = TaskDB.Get(task.id);
            if (other != null && other.id != def.id && other.name == def.name) return true;
        }
        return false;
    }

    // Official 1.5.8 token 06000e68.
    private static int ColorBucket(TaskDB.Def def)
    {
        int difference = unchecked(CharData.level - def.recomLevel);
        return difference < 4 ? 2 : difference > 10 ? 1 : 0;
    }

    // Official 1.5.8 token 06000e6a.
    public static bool PrereqMet(TaskDB.Def def)
    {
        if (def.prereq != null)
            foreach (int id in def.prereq)
                if (id != 0 && !Done.Contains(id)) return false;
        return true;
    }
}

}
