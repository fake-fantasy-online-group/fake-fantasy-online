package org.fakefantasyonline.updates;

import android.app.Activity;
import android.app.PendingIntent;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.*;
import android.os.Build;
import java.io.File;
import java.lang.reflect.Field;
import java.nio.file.Files;
import java.security.MessageDigest;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;

/** Executes production state-machine code with explicit Android test doubles, not real APKs. */
public final class InstallerLifecycleContract {
    private static int assertions;
    private static final byte[] CONTENT={1,4,9,16};
    private static String hash;
    private static Object field(String name) throws Exception { Field f=ClientPackageInstaller.class.getDeclaredField(name);f.setAccessible(true);return f.get(null); }
    private static void field(String name,Object value) throws Exception { Field f=ClientPackageInstaller.class.getDeclaredField(name);f.setAccessible(true);f.set(null,value); }
    private static void check(boolean ok,String message) { if(!ok)throw new AssertionError(message);assertions++; }
    private static PackageInfo info(String name,int code,byte signer) {
        PackageInfo p=new PackageInfo(); p.packageName="installed.self";p.versionName=name;p.versionCode=code;p.longCode=code;
        p.signatures=new Signature[]{new Signature(new byte[]{signer})};p.signingInfo=new SigningInfo(p.signatures);return p;
    }
    private static Activity fresh() throws Exception {
        field("generation",(Long)field("generation")+1);field("app",null);field("sessionId",-1);field("prepared",null);
        field("state","Idle");field("confirmation",null);field("callback",null);
        Build.VERSION.SDK_INT=35;
        Activity a=new Activity();a.root=Files.createTempDirectory("ffo-install-contract").toFile();
        a.manager.installed=info("1.0.0",1,(byte)7);a.manager.candidate=info("1.1.0",2,(byte)7);return a;
    }
    private static File source(Activity a) throws Exception { File f=new File(a.getFilesDir(),"update.apk");Files.write(f.toPath(),CONTENT);return f; }
    private static void prepare(Activity a) throws Exception { ClientPackageInstaller.prepare(a,source(a).getPath(),"1.1.0",hash,CONTENT.length); }
    private static String state(Activity a){return ClientPackageInstaller.poll(a)[0];}
    private static void await(Activity a,String expected) throws Exception {
        long end=System.nanoTime()+5_000_000_000L;
        while(!state(a).equals(expected)&&System.nanoTime()<end)Thread.sleep(2);
        check(state(a).equals(expected),"expected "+expected+" got "+state(a)+": "+ClientPackageInstaller.poll(a)[1]);
    }
    private static Intent status(int status) throws Exception {
        Intent base=PendingIntent.last.intent;
        return new Intent().setAction(base.getAction()).setData(base.getData())
            .putExtra(PackageInstaller.EXTRA_SESSION_ID,(Integer)field("sessionId"))
            .putExtra(PackageInstaller.EXTRA_STATUS,status);
    }
    private static void ready(Activity a)throws Exception{prepare(a);await(a,"Ready");}
    private static void committed(Activity a)throws Exception{ready(a);ClientPackageInstaller.begin(a);await(a,"AwaitingResult");}
    private static CountDownLatch blockWorker()throws Exception{
        CountDownLatch entered=new CountDownLatch(1),release=new CountDownLatch(1);
        ((ExecutorService)field("WORKER")).execute(()->{entered.countDown();try{release.await();}catch(InterruptedException e){throw new RuntimeException(e);}});
        entered.await();return release;
    }
    public static void main(String[] args)throws Exception{
        try { run(); System.out.println("PASS "+assertions+" synthetic Android lifecycle assertions (production Java, fake OS; no APK/device)."); }
        finally { ((ExecutorService)field("WORKER")).shutdownNow(); }
    }
    private static void run()throws Exception{
        hash=PackagePolicy.hex(MessageDigest.getInstance("SHA-256").digest(CONTENT));
        Activity a=fresh();a.manager.permission=false;prepare(a);await(a,"PermissionRequired");
        ClientPackageInstaller.begin(a);check(a.manager.installer.creates==0,"no session without permission");
        ClientPackageInstaller.openPermissionSettings(a);check(a.started.size()==1,"explicit settings handoff");
        check(!a.manager.permission&&state(a).equals("PermissionRequired"),"settings launch does not grant permission");
        a.manager.permission=true;await(a,"Ready");check(a.manager.installer.creates==0,"permission return does not begin installation");
        ClientPackageInstaller.begin(a);await(a,"AwaitingResult");check(a.manager.installer.commits==1,"one commit");
        ClientPackageInstaller.cancel(a);check(state(a).equals("AwaitingResult"),"cancel never overwrites a submitted result");
        ClientPackageInstaller.begin(a);check(a.manager.installer.commits==1,"duplicate begin ignored");
        check(a.manager.installer.lastParams.action==PackageInstaller.SessionParams.USER_ACTION_REQUIRED,"explicit confirmation required");
        check((PendingIntent.last.flags&PendingIntent.FLAG_MUTABLE)!=0,"mutable callback on API35");
        Intent forged=status(PackageInstaller.STATUS_PENDING_USER_ACTION).putExtra(PackageInstaller.EXTRA_SESSION_ID,999);
        ClientPackageInstaller.receive(a,forged);check(state(a).equals("AwaitingResult"),"wrong session callback rejected");
        ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_PENDING_USER_ACTION).putExtra(Intent.EXTRA_INTENT,new Intent("system-confirm")));
        check(state(a).equals("AwaitingConfirmation")&&a.started.size()==1,"receiver never launches system UI");
        ClientPackageInstaller.openSystemConfirmation(a);check(state(a).equals("AwaitingResult")&&a.started.size()==2,"explicit system confirmation launch");
        ClientPackageInstaller.cancel(a);check(state(a).equals("AwaitingResult"),"no false cancel after handing to system");
        ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_FAILURE_ABORTED));await(a,"Cancelled");
        check(PendingIntent.last.cancelled,"terminal callback revoked");
        a=fresh();File cache=new File(a.getCacheDir(),"client-package-installer");cache.mkdirs();
        File leftover=new File(cache,"verified-leftover.apk"),unrelated=new File(cache,"keep.txt");
        Files.write(leftover.toPath(),CONTENT);Files.write(unrelated.toPath(),CONTENT);
        File target=new File(a.getFilesDir(),"keep-target.apk");Files.write(target.toPath(),CONTENT);
        File link=new File(cache,"verified-link.apk");Files.createSymbolicLink(link.toPath(),target.toPath());
        ready(a);check(!leftover.exists()&&unrelated.exists()&&Files.isSymbolicLink(link.toPath())&&target.exists(),"only ordinary updater snapshots cleaned, no symlink following");
        ClientPackageInstaller.cancel(a);check(state(a).equals("Cancelled"),"cancel ready");
        check(source(a).isFile(),"download source retained");
        a=fresh();CountDownLatch release=blockWorker();prepare(a);ClientPackageInstaller.cancel(a);release.countDown();Thread.sleep(30);
        check(state(a).equals("Cancelled")&&a.manager.installer.creates==0,"cancel preparing cannot revive operation");
        a=fresh();ready(a);release=blockWorker();ClientPackageInstaller.begin(a);ClientPackageInstaller.cancel(a);release.countDown();Thread.sleep(30);
        check(state(a).equals("Cancelled")&&a.manager.installer.commits==0,"cancel staging never commits");
        for(int bad=0;bad<5;bad++){
            a=fresh();if(bad==0)a.manager.candidate.packageName="other";if(bad==1)a.manager.candidate=info("1.1.0",2,(byte)8);
            if(bad==2){a.manager.candidate.versionCode=1;a.manager.candidate.longCode=1;}
            if(bad==3){a.manager.candidate.versionCode=0;a.manager.candidate.longCode=0;}
            if(bad==4){a.manager.candidate.signingInfo=null;}
            prepare(a);await(a,"Failed");check(a.manager.installer.commits==0,"bad metadata never commits "+bad);
        }
        a=fresh();ready(a);a.manager.permission=false;ClientPackageInstaller.begin(a);await(a,"PermissionRequired");
        check(a.manager.installer.creates==0,"revoked permission checked again before begin");
        a=fresh();ready(a);Files.write(((File)field("prepared")).toPath(),new byte[]{9,9,9,9});ClientPackageInstaller.begin(a);await(a,"Failed");
        check(a.manager.installer.commits==0,"private cache tamper rehashed");
        a=fresh();a.manager.installer.writeFails=true;ready(a);ClientPackageInstaller.begin(a);await(a,"Failed");
        check(a.manager.installer.commits==0,"storage failure never commits");
        a=fresh();a.prefs.writeSucceeds=false;ready(a);ClientPackageInstaller.begin(a);await(a,"Failed");
        check(a.manager.installer.commits==0,"recovery record failure never commits");
        a=fresh();a.manager.installer.corruptRead=true;ready(a);ClientPackageInstaller.begin(a);await(a,"Failed");
        check(a.manager.installer.commits==0,"session digest checked before commit");
        a=fresh();a.policy.owner=true;prepare(a);await(a,"Failed");check(a.manager.installer.creates==0,"device owner rejected");
        a=fresh();a.manager.privileged=true;prepare(a);await(a,"Failed");check(a.manager.installer.creates==0,"privileged silent installer rejected");
        a=fresh();Build.VERSION.SDK_INT=25;a.resolver.allowed=0;prepare(a);await(a,"PermissionRequired");
        ClientPackageInstaller.openPermissionSettings(a);check(a.started.get(0).getAction().equals("security"),"legacy permission handoff");
        a.resolver.allowed=1;await(a,"Ready");ClientPackageInstaller.begin(a);await(a,"AwaitingResult");
        check((PendingIntent.last.flags&PendingIntent.FLAG_MUTABLE)==0,"legacy callback flags");
        a=fresh();ready(a);final Activity stagingOwner=a;
        a.manager.installer.beforeWrite=()->check(stagingOwner.prefs.getInt("session",-1)>0,"session ID persisted before large write");
        ClientPackageInstaller.begin(a);await(a,"AwaitingResult");
        a=fresh();ready(a);final Activity crashOwner=a;
        final SharedPreferences[] savedPrefs=new SharedPreferences[1];final PackageInstaller[] savedSessions=new PackageInstaller[1];
        a.manager.installer.beforeWrite=()->{savedPrefs[0]=crashOwner.prefs.snapshot();savedSessions[0]=crashOwner.manager.installer.snapshot();throw new IllegalStateException("stop at durable checkpoint");};
        ClientPackageInstaller.begin(a);await(a,"Failed");
        check(savedPrefs[0].getInt("session",-1)>0,"staging checkpoint includes durable session ID");
        // Restore ONLY the persisted checkpoint to model a process lost before cleanup/callback.
        a.prefs=savedPrefs[0];a.manager.installer=savedSessions[0];
        field("app",null);field("state","Idle");await(a,"Interrupted");
        ready(a);check(a.manager.installer.abandons>0,"restart abandons interrupted staging session");
        a=fresh();PackageInstaller.SessionParams orphan=new PackageInstaller.SessionParams(1);orphan.setAppPackageName(a.getPackageName());
        a.manager.installer.createSession(orphan);ready(a);check(a.manager.installer.abandons==1,"unrecorded own orphan reclaimed before retry");
        a=fresh();committed(a);a.prefs.edit().putString("terminal","Failed").commit();
        field("app",null);field("state","Idle");await(a,"Failed");
        check(a.manager.installer.sessions.isEmpty(),"restart recovers death between terminal record and session abandonment");
        a=fresh();committed(a);ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_SUCCESS));await(a,"Failed");
        check(!state(a).equals("Succeeded"),"success callback alone insufficient");
        a=fresh();committed(a);a.manager.installed=a.manager.candidate;
        field("app",null);field("state","Idle");await(a,"Interrupted");check(!state(a).equals("Succeeded"),"restart without success callback never infers success");
        a.manager.installed=info("1.0.0",1,(byte)7);
        ready(a);check(a.manager.installer.abandons>0,"explicit retry abandons interrupted session");
        a=fresh();committed(a);a.manager.installed=a.manager.candidate;ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_SUCCESS));await(a,"Succeeded");ClientPackageInstaller.cancel(a);check(state(a).equals("Succeeded"),"destruction cancel preserves terminal success");
        a=fresh();committed(a);release=blockWorker();a.manager.installed=a.manager.candidate;
        ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_SUCCESS));
        check(a.prefs.getBoolean("successObserved",false),"success observation durable before async work");
        ClientPackageInstaller.receive(a,status(PackageInstaller.STATUS_FAILURE_BLOCKED));release.countDown();Thread.sleep(30);
        check(state(a).equals("Failed"),"late success verifier cannot overwrite newer terminal failure");
    }
}
