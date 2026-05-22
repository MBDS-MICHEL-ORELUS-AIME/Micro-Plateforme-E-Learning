using System.Security.Cryptography;
using System.Text;

namespace E_learningProject.Web.Security;

public static class PasswordSecurity
{
    private const string Prefix = "sha256:";

    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Prefix + Convert.ToBase64String(bytes);
    }

    public static bool Verify(string password, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        if (stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return string.Equals(Hash(password), stored, StringComparison.Ordinal);
        }

        // Backward compatibility for old seeded/plain values.
        return string.Equals(password, stored, StringComparison.Ordinal);
    }

    public static bool IsHashed(string stored)
        => !string.IsNullOrWhiteSpace(stored) && stored.StartsWith(Prefix, StringComparison.Ordinal);
}
