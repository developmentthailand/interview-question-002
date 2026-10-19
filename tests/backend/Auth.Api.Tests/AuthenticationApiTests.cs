using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Example.Com.InterviewQuestion002.Contracts;
using Example.Com.InterviewQuestion002.Data;
using Example.Com.InterviewQuestion002.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Example.Com.InterviewQuestion002.Tests;

public sealed class AuthenticationApiTests
{
    private const string Username = "narin";
    private const string Password = "Correct Horse Battery Staple";

    [Fact]
    public async Task Login_issues_a_short_lived_token_that_the_protected_endpoint_accepts()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, Username, Password);

        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "NARIN", password = Password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.Equal("Bearer", login.TokenType);
        Assert.True(login.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.True(login.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(16));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var meResponse = await client.GetAsync("/api/auth/me");
        var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal(Username, currentUser.Username);
    }

    [Fact]
    public async Task Login_returns_the_same_generic_error_for_unknown_user_and_wrong_password()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, Username, Password);

        using var unknownResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "missing", password = Password });
        using var wrongPasswordResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = "some wrong password" });
        using var unknownBody = System.Text.Json.JsonDocument.Parse(await unknownResponse.Content.ReadAsStringAsync());
        using var wrongPasswordBody = System.Text.Json.JsonDocument.Parse(await wrongPasswordResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(unknownBody.RootElement.GetProperty("title").GetString(), wrongPasswordBody.RootElement.GetProperty("title").GetString());
        Assert.Equal(unknownBody.RootElement.GetProperty("detail").GetString(), wrongPasswordBody.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Protected_endpoint_rejects_a_missing_bearer_token()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("not-a-jwt")]
    [InlineData("")]
    public async Task Protected_endpoint_rejects_malformed_bearer_tokens(string token)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_expired_tokens()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-2)));

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    public async Task Protected_endpoint_rejects_tokens_with_invalid_trust_metadata(string invalidPart)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var token = invalidPart switch
        {
            "issuer" => CreateToken(issuer: "https://wrong-issuer.test"),
            "audience" => CreateToken(audience: "wrong-audience"),
            "signature" => CreateToken(key: Enumerable.Repeat((byte)0xA5, 32).ToArray()),
            _ => throw new ArgumentOutOfRangeException(nameof(invalidPart))
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_limits_the_sixth_attempt_and_returns_retry_after()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "missing", password = Password });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        using var limitedResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = "missing", password = Password });

        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        Assert.True(limitedResponse.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter!.Single()) > 0);
    }

    [Fact]
    public async Task Login_rehashes_passwords_when_the_stored_hash_uses_old_parameters()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        await client.GetAsync("/api/auth/me");

        int userId;
        string oldHash;
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = new User
            {
                Username = Username,
                NormalizedUsername = "NARIN",
                PasswordHash = string.Empty,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            var legacyHasher = new PasswordHasher<User>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }));
            user.PasswordHash = legacyHasher.HashPassword(user, Password);
            oldHash = user.PasswordHash;
            database.Users.Add(user);
            await database.SaveChangesAsync();
            userId = user.Id;
        }

        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = Username, password = Password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDatabase = verifyScope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var rehashed = await verifyDatabase.Users.AsNoTracking().SingleAsync(user => user.Id == userId);
        Assert.NotEqual(oldHash, rehashed.PasswordHash);
    }

    private static async Task RegisterAsync(HttpClient client, string username, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            confirmPassword = password
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string CreateToken(
        string? issuer = null,
        string? audience = null,
        byte[]? key = null,
        DateTimeOffset? expiresAt = null)
    {
        var signingKey = new SymmetricSecurityKey(key ?? AuthApiFactory.TestSigningKey);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var issuedAt = expiresAt is { } expired && expired < DateTimeOffset.UtcNow
            ? expired.AddMinutes(-1)
            : DateTimeOffset.UtcNow.AddMinutes(-1);
        var token = new JwtSecurityToken(
            issuer: issuer ?? AuthApiFactory.TestIssuer,
            audience: audience ?? AuthApiFactory.TestAudience,
            claims:
            [
                new Claim("sub", "1"),
                new Claim("username", Username)
            ],
            notBefore: issuedAt.UtcDateTime,
            expires: (expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(10)).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
