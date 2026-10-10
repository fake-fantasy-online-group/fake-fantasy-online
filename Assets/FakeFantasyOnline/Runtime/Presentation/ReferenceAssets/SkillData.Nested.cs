using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class SkillEffectDB
{
    [Serializable] private class JPh { public int s; public int e; public int d; }
    [Serializable] private class JShow { public int id; public int phy; public JPh[] ph; }
    [Serializable] private class JFile { public JShow[] shows; }
}
public static partial class SkillSoundDB
{
    [Serializable] private class JSnd { public int id; public int cast; public int cd; public int[] fire; public int[] fd; }
    [Serializable] private class JFile { public JSnd[] sounds; }
}
public static partial class SkillElementDB
{
    [Serializable] private class JElem { public int id; public int e; }
    [Serializable] private class JFile { public JElem[] elems; }
}
}
