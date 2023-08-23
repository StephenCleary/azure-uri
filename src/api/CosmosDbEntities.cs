namespace Api;

#pragma warning disable IDE1006

public record SlugEntity(
    string id,
    string slug,
    string url);

public record LogEntity(
    string id,
    DateTimeOffset timestamp,
    string slug,
    string requestUrl,
    string requestHeaders,
    string clientIpAddress,
    int resultStatusCode,
    string resultUrl);
