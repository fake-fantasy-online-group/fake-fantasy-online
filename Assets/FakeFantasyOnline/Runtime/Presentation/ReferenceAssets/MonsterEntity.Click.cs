namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity click routing: a monster click either arms combat or just selects,
/// depending on whether the clicked monster is friendly.
/// </summary>
public partial class MonsterEntity
{
    private void OnMouseDown()
    {
        if (MobileClientPresentation.Enabled || GameHud.OverUI)
        {
            return;
        }

        if (!friendly && !ally)
        {
            CombatWorld.SetTarget(this, true);
            return;
        }

        TargetSelect.Select(this);
    }
}

}
