namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// HttpClient handler that signs every outgoing HashBank Gateway request.
/// </summary>
public sealed class GatewaySigningHandler : DelegatingHandler
{
    private readonly GatewaySdkOptions _options;

    /// <summary>
    /// Creates a signing handler using the supplied Gateway SDK options.
    /// </summary>
    public GatewaySigningHandler(GatewaySdkOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Signs the outgoing request and sends it through the inner handler.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && !request.RequestUri.IsAbsoluteUri && _options.BaseAddress is not null)
            request.RequestUri = new Uri(_options.BaseAddress, request.RequestUri);

        await GatewayRequestSigner.SignAsync(request, _options, cancellationToken).ConfigureAwait(false);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
