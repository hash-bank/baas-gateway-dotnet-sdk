package ge.hashbank.baas.gateway;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;

public final class HashBaasGatewayClient {
    private final GatewaySdkOptions options;
    private final HttpClient httpClient;

    public HashBaasGatewayClient(GatewaySdkOptions options) {
        this(options, HttpClient.newHttpClient());
    }

    public HashBaasGatewayClient(GatewaySdkOptions options, HttpClient httpClient) {
        this.options = options;
        this.httpClient = httpClient;
    }

    public HttpResponse<String> send(String method, String relativePath, String jsonBody, String contentType)
            throws java.io.IOException, InterruptedException {
        var uri = options.baseAddress().resolve(relativePath);
        var body = jsonBody == null ? null : jsonBody.getBytes(StandardCharsets.UTF_8);
        var request = GatewayRequestSigner.sign(method, uri, body, contentType, options);
        return httpClient.send(request, HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
    }

    public HttpResponse<String> send(String method, URI absoluteUri, String jsonBody, String contentType)
            throws java.io.IOException, InterruptedException {
        var body = jsonBody == null ? null : jsonBody.getBytes(StandardCharsets.UTF_8);
        var request = GatewayRequestSigner.sign(method, absoluteUri, body, contentType, options);
        return httpClient.send(request, HttpResponse.BodyHandlers.ofString(StandardCharsets.UTF_8));
    }
}
