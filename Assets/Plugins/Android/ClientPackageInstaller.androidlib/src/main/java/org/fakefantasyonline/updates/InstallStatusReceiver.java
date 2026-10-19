package org.fakefantasyonline.updates;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/** Not exported. Never launches an activity from a background broadcast. */
public final class InstallStatusReceiver extends BroadcastReceiver {
    @Override public void onReceive(Context context, Intent intent) {
        ClientPackageInstaller.receive(context, intent);
    }
}
