# HashBank Gateway .NET SDK

![SDK](https://img.shields.io/badge/SDK-HashBank%20Gateway-111827?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4?style=for-the-badge&logo=dotnet)
![RFC 9421](https://img.shields.io/badge/RFC%209421-HTTP%20Signatures-0F766E?style=for-the-badge)
![Ed25519](https://img.shields.io/badge/Ed25519-Signatures-1F2937?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-0A0A0A?style=for-the-badge)

Production-ready .NET SDK and NuGet package for HashBank Gateway authorization, request signing, response verification, and Gateway API usage.

The SDK removes the low-level complexity of HashBank Gateway integration:

- Generates and validates Ed25519 key pairs.
- Signs requests using the HashBank Gateway RFC 9421 profile.
- Adds required Gateway headers consistently.
- Adds `Content-Digest` and `Idempotency-Key` for mutating requests.
- Provides manual signed-response verification helper.
- Provides a typed .NET Gateway client for the full v1 flow.

Store production private keys in your secret manager. Do not commit private keys or paste production keys into browser tooling.

## Contents

- [Architecture](#architecture)
- [SDK Matrix](#sdk-matrix)
- [Gateway Authorization Model](#gateway-authorization-model)
- [.NET Quick Start](#net-quick-start)
- [.NET Typed Gateway Client](#net-typed-gateway-client)
- [Request Signing Profile](#request-signing-profile)
- [Response Verification](#response-verification)
- [Configuration Reference](#configuration-reference)
- [Packaging](#packaging)
- [Repository Layout](#repository-layout)
- [Security Checklist](#security-checklist)
- [License](#license)

## Architecture

```mermaid
sequenceDiagram
    autonumber
    participant App as Client Application
    participant SDK as HashBank Gateway .NET SDK
    participant Portal as Developer Portal
    participant Gateway as HashBank Gateway

    App->>SDK: Configure clientId, product definition, private/public PEM
    SDK->>SDK: Derive keyId from public key
    App->>SDK: Send Gateway request
    SDK->>SDK: Build Content-Digest and RFC 9421 signature
    SDK->>Gateway: Signed HTTP request
    Gateway->>Portal: Resolve client product and active public key
    Portal-->>Gateway: Product code, issuer, active key
    Gateway->>Gateway: Verify signature and idempotency
    Gateway-->>SDK: JSON response + optional response signature
    SDK-->>App: Typed response or GatewayApiException
```

## SDK Matrix

| SDK | Status | Runtime | Scope |
| --- | --- | --- | --- |
| `.NET` | Ready | `.NET 8`, `.NET 10` | Full typed Gateway v1 client, request signing, response verification, key generation |

Repository name:

```text
hash-baas-gateway-dotnet-sdk
```

## Gateway Authorization Model

HashBank Gateway uses signed HTTP requests instead of bearer-token authorization for partner-facing Gateway operations.

Embedded Gateway requests must include:

| Header / Option | Example | Description |
| --- | --- | --- |
| `X-Client-Id` | `00000000-0000-0000-0000-000000000000` | Developer Portal client identifier |
| `X-Product-Code` | `TEST_PRODUCT` | Product definition code received by Gateway |
| `X-Audit-User-Id` | `my-service` | Calling service or user identifier |
| `X-Audit-Source-Type` | `Backend` | Calling channel |
| `Signature-Input` | `sig1=(...)` | RFC 9421 signature metadata |
| `Signature` | `sig1=:...:` | Ed25519 signature |
| `Content-Digest` | `sha-256=:...:` | Required for body-bearing and mutating requests |
| `Idempotency-Key` | GUID | Required for mutating requests |

Gateway resolves the real internal product code from:

```text
X-Client-Id + X-Product-Code
```

The SDK signs the product definition code exactly as sent in `X-Product-Code`.

Corporate Gateway requests (`v1/corporate/*`) use the **same** RFC 9421 HTTP signature profile and the **same** signed header set as embedded — there is **no** bearer token and **no** `Authorization` header. The SDK signs corporate requests exactly like embedded ones (including `X-Client-Id`); the corporate customer is resolved from the signing key, not from a token.

Corporate Gateway requests include the same headers as embedded:

| Header / Option | Example | Description |
| --- | --- | --- |
| `X-Client-Id` | `00000000-0000-0000-0000-000000000000` | Developer Portal client identifier |
| `X-Product-Code` | `TEST_PRODUCT` | Product definition code received by Gateway |
| `X-Audit-User-Id` | `my-service` | Calling service or user identifier |
| `X-Audit-Source-Type` | `Backend` | Calling channel |
| `Signature-Input` | `sig1=(...)` | RFC 9421 signature metadata |
| `Signature` | `sig1=:...:` | Ed25519 signature |
| `Content-Digest` | `sha-256=:...:` | Required for body-bearing and mutating requests |
| `Idempotency-Key` | GUID | Required for mutating requests |

## .NET Quick Start

### Install from NuGet

```bash
dotnet add package Hash.BaaS.Gateway.Sdk
```

For local development before publishing a new package version, reference the project directly:

```bash
dotnet add reference ../hash-baas-gateway-dotnet-sdk/src/Hash.BaaS.Gateway.Sdk/Hash.BaaS.Gateway.Sdk.csproj
```

### Generate Keys

Generate an Ed25519 key pair for Developer Portal registration:

```csharp
using Hash.BaaS.Gateway.Sdk;

var keyPair = GatewayKeyGenerator.Generate();

Console.WriteLine(keyPair.KeyId);
Console.WriteLine(keyPair.PublicKeyPem);
Console.WriteLine(keyPair.PrivateKeyPem);
```

Register `PublicKeyPem` in Developer Portal. Keep `PrivateKeyPem` only in your application secret store.

Validate a configured key pair before deployment:

```csharp
GatewayKeyGenerator.ValidateKeyPair(privateKeyPem, publicKeyPem);
```

Derive the key ID from an existing public key:

```csharp
var keyId = GatewayKeyGenerator.DeriveKeyId(publicKeyPem);
```

The .NET SDK derives `keyid` automatically from `PublicKeyPem` during signing.

### Create Client

```csharp
using Hash.BaaS.Gateway.Sdk;

var options = new GatewaySdkOptions
{
    BaseAddress = new Uri("https://gateway.example.com/"),
    ClientId = "00000000-0000-0000-0000-000000000000",
    ProductCode = "TEST_PRODUCT",
    PrivateKeyPem = privateKeyPem,
    PublicKeyPem = publicKeyPem,
    AuditUserId = "my-service",
    AuditSourceType = "Backend"
};

var gateway = HashBaasGatewayClient.Create(options);

var terms = await gateway.GetTermsAsync("en-US");
```

## .NET Typed Gateway Client

### System, Terms, and Country Eligibility

```csharp
await gateway.GetStatusAsync();
await gateway.GetTermsAsync("en-US");

var requirements = await gateway.GetCountryOnboardingRequirementsAsync();
var georgia = await gateway.GetCountryOnboardingRequirementsAsync("GE");
```

### Onboarding

```csharp
var person = await gateway.CreatePersonAsync(createPersonRequest);

await gateway.GetPersonAsync(person.Person.Id);
await gateway.GetPersonByExternalIdAsync("external-person-001");
await gateway.UpdatePersonAsync(person.Person.Id, updatePersonRequest);
await gateway.DeactivatePersonAsync(person.Person.Id);
```

### KYC

```csharp
var check = await gateway.CreateKycCheckAsync(createKycCheckRequest);
var kycCheckId = Guid.Parse(check.KycCheckId);

await gateway.UploadKycDocumentAsync(new UploadKycDocumentRequest
{
    KycCheckId = check.KycCheckId,
    Type = IdvDocumentType.IDVDocument,
    Subtype = IdvDocumentSubtype.Passport,
    FileContent = File.OpenRead("passport.jpg"),
    FileName = "passport.jpg",
    ContentType = "image/jpeg",
    Number = "P1234567",
    Issuer = "GEO"
});

await gateway.InitiateKycCheckAsync(kycCheckId);
await gateway.GetKycCheckAsync(kycCheckId);
await gateway.DeleteKycCheckAsync(kycCheckId);
```

### Accounts

```csharp
var account = await gateway.CreateAccountAsync(createAccountRequest);
var accountId = Guid.Parse(account.Account.Id);

await gateway.GetAccountAsync(accountId);
await gateway.GetAccountCardsAsync(accountId);
await gateway.CloseAccountAsync(accountId, new CloseAccountPatchRequest
{
    CloseReason = AccountCloseReason.ClosedByClient
});
```

### Cards

```csharp
var card = await gateway.CreateCardAsync(createCardRequest);
var cardId = Guid.Parse(card.Card.Id);

await gateway.GetCardAsync(cardId);
await gateway.ActivateCardAsync(cardId);
await gateway.PrepareDigitalCardViewAsync(cardId);
await gateway.BlockCardAsync(cardId, new BlockCardRequest
{
    BlockType = ApiBlockType.BlockedByCardholder
});
await gateway.UnblockCardAsync(cardId);
await gateway.ResetCardPinCounterAsync(cardId);
```

### PIN Management

```csharp
var pinKey = await gateway.GeneratePinKeyAsync(cardId, acceptLanguage: "en-US");

await gateway.SetPinAsync(cardId, new SetPinRequest
{
    PinSet = new PinSetRequestModel
    {
        RequestId = pinKey.PinKey.RequestId,
        PinBlock = encryptedPinBlockHex,
        EncryptedSessionZpk = encryptedSessionZpkBase64
    }
});
```

### Corporate Accounts

Corporate endpoints use the same Ed25519 HTTP-signature authorization as embedded — no bearer token is required or accepted; the corporate customer is resolved from the signing key.

```csharp
var createdAccounts = await gateway.CreateCorporateAccountsAsync(
    new CreateCorporateAccountRequest
    {
        Account = new CreateCorporateAccountModel
        {
            CurrencyCodes = ["GEL", "USD"],
            Name = "Corporate Operating Account",
            ExternalId = "corp-account-001"
        }
    });

var accounts = await gateway.ListCorporateAccountsAsync(
    currency: "GEL",
    statuses: [AccountStatus.Active]);

var accountId = accounts.Accounts[0].AccountId;
await gateway.GetCorporateAccountAsync(accountId);
await gateway.GetCorporateAccountBalancesAsync(accountId);
await gateway.GetCorporateAccountRequisitesAsync(accountId);
await gateway.GetCorporateAccountRestrictionsAsync(accountId);
await gateway.RenameCorporateAccountAsync(
    accountId,
    new RenameCorporateAccountRequest
    {
        Account = new RenameCorporateAccountModel { Name = "Updated Corporate Account" }
    });
await gateway.AddCorporateAccountCurrencyAsync(
    accountId,
    new AddCorporateAccountCurrencyRequest { CurrencyCode = "EUR" });
```

Close is terminal:

```csharp
await gateway.CloseCorporateAccountAsync(
    accountId,
    new CloseCorporateAccountRequest { CloseReason = AccountCloseReason.ClosedByClient });
```

Download statement files:

```csharp
var statement = await gateway.DownloadCorporateAccountStatementAsync(
    accountId,
    StatementFormat.Xlsx,
    fromDate: new DateOnly(2026, 1, 1),
    toDate: new DateOnly(2026, 1, 31),
    language: "en");

File.WriteAllBytes(statement.FileName ?? "statement.xlsx", statement.Content);
```

### Corporate Cards

```csharp
var designTypes = await gateway.ListCorporateCardDesignTypesAsync();

var createdCard = await gateway.CreateCorporateCardAsync(
    new CreateCorporateCardRequest
    {
        Card = new CreateCorporateCardModel
        {
            AccountNumber = "GE12NB0000000123456789",
            Currency = "GEL",
            CardDesignTypeId = designTypes.DesignTypes[0].DesignId,
            IsVirtual = true,
            ExternalId = "corp-card-001",
            NameOnCard = "GIORGI BERIDZE",
            Assignee = new AssigneePersonModel
            {
                Name = "Giorgi",
                Surname = "Beridze",
                DateOfBirth = new DateOnly(1988, 6, 14),
                CitizenshipCountryCode = "GE",
                PersonalId = "01023456789",
                Phone = "+995555111111"
            }
        }
    });

var cards = await gateway.ListCorporateCardsAsync(
    accountId: accountId,
    cardStatuses: [CardStatus.Active, CardStatus.Blocked]);

var cardId = cards.Cards[0].CardId;
await gateway.GetCorporateCardAsync(cardId);
await gateway.ActivateCorporateCardAsync(cardId);
await gateway.PrepareCorporateDigitalCardViewAsync(cardId);
await gateway.FreezeCorporateCardAsync(cardId);
await gateway.UnfreezeCorporateCardAsync(cardId);
await gateway.ResetCorporateCardPinCounterAsync(cardId);
await gateway.CloseCorporateCardAsync(cardId);
```

Corporate PIN flow:

```csharp
var corporatePinKey = await gateway.GenerateCorporatePinKeyAsync(
    cardId,
    acceptLanguage: "en-US");

await gateway.SetCorporatePinAsync(
    cardId,
    new SetPinRequest
    {
        PinSet = new PinSetRequestModel
        {
            RequestId = corporatePinKey.PinKey.RequestId,
            PinBlock = encryptedPinBlockHex,
            EncryptedSessionZpk = encryptedSessionZpkBase64
        }
    });
```

### Corporate Transactions

```csharp
var transactions = await gateway.ListCorporateTransactionsAsync(
    cardId: cardId,
    accountNumber: "GE00...",
    currency: "GEL",
    transactionStatuses: [TransactionStatus.Posted],
    currencyCode: "USD",
    fromDate: new DateOnly(2026, 1, 1),
    toDate: new DateOnly(2026, 12, 31));

if (transactions.Transactions.Count > 0)
{
    await gateway.GetCorporateTransactionAsync(
        transactions.Transactions[0].TransactionId);
}
```

### Corporate FX — Rates & Exchange

```csharp
// Rate board for a pair (no amount).
var board = await gateway.GetCorporateRateBoardAsync("USD", "GEL");
// board.SellRate, board.BuyRate

// Indicative, customer-scoped conversion quote (amount supplied).
var quote = await gateway.GetCorporateConversionQuoteAsync("USD", "GEL", amount: 150m);
// quote.ConvertedAmount, quote.Rate

// Own-accounts, market-rate exchange. from == to is valid (each currency shares one IBAN).
var exchange = await gateway.MakeCorporateExchangeAsync(new MakeCorporateExchangeRequest
{
    FromAccountNumber = "GE00...",
    ToAccountNumber   = "GE00...",
    Amount            = 150m,
    Currency          = "USD",
    ToCurrency        = "GEL",
});
// exchange.Id
```

### Error Handling

Non-success responses throw `GatewayApiException` and keep the raw Gateway response:

```csharp
try
{
    await gateway.CreateAccountAsync(request);
}
catch (GatewayApiException ex)
{
    Console.WriteLine(ex.StatusCode);
    Console.WriteLine(ex.ResponseBody);
}
```

## Request Signing Profile

Body-less requests sign:

```text
"@method" "@target-uri" "@authority"
"x-product-code" "x-client-id" "x-audit-source-type" "x-audit-user-id"
```

Corporate requests sign the **same** component set as the embedded requests (including `x-client-id`); there is no `authorization` component.

Body-bearing and mutating requests sign:

```text
"@method" "@target-uri" "@authority"
"content-digest" "content-type"
"x-product-code" "x-client-id" "x-audit-source-type" "x-audit-user-id"
"idempotency-key"
```

Signature metadata:

```text
label = "sig1"
alg   = "ed25519"
tag   = "hash-baas-v1"
```

For `POST`, `PUT`, `PATCH`, and `DELETE`, the SDK automatically adds:

- `Content-Digest`
- `Idempotency-Key`
- signed `content-digest`
- signed `content-type`
- signed `idempotency-key`

## Response Verification

Manual response verification is available when you have the Gateway platform public key:

```csharp
var verification = await GatewayResponseVerifier.VerifyAsync(
    response,
    platformPublicKeyPem,
    expectedProductCode: "TEST_PRODUCT");

if (!verification.Verified)
{
    Console.WriteLine(verification.Error);
}
```

Verification checks:

- `Signature-Input`
- `Signature`
- `Content-Digest`
- response body integrity
- Ed25519 signature against the platform public key

## Configuration Reference

```json
{
  "HashBaasGateway": {
    "BaseAddress": "https://gateway.example.com/",
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "ProductCode": "TEST_PRODUCT",
    "PrivateKeyPem": "-----BEGIN PRIVATE KEY-----...",
    "PublicKeyPem": "-----BEGIN PUBLIC KEY-----...",
    "AuditUserId": "my-service",
    "AuditSourceType": "Backend"
  }
}
```

| Option | Required | Description |
| --- | --- | --- |
| `BaseAddress` | Yes | Gateway base URL |
| `ClientId` | Yes | Developer Portal client ID |
| `ProductCode` | Yes | Product definition code, for example `TEST_PRODUCT` |
| `PrivateKeyPem` | Yes | Ed25519 PKCS#8 private key |
| `PublicKeyPem` | Yes | Ed25519 SPKI public key registered in Developer Portal |
| `AuditUserId` | Yes | Calling service/user identifier |
| `AuditSourceType` | Yes | Calling channel, usually `Backend` |

## Packaging

Build:

```bash
dotnet build src/Hash.BaaS.Gateway.Sdk/Hash.BaaS.Gateway.Sdk.csproj -c Release
```

Pack:

```bash
dotnet pack src/Hash.BaaS.Gateway.Sdk/Hash.BaaS.Gateway.Sdk.csproj -c Release -o artifacts
```

The NuGet package targets:

```text
net8.0
net10.0
```

## Repository Layout

```text
hash-baas-gateway-dotnet-sdk/
|-- src/
|   `-- Hash.BaaS.Gateway.Sdk/        # .NET SDK and NuGet package source
|-- README.md
`-- LICENSE
```

## Security Checklist

- Use separate keys for development, staging, and production.
- Register only public keys in Developer Portal.
- Store private keys in a secret manager or secure environment configuration.
- Rotate keys periodically.
- Validate key pairs before deployment.
- Use response verification when the platform public key is available.
- Do not commit private keys.
- Do not reuse demo keys in production.

## License

This repository is licensed under the MIT License. See [LICENSE](LICENSE).
