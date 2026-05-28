namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Exception thrown when the Gateway returns a non-success HTTP response.
/// </summary>
public sealed class GatewayApiException : HttpRequestException
{
    public GatewayApiException(HttpResponseMessage response, string responseBody)
        : base($"Hash BaaS Gateway returned {(int)response.StatusCode} {response.ReasonPhrase}.")
    {
        StatusCode = response.StatusCode;
        ReasonPhrase = response.ReasonPhrase;
        Headers = response.Headers.ToDictionary(
            header => header.Key,
            header => string.Join(", ", header.Value),
            StringComparer.OrdinalIgnoreCase);
        ResponseBody = responseBody;
    }

    public new System.Net.HttpStatusCode StatusCode { get; }

    public string? ReasonPhrase { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public string ResponseBody { get; }
}
