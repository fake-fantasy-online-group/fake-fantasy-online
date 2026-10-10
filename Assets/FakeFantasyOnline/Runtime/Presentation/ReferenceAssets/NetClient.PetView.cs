using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    private Action<int> writePetView;
    internal void BindPetViewWriterForIndependentRuntime(Action<int> writer) => writePetView = writer;
    public void SetPetViewForIndependentRuntime(int mask) => writePetView(mask);
}

}
