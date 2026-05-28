namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Configuration required to sign Hash BaaS Gateway requests.
/// </summary>
public sealed class GatewaySdkOptions
{
    private readonly object _keyIdLock = new();
    private string? _resolvedKeyId;

    /// <summary>
    /// Gateway base address, for example https://baasgateway-dev.services.hashbank.ge/.
    /// </summary>
    public Uri? BaseAddress { get; init; }

    /// <summary>
    /// Developer Portal client ID sent as X-Client-Id.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Product definition code sent as X-Product-Code, for example NOVA_CARD.
    /// </summary>
    public required string ProductCode { get; init; }

    /// <summary>
    /// Ed25519 PKCS#8 private key PEM used to sign requests.
    /// </summary>
    public required string PrivateKeyPem { get; init; }

    /// <summary>
    /// Ed25519 SPKI public key PEM registered in Developer Portal. Used to derive the request key ID.
    /// </summary>
    public required string PublicKeyPem { get; init; }

    /// <summary>
    /// Optional precomputed key ID. Leave null for normal usage; the SDK derives it from PublicKeyPem.
    /// </summary>
    public string? KeyId { get; init; }

    /// <summary>
    /// Audit source sent as X-Audit-Source-Type.
    /// </summary>
    public string AuditSourceType { get; init; } = "Backend";

    /// <summary>
    /// Audit actor sent as X-Audit-User-Id.
    /// </summary>
    public string AuditUserId { get; init; } = "gateway-sdk";

    /// <summary>
    /// Request signature lifetime in seconds.
    /// </summary>
    public int MaxSignatureLifetimeSeconds { get; init; } = 300;

    /// <summary>
    /// Returns the configured key ID or derives it from PublicKeyPem.
    /// </summary>
    public string GetKeyId()
    {
        if (!string.IsNullOrWhiteSpace(_resolvedKeyId))
            return _resolvedKeyId;

        lock (_keyIdLock)
        {
            _resolvedKeyId ??= string.IsNullOrWhiteSpace(KeyId)
                ? GatewayKeyGenerator.DeriveKeyId(PublicKeyPem)
                : KeyId;

            return _resolvedKeyId;
        }
    }

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Gateway ClientId is required.");
        if (string.IsNullOrWhiteSpace(ProductCode))
            throw new InvalidOperationException("Gateway ProductCode is required.");
        if (string.IsNullOrWhiteSpace(PrivateKeyPem))
            throw new InvalidOperationException("Gateway PrivateKeyPem is required.");
        if (string.IsNullOrWhiteSpace(PublicKeyPem) && string.IsNullOrWhiteSpace(KeyId))
            throw new InvalidOperationException("Gateway PublicKeyPem is required unless KeyId is explicitly supplied.");
        if (string.IsNullOrWhiteSpace(AuditSourceType))
            throw new InvalidOperationException("Gateway AuditSourceType is required.");
        if (string.IsNullOrWhiteSpace(AuditUserId))
            throw new InvalidOperationException("Gateway AuditUserId is required.");
        if (MaxSignatureLifetimeSeconds <= 0)
            throw new InvalidOperationException("Gateway MaxSignatureLifetimeSeconds must be positive.");
    }
}
