# Hash BaaS Gateway .NET SDK

![SDK](https://img.shields.io/badge/SDK-Hash%20BaaS-111827?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4?style=for-the-badge&logo=dotnet)
![RFC 9421](https://img.shields.io/badge/RFC%209421-HTTP%20Signatures-0F766E?style=for-the-badge)
![Ed25519](https://img.shields.io/badge/Ed25519-Signatures-1F2937?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-0A0A0A?style=for-the-badge)

Production-ready .NET SDK and NuGet package for Hash BaaS Gateway authorization, request signing, response verification, and Gateway API usage.

The SDK removes the low-level complexity of Hash BaaS Gateway integration:

- Generates and validates Ed25519 key pairs.
- Signs requests using the Hash BaaS RFC 9421 profile.
- Adds required Gateway headers consistently.
- Adds `Content-Digest` and `Idempotency-Key` for mutating requests.
- Verifies signed Gateway responses when the platform public key is configured.
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
    participant SDK as Hash BaaS Gateway .NET SDK
    participant Portal as Developer Portal
    participant Gateway as Hash BaaS Gateway

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

Hash BaaS Gateway uses signed HTTP requests instead of bearer-token authorization for partner-facing Gateway operations.

Every Gateway request must include:

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
    AuditSourceType = "Backend",
    ResponseSigningPublicKeyPem = gatewayPlatformPublicKeyPem
};

var gateway = HashBaasGatewayClient.Create(options);

var terms = await gateway.GetTermsAsync("en-US");
```

## .NET Typed Gateway Client

### System and Terms

```csharp
await gateway.GetStatusAsync();
await gateway.GetTermsAsync("en-US");
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

await gateway.UploadKycDocumentAsync(new UploadKycDocumentRequest
{
    KycCheckId = check.KycCheck.Id.ToString(),
    Type = IdvDocumentType.IDVDocument,
    Subtype = IdvDocumentSubtype.Passport,
    FileContent = File.OpenRead("passport.jpg"),
    FileName = "passport.jpg",
    ContentType = "image/jpeg",
    Number = "P1234567",
    Issuer = "GEO"
});

await gateway.InitiateKycCheckAsync(check.KycCheck.Id);
await gateway.GetKycCheckAsync(check.KycCheck.Id);
await gateway.DeleteKycCheckAsync(check.KycCheck.Id);
```

### Accounts

```csharp
var account = await gateway.CreateAccountAsync(createAccountRequest);

await gateway.GetAccountAsync(account.Account.Id);
await gateway.GetAccountCardsAsync(account.Account.Id);
await gateway.CloseAccountAsync(account.Account.Id, new CloseAccountPatchRequest
{
    CloseReason = AccountCloseReason.ClosedByClient
});
```

### Cards

```csharp
var card = await gateway.CreateCardAsync(createCardRequest);

await gateway.GetCardAsync(card.Card.Id);
await gateway.ActivateCardAsync(card.Card.Id);
await gateway.PrepareDigitalCardViewAsync(card.Card.Id);
await gateway.BlockCardAsync(card.Card.Id, new BlockCardRequest
{
    BlockType = ApiBlockType.BlockedByCardholder
});
await gateway.UnblockCardAsync(card.Card.Id);
await gateway.ResetCardPinCounterAsync(card.Card.Id);
```

### PIN Management

```csharp
var pinKey = await gateway.GeneratePinKeyAsync(card.Card.Id, acceptLanguage: "en-US");

await gateway.SetPinAsync(card.Card.Id, new SetPinRequest
{
    PinSet = new PinSetRequestModel
    {
        RequestId = pinKey.PinKey.RequestId,
        PinBlock = encryptedPinBlockHex,
        EncryptedSessionZpk = encryptedSessionZpkBase64
    }
});
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

If `ResponseSigningPublicKeyPem` is configured in .NET options, the typed client verifies signed Gateway responses automatically.

Manual response verification is also available:

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
    "AuditSourceType": "Backend",
    "ResponseSigningPublicKeyPem": "-----BEGIN PUBLIC KEY-----..."
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
| `ResponseSigningPublicKeyPem` | No | Gateway platform public key for response verification |

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
