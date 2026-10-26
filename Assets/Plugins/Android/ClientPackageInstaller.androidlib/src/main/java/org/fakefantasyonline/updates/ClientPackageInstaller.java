package org.fakefantasyonline.updates;

import android.Manifest;
import android.app.Activity;
import android.app.PendingIntent;
import android.app.admin.DevicePolicyManager;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.content.pm.PackageInstaller;
import android.content.pm.PackageManager;
import android.content.pm.Signature;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.util.Log;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.security.MessageDigest;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** A single, fail-closed self-update; Android retains all permission and confirmation decisions. No Unity or downloaded-code dependency. */
public final class ClientPackageInstaller {
    private static final Object LOCK = new Object();
    private static final ExecutorService WORKER = Executors.newSingleThreadExecutor();
    private static final String PREFS = "ffo.package.install";
    private static final String ACTION = "org.fakefantasyonline.updates.INSTALL_STATUS";
    private static Context app;
    private static String state = "Idle", message = "等待安装包。";
    private static float progress = -1;
    private static long generation, outcomeEpoch;
    private static File prepared;
    private static String version, sha, signers, token;
    private static long bytes, code;
    private static int sessionId = -1;
    private static Intent confirmation;
    private static PendingIntent callback;

    private ClientPackageInstaller() { }

    private static void initialize(Context context) {
        if (app != null) return;
        app = context.getApplicationContext();
        SharedPreferences prefs = app.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
        sessionId = prefs.getInt("session", -1);
        final Context owner = app;
        final int retainedSession = sessionId;
        // Also recover the narrow createSession -> durable-record crash window. Only this
        // installer's own sessions targeting this running package are eligible.
        WORKER.execute(() -> cleanupOrphanedSessions(owner, retainedSession));
        if (sessionId < 0) return;
        version = prefs.getString("version", "");
        code = prefs.getLong("code", -1);
        signers = prefs.getString("signers", "");
        token = prefs.getString("token", "");
        String terminal = prefs.getString("terminal", "");
        if ("Failed".equals(terminal) || "Cancelled".equals(terminal)) {
            set(terminal, "上次系统安装没有完成，请重新校验安装包后重试。", -1);
            abandon(sessionId); // Recover a death between persisting failure and abandoning it.
            clearRecord(); sessionId = -1; return;
        }
        // A missing callback or missing session is NEVER interpreted as install success.
        if (!prefs.getBoolean("successObserved", false)) {
            set("Interrupted", "上次系统安装结果未确认。请取消本次交接后重新校验安装包；不会自动安装。", -1);
            return;
        }
        set("AwaitingResult", "正在核实上次安装结果…", -1);
        final long current = generation, observedOutcome = outcomeEpoch;
        WORKER.execute(() -> {
            boolean installed = isExpectedInstalled();
            synchronized (LOCK) {
                if (current != generation || observedOutcome != outcomeEpoch || !"AwaitingResult".equals(state)) return;
                if (installed) finish("Succeeded", "已核实新版本安装成功，继续启动游戏。");
                else set("Interrupted", "上次安装已中断或结果尚未确认。请取消本次交接后重新校验安装包；不会自动安装。", -1);
            }
        });
    }

    public static void prepare(Activity activity, String path, String expectedVersion, String expectedSha, long expectedBytes) {
        synchronized (LOCK) {
            initialize(activity);
            if ("Interrupted".equals(state)) {
                ++generation;
                if (sessionId >= 0) abandon(sessionId);
                sessionId = -1; confirmation = null; revokeCallback(); clearRecord(); deletePrepared();
            }
            if (active() || sessionId >= 0) return;
            final long current = ++generation;
            deletePrepared(); confirmation = null;
            version = expectedVersion; sha = expectedSha; bytes = expectedBytes;
            set("Preparing", "正在重新校验 Android 安装包及签名…", 0);
            WORKER.execute(() -> prepareWorker(current, path, expectedVersion, expectedSha, expectedBytes));
        }
    }

    private static void prepareWorker(long current, String path, String expectedVersion, String expectedSha, long expectedBytes) {
        File copy = null;
        try {
            PackagePolicy.requireExpected(expectedVersion, expectedSha, expectedBytes);
            requireInteractiveInstaller();
            File source = ownedFile(path);
            if (!source.isFile() || source.length() != expectedBytes) throw new IOException("安装包缺失或长度已改变，请重新下载。");
            File directory = new File(app.getCacheDir().getCanonicalFile(), "client-package-installer");
            if (!directory.getCanonicalFile().equals(directory.getAbsoluteFile()))
                throw new IOException("私有安装缓存路径异常，已拒绝安装。");
            if (!directory.isDirectory() && !directory.mkdirs()) throw new IOException("无法创建私有安装缓存，请检查剩余空间。");
            // Only updater-owned snapshots. Never delete the downloaded source or game resources.
            File[] leftovers = directory.listFiles();
            if (leftovers != null) for (File old : leftovers) {
                if (old.getCanonicalFile().equals(old.getAbsoluteFile()) && old.isFile() && old.getName().startsWith("verified-") && old.getName().endsWith(".apk")) old.delete();
            }
            copy = File.createTempFile("verified-", ".apk", directory);
            try (InputStream input = new FileInputStream(source); FileOutputStream output = new FileOutputStream(copy)) {
                transfer(current, input, output, expectedBytes, expectedSha, true);
                output.getFD().sync();
            }
            PackageInfo candidate = validate(copy, expectedVersion);
            synchronized (LOCK) {
                checkCurrent(current);
                prepared = copy; copy = null;
                code = versionCode(candidate); signers = PackagePolicy.signerIdentity(currentSigners(candidate));
                set(permissionGranted() ? "Ready" : "PermissionRequired", permissionGranted()
                        ? "安装包和当前签名已核实，正在交给系统安装。"
                        : "需要您在系统设置中允许此来源安装应用。授权返回后自动继续。", 1);
            }
        } catch (Exception error) { fail(current, error); }
        finally { if (copy != null) copy.delete(); }
    }

    public static void begin(Activity activity) {
        synchronized (LOCK) {
            initialize(activity);
            if (!("Ready".equals(state) || "PermissionRequired".equals(state)) || prepared == null) return;
            if (!permissionGranted()) { set("PermissionRequired", "需要系统安装来源授权；是否允许由您决定。", 1); return; }
            final long current = generation;
            final File source = prepared;
            set("Staging", "正在校验并写入系统安装会话…", 0);
            WORKER.execute(() -> stage(current, source));
        }
    }

    private static void stage(long current, File source) {
        int created = -1;
        boolean committed = false;
        try {
            requireInteractiveInstaller();
            validate(source, version);
            PackageInstaller installer = app.getPackageManager().getPackageInstaller();
            PackageInstaller.SessionParams params = new PackageInstaller.SessionParams(PackageInstaller.SessionParams.MODE_FULL_INSTALL);
            params.setAppPackageName(app.getPackageName());
            params.setSize(bytes);
            // Request the documented ordinary self-update lane. Android decides eligibility
            // (target SDK, ownership and permission) and may still require user action.
            if (Build.VERSION.SDK_INT >= 31) params.setRequireUserAction(PackageInstaller.SessionParams.USER_ACTION_NOT_REQUIRED);
            if (Build.VERSION.SDK_INT >= 33) params.setPackageSource(PackageInstaller.PACKAGE_SOURCE_DOWNLOADED_FILE);
            created = installer.createSession(params);
            synchronized (LOCK) {
                checkCurrent(current); sessionId = created; token = "";
                // Session files survive process death. Persist its identity BEFORE any large write.
                if (!record()) throw new IOException("无法保存系统安装会话恢复记录，已取消交接。");
            }
            try (PackageInstaller.Session session = installer.openSession(created)) {
                try (InputStream input = new FileInputStream(source); OutputStream output = session.openWrite("base.apk", 0, bytes)) {
                    transfer(current, input, output, bytes, sha, true);
                    session.fsync(output);
                }
                // Verify the actual session contents too, not merely a declared digest or source path.
                try (InputStream staged = session.openRead("base.apk")) { transfer(current, staged, null, bytes, sha, false); }
                validate(source, version); // Includes a fresh installed version/signer comparison.
                requireInteractiveInstaller();
                synchronized (LOCK) {
                    checkCurrent(current);
                    if (!permissionGranted()) throw new IOException("安装权限已被关闭，请返回系统设置后重新校验安装包。");
                    token = UUID.randomUUID().toString();
                    Intent result = new Intent(app, InstallStatusReceiver.class).setAction(ACTION)
                            .setData(Uri.parse("ffo-install:" + token));
                    int flags = PendingIntent.FLAG_UPDATE_CURRENT;
                    if (Build.VERSION.SDK_INT >= 31) flags |= PendingIntent.FLAG_MUTABLE;
                    callback = PendingIntent.getBroadcast(app, created, result, flags);
                    if (!record()) throw new IOException("无法保存安装恢复记录，已取消交接。");
                    set("AwaitingResult", "系统正在检查安装请求；尚未确认安装成功。", -1);
                    session.commit(callback.getIntentSender());
                    committed = true;
                    deletePrepared();
                }
            }
        } catch (Exception error) { fail(current, error); }
        finally {
            if (!committed && created >= 0) {
                abandon(created);
                synchronized (LOCK) { if (generation == current) { revokeCallback(); clearRecord(); sessionId = -1; } }
            }
        }
    }

    public static String[] poll(Activity activity) {
        synchronized (LOCK) {
            initialize(activity);
            if ("PermissionRequired".equals(state) && prepared != null && permissionGranted())
                set("Ready", "系统安装权限已开启，将自动继续。", 1);
            else if ("Ready".equals(state) && !permissionGranted())
                set("PermissionRequired", "系统安装权限未开启，请由您在设置中决定是否允许。", 1);
            return new String[] { state, message, Float.toString(progress) };
        }
    }

    public static void openPermissionSettings(Activity activity) {
        synchronized (LOCK) {
            initialize(activity);
            if (!"PermissionRequired".equals(state)) return;
            final long current = generation;
            activity.runOnUiThread(() -> {
                synchronized (LOCK) {
                    if (current != generation || !"PermissionRequired".equals(state)) return;
                    try {
                        Intent intent = Build.VERSION.SDK_INT >= 26
                                ? new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + app.getPackageName()))
                                : new Intent(Settings.ACTION_SECURITY_SETTINGS);
                        activity.startActivity(intent);
                        message = "已打开系统设置。是否授权由您决定；授权返回后自动继续。";
                    } catch (Exception error) { message = "无法打开系统设置，请手动进入应用的安装未知应用权限页。"; }
                }
            });
        }
    }

    public static void openSystemConfirmation(Activity activity) {
        synchronized (LOCK) {
            initialize(activity);
            if (!"AwaitingConfirmation".equals(state) || confirmation == null) return;
            final long current = generation;
            final Intent intent = confirmation;
            set("AwaitingResult", "请在 Android 系统界面确认或取消安装；尚未确认安装成功。", -1);
            activity.runOnUiThread(() -> {
                synchronized (LOCK) {
                    if (current != generation || !"AwaitingResult".equals(state)) return;
                    try { activity.startActivity(intent); confirmation = null; }
                    catch (Exception error) {
                        confirmation = intent;
                        set("AwaitingConfirmation", "无法打开系统安装确认，请重试或取消此次交接。", -1);
                    }
                }
            });
        }
    }

    public static void cancel(Activity activity) {
        synchronized (LOCK) {
            initialize(activity);
            // Once system UI has been handed over, do not promise that an in-flight install stopped.
            if ("AwaitingResult".equals(state)) { message = "安装已交给系统处理。请在系统安装界面取消，或等待实际结果。"; return; }
            if ("Succeeded".equals(state) || "Failed".equals(state) || "Cancelled".equals(state)) return;
            ++generation;
            if (sessionId >= 0) abandon(sessionId);
            sessionId = -1; confirmation = null;
            revokeCallback(); clearRecord(); deletePrepared();
            set("Cancelled", "已取消本次安装交接，原下载文件仍保留。", -1);
        }
    }

    static void receive(Context context, Intent intent) {
        synchronized (LOCK) {
            initialize(context);
            if (intent == null || sessionId < 0 || token == null || !ACTION.equals(intent.getAction()) ||
                    !Uri.parse("ffo-install:" + token).equals(intent.getData()) ||
                    intent.getIntExtra(PackageInstaller.EXTRA_SESSION_ID, -1) != sessionId) return;
            int status = intent.getIntExtra(PackageInstaller.EXTRA_STATUS, Integer.MIN_VALUE);
            if (status < PackageInstaller.STATUS_PENDING_USER_ACTION) return;
            final long observedOutcome = ++outcomeEpoch;
            if (status == PackageInstaller.STATUS_PENDING_USER_ACTION) {
                confirmation = intent.getParcelableExtra(Intent.EXTRA_INTENT);
                if (confirmation == null) { finish("Failed", "系统未提供安装确认界面，请重新校验后重试。"); return; }
                set("AwaitingConfirmation", "需要 Android 系统安装确认，正在打开系统界面。", -1);
            } else if (status == PackageInstaller.STATUS_SUCCESS) {
                if (!app.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putBoolean("successObserved", true).commit()) {
                    finish("Failed", "无法保存系统安装结果，请重新启动并检查实际版本。"); return;
                }
                final long current = generation;
                set("AwaitingResult", "系统报告安装完成，正在核实已安装版本…", -1);
                WORKER.execute(() -> {
                    boolean installed = isExpectedInstalled();
                    synchronized (LOCK) {
                        if (current != generation || observedOutcome != outcomeEpoch) return;
                        if (installed) { relaunchOnce(); finish("Succeeded", "新版本已安装，已请求打开游戏；若系统阻止，请从桌面打开。"); }
                        else finish("Failed", "系统报告安装完成，但无法核实目标版本。请重新启动客户端检查，不要视为安装成功。");
                    }
                });
            } else if (status == PackageInstaller.STATUS_FAILURE_ABORTED) {
                finish("Cancelled", "系统安装已取消，原客户端及下载文件仍保留。");
            } else if (status >= PackageInstaller.STATUS_FAILURE) {
                Log.w("FFO.PackageInstaller", "System install rejected: " + status);
                String reason = status == PackageInstaller.STATUS_FAILURE_BLOCKED ? "系统权限或管理策略阻止安装"
                        : status == PackageInstaller.STATUS_FAILURE_INVALID ? "APK 无效或签名校验未通过"
                        : status == PackageInstaller.STATUS_FAILURE_CONFLICT ? "现有安装与 APK 冲突"
                        : status == PackageInstaller.STATUS_FAILURE_STORAGE ? "可用存储空间不足"
                        : status == PackageInstaller.STATUS_FAILURE_INCOMPATIBLE ? "APK 与设备不兼容" : "系统无法完成安装";
                finish("Failed", reason + "（状态 " + status + "）。未确认安装成功，请检查系统提示后重试。");
            }
        }
    }

    /** Called only by the non-exported receiver for Android's protected replacement broadcast. */
    static void replaced(Context context, Intent intent) {
        if (intent == null || !Intent.ACTION_MY_PACKAGE_REPLACED.equals(intent.getAction())) return;
        synchronized (LOCK) {
            initialize(context);
            if (sessionId < 0 || token == null || token.isEmpty()) return;
            if ("Failed".equals(state) || "Cancelled".equals(state)) return;
            // The protected broadcast is independent evidence of replacement, not a guessed
            // success from a missing session. Verify the recorded exact identity before launch.
            if (!isExpectedInstalled()) return;
            relaunchOnce();
            finish("Succeeded", "已核实系统替换为目标版本；已请求打开游戏。");
        }
    }

    private static void relaunchOnce() {
        // Separate from the session record so success cleanup/duplicate broadcasts cannot
        // relaunch again. Persist BEFORE attempting; never retry background launches in a loop.
        SharedPreferences launches = app.getSharedPreferences(PREFS + ".launch", Context.MODE_PRIVATE);
        if (token == null || token.isEmpty() || token.equals(launches.getString("token", ""))) return;
        if (!launches.edit().putString("token", token).commit()) return;
        try {
            Intent launch = app.getPackageManager().getLaunchIntentForPackage(app.getPackageName());
            if (launch == null) return;
            launch.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
            app.startActivity(launch);
        } catch (Exception error) {
            // Ordinary apps cannot guarantee background launches. No overlay, privileged
            // permission, alarm or accessibility workaround: the launcher remains recovery.
            Log.w("FFO.PackageInstaller", "Updated game launch blocked; open game from launcher", error);
        }
    }

    private static PackageInfo validate(File file, String expectedVersion) throws Exception {
        PackageManager manager = app.getPackageManager();
        int flags = Build.VERSION.SDK_INT >= 28 ? PackageManager.GET_SIGNING_CERTIFICATES : PackageManager.GET_SIGNATURES;
        PackageInfo installed = manager.getPackageInfo(app.getPackageName(), flags);
        PackageInfo candidate = manager.getPackageArchiveInfo(file.getAbsolutePath(), flags);
        if (candidate == null || installed == null) throw new IOException("无法解析或验证 APK 签名，请重新下载正确的完整安装包。");
        if (candidate.splitNames != null && candidate.splitNames.length > 0)
            throw new IOException("不支持分片安装包，请使用完整 APK。");
        PackagePolicy.requireUpgrade(app.getPackageName(), versionCode(installed), currentSigners(installed),
                candidate.packageName, candidate.versionName, versionCode(candidate), currentSigners(candidate), expectedVersion);
        return candidate;
    }

    @SuppressWarnings("deprecation")
    private static byte[][] currentSigners(PackageInfo info) {
        // Strict current signer identity is intentional: signing history / rotation is not accepted.
        Signature[] signatures = Build.VERSION.SDK_INT >= 28
                ? (info.signingInfo == null ? null : info.signingInfo.getApkContentsSigners()) : info.signatures;
        if (signatures == null) return null;
        byte[][] result = new byte[signatures.length][];
        for (int i = 0; i < signatures.length; i++) result[i] = signatures[i] == null ? null : signatures[i].toByteArray();
        return result;
    }

    @SuppressWarnings("deprecation")
    private static long versionCode(PackageInfo info) { return Build.VERSION.SDK_INT >= 28 ? info.getLongVersionCode() : info.versionCode; }

    @SuppressWarnings("deprecation")
    private static boolean permissionGranted() {
        try {
            return Build.VERSION.SDK_INT >= 26 ? app.getPackageManager().canRequestPackageInstalls()
                    : Settings.Secure.getInt(app.getContentResolver(), Settings.Secure.INSTALL_NON_MARKET_APPS, 0) == 1;
        } catch (Exception error) { return false; }
    }

    private static void requireInteractiveInstaller() throws IOException {
        DevicePolicyManager policy = (DevicePolicyManager) app.getSystemService(Context.DEVICE_POLICY_SERVICE);
        if (policy == null || policy.isDeviceOwnerApp(app.getPackageName()) || policy.isProfileOwnerApp(app.getPackageName()) ||
                app.getPackageManager().checkPermission(Manifest.permission.INSTALL_PACKAGES, app.getPackageName()) == PackageManager.PERMISSION_GRANTED)
            throw new IOException("此更新路径仅支持普通应用权限，不使用管理或系统权限安装。");
    }

    private static File ownedFile(String path) throws IOException {
        if (path == null || path.isEmpty()) throw new IOException("安装包路径为空。");
        File file = new File(path).getCanonicalFile();
        if (inside(file, app.getFilesDir()) || inside(file, app.getCacheDir())) return file;
        for (File root : app.getExternalFilesDirs(null)) if (root != null && inside(file, root)) return file;
        throw new IOException("安装包必须位于此客户端自己的下载目录中。");
    }

    private static boolean inside(File file, File directory) throws IOException {
        return file.getPath().startsWith(directory.getCanonicalPath() + File.separator);
    }

    private static void transfer(long current, InputStream input, OutputStream output, long expectedBytes,
            String expectedSha, boolean reportProgress) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        byte[] buffer = new byte[128 * 1024];
        long count = 0;
        int read;
        while ((read = input.read(buffer)) != -1) {
            synchronized (LOCK) { checkCurrent(current); }
            if (read == 0) continue;
            if (read > expectedBytes - count) throw new IOException("安装包超出发布长度，已拒绝安装。");
            count += read;
            digest.update(buffer, 0, read);
            if (output != null) output.write(buffer, 0, read);
            if (reportProgress) synchronized (LOCK) { if (current == generation) progress = (float)((double)count / expectedBytes); }
        }
        PackagePolicy.requireContent(count, digest.digest(), expectedBytes, expectedSha);
    }

    private static boolean isExpectedInstalled() {
        try {
            int flags = Build.VERSION.SDK_INT >= 28 ? PackageManager.GET_SIGNING_CERTIFICATES : PackageManager.GET_SIGNATURES;
            PackageInfo installed = app.getPackageManager().getPackageInfo(app.getPackageName(), flags);
            return code > 0 && version != null && version.equals(installed.versionName) && code == versionCode(installed) &&
                    signers != null && signers.equals(PackagePolicy.signerIdentity(currentSigners(installed)));
        } catch (Exception error) { return false; }
    }

    private static boolean record() {
        return app.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().clear().putInt("session", sessionId)
                .putString("version", version).putLong("code", code).putString("signers", signers).putString("token", token).commit();
    }
    private static void clearRecord() { if (app != null) app.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().clear().commit(); }
    private static void revokeCallback() { if (callback != null) { callback.cancel(); callback = null; } }
    private static void cleanupOrphanedSessions(Context owner, int retainedSession) {
        try {
            PackageInstaller installer = owner.getPackageManager().getPackageInstaller();
            for (PackageInstaller.SessionInfo session : installer.getMySessions()) {
                if (session.getSessionId() != retainedSession && owner.getPackageName().equals(session.getAppPackageName()))
                    installer.abandonSession(session.getSessionId());
            }
        } catch (Exception error) { Log.w("FFO.PackageInstaller", "Unable to reclaim interrupted own install sessions", error); }
    }
    private static void abandon(int id) { try { app.getPackageManager().getPackageInstaller().abandonSession(id); } catch (Exception ignored) { } }
    private static void deletePrepared() { if (prepared != null) { prepared.delete(); prepared = null; } }
    private static boolean active() {
        return "Preparing".equals(state) || "Staging".equals(state) || "AwaitingConfirmation".equals(state) || "AwaitingResult".equals(state);
    }
    private static void checkCurrent(long current) throws IOException { if (generation != current) throw new IOException("安装已取消。"); }
    private static void set(String next, String detail, float value) { state = next; message = detail; progress = value; }
    private static void finish(String next, String detail) {
        ++outcomeEpoch;
        // Keep a terminal failure record for a process restarted by the broadcast receiver.
        if ("Succeeded".equals(next)) clearRecord();
        else app.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString("terminal", next).commit();
        if (!"Succeeded".equals(next) && sessionId >= 0) abandon(sessionId);
        sessionId = -1; confirmation = null; revokeCallback(); deletePrepared();
        set(next, detail, "Succeeded".equals(next) ? 1 : -1);
    }
    private static void fail(long current, Exception error) {
        synchronized (LOCK) {
            if (current != generation) return;
            deletePrepared();
            Log.w("FFO.PackageInstaller", "Install handoff failed", error);
            String detail = error instanceof IOException ? error.getMessage() : "安装交接失败，请检查空间、系统安装权限及安装包后重试。";
            set("Failed", detail, -1);
        }
    }
}
