namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CursorManager
{
    public void SetFetchHover(bool on)
    {
        if (fetchHover == on) return;
        fetchHover = on;
        Refresh();
    }
}
}
