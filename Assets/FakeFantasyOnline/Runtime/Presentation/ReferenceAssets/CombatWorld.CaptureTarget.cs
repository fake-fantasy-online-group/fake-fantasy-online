namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    public static int CaptureTargetId
    {
        get
        {
            MonsterEntity selected = TargetSelect.Current as MonsterEntity;
            if (selected != null) return selected.id;
            if (I != null && I.target != null) return I.target.id;
            return 0;
        }
    }
}
}
