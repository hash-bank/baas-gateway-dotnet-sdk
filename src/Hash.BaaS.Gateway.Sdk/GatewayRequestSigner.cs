using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Signs outbound HTTP requests using the Hash BaaS Gateway RFC 9421 profile.
/// </summary>
public static class GatewayRequestSigner
{
    private const string Algorithm = "ed25519";
    private const string Tag = "hash-baas-v1";
    private const string SignatureLabel = "sig1";

    /// <summary>
    /// Mutates <paramref name="request"/> by adding required Gateway headers and signature headers.
    /// </summary>
    public static async Task SignAsync(
        HttpRequestMessage request,
        GatewaySdkOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (request.RequestUri is null || !request.RequestUri.IsAbsoluteUri)
            throw new InvalidOperationException("RequestUri must be an absolute URI before signing.");

        var isBodyBearing = IsUnsafeMethod(request);

        UpsertHeader(request, "X-Product-Code", options.ProductCode);
        UpsertHeader(request, "X-Client-Id", options.ClientId);
        UpsertHeader(request, "X-Audit-Source-Type", options.AuditSourceType);
        UpsertHeader(request, "X-Audit-User-Id", options.AuditUserId);

        string? contentDigest = null;
        string? contentType = null;
        string? idempotencyKey = null;

        if (isBodyBearing)
        {
            request.Content ??= new StringContent(string.Empty, Encoding.UTF8, "application/json");

            var bodyBytes = await request.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            contentDigest = $"sha-256=:{Convert.ToBase64String(SHA256.HashData(bodyBytes))}:";
            UpsertHeader(request, "Content-Digest", contentDigest);

            contentType = request.Content.Headers.ContentType?.ToString() ?? string.Empty;
            request.Content = CloneContent(request.Content, bodyBytes);

            if (!TryGetHeader(request, "Idempotency-Key", out idempotencyKey))
            {
                idempotencyKey = Guid.NewGuid().ToString();
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }
        }

        var componentNames = BuildComponentNames(isBodyBearing);
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expires = created + options.MaxSignatureLifetimeSeconds;

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["created"] = created.ToString(CultureInfo.InvariantCulture),
            ["expires"] = expires.ToString(CultureInfo.InvariantCulture),
            ["keyid"] = options.GetKeyId(),
            ["nonce"] = GenerateNonce(),
            ["alg"] = Algorithm,
            ["tag"] = Tag
        };

        var signatureBase = BuildSignatureBase(
            request,
            componentNames,
            parameters,
            contentDigest,
            contentType,
            idempotencyKey,
            options);

        byte[] signatureBytes;
        using (var key = Key.Import(
            SignatureAlgorithm.Ed25519,
            Encoding.UTF8.GetBytes(options.PrivateKeyPem),
            KeyBlobFormat.PkixPrivateKeyText))
        {
            signatureBytes = SignatureAlgorithm.Ed25519.Sign(key, Encoding.UTF8.GetBytes(signatureBase));
        }

        request.Headers.Remove("Signature-Input");
        request.Headers.Remove("Signature");
        request.Headers.TryAddWithoutValidation("Signature-Input", $"{SignatureLabel}={SerializeSignatureParams(componentNames, parameters)}");
        request.Headers.TryAddWithoutValidation("Signature", $"{SignatureLabel}=:{Convert.ToBase64String(signatureBytes)}:");
    }

    private static ByteArrayContent CloneContent(HttpContent source, byte[] bodyBytes)
    {
        var clone = new ByteArrayContent(bodyBytes);
        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    private static bool IsUnsafeMethod(HttpRequestMessage request)
    {
        var method = request.Method;
        return method == HttpMethod.Post
            || method == HttpMethod.Put
            || method == HttpMethod.Patch
            || (method == HttpMethod.Delete && request.Content is not null);
    }

    private static List<string> BuildComponentNames(bool isBodyBearing)
    {
        var names = new List<string> { "@method", "@target-uri", "@authority" };
        if (isBodyBearing)
        {
            names.Add("content-digest");
            names.Add("content-type");
        }

        names.Add("x-product-code");
        names.Add("x-client-id");
        names.Add("x-audit-source-type");
        names.Add("x-audit-user-id");

        if (isBodyBearing)
            names.Add("idempotency-key");

        return names;
    }

    private static void UpsertHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
    }

    private static bool TryGetHeader(HttpRequestMessage request, string name, out string value)
    {
        if (request.Headers.TryGetValues(name, out var values))
        {
            value = string.Join(", ", values).Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string BuildSignatureBase(
        HttpRequestMessage request,
        IReadOnlyList<string> componentNames,
        IReadOnlyDictionary<string, string> parameters,
        string? contentDigest,
        string? contentType,
        string? idempotencyKey,
        GatewaySdkOptions options)
    {
        var sb = new StringBuilder();
        foreach (var name in componentNames)
        {
            var value = ResolveComponentValue(request, name, contentDigest, contentType, idempotencyKey, options);
            sb.Append('"').Append(name).Append("\": ").Append(value).Append('\n');
        }

        sb.Append("\"@signature-params\": ");
        sb.Append(SerializeSignatureParams(componentNames, parameters));
        return sb.ToString();
    }

    private static string ResolveComponentValue(
        HttpRequestMessage request,
        string name,
        string? contentDigest,
        string? contentType,
        string? idempotencyKey,
        GatewaySdkOptions options)
    {
        return name switch
        {
            "@method" => request.Method.Method.ToUpperInvariant(),
            "@target-uri" => request.RequestUri?.AbsoluteUri ?? string.Empty,
            "@authority" => BuildAuthority(request.RequestUri),
            "content-digest" => contentDigest ?? string.Empty,
            "content-type" => contentType ?? string.Empty,
            "x-product-code" => options.ProductCode,
            "x-client-id" => options.ClientId,
            "x-audit-source-type" => options.AuditSourceType,
            "x-audit-user-id" => options.AuditUserId,
            "idempotency-key" => idempotencyKey ?? string.Empty,
            _ => TryGetHeader(request, name, out var headerValue) ? headerValue : string.Empty
        };
    }

    private static string BuildAuthority(Uri? uri)
    {
        if (uri is null)
            return string.Empty;

        var host = uri.Host.ToLowerInvariant();
        var defaultPort = uri.Scheme switch
        {
            "https" => 443,
            "http" => 80,
            _ => -1
        };

        return uri.Port == defaultPort || uri.IsDefaultPort
            ? host
            : $"{host}:{uri.Port}";
    }

    private static string SerializeSignatureParams(
        IReadOnlyList<string> componentNames,
        IReadOnlyDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        sb.Append('(');
        for (var i = 0; i < componentNames.Count; i++)
        {
            if (i > 0)
                sb.Append(' ');

            sb.Append('"').Append(componentNames[i]).Append('"');
        }

        sb.Append(')');

        foreach (var kvp in parameters)
        {
            sb.Append(';').Append(kvp.Key).Append('=');
            if (kvp.Key is "created" or "expires")
                sb.Append(kvp.Value);
            else
                sb.Append('"').Append(kvp.Value).Append('"');
        }

        return sb.ToString();
    }
}
