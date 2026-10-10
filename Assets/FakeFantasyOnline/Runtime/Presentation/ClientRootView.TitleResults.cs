using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView
{
    internal void SetupNativeTitleLoginResultForIndependentRuntime()
    {
        ClientSessionCoordinator session = _session;
        TitleFlow title = _titleCharacter.NativeOwnerForIndependentRuntime;
        session.BindTitleLoginResultForIndependentRuntime(
            title.ReceiveNativeTitleLoginResult);
        _titleCharacter.BindLoginResultReceiptReleaseForIndependentRuntime(
            () => session.BindTitleLoginResultForIndependentRuntime(null));
    }

    internal void SetupNativeTitleCreateResultForIndependentRuntime()
    {
        ClientSessionCoordinator session = _session;
        TitleFlow title = _titleLogin;
        session.BindTitleCreateResultForIndependentRuntime(
            title.ReceiveNativeTitleCreateResult);
        _titleCharacter.BindCreateResultReceiptReleaseForIndependentRuntime(
            () => session.BindTitleCreateResultForIndependentRuntime(null));
    }

}
}
