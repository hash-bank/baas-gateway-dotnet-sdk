namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Ed25519 key material used by HashBank Gateway request signing.
/// </summary>
public sealed record GatewayKeyPair(
    string PrivateKeyPem,
    string PublicKeyPem,
    string KeyId);
