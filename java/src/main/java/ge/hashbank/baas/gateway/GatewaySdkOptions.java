package ge.hashbank.baas.gateway;

import java.net.URI;
import java.util.Objects;

public final class GatewaySdkOptions {
    private final URI baseAddress;
    private final String clientId;
    private final String productCode;
    private final String privateKeyPem;
    private final String publicKeyPem;
    private final String auditUserId;
    private final String auditSourceType;
    private final int maxSignatureLifetimeSeconds;
    private volatile String keyId;

    public GatewaySdkOptions(
            URI baseAddress,
            String clientId,
            String productCode,
            String privateKeyPem,
            String publicKeyPem,
            String auditUserId,
            String auditSourceType,
            int maxSignatureLifetimeSeconds) {
        this.baseAddress = Objects.requireNonNull(baseAddress, "baseAddress");
        this.clientId = requireText(clientId, "clientId");
        this.productCode = requireText(productCode, "productCode");
        this.privateKeyPem = requireText(privateKeyPem, "privateKeyPem");
        this.publicKeyPem = requireText(publicKeyPem, "publicKeyPem");
        this.auditUserId = requireText(auditUserId, "auditUserId");
        this.auditSourceType = requireText(auditSourceType, "auditSourceType");
        if (maxSignatureLifetimeSeconds <= 0) {
            throw new IllegalArgumentException("maxSignatureLifetimeSeconds must be positive");
        }
        this.maxSignatureLifetimeSeconds = maxSignatureLifetimeSeconds;
    }

    public URI baseAddress() {
        return baseAddress;
    }

    public String clientId() {
        return clientId;
    }

    public String productCode() {
        return productCode;
    }

    public String privateKeyPem() {
        return privateKeyPem;
    }

    public String publicKeyPem() {
        return publicKeyPem;
    }

    public String auditUserId() {
        return auditUserId;
    }

    public String auditSourceType() {
        return auditSourceType;
    }

    public int maxSignatureLifetimeSeconds() {
        return maxSignatureLifetimeSeconds;
    }

    public String keyId() {
        var resolved = keyId;
        if (resolved == null) {
            synchronized (this) {
                resolved = keyId;
                if (resolved == null) {
                    resolved = GatewayKeyGenerator.deriveKeyId(publicKeyPem);
                    keyId = resolved;
                }
            }
        }
        return resolved;
    }

    private static String requireText(String value, String name) {
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException(name + " is required");
        }
        return value;
    }
}
