using System.Globalization;
using System.Security.Cryptography;

namespace Luminalium.Core.Security;

/// <summary>
/// Creates and verifies password hashes in the format
/// <c>pbkdf2$sha256$iterations$salt$derived</c>, where the last two fields
/// are unpadded base64url values.
/// </summary>
public sealed class PasswordHashService : IPasswordHashService
{
    public const string AlgorithmPrefix = "pbkdf2";
    public const string HashAlgorithmName = "sha256";
    public const int DefaultIterations = 600_000;
    public const int MinimumIterations = 100_000;
    public const int MaximumIterations = 10_000_000;
    public const int SaltLength = 16;
    public const int DerivedKeyLength = 32;

    /// <summary>
    /// Hashes a non-empty password. Returns <see langword="null"/> for an
    /// empty password or if the supplied iteration count is invalid.
    /// </summary>
    public string? HashPassword(string? password, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password) || !IsValidIterations(iterations))
        {
            return null;
        }

        var salt = new byte[SaltLength];
        RandomNumberGenerator.Fill(salt);

        byte[]? derived = null;
        try
        {
            derived = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                DerivedKeyLength);

            return string.Join(
                '$',
                AlgorithmPrefix,
                HashAlgorithmName,
                iterations.ToString(CultureInfo.InvariantCulture),
                EncodeBase64Url(salt),
                EncodeBase64Url(derived));
        }
        finally
        {
            if (derived is not null)
            {
                CryptographicOperations.ZeroMemory(derived);
            }

            CryptographicOperations.ZeroMemory(salt);
        }
    }

    /// <summary>
    /// Verifies a candidate password without throwing for malformed or
    /// unsupported hash values.
    /// </summary>
    public bool VerifyPassword(string? password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(password) || !TryParseHash(passwordHash, out var parsed))
        {
            return false;
        }

        byte[]? derived = null;
        try
        {
            derived = Rfc2898DeriveBytes.Pbkdf2(
                password,
                parsed.Salt,
                parsed.Iterations,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                DerivedKeyLength);
            return CryptographicOperations.FixedTimeEquals(derived, parsed.Derived);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
        finally
        {
            if (derived is not null)
            {
                CryptographicOperations.ZeroMemory(derived);
            }

            CryptographicOperations.ZeroMemory(parsed.Salt);
            CryptographicOperations.ZeroMemory(parsed.Derived);
        }
    }

    private static bool TryParseHash(string? passwordHash, out ParsedHash parsed)
    {
        parsed = default;

        if (string.IsNullOrEmpty(passwordHash) || passwordHash.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var fields = passwordHash.Split('$');
        if (fields.Length != 5 ||
            fields[0] != AlgorithmPrefix ||
            fields[1] != HashAlgorithmName ||
            !int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            !IsValidIterations(iterations))
        {
            return false;
        }

        if (!TryDecodeBase64Url(fields[3], out var salt))
        {
            return false;
        }

        if (salt.Length != SaltLength)
        {
            CryptographicOperations.ZeroMemory(salt);
            return false;
        }

        if (!TryDecodeBase64Url(fields[4], out var derived))
        {
            CryptographicOperations.ZeroMemory(salt);
            return false;
        }

        if (derived.Length != DerivedKeyLength)
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(derived);
            return false;
        }

        parsed = new ParsedHash(iterations, salt, derived);
        return true;
    }

    private static bool IsValidIterations(int iterations) =>
        iterations is >= MinimumIterations and <= MaximumIterations;

    private static string EncodeBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static bool TryDecodeBase64Url(string value, out byte[] bytes)
    {
        bytes = [];
        if (value.Length == 0 || value.Contains('=') || value.Any(character =>
                character is not (>= 'A' and <= 'Z') and
                not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and
                not '-' and
                not '_') || value.Length % 4 == 1)
        {
            return false;
        }

        var padded = value.Replace('-', '+').Replace('_', '/') +
            new string('=', (4 - value.Length % 4) % 4);

        try
        {
            bytes = Convert.FromBase64String(padded);
            if (EncodeBase64Url(bytes) == value)
            {
                return true;
            }

            CryptographicOperations.ZeroMemory(bytes);
            bytes = [];
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private readonly record struct ParsedHash(int Iterations, byte[] Salt, byte[] Derived);
}
