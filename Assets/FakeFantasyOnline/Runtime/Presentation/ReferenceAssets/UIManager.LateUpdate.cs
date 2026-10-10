namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official UIManager.LateUpdate (0x06000fa7, arm64 rva=0x3a7644 / x86_64 rva=0x3ba450,
// emittedExtent=304 on both slices). The body is one foreach and nothing else: it reads the
// instance field +0x28 (stack), null-checks it, then enumerates
// (List<Win>.GetEnumerator -> Enumerator.MoveNext -> Enumerator.Dispose), calling
// UIManager.Layout (0x06000fa6) once per stacked window.
// A null stack neither skips nor defaults: both slices fall into the il2cpp throw helper
// (FUN_00177a74 arm64 / FUN_0016c1b0 x86_64), which is the same null dereference the managed
// foreach produces when it calls GetEnumerator on the field.
// The method itself never reads a camera - the Camera.main read lives in Layout.
public partial class UIManager
{
    private void LateUpdate()
    {
        foreach (Win win in stack)
        {
            Layout(win);
        }
    }
}
}
