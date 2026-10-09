using System.Security.Cryptography;
using System.Text;

namespace FHIRAPI.Helpers;

/// <summary>
/// Small security helpers: random ids for tokens and codes, PKCE hashing, safe secret comparison.
/// </summary>
public static class RandomIds
{
    /// <summary>Creates a random URL-safe id from the given number of random bytes.</summary>
    public static string New(int bytes = 32) => Base64Url(RandomNumberGenerator.GetBytes(bytes));

    /// <summary>PKCE S256: base64url(SHA-256(code_verifier)).</summary>
    public static string Sha256Base64Url(string value) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(value)));

    /// <summary>Compares two secrets in constant time (prevents timing attacks on the internal key).</summary>
    public static bool SecretEquals(string? a, string? b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a ?? ""), Encoding.UTF8.GetBytes(b ?? ""));

    /// <summary>Base64 without padding, using - and _ instead of + and /.</summary>
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
