using System.Net;
using System.Net.Http.Json;
using Example.Com.InterviewQuestion002.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Com.InterviewQuestion002.Tests;

public sealed class RegistrationApiTests
{
    private const string LongPassword = "Correct Horse Battery Staple";

    [Fact]
    public async Task Register_creates_account_and_never_returns_password_material()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "  Narin  ",
            password = LongPassword,
            confirmPassword = LongPassword
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        Assert.NotNull(json);
        Assert.Equal("Narin", json["username"].GetString());
        Assert.Equal(2, json.Count);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", responseBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var storedUser = await database.Users.AsNoTracking().SingleAsync();
        Assert.Equal("NARIN", storedUser.NormalizedUsername);
        Assert.NotEqual(LongPassword, storedUser.PasswordHash);
        Assert.NotEmpty(storedUser.PasswordHash);
    }

    [Theory]
    [InlineData("", "long-enough-password", "long-enough-password")]
    [InlineData("valid-user", "short", "short")]
    [InlineData("valid-user", "long-enough-password", "different-password")]
    public async Task Register_rejects_missing_or_invalid_credentials(string username, string password, string confirmation)
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password,
            confirmPassword = confirmation
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_accepts_exactly_fifteen_unicode_code_points()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        const string password = "😀😀😀😀😀😀😀😀😀😀😀😀😀😀😀";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "unicode-user",
            password,
            confirmPassword = password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_password_longer_than_128_unicode_code_points()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var password = string.Concat(Enumerable.Repeat("😀", 129));

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "unicode-user",
            password,
            confirmPassword = password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_accepts_128_unicode_code_points()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        var password = string.Concat(Enumerable.Repeat("😀", 128));

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "maximum-password-user",
            password,
            confirmPassword = password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_14_unicode_code_points()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        const string password = "12345678901234";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "minimum-password-user",
            password,
            confirmPassword = password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_accepts_64_unicode_code_point_usernames()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = new string('u', 64),
            password = LongPassword,
            confirmPassword = LongPassword
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_65_unicode_code_point_usernames()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = new string('u', 65),
            password = LongPassword,
            confirmPassword = LongPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_treats_unicode_and_case_equivalent_usernames_as_duplicates()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "  Café  ",
            password = LongPassword,
            confirmPassword = LongPassword
        });
        using var second = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "CAFE\u0301",
            password = LongPassword,
            confirmPassword = LongPassword
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
