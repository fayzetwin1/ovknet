using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Ovk.Net.Core.Security;

public static class ChandlerPasswordHasher
{
    public static bool VerifyHash(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 2) return false;

            var hashHex = parts[0];
            var saltHex = parts[1];

            var salt = Convert.FromHexString(saltHex);
            var expectedHash = Convert.FromHexString(hashHex);

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 1,
                MemorySize = 65536, // 64 MB (SODIUM_CRYPTO_PWHASH_MEMLIMIT_INTERACTIVE)
                Iterations = 2      // (SODIUM_CRYPTO_PWHASH_OPSLIMIT_INTERACTIVE)
            };

            var hash = argon2.GetBytes(16); // 16 bytes output length requested by sodium
            
            return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public static string MakeHash(string password)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 1,
            MemorySize = 65536,
            Iterations = 2
        };

        var hash = argon2.GetBytes(16);
        return $"{Convert.ToHexString(hash).ToLower()}${Convert.ToHexString(salt).ToLower()}";
    }
}
