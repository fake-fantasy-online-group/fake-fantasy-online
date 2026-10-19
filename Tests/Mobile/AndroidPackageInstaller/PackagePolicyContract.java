package org.fakefantasyonline.updates;

import java.io.IOException;
import java.security.MessageDigest;

public final class PackagePolicyContract {
    private static int assertions;
    interface Checked { void run() throws Exception; }
    static void reject(Checked action, String message) throws Exception {
        try { action.run(); throw new AssertionError("accepted " + message); }
        catch (IOException expected) { assertions++; }
    }
    static void equal(Object a, Object b) { if (!a.equals(b)) throw new AssertionError(a + " != " + b); assertions++; }
    public static void main(String[] ignored) throws Exception {
        byte[][] first = {{1, 2}, {3, 4}}, reordered = {{3, 4}, {1, 2}}, different = {{1, 2}, {5, 6}};
        String hash = PackagePolicy.hex(MessageDigest.getInstance("SHA-256").digest(new byte[] {7, 8, 9}));
        PackagePolicy.requireExpected("1.2.0", hash, 3);
        PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.2.0", 8, reordered, "1.2.0");
        equal(PackagePolicy.signerIdentity(first), PackagePolicy.signerIdentity(reordered));
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "other", "1.2.0", 8, first, "1.2.0"), "wrong package");
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.1.0", 8, first, "1.2.0"), "wrong versionName");
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.2.0", 7, first, "1.2.0"), "same versionCode");
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.2.0", 6, first, "1.2.0"), "downgrade");
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.2.0", 8, different, "1.2.0"), "partial signer overlap");
        reject(() -> PackagePolicy.requireUpgrade("installed.self", 7, first, "installed.self", "1.2.0", 8, new byte[][] {{1, 2}}, "1.2.0"), "missing signer");
        reject(() -> PackagePolicy.signerIdentity(null), "unknown signers");
        reject(() -> PackagePolicy.signerIdentity(new byte[0][]), "empty signers");
        reject(() -> PackagePolicy.signerIdentity(new byte[][] {null}), "null signer");
        reject(() -> PackagePolicy.signerIdentity(new byte[][] {{}}), "empty signer");
        reject(() -> PackagePolicy.signerIdentity(new byte[][] {{1}, {1}}), "duplicate signer");
        reject(() -> PackagePolicy.requireExpected("1.2.0", "declared-hash", 3), "invalid digest");
        reject(() -> PackagePolicy.requireExpected("1.2.0", hash, -1), "invalid bytes");
        reject(() -> PackagePolicy.requireExpected("", hash, 3), "empty expected version");
        byte[] digest = MessageDigest.getInstance("SHA-256").digest(new byte[] {7, 8, 9});
        PackagePolicy.requireContent(3, digest, 3, hash.toUpperCase());
        reject(() -> PackagePolicy.requireContent(2, digest, 3, hash), "truncated bytes");
        reject(() -> PackagePolicy.requireContent(4, digest, 3, hash), "extra bytes");
        reject(() -> PackagePolicy.requireContent(3, new byte[32], 3, hash), "wrong bytes with claimed length");
        System.out.println("PASS " + assertions + " pure Java package trust/content assertions (synthetic identities, no device/API execution).");
    }
}
