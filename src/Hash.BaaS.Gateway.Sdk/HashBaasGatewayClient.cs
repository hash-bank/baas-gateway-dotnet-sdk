using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Hash.BaaS.Gateway.Sdk;

/// <summary>
/// Typed client for HashBank Gateway v1 endpoints.
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
        => SendAsync<StatusResponse>(HttpMethod.Get, "v1/embedded/status", null, ct);

    public Task<GetTermsResponse> GetTermsAsync(string? acceptLanguage = null, CancellationToken ct = default)
        => SendAsync<GetTermsResponse>(HttpMethod.Get, "v1/embedded/terms", null, ct, request =>
        {
            if (!string.IsNullOrWhiteSpace(acceptLanguage))
                request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        });

    public Task<CountryOnboardingRequirementsResponse> GetCountryOnboardingRequirementsAsync(string? countryCode = null, CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(countryCode)
            ? string.Empty
            : $"?country_code={Uri.EscapeDataString(countryCode.Trim().ToUpperInvariant())}";

        return SendAsync<CountryOnboardingRequirementsResponse>(HttpMethod.Get, $"v1/embedded/countries/onboarding-requirements{query}", null, ct);
    }

    public Task<CreatePersonResponse> CreatePersonAsync(CreatePersonRequest request, CancellationToken ct = default)
        => SendAsync<CreatePersonResponse>(HttpMethod.Post, "v1/embedded/persons", request, ct);

    public Task<GetPersonResponse> GetPersonAsync(Guid personId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Get, $"v1/embedded/persons/{personId}", null, ct);

    public Task<GetPersonResponse> GetPersonByExternalIdAsync(string externalId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Get, $"v1/embedded/persons-by-external-id/{Uri.EscapeDataString(externalId)}", null, ct);

    public Task<GetPersonResponse> UpdatePersonAsync(Guid personId, UpdatePersonRequest request, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Patch, $"v1/embedded/persons/{personId}", request, ct);

    public Task<GetPersonResponse> DeactivatePersonAsync(Guid personId, CancellationToken ct = default)
        => SendAsync<GetPersonResponse>(HttpMethod.Delete, $"v1/embedded/persons/{personId}", null, ct);

    public Task<CreateKycCheckResponse> CreateKycCheckAsync(CreateKycCheckRequest request, CancellationToken ct = default)
        => SendAsync<CreateKycCheckResponse>(HttpMethod.Post, "v1/embedded/kyc-checks", request, ct);

    public Task<GetKycCheckResponse> GetKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendAsync<GetKycCheckResponse>(HttpMethod.Get, $"v1/embedded/kyc-checks/{kycCheckId}", null, ct);

    public Task DeleteKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendNoContentAsync(HttpMethod.Delete, $"v1/embedded/kyc-checks/{kycCheckId}", ct);

    public Task<GetKycCheckResponse> InitiateKycCheckAsync(Guid kycCheckId, CancellationToken ct = default)
        => SendAsync<GetKycCheckResponse>(HttpMethod.Post, $"v1/embedded/kyc-checks/{kycCheckId}/initiate", null, ct);

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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/embedded/kyc-documents") { Content = form };
        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        return await ReadSuccessAsync<UploadKycDocumentResponse>(response, ct).ConfigureAwait(false);
    }

    public Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
        => SendAsync<CreateAccountResponse>(HttpMethod.Post, "v1/embedded/accounts", request, ct);

    public Task<GetAccountResponse> GetAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Get, $"v1/embedded/accounts/{accountId}", null, ct);

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

        return SendAsync<GetAccountCardsResponse>(HttpMethod.Get, $"v1/embedded/accounts/{accountId}/cards{query}", null, ct);
    }

    public Task<GetAccountResponse> CloseAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Delete, $"v1/embedded/accounts/{accountId}/close", null, ct);

    public Task<GetAccountResponse> CloseAccountAsync(Guid accountId, CloseAccountPatchRequest request, CancellationToken ct = default)
        => SendAsync<GetAccountResponse>(HttpMethod.Patch, $"v1/embedded/accounts/{accountId}/close", request, ct);

    public Task<CreateCardResponse> CreateCardAsync(CreateCardRequest request, bool sendNotification = true, CancellationToken ct = default)
        => SendAsync<CreateCardResponse>(HttpMethod.Post, $"v1/embedded/cards?send_notification={sendNotification.ToString().ToLowerInvariant()}", request, ct);

    public Task<GetCardResponse> GetCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<GetCardResponse>(HttpMethod.Get, $"v1/embedded/cards/{cardId}", null, ct);

    public Task<ActivateCardResponse> ActivateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ActivateCardResponse>(HttpMethod.Patch, $"v1/embedded/cards/{cardId}/activate", null, ct);

    public Task<DigitalCardViewResponse> PrepareDigitalCardViewAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<DigitalCardViewResponse>(HttpMethod.Post, $"v1/embedded/cards/{cardId}/digital-card-view", null, ct);

    public Task<BlockCardResponse> BlockCardAsync(Guid cardId, BlockCardRequest request, CancellationToken ct = default)
        => SendAsync<BlockCardResponse>(HttpMethod.Patch, $"v1/embedded/cards/{cardId}/block", request, ct);

    public Task<UnblockCardResponse> UnblockCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<UnblockCardResponse>(HttpMethod.Patch, $"v1/embedded/cards/{cardId}/unblock", null, ct);

    public Task<ResetPinCounterResponse> ResetCardPinCounterAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ResetPinCounterResponse>(HttpMethod.Post, $"v1/embedded/cards/{cardId}/reset-pin-counter", null, ct);

    public Task<GeneratePinKeyResponse> GeneratePinKeyAsync(Guid cardId, GeneratePinKeyRequest? request = null, string? acceptLanguage = null, CancellationToken ct = default)
        => SendAsync<GeneratePinKeyResponse>(HttpMethod.Post, $"v1/embedded/cards/{cardId}/pin/key", request ?? new GeneratePinKeyRequest(), ct, httpRequest =>
        {
            if (!string.IsNullOrWhiteSpace(acceptLanguage))
                httpRequest.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        });

    public Task<SetPinResponse> SetPinAsync(Guid cardId, SetPinRequest request, CancellationToken ct = default)
        => SendAsync<SetPinResponse>(HttpMethod.Post, $"v1/embedded/cards/{cardId}/pin/set", request, ct);

    public Task<CreateCorporateAccountResponse> CreateCorporateAccountsAsync(CreateCorporateAccountRequest request, CancellationToken ct = default)
        => SendAsync<CreateCorporateAccountResponse>(HttpMethod.Post, "v1/corporate/accounts", request, ct);

    public Task<ListCorporateAccountsResponse> ListCorporateAccountsAsync(
        string? currency = null,
        IReadOnlyCollection<AccountStatus>? statuses = null,
        int fromRecord = 0,
        int recordsCount = 25,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"?from_record={fromRecord}&records_count={recordsCount}");
        if (!string.IsNullOrWhiteSpace(currency))
            query.Append("&currency=").Append(Uri.EscapeDataString(currency.Trim().ToUpperInvariant()));
        if (statuses is not null)
        {
            foreach (var status in statuses)
                query.Append("&status=").Append(Uri.EscapeDataString(status.ToString()));
        }

        return SendAsync<ListCorporateAccountsResponse>(HttpMethod.Get, $"v1/corporate/accounts{query}", null, ct);
    }

    public Task<GetCorporateAccountResponse> GetCorporateAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountResponse>(HttpMethod.Get, $"v1/corporate/accounts/{accountId}", null, ct);

    public Task<GetCorporateAccountResponse> CloseCorporateAccountAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountResponse>(HttpMethod.Delete, $"v1/corporate/accounts/{accountId}/close", null, ct);

    public Task<GetCorporateAccountResponse> CloseCorporateAccountAsync(Guid accountId, CloseCorporateAccountRequest request, CancellationToken ct = default)
    {
        var reason = request.CloseReason.HasValue
            ? $"?close_reason={Uri.EscapeDataString(request.CloseReason.Value.ToString())}"
            : string.Empty;

        return SendAsync<GetCorporateAccountResponse>(HttpMethod.Delete, $"v1/corporate/accounts/{accountId}/close{reason}", null, ct);
    }

    public Task<GetCorporateAccountResponse> RenameCorporateAccountAsync(Guid accountId, RenameCorporateAccountRequest request, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountResponse>(HttpMethod.Patch, $"v1/corporate/accounts/{accountId}/name", request, ct);

    public Task<GetCorporateAccountResponse> AddCorporateAccountCurrencyAsync(Guid accountId, AddCorporateAccountCurrencyRequest request, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountResponse>(HttpMethod.Post, $"v1/corporate/accounts/{accountId}/currencies", request, ct);

    public Task<GetCorporateAccountBalancesResponse> GetCorporateAccountBalancesAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountBalancesResponse>(HttpMethod.Get, $"v1/corporate/accounts/{accountId}/balances", null, ct);

    public Task<GetCorporateAccountRequisitesResponse> GetCorporateAccountRequisitesAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountRequisitesResponse>(HttpMethod.Get, $"v1/corporate/accounts/{accountId}/requisites", null, ct);

    public Task<GetCorporateAccountRestrictionsResponse> GetCorporateAccountRestrictionsAsync(Guid accountId, CancellationToken ct = default)
        => SendAsync<GetCorporateAccountRestrictionsResponse>(HttpMethod.Get, $"v1/corporate/accounts/{accountId}/restrictions", null, ct);

    public Task<GatewayFileResponse> DownloadCorporateAccountStatementAsync(
        Guid accountId,
        StatementFormat format,
        DateOnly fromDate,
        DateOnly toDate,
        string language = "en",
        CancellationToken ct = default)
    {
        var query = new StringBuilder()
            .Append("?format=").Append(Uri.EscapeDataString(format.ToString().ToLowerInvariant()))
            .Append("&from_date=").Append(Uri.EscapeDataString(fromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
            .Append("&to_date=").Append(Uri.EscapeDataString(toDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
            .Append("&lang=").Append(Uri.EscapeDataString(string.IsNullOrWhiteSpace(language) ? "en" : language.Trim()));

        return SendFileAsync(HttpMethod.Get, $"v1/corporate/accounts/{accountId}/download-statement{query}", ct);
    }

    public Task<CreateCorporateCardResponse> CreateCorporateCardAsync(CreateCorporateCardRequest request, CancellationToken ct = default)
        => SendAsync<CreateCorporateCardResponse>(HttpMethod.Post, "v1/corporate/cards", request, ct);

    public Task<ListCorporateCardDesignTypesResponse> ListCorporateCardDesignTypesAsync(CancellationToken ct = default)
        => SendAsync<ListCorporateCardDesignTypesResponse>(HttpMethod.Get, "v1/corporate/cards/design-types", null, ct);

    public Task<ListCorporateCardsResponse> ListCorporateCardsAsync(
        Guid? accountId = null,
        IReadOnlyCollection<CardStatus>? cardStatuses = null,
        int fromRecord = 0,
        int recordsCount = 25,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"?from_record={fromRecord}&records_count={recordsCount}");
        if (accountId.HasValue)
            query.Append("&account_id=").Append(Uri.EscapeDataString(accountId.Value.ToString()));
        if (cardStatuses is not null)
        {
            foreach (var status in cardStatuses)
                query.Append("&card_statuses=").Append(Uri.EscapeDataString(status.ToString()));
        }

        return SendAsync<ListCorporateCardsResponse>(HttpMethod.Get, $"v1/corporate/cards{query}", null, ct);
    }

    public Task<GetCorporateCardResponse> GetCorporateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<GetCorporateCardResponse>(HttpMethod.Get, $"v1/corporate/cards/{cardId}", null, ct);

    public Task<FreezeCorporateCardResponse> FreezeCorporateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<FreezeCorporateCardResponse>(HttpMethod.Patch, $"v1/corporate/cards/{cardId}/freeze", null, ct);

    public Task<UnfreezeCorporateCardResponse> UnfreezeCorporateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<UnfreezeCorporateCardResponse>(HttpMethod.Patch, $"v1/corporate/cards/{cardId}/unfreeze", null, ct);

    public Task<ActivateCorporateCardResponse> ActivateCorporateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ActivateCorporateCardResponse>(HttpMethod.Patch, $"v1/corporate/cards/{cardId}/activate", null, ct);

    public Task<CloseCorporateCardResponse> CloseCorporateCardAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<CloseCorporateCardResponse>(HttpMethod.Delete, $"v1/corporate/cards/{cardId}/close", null, ct);

    public Task<DigitalCardViewResponse> PrepareCorporateDigitalCardViewAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<DigitalCardViewResponse>(HttpMethod.Post, $"v1/corporate/cards/{cardId}/digital-card-view", null, ct);

    public Task<ResetPinCounterResponse> ResetCorporateCardPinCounterAsync(Guid cardId, CancellationToken ct = default)
        => SendAsync<ResetPinCounterResponse>(HttpMethod.Post, $"v1/corporate/cards/{cardId}/reset-pin-counter", null, ct);

    public Task<GeneratePinKeyResponse> GenerateCorporatePinKeyAsync(
        Guid cardId,
        GeneratePinKeyRequest? request = null,
        string? acceptLanguage = null,
        CancellationToken ct = default)
        => SendAsync<GeneratePinKeyResponse>(
            HttpMethod.Post,
            $"v1/corporate/cards/{cardId}/pin/key",
            request ?? new GeneratePinKeyRequest(),
            ct,
            httpRequest =>
            {
                if (!string.IsNullOrWhiteSpace(acceptLanguage))
                    httpRequest.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
            });

    public Task<SetPinResponse> SetCorporatePinAsync(Guid cardId, SetPinRequest request, CancellationToken ct = default)
        => SendAsync<SetPinResponse>(HttpMethod.Post, $"v1/corporate/cards/{cardId}/pin/set", request, ct);

    public Task<ListCorporateTransactionsResponse> ListCorporateTransactionsAsync(
        Guid? cardId = null,
        string? accountNumber = null,
        string? currency = null,
        IReadOnlyCollection<TransactionStatus>? transactionStatuses = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? currencyCode = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int fromRecord = 0,
        int recordsCount = 25,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"?from_record={fromRecord}&records_count={recordsCount}");
        if (cardId.HasValue)
            query.Append("&card_id=").Append(Uri.EscapeDataString(cardId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(accountNumber))
            query.Append("&account_number=").Append(Uri.EscapeDataString(accountNumber.Trim()));
        if (!string.IsNullOrWhiteSpace(currency))
            query.Append("&currency=").Append(Uri.EscapeDataString(currency.Trim().ToUpperInvariant()));
        if (transactionStatuses is not null)
        {
            foreach (var status in transactionStatuses)
                query.Append("&transaction_statuses=").Append(Uri.EscapeDataString(status.ToString()));
        }
        if (minAmount.HasValue)
            query.Append("&min_amount=").Append(Uri.EscapeDataString(minAmount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (maxAmount.HasValue)
            query.Append("&max_amount=").Append(Uri.EscapeDataString(maxAmount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (!string.IsNullOrWhiteSpace(currencyCode))
            query.Append("&currency_code=").Append(Uri.EscapeDataString(currencyCode.Trim().ToUpperInvariant()));
        if (fromDate.HasValue)
            query.Append("&from_date=").Append(Uri.EscapeDataString(fromDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
        if (toDate.HasValue)
            query.Append("&to_date=").Append(Uri.EscapeDataString(toDate.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));

        return SendAsync<ListCorporateTransactionsResponse>(HttpMethod.Get, $"v1/corporate/transactions{query}", null, ct);
    }

    public Task<GetCorporateTransactionResponse> GetCorporateTransactionAsync(long transactionId, CancellationToken ct = default)
        => SendAsync<GetCorporateTransactionResponse>(HttpMethod.Get, $"v1/corporate/transactions/{transactionId}", null, ct);

    /// <summary>
    /// POST /v1/corporate/transactions/exchanges — executes an own-accounts, market-rate currency
    /// exchange (the source/target accounts must both belong to the authenticated corporate customer).
    /// Returns the accepted exchange operation id.
    /// </summary>
    public Task<MakeCorporateExchangeResponse> MakeCorporateExchangeAsync(MakeCorporateExchangeRequest request, CancellationToken ct = default)
        => SendAsync<MakeCorporateExchangeResponse>(HttpMethod.Post, "v1/corporate/transactions/exchanges", request, ct);

    /// <summary>
    /// GET /v1/corporate/rates?from=&amp;to= — the current sell/buy FX rate board for a currency pair.
    /// </summary>
    public Task<CorporateRateBoardResponse> GetCorporateRateBoardAsync(string from, string to, CancellationToken ct = default)
    {
        var query = $"?from={Uri.EscapeDataString(from.Trim().ToUpperInvariant())}&to={Uri.EscapeDataString(to.Trim().ToUpperInvariant())}";
        return SendAsync<CorporateRateBoardResponse>(HttpMethod.Get, $"v1/corporate/rates{query}", null, ct);
    }

    /// <summary>
    /// GET /v1/corporate/rates?from=&amp;to=&amp;amount= — an indicative, customer-scoped conversion
    /// quote for <paramref name="amount"/> of <paramref name="from"/> into <paramref name="to"/>.
    /// </summary>
    public Task<CorporateConversionQuoteResponse> GetCorporateConversionQuoteAsync(string from, string to, decimal amount, CancellationToken ct = default)
    {
        var query = $"?from={Uri.EscapeDataString(from.Trim().ToUpperInvariant())}"
                    + $"&to={Uri.EscapeDataString(to.Trim().ToUpperInvariant())}"
                    + $"&amount={Uri.EscapeDataString(amount.ToString(System.Globalization.CultureInfo.InvariantCulture))}";
        return SendAsync<CorporateConversionQuoteResponse>(HttpMethod.Get, $"v1/corporate/rates{query}", null, ct);
    }

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

    private async Task<GatewayFileResponse> SendFileAsync(
        HttpMethod method,
        string requestUri,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct).ConfigureAwait(false);

        var content = response.Content is null
            ? []
            : await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

        return new GatewayFileResponse
        {
            Content = content,
            ContentType = response.Content?.Headers.ContentType?.ToString(),
            FileName = response.Content?.Headers.ContentDisposition?.FileNameStar
                ?? response.Content?.Headers.ContentDisposition?.FileName?.Trim('"')
        };
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
