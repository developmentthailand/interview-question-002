namespace Example.Com.InterviewQuestion002.Contracts;

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc);
