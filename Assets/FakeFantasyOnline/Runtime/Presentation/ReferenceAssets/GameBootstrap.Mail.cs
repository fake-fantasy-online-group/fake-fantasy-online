using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    private ReferenceFirstWorldMailDialog persistentMail;
    internal bool HasMailOwnerForIndependentRuntime => persistentMail != null;

    internal ReferenceFirstWorldMailDialog AcquireMailForIndependentRuntime(ClientSessionCoordinator valueSession)
    {
        if (valueSession == null) throw new ArgumentNullException(nameof(valueSession));
        if (persistentMail == null)
            persistentMail = new ReferenceFirstWorldMailDialog(valueSession.MailCommands, valueSession.MailStateSource);
        return persistentMail;
    }

    private void ReleaseMailForIndependentRuntime(bool nativeNotificationCleared)
    {
        persistentMail?.Dispose(nativeNotificationCleared);
        persistentMail = null;
        if (!nativeNotificationCleared) MailDlg.HasNew = false;
    }
}

}
