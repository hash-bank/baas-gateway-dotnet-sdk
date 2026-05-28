package ge.hashbank.baas.gateway;

import java.security.KeyFactory;
import java.security.KeyPairGenerator;
import java.security.MessageDigest;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.Signature;
import java.security.spec.PKCS8EncodedKeySpec;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;
import java.util.HexFormat;

public final class GatewayKeyGenerator {
    private GatewayKeyGenerator() {
    }

    public static GatewayKeyPair generate() {
        try {
            var generator = KeyPairGenerator.getInstance("Ed25519");
            var keyPair = generator.generateKeyPair();
            var privateKeyPem = toPem("PRIVATE KEY", keyPair.getPrivate().getEncoded());
            var publicKeyPem = toPem("PUBLIC KEY", keyPair.getPublic().getEncoded());
            return new GatewayKeyPair(privateKeyPem, publicKeyPem, deriveKeyId(publicKeyPem));
        } catch (Exception ex) {
            throw new IllegalStateException("Failed to generate Ed25519 key pair", ex);
        }
    }

    public static String deriveKeyId(String publicKeyPem) {
        try {
            var der = parsePem(publicKeyPem, "PUBLIC KEY");
            var digest = MessageDigest.getInstance("SHA-256").digest(der);
            return HexFormat.of().formatHex(digest);
        } catch (Exception ex) {
            throw new IllegalArgumentException("Failed to derive key ID from public key PEM", ex);
        }
    }

    public static PrivateKey importPrivateKey(String privateKeyPem) {
        try {
            var der = parsePem(privateKeyPem, "PRIVATE KEY");
            return KeyFactory.getInstance("Ed25519").generatePrivate(new PKCS8EncodedKeySpec(der));
        } catch (Exception ex) {
            throw new IllegalArgumentException("Invalid Ed25519 PKCS#8 private key PEM", ex);
        }
    }

    public static PublicKey importPublicKey(String publicKeyPem) {
        try {
            var der = parsePem(publicKeyPem, "PUBLIC KEY");
            return KeyFactory.getInstance("Ed25519").generatePublic(new X509EncodedKeySpec(der));
        } catch (Exception ex) {
            throw new IllegalArgumentException("Invalid Ed25519 SPKI public key PEM", ex);
        }
    }

    public static void validateKeyPair(String privateKeyPem, String publicKeyPem) {
        try {
            var payload = "hash-baas-gateway-sdk-key-validation".getBytes(java.nio.charset.StandardCharsets.UTF_8);
            var signer = Signature.getInstance("Ed25519");
            signer.initSign(importPrivateKey(privateKeyPem));
            signer.update(payload);
            var signature = signer.sign();

            var verifier = Signature.getInstance("Ed25519");
            verifier.initVerify(importPublicKey(publicKeyPem));
            verifier.update(payload);
            if (!verifier.verify(signature)) {
                throw new IllegalArgumentException("Private key does not match public key");
            }
        } catch (IllegalArgumentException ex) {
            throw ex;
        } catch (Exception ex) {
            throw new IllegalArgumentException("Failed to validate key pair", ex);
        }
    }

    static byte[] parsePem(String pem, String label) {
        if (pem == null || pem.isBlank()) {
            throw new IllegalArgumentException(label + " PEM is required");
        }
        var base64 = pem
                .replace("-----BEGIN " + label + "-----", "")
                .replace("-----END " + label + "-----", "")
                .replaceAll("\\s", "");
        return Base64.getDecoder().decode(base64);
    }

    private static String toPem(String label, byte[] der) {
        var base64 = Base64.getMimeEncoder(64, "\n".getBytes(java.nio.charset.StandardCharsets.US_ASCII)).encodeToString(der);
        return "-----BEGIN " + label + "-----\n" + base64 + "\n-----END " + label + "-----";
    }
}
