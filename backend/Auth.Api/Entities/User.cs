namespace Example.Com.InterviewQuestion002.Entities;

public sealed class User
{
    public int Id { get; set; }

    public required string Username { get; set; }

    public required string NormalizedUsername { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
