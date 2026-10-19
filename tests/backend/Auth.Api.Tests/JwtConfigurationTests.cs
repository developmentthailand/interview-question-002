using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Example.Com.InterviewQuestion002.Tests;

public sealed class JwtConfigurationTests
{
    [Fact]
    public void Application_startup_requires_a_valid_external_signing_key()
    {
        using var factory = new MissingJwtConfigurationFactory();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("JWT", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Convert.ToBase64String(AuthApiFactory.TestSigningKey), exception.ToString(), StringComparison.Ordinal);
    }

    private sealed class MissingJwtConfigurationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }
}
