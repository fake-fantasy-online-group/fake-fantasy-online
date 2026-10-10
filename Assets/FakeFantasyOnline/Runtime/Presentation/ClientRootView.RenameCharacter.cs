using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IRenameCharacterReceiveContext
{
    void IRenameCharacterReceiveContext.ReceiveRenameCharacterResult(int code, string message, string name) =>
        NetClient.ApplyRenameResultForIndependentRuntime(code, message, name);
}
}
