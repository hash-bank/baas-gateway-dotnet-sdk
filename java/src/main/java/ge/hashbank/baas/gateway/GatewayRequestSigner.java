package ge.hashbank.baas.gateway;

import java.net.URI;
import java.net.http.HttpRequest;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.security.Signature;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Base64;
import java.util.LinkedHashMap;
import java.util.Locale;
import java.util.UUID;

public final class GatewayRequestSigner {
    private static final String SIGNATURE_LABEL = "sig1";
    private static final String ALGORITHM = "ed25519";
    private static final String TAG = "hash-baas-v1";
    private static final SecureRandom RANDOM = new SecureRandom();

    private GatewayRequestSigner() {
    }

    public static HttpRequest sign(String method, URI uri, byte[] body, String contentType, GatewaySdkOptions options) {
        var unsafe = isUnsafe(method, body);
        var requestBody = unsafe ? (body == null ? new byte[0] : body) : null;

        var builder = HttpRequest.newBuilder(uri)
                .header("Accept", "application/json")
                .header("X-Product-Code", options.productCode())
                .header("X-Client-Id", options.clientId())
                .header("X-Audit-Source-Type", options.auditSourceType())
                .header("X-Audit-User-Id", options.auditUserId());

        String contentDigest = null;
        String idempotencyKey = null;
        String effectiveContentType = contentType == null || contentType.isBlank()
                ? "application/json"
                : contentType;

        if (unsafe) {
            contentDigest = "sha-256=:" + Base64.getEncoder().encodeToString(sha256(requestBody)) + ":";
            idempotencyKey = UUID.randomUUID().toString();
            builder.header("Content-Digest", contentDigest)
                    .header("Content-Type", effectiveContentType)
                    .header("Idempotency-Key", idempotencyKey);
        }

        var components = buildComponents(unsafe);
        var created = Instant.now().getEpochSecond();
        var parameters = new LinkedHashMap<String, String>();
        parameters.put("created", Long.toString(created));
        parameters.put("expires", Long.toString(created + options.maxSignatureLifetimeSeconds()));
        parameters.put("keyid", options.keyId());
        parameters.put("nonce", nonce());
        parameters.put("alg", ALGORITHM);
        parameters.put("tag", TAG);

        var signatureBase = buildSignatureBase(method, uri, components, parameters, contentDigest, effectiveContentType, idempotencyKey, options);
        var signature = sign(signatureBase, options.privateKeyPem());
        builder.header("Signature-Input", SIGNATURE_LABEL + "=" + serializeSignatureParams(components, parameters))
                .header("Signature", SIGNATURE_LABEL + "=:" + Base64.getEncoder().encodeToString(signature) + ":");

        if (unsafe) {
            builder.method(method.toUpperCase(Locale.ROOT), HttpRequest.BodyPublishers.ofByteArray(requestBody));
        } else {
            builder.method(method.toUpperCase(Locale.ROOT), HttpRequest.BodyPublishers.noBody());
        }

        return builder.build();
    }

    private static boolean isUnsafe(String method, byte[] body) {
        var upper = method.toUpperCase(Locale.ROOT);
        return upper.equals("POST")
                || upper.equals("PUT")
                || upper.equals("PATCH")
                || (upper.equals("DELETE") && body != null);
    }

    private static ArrayList<String> buildComponents(boolean unsafe) {
        var components = new ArrayList<String>();
        components.add("@method");
        components.add("@target-uri");
        components.add("@authority");
        if (unsafe) {
            components.add("content-digest");
            components.add("content-type");
        }
        components.add("x-product-code");
        components.add("x-client-id");
        components.add("x-audit-source-type");
        components.add("x-audit-user-id");
        if (unsafe) {
            components.add("idempotency-key");
        }
        return components;
    }

    private static String buildSignatureBase(
            String method,
            URI uri,
            ArrayList<String> components,
            LinkedHashMap<String, String> parameters,
            String contentDigest,
            String contentType,
            String idempotencyKey,
            GatewaySdkOptions options) {
        var base = new StringBuilder();
        for (var component : components) {
            base.append('"').append(component).append("\": ")
                    .append(resolve(component, method, uri, contentDigest, contentType, idempotencyKey, options))
                    .append('\n');
        }
        base.append("\"@signature-params\": ").append(serializeSignatureParams(components, parameters));
        return base.toString();
    }

    private static String resolve(
            String component,
            String method,
            URI uri,
            String contentDigest,
            String contentType,
            String idempotencyKey,
            GatewaySdkOptions options) {
        return switch (component) {
            case "@method" -> method.toUpperCase(Locale.ROOT);
            case "@target-uri" -> uri.toString();
            case "@authority" -> authority(uri);
            case "content-digest" -> contentDigest;
            case "content-type" -> contentType;
            case "x-product-code" -> options.productCode();
            case "x-client-id" -> options.clientId();
            case "x-audit-source-type" -> options.auditSourceType();
            case "x-audit-user-id" -> options.auditUserId();
            case "idempotency-key" -> idempotencyKey;
            default -> "";
        };
    }

    private static String authority(URI uri) {
        var host = uri.getHost().toLowerCase(Locale.ROOT);
        var port = uri.getPort();
        var scheme = uri.getScheme();
        if (port == -1 || (scheme.equals("https") && port == 443) || (scheme.equals("http") && port == 80)) {
            return host;
        }
        return host + ":" + port;
    }

    private static String serializeSignatureParams(ArrayList<String> components, LinkedHashMap<String, String> parameters) {
        var result = new StringBuilder("(");
        for (var i = 0; i < components.size(); i++) {
            if (i > 0) {
                result.append(' ');
            }
            result.append('"').append(components.get(i)).append('"');
        }
        result.append(')');
        for (var entry : parameters.entrySet()) {
            result.append(';').append(entry.getKey()).append('=');
            if (entry.getKey().equals("created") || entry.getKey().equals("expires")) {
                result.append(entry.getValue());
            } else {
                result.append('"').append(entry.getValue()).append('"');
            }
        }
        return result.toString();
    }

    private static byte[] sign(String signatureBase, String privateKeyPem) {
        try {
            var signer = Signature.getInstance("Ed25519");
            signer.initSign(GatewayKeyGenerator.importPrivateKey(privateKeyPem));
            signer.update(signatureBase.getBytes(StandardCharsets.UTF_8));
            return signer.sign();
        } catch (Exception ex) {
            throw new IllegalStateException("Failed to sign Gateway request", ex);
        }
    }

    private static byte[] sha256(byte[] value) {
        try {
            return MessageDigest.getInstance("SHA-256").digest(value);
        } catch (Exception ex) {
            throw new IllegalStateException("SHA-256 is not available", ex);
        }
    }

    private static String nonce() {
        var bytes = new byte[16];
        RANDOM.nextBytes(bytes);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }
}
