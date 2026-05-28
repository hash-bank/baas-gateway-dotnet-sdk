using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Verifies Hash BaaS Gateway response signatures.
/// </summary>
public static class GatewayResponseVerifier
{
    private const string SignatureLabel = "sig1";

    /// <summary>
    /// Verifies the Gateway response signature and content digest against the platform public key.
    /// </summary>
    public static async Task<GatewayResponseVerificationResult> VerifyAsync(
        HttpResponseMessage response,
        string platformPublicKeyPem,
        string? expectedProductCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (string.IsNullOrWhiteSpace(platformPublicKeyPem))
            return GatewayResponseVerificationResult.Fail("Platform public key is missing.");

        if (!TryGetSingle(response.Headers, "Signature-Input", out var signatureInputHeader))
            return GatewayResponseVerificationResult.Fail("Response Signature-Input header is missing.");

        if (!TryGetSingle(response.Headers, "Signature", out var signatureHeader))
            return GatewayResponseVerificationResult.Fail("Response Signature header is missing.");

        if (response.Content is null)
            return GatewayResponseVerificationResult.Fail("Response content is missing.");

        if (!TryGetSingle(response.Headers, "Content-Digest", out var contentDigestHeader) &&
            !TryGetSingle(response.Content.Headers, "Content-Digest", out contentDigestHeader))
            return GatewayResponseVerificationResult.Fail("Response Content-Digest header is missing.");

        var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        response.Content = CloneContent(response.Content, bodyBytes);
        var expectedDigestBase64 = ParseSha256Digest(contentDigestHeader);
        if (expectedDigestBase64 is null)
            return GatewayResponseVerificationResult.Fail("Response Content-Digest is malformed.");

        var actualDigestBase64 = Convert.ToBase64String(SHA256.HashData(bodyBytes));
        if (!string.Equals(expectedDigestBase64, actualDigestBase64, StringComparison.Ordinal))
            return GatewayResponseVerificationResult.Fail("Response body does not match Content-Digest.");

        if (!TryParseSignatureInput(signatureInputHeader, out var componentNames, out var parameters))
            return GatewayResponseVerificationResult.Fail("Response Signature-Input is malformed.");

        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
        var productCode = response.Headers.TryGetValues("X-Product-Code", out var productValues)
            ? string.Join(", ", productValues).Trim()
            : expectedProductCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(productCode))
            return GatewayResponseVerificationResult.Fail("Resolved response product code is missing.");

        var headers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["content-digest"] = contentDigestHeader,
            ["content-type"] = contentType,
            ["x-product-code"] = productCode
        };

        var signatureBase = BuildResponseSignatureBase((int)response.StatusCode, headers, componentNames, parameters);
        if (signatureBase is null)
            return GatewayResponseVerificationResult.Fail("Response signature base reconstruction failed.");

        var signatureBytes = ExtractSignatureBytes(signatureHeader);
        if (signatureBytes is null)
            return GatewayResponseVerificationResult.Fail("Response Signature header is malformed.");

        try
        {
            var publicKey = PublicKey.Import(
                SignatureAlgorithm.Ed25519,
                Encoding.UTF8.GetBytes(platformPublicKeyPem),
                KeyBlobFormat.PkixPublicKeyText);

            var verified = SignatureAlgorithm.Ed25519.Verify(
                publicKey,
                Encoding.UTF8.GetBytes(signatureBase),
                signatureBytes);

            return verified
                ? GatewayResponseVerificationResult.Success()
                : GatewayResponseVerificationResult.Fail("Response signature verification failed.");
        }
        catch (Exception ex)
        {
            return GatewayResponseVerificationResult.Fail($"Response signature verification failed: {ex.Message}");
        }
    }

    private static bool TryGetSingle(System.Net.Http.Headers.HttpHeaders headers, string name, out string value)
    {
        if (headers.TryGetValues(name, out var values))
        {
            value = string.Join(", ", values).Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static ByteArrayContent CloneContent(HttpContent source, byte[] bodyBytes)
    {
        var clone = new ByteArrayContent(bodyBytes);
        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    private static string? ParseSha256Digest(string header)
    {
        foreach (var rawEntry in header.Split(','))
        {
            var entry = rawEntry.Trim();
            if (!entry.StartsWith("sha-256=", StringComparison.OrdinalIgnoreCase))
                continue;

            var valuePart = entry["sha-256=".Length..];
            if (valuePart.Length < 2 || valuePart[0] != ':' || valuePart[^1] != ':')
                return null;

            return valuePart[1..^1];
        }

        return null;
    }

    private static byte[]? ExtractSignatureBytes(string header)
    {
        const string prefix = SignatureLabel + "=:";
        if (!header.StartsWith(prefix, StringComparison.Ordinal) || !header.EndsWith(":", StringComparison.Ordinal))
            return null;

        var base64 = header.Substring(prefix.Length, header.Length - prefix.Length - 1);
        if (base64.Length == 0)
            return null;

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static bool TryParseSignatureInput(
        string header,
        out List<string> componentNames,
        out Dictionary<string, string> parameters)
    {
        componentNames = new List<string>();
        parameters = new Dictionary<string, string>(StringComparer.Ordinal);

        const string prefix = SignatureLabel + "=";
        if (!header.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var body = header[prefix.Length..];
        var open = body.IndexOf('(');
        var close = body.IndexOf(')');
        if (open != 0 || close <= open)
            return false;

        var inner = body.Substring(open + 1, close - open - 1);
        foreach (var raw in inner.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = raw.Trim('"');
            if (trimmed.Length > 0)
                componentNames.Add(trimmed);
        }

        var paramsPart = body[(close + 1)..];
        foreach (var rawParam in paramsPart.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = rawParam.Trim();
            var equalsIndex = pair.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            var key = pair[..equalsIndex];
            var value = pair[(equalsIndex + 1)..].Trim('"');
            parameters[key] = value;
        }

        return componentNames.Count > 0 && parameters.Count > 0;
    }

    private static string? BuildResponseSignatureBase(
        int statusCode,
        IReadOnlyDictionary<string, string> headers,
        IReadOnlyList<string> componentNames,
        IReadOnlyDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var name in componentNames)
        {
            var value = name == "@status"
                ? statusCode.ToString(CultureInfo.InvariantCulture)
                : headers.TryGetValue(name, out var headerValue) ? headerValue.Trim() : null;

            if (value is null)
                return null;

            sb.Append('"').Append(name).Append("\": ").Append(value).Append('\n');
        }

        sb.Append("\"@signature-params\": ");
        sb.Append('(');
        for (var i = 0; i < componentNames.Count; i++)
        {
            if (i > 0)
                sb.Append(' ');

            sb.Append('"').Append(componentNames[i]).Append('"');
        }

        sb.Append(')');

        var canonicalOrder = new[] { "created", "expires", "keyid", "nonce", "alg", "tag" };
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in canonicalOrder)
        {
            if (!parameters.TryGetValue(key, out var value))
                continue;

            AppendParam(sb, key, value);
            emitted.Add(key);
        }

        foreach (var kvp in parameters)
        {
            if (emitted.Contains(kvp.Key))
                continue;

            AppendParam(sb, kvp.Key, kvp.Value);
        }

        return sb.ToString();
    }

    private static void AppendParam(StringBuilder sb, string key, string value)
    {
        sb.Append(';').Append(key).Append('=');
        if (key is "created" or "expires")
            sb.Append(value);
        else
            sb.Append('"').Append(value).Append('"');
    }
}
