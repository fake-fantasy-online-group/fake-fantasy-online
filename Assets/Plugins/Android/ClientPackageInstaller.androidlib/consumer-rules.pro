# Unity reaches this entry point through JNI; preserve names under release minification.
-keep class org.fakefantasyonline.updates.ClientPackageInstaller { public static *; }
-keep class org.fakefantasyonline.updates.InstallStatusReceiver { public *; }
-keep class org.fakefantasyonline.updates.PackageReplacedReceiver { public *; }
