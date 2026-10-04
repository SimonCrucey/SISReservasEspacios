using System.Text.RegularExpressions;

namespace SISReservas.Api.Security;

public static class PasswordPolicy
{
    private static readonly Regex PasswordRegex =
        new(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$");

    public static bool IsValid(string password)
    {
        return !string.IsNullOrWhiteSpace(password)
               && PasswordRegex.IsMatch(password);
    }
}