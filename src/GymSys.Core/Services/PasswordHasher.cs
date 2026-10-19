using System.Security.Cryptography;

namespace GymSys.Core.Services;

/// <summary>PBKDF2 SHA-256, 100.000 iterasi, salt 16 byte, hash 32 byte (NFR-03).</summary>
public static class PasswordHasher
{
    public const int SaltSize = 16;
    public const int HashSize = 32;
    public const int Iterations = 100_000;

    public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

    public static bool Verify(string password, byte[] salt, byte[] expectedHash) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, salt), expectedHash);
}
