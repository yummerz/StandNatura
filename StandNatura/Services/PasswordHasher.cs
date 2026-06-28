using System;
using System.Security.Cryptography;

namespace StandNatura.Services
{
    /// <summary>
    /// Salted password hashing using PBKDF2 (SHA-256), built into .NET.
    /// Stores a per-user random salt and the derived hash, both as Base64.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;        // 128-bit salt
        private const int HashSize = 32;        // 256-bit hash
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;

        // Returns (Base64 hash, Base64 salt) for a brand-new password.
        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algo, HashSize);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        // Re-hashes the entered password with the stored salt and compares (constant-time).
        public static bool Verify(string password, string storedHashBase64, string saltBase64)
        {
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] storedHash = Convert.FromBase64String(storedHashBase64);
            byte[] computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algo, HashSize);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash);
        }
    }
}
