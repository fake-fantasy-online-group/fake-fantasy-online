package org.fakefantasyonline.updates;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/** Best-effort ordinary launcher handoff after a verified self-update, subject to OS policy. */
public final class PackageReplacedReceiver extends BroadcastReceiver {
    @Override public void onReceive(Context context, Intent intent) {
        ClientPackageInstaller.replaced(context, intent);
    }
}
