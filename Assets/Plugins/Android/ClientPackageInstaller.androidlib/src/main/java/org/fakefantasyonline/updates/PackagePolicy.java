package org.fakefantasyonline.updates;

import java.io.IOException;
import java.util.Set;
import java.util.TreeSet;

/** Pure policy, independently exercised without pretending Android parsed a real APK. */
final class PackagePolicy {
    private PackagePolicy() { }

    static void requireExpected(String version, String sha256, long bytes) throws IOException {
        if (version == null || version.isEmpty() || version.length() > 128 ||
                sha256 == null || !sha256.matches("[a-fA-F0-9]{64}") || bytes <= 0)
            throw new IOException("发布安装包的版本、长度或 SHA-256 无效。");
    }

    static void requireUpgrade(String installedPackage, long installedCode, byte[][] installedSigners,
            String incomingPackage, String incomingVersion, long incomingCode, byte[][] incomingSigners,
            String expectedVersion) throws IOException {
        if (installedPackage == null || installedPackage.isEmpty() || !installedPackage.equals(incomingPackage))
            throw new IOException("安装包包名与当前客户端不一致，已拒绝安装。");
        if (expectedVersion == null || !expectedVersion.equals(incomingVersion))
            throw new IOException("安装包版本名与发布版本不一致，已拒绝安装。");
        if (installedCode <= 0 || incomingCode <= installedCode)
            throw new IOException("安装包版本码没有严格递增，已拒绝同版或降级安装。");
        if (!signerIdentity(installedSigners).equals(signerIdentity(incomingSigners)))
            throw new IOException("安装包当前签名证书集合与已安装客户端不一致，已拒绝安装。");
    }

    static String signerIdentity(byte[][] signers) throws IOException {
        if (signers == null || signers.length == 0) throw new IOException("无法确认安装包签名，已拒绝安装。");
        Set<String> certificates = new TreeSet<>();
        for (byte[] signer : signers) {
            if (signer == null || signer.length == 0 || !certificates.add(hex(signer)))
                throw new IOException("安装包签名证书缺失或异常，已拒绝安装。");
        }
        // Full DER bytes, order independent, not release-manifest certificate declarations.
        StringBuilder identity = new StringBuilder();
        for (String certificate : certificates) {
            if (identity.length() > 0) identity.append(':');
            identity.append(certificate);
        }
        return identity.toString();
    }

    static void requireContent(long bytes, byte[] digest, long expectedBytes, String expectedSha) throws IOException {
        if (bytes != expectedBytes || !hex(digest).equalsIgnoreCase(expectedSha))
            throw new IOException("安装包长度或 SHA-256 校验失败，已拒绝安装。");
    }

    static String hex(byte[] bytes) {
        StringBuilder out = new StringBuilder(bytes.length * 2);
        for (byte value : bytes) {
            out.append(Character.forDigit((value >>> 4) & 15, 16));
            out.append(Character.forDigit(value & 15, 16));
        }
        return out.toString();
    }
}
