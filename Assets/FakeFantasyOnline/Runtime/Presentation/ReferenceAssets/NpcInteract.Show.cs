namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcInteract
{
    // Official 1.5.8 token 06000926. The official body tests the recovered dialog singleton with
    // UnityEngine.Object::op_Inequality and then forwards itself; it never owns the open path.
    public void Show()
    {
        if (DialogUI.I != null) DialogUI.I.Show(this);
    }
}

}
