using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Generates Ed25519 key pairs and derives HashBank Gateway key IDs.
/// </summary>
public static class GatewayKeyGenerator
{
    /// <summary>
    /// Generates a new Ed25519 key pair and derives the key ID from the public key.
    /// </summary>
    public static GatewayKeyPair Generate()
    {
        var creationParameters = new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        };

        using var key = Key.Create(SignatureAlgorithm.Ed25519, creationParameters);

        var privateKeyPem = Encoding.UTF8.GetString(key.Export(KeyBlobFormat.PkixPrivateKeyText));
        var publicKeyPem = Encoding.UTF8.GetString(key.PublicKey.Export(KeyBlobFormat.PkixPublicKeyText));
        var keyId = DeriveKeyId(publicKeyPem);

        return new GatewayKeyPair(privateKeyPem, publicKeyPem, keyId);
    }

    /// <summary>
    /// Derives the Gateway key ID as lowercase hex SHA-256(SPKI DER) from an SPKI public key PEM.
    /// </summary>
    public static string DeriveKeyId(string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("Public key PEM is required.", nameof(publicKeyPem));

        var base64 = publicKeyPem
            .Replace("-----BEGIN PUBLIC KEY-----", string.Empty, StringComparison.Ordinal)
            .Replace("-----END PUBLIC KEY-----", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        var spkiDer = Convert.FromBase64String(base64);
        return Convert.ToHexString(SHA256.HashData(spkiDer)).ToLowerInvariant();
    }

    /// <summary>
    /// Validates that a PKCS#8 private key PEM can be imported for Ed25519 signing.
    /// </summary>
    public static void ValidatePrivateKey(string privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            throw new ArgumentException("Private key PEM is required.", nameof(privateKeyPem));

        using var _ = Key.Import(
            SignatureAlgorithm.Ed25519,
            Encoding.UTF8.GetBytes(privateKeyPem),
            KeyBlobFormat.PkixPrivateKeyText);
    }

    /// <summary>
    /// Validates that the private key can sign and the public key can verify the same payload.
    /// </summary>
    public static void ValidateKeyPair(string privateKeyPem, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new ArgumentException("Public key PEM is required.", nameof(publicKeyPem));

        var payload = Encoding.UTF8.GetBytes("hash-baas-gateway-sdk-key-validation");

        using var privateKey = Key.Import(
            SignatureAlgorithm.Ed25519,
            Encoding.UTF8.GetBytes(privateKeyPem),
            KeyBlobFormat.PkixPrivateKeyText);

        var publicKey = PublicKey.Import(
            SignatureAlgorithm.Ed25519,
            Encoding.UTF8.GetBytes(publicKeyPem),
            KeyBlobFormat.PkixPublicKeyText);

        var signature = SignatureAlgorithm.Ed25519.Sign(privateKey, payload);
        if (!SignatureAlgorithm.Ed25519.Verify(publicKey, payload, signature))
            throw new ArgumentException("PrivateKeyPem does not match PublicKeyPem.");
    }
}
