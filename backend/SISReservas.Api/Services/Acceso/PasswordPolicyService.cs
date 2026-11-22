using System.Text.RegularExpressions;

namespace SISReservas.Api.Services;

public class PasswordPolicyService
{
    public bool IsValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < 8)
            return false;

        var hasLetter = password.Any(char.IsLetter);
        var hasNumber = password.Any(char.IsDigit);

        return hasLetter && hasNumber;
    }
}