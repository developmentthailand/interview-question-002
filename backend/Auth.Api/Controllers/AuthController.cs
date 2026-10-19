using System.Globalization;
using System.Security.Claims;
using Example.Com.InterviewQuestion002.Contracts;
using Example.Com.InterviewQuestion002.Data;
using Example.Com.InterviewQuestion002.Entities;
using Example.Com.InterviewQuestion002.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Example.Com.InterviewQuestion002.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AuthDbContext database,
    IPasswordHasher<User> passwordHasher,
    AccessTokenService accessTokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisteredUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!CredentialRules.TryNormalizeUsername(request.Username, out var username, out var normalizedUsername, out var usernameError))
        {
            errors[nameof(request.Username)] = [usernameError];
        }

        if (!CredentialRules.IsValidPassword(request.Password))
        {
            errors[nameof(request.Password)] = ["Password must contain 15 to 128 Unicode characters."];
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            errors[nameof(request.ConfirmPassword)] = ["Password and confirmation must match exactly."];
        }

        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var user = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        database.Users.Add(user);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Username already exists",
                detail: "Choose a different username.");
        }

        return StatusCode(StatusCodes.Status201Created, new RegisteredUserResponse(user.Id, user.Username));
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!CredentialRules.TryNormalizeUsername(request.Username, out _, out var normalizedUsername, out _))
        {
            return InvalidCredentials();
        }

        var user = await database.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.NormalizedUsername == normalizedUsername, cancellationToken);

        if (user is null)
        {
            return InvalidCredentials();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var trackedUser = await database.Users.SingleAsync(candidate => candidate.Id == user.Id, cancellationToken);
            trackedUser.PasswordHash = passwordHasher.HashPassword(trackedUser, request.Password);
            await database.SaveChangesAsync(cancellationToken);
        }

        var token = accessTokenService.Create(user);
        return Ok(new LoginResponse(token.Token, "Bearer", token.ExpiresAtUtc));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var idClaim = User.FindFirstValue("sub");
        var username = User.FindFirstValue("username");
        if (!int.TryParse(idClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(id, username));
    }

    private IActionResult InvalidCredentials() => Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Invalid username or password",
        detail: "Check your credentials and try again.");

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 or 2067 };
}
