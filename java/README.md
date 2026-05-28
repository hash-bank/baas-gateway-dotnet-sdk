# Hash BaaS Gateway Java SDK

Java SDK for Hash BaaS Gateway request authorization.

Supported Java version:

- Java 17+

The SDK provides:

- Ed25519 key generation
- PEM import/export
- Key ID derivation from SPKI public key PEM
- RFC 9421 `Signature-Input` and `Signature` headers
- `Content-Digest` and `Idempotency-Key` for unsafe requests
- Signed Java `HttpClient` requests

## Usage

```java
import ge.hashbank.baas.gateway.*;

var keyPair = GatewayKeyGenerator.generate();

var options = new GatewaySdkOptions(
    URI.create("https://gateway.example.com/"),
    "00000000-0000-0000-0000-000000000000",
    "TEST_PRODUCT",
    keyPair.privateKeyPem(),
    keyPair.publicKeyPem(),
    "my-service",
    "Backend",
    300
);

var client = new HashBaasGatewayClient(options);
var response = client.send("GET", "v1/terms", null, "application/json");

System.out.println(response.statusCode());
System.out.println(response.body());
```

Register `publicKeyPem` in Developer Portal. Store `privateKeyPem` in a secure secret store.

## Package

```bash
mvn package
```
