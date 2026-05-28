namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Result of Gateway response signature verification.
/// </summary>
public sealed record GatewayResponseVerificationResult(bool Verified, string? Error)
{
    /// <summary>
    /// Creates a successful response verification result.
    /// </summary>
    public static GatewayResponseVerificationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed response verification result with a diagnostic message.
    /// </summary>
    public static GatewayResponseVerificationResult Fail(string error) => new(false, error);
}
