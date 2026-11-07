namespace MeloongCore;

public class HttpRequestCodeException(string message, HttpStatusCode statusCode, string? response) : HttpRequestException(message) {

    public HttpStatusCode StatusCode { get; } = statusCode;

    public string? Response { get; } = response;
}
