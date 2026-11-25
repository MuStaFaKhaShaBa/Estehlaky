using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Estehlaky.API.Contracts;
using Estehlaky.Application.Auth;
using Estehlaky.Application.Common.Results;
using Estehlaky.Application.Me;

namespace Estehlaky.API.Tests.Support;

/// <summary>A signed-up user plus the tokens from the sign-up response.</summary>
public sealed record TestAccount(string Email, string Password, AuthResponseDto Auth)
{
    public string AccessToken => Auth.AccessToken;
    public string RefreshToken => Auth.RefreshToken;
    public Guid UserId => Auth.User.Id;
}

/// <summary>Every successful response is wrapped like this: { success, statusCode, message, data }.</summary>
public sealed record ApiEnvelope<T>(bool Success, int StatusCode, string? Message, T? Data);

/// <summary>Error body of the API (RFC 7807 plus the stable "code" and "fieldErrors" extensions).</summary>
public sealed record ApiProblem(
    string? Title, int? Status, string? Detail, string? Code, string? TraceId, List<FieldError>? FieldErrors);

public sealed class ApiClient(HttpClient http) : IDisposable
{
    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, string? bearer = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: Json.Options);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return http.SendAsync(request);
    }

    public Task<HttpResponseMessage> GetAsync(string url, string? bearer = null) =>
        SendAsync(HttpMethod.Get, url, null, bearer);

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null, string? bearer = null) =>
        SendAsync(HttpMethod.Post, url, body ?? new { }, bearer);

    // ---- raw calls (any status) -------------------------------------------------

    public Task<HttpResponseMessage> RegisterAsync(RegisterRequest request) =>
        PostAsync("/api/v1/auth/register", request);

    public Task<HttpResponseMessage> LoginAsync(string email,string? phone ,string password) =>
        PostAsync("/api/v1/auth/login", new LoginRequest(email,phone, password, null));

    public Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        PostAsync("/api/v1/auth/refresh", new RefreshRequest(refreshToken, null));

    public Task<HttpResponseMessage> LogoutAsync(string refreshToken) =>
        PostAsync("/api/v1/auth/logout", new LogoutRequest(refreshToken));

    public Task<HttpResponseMessage> LogoutAllAsync(string? accessToken) =>
        PostAsync("/api/v1/auth/logout-all", null, accessToken);

    public Task<HttpResponseMessage> MeAsync(string? accessToken) =>
        GetAsync("/api/v1/me", accessToken);

    // ---- happy-path helpers (assert the expected status) -------------------------

    public async Task<TestAccount> NewAccountAsync(string? password = null, string language = "en")
    {
        var email = TestConfig.NewEmail();
        password ??= TestConfig.Password;

        var response = await RegisterAsync(new RegisterRequest(email, password, "Test User",null, language, null));
        await response.ExpectAsync(HttpStatusCode.Created);
        return new TestAccount(email, password, await response.ReadAsync<AuthResponseDto>());
    }

    public async Task<AuthResponseDto> SignInAsync(string email, string? phone, string password)
    {
        var response = await LoginAsync(email, phone, password);
        await response.ExpectAsync(HttpStatusCode.OK);
        return await response.ReadAsync<AuthResponseDto>();
    }

    public async Task<AuthResponseDto> RefreshOkAsync(string refreshToken)
    {
        var response = await RefreshAsync(refreshToken);
        await response.ExpectAsync(HttpStatusCode.OK);
        return await response.ReadAsync<AuthResponseDto>();
    }

    public async Task<UserDto> GetMeAsync(string accessToken)
    {
        var response = await MeAsync(accessToken);
        await response.ExpectAsync(HttpStatusCode.OK);
        return await response.ReadAsync<UserDto>();
    }

    public void Dispose() => http.Dispose();
}

internal static class ResponseExtensions
{
    /// <summary>Asserts the status code; the failure message includes the body so CI logs are useful.</summary>
    public static async Task<HttpResponseMessage> ExpectAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected HTTP {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
        return response;
    }

    /// <summary>Reads the "data" part of a successful { success, statusCode, message, data } response.</summary>
    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var envelope = System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<T>>(body, Json.Options);

        Assert.True(envelope is { Success: true }, $"Expected a success envelope but got: {body}");
        return envelope!.Data ?? throw new InvalidOperationException($"The response has no data; expected {typeof(T).Name}. Body: {body}");
    }

    /// <summary>Asserts an error response: status, JSON content type, stable code and a trace id.</summary>
    public static async Task<ApiProblem> ExpectProblemAsync(
        this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        await response.ExpectAsync(status);
        // The API sends errors as application/json; RFC 7807 clients would expect application/problem+json.
        Assert.Contains(response.Content.Headers.ContentType?.MediaType, new[] { "application/json", "application/problem+json" });

        var problem = await response.ReadAsync<ApiProblem>();
        Assert.Equal(code, problem.Code);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
        return problem;
    }
}
