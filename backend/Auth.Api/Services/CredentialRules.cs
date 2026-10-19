using System.Text;

namespace Example.Com.InterviewQuestion002.Services;

public static class CredentialRules
{
    public static bool TryNormalizeUsername(string? value, out string username, out string normalized, out string error)
    {
        username = value?.Trim() ?? string.Empty;
        normalized = string.Empty;
        error = "Enter a username containing 1 to 64 characters without control characters.";

        if (username.Length == 0)
        {
            return false;
        }

        var usernameLength = 0;
        foreach (var rune in username.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || ++usernameLength > 64)
            {
                return false;
            }
        }

        try
        {
            normalized = username.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
            foreach (var rune in normalized.EnumerateRunes())
            {
                if (Rune.IsControl(rune))
                {
                    return false;
                }
            }

            return normalized.Length <= 256;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password) =>
        password is not null && password.EnumerateRunes().Count() is >= 15 and <= 128;
}
