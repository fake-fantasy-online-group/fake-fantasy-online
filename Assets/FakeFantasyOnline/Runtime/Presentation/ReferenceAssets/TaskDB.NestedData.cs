using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class TaskDB
{
    [Serializable] public class Obj { public int id; public int count; public string name; }
    [Serializable] public class Reward { public int exp; public long money; public int honor; }
    [Serializable] public class Def
    {
        public int id; public int type; public string name; public string desc; public string publisher;
        public string place; public string award; public Reward reward; public int minLevel; public int maxLevel;
        public int recomLevel; public bool disabled; public bool repeatable; public int weeklyLimit;
        public int[] prereq; public Obj[] items; public Obj[] monsters;
        public Obj[] deliverItems; public string[] deliverTo;
    }
    [Serializable] private class Wrap { public Def[] tasks; }
}
}
