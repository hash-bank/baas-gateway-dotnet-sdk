using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Typed client for Hash BaaS Gateway v1 endpoints.
/// </summary>
public sealed class HashBaasGatewayClient
{
    private readonly HttpClient _httpClient;

    public HashBaasGatewayClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public static HashBaasGatewayClient Create(GatewaySdkOptions options, HttpMessageHandler? innerHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.BaseAddress is null)
            throw new InvalidOperationException("Gateway BaseAddress is required when using the SDK client factory.");

        var signingHandler = new GatewaySigningHandler(options)
        {
            InnerHandler = innerHandler ?? new HttpClientHandler()
        };

        return new HashBaasGatewayClient(new HttpClient(signingHandler)
        {
            BaseAddress = options.BaseAddress
        });
    }

    public Task<StatusResponse> GetStatusAsync(CancellationToken ct = default)
        => SendAsync<StatusResponse>(HttpMethod.Get, "v1/status", null, ct);

    public Task<GetTermsResponse> GetTermsAsync(string? acceptLanguage = null, CancellationToken ct = default)
        => SendAsync<GetTermsResponse>(HttpMethod.Get, "v1/terms", null, ct, request =>
        {
            if (!string.IsNullOrWhiteSpace(acceptLanguage))
                request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        });

    public Task<CreatePersonResponse> CreatePersonAsync(CreatePersonRequest request, CancellationToken ct = default)
        => SendAsync<CreatePersonResponse>(HttpMethod.Post, "v1/persons", request, ct);

    public Task<GetPersonResponse> GetPersonAsync(Guid personId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Get, $"v1/persons/{personId}", null, ct);

    public Task<GetPersonResponse> GetPersonByExternalIdAsync(string externalId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Get, $"v1/persons-by-external-id/{Uri.EscapeDataString(externalId)}", null, ct);

    public Task<GetPersonResponse> UpdatePersonAsync(Guid personId, UpdatePersonRequest request, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Patch, $"v1/persons/{personId}", request, ct);

    public Task<GetPersonResponse> DeactivatePersonAsync(Guid personId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Delete, $"v1/persons/{personId}", null, ct);

    public Task<CreateKycCheckResponse> CreateKycCheckAsync(CreateKycCheckRequest request, CancellationToken ct = default)
        => SendAsync<CreateKycCheckResponse>(HttpMethod.Post, "v1/kyc-checks", request, ct);

    public Task<GetKycCheckResponse> GetKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendAsync<GetKycCheckResponse>(HttpMethod.Get, $"v1/kyc-checks/{kycCheckId}", null, ct);

    public Task DeleteKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendNoContentAsync(HttpMethod.Delete, $"v1/kyc-checks/{kycCheckId}", ct);

    public Task<GetKycCheckResponse> InitiateKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendAsync<GetKycCheckResponse>(HttpMethod.Post, $"v1/kyc-checks/{kycCheckId}/initiate", null, ct);

    public async Task<UploadKycDocumentResponse> UploadKycDocumentAsync(UploadKycDocumentRequest request, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.KycCheckId), "kyc_check_id");
        form.Add(new StringContent(request.Type.ToString()), "type");

        if (request.Subtype is not null)
            form.Add(new StringContent(request.Subtype.Value.ToString()), "subtype");
        if (!string.IsNullOrWhiteSpace(request.Number))
            form.Add(new StringContent(request.Number), "number");
        if (!string.IsNullOrWhiteSpace(request.Issuer))
            form.Add(new StringContent(request.Issuer), "issuer");

        var file = new StreamContent(request.FileContent);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        form.Add(file, "file", request.FileName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/kyc-documents") { Content = form };
        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadSuccessAsync<UploadKycDocumentResponse>(response, ct).ConfigureAwait(false);
    }

    public Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
        => SendAsync<CreateAccountResponse>(HttpMethod.Post, "v1/accounts", request, ct);

    public Task<GetAccountResponse> GetAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Get, $"v1/accounts/{accountId}", null, ct);

    public Task<GetAccountCardsResponse> GetAccountCardsAsync(
        Guid accountId,
        int fromRecord = 0,
        int recordsCount = 25,
        IReadOnlyCollection<CardStatus>? cardStatuses = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"?from_record={fromRecord}&records_count={recordsCount}");
        if (cardStatuses is not null)
        {
            foreach (var status in cardStatuses)
                query.Append("&card_statuses=").Append(Uri.EscapeDataString(status.ToString()));
        }

        return SendAsync<GetAccountCardsResponse>(HttpMethod.Get, $"v1/accounts/{accountId}/cards{query}", null, ct);
    }

    public Task<GetAccountResponse> CloseAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Delete, $"v1/accounts/{accountId}/close", null, ct);

    public Task<GetAccountResponse> CloseAccountAsync(Guid accountId, CloseAccountPatchRequest request, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Patch, $"v1/accounts/{accountId}/close", request, ct);

    public Task<CreateCardResponse> CreateCardAsync(CreateCardRequest request, bool sendNotification = true, CancellationToken ct = default)
        => SendAsync<CreateCardResponse>(HttpMethod.Post, $"v1/cards?send_notification={sendNotification.ToString().ToLowerInvariant()}", request, ct);

    public Task<GetCardResponse> GetCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<GetCardResponse>(HttpMethod.Get, $"v1/cards/{cardId}", null, ct);

    public Task<ActivateCardResponse> ActivateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ActivateCardResponse>(HttpMethod.Patch, $"v1/cards/{cardId}/activate", null, ct);

    public Task<DigitalCardViewResponse> PrepareDigitalCardViewAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<DigitalCardViewResponse>(HttpMethod.Post, $"v1/cards/{cardId}/digital-card-view", null, ct);

    public Task<BlockCardResponse> BlockCardAsync(Guid cardId, BlockCardRequest request, CancellationToken ct = default)
        => SendAsync<BlockCardResponse>(HttpMethod.Patch, $"v1/cards/{cardId}/block", request, ct);

    public Task<UnblockCardResponse> UnblockCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<UnblockCardResponse>(HttpMethod.Patch, $"v1/cards/{cardId}/unblock", null, ct);

    public Task<ResetPinCounterResponse> ResetCardPinCounterAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ResetPinCounterResponse>(HttpMethod.Post, $"v1/cards/{cardId}/reset-pin-counter", null, ct);

    public Task<GeneratePinKeyResponse> GeneratePinKeyAsync(Guid cardId, GeneratePinKeyRequest? request = null, string? acceptLanguage = null, CancellationToken ct = default)
        => SendAsync<GeneratePinKeyResponse>(HttpMethod.Post, $"v1/cards/{cardId}/pin/key", request ?? new GeneratePinKeyRequest(), ct, httpRequest =>
        {
            if (!string.IsNullOrWhiteSpace(acceptLanguage))
                httpRequest.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        });

    public Task<SetPinResponse> SetPinAsync(Guid cardId, SetPinRequest request, CancellationToken ct = default)
        => SendAsync<SetPinResponse>(HttpMethod.Post, $"v1/cards/{cardId}/pin/set", request, ct);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string requestUri,
        object? body,
        CancellationToken ct,
        Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Accept.ParseAdd("application/json");
        configure?.Invoke(request);

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, GatewayJson.Options);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        return await ReadSuccessAsync<T>(response, ct).ConfigureAwait(false);
    }

    private async Task SendNoContentAsync(HttpMethod method, string requestUri, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var result = JsonSerializer.Deserialize<T>(json, GatewayJson.Options);
        return result ?? throw new JsonException($"Gateway returned an empty or invalid {typeof(T).Name} response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        throw new GatewayApiException(response, body);
    }
}
