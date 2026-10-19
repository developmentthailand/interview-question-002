using System.ComponentModel.DataAnnotations;

namespace Example.Com.InterviewQuestion002.Contracts;

public sealed class RegisterRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; init; } = string.Empty;
}
