using System.Security.Cryptography;

namespace Spacebox.Network.Auth;

public static class PasswordCrypto
{
    public const int SaltSize = 16;
    public const int KeySize = 32;
    public const int NonceSize = 32;
    public const int DefaultIterations = 100_000;
    public const int MinIterations = 10_000;
    public const int MaxIterations = 1_000_000;
    public const int MinPasswordLength = 4;
    public const int MaxPasswordLength = 64;

    public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] NewNonce() => RandomNumberGenerator.GetBytes(NonceSize);

    public static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password ?? "", salt, Math.Clamp(iterations, MinIterations, MaxIterations), HashAlgorithmName.SHA256, KeySize);
    }

    public static byte[] ComputeProof(byte[] key, byte[] nonce)
    {
        return HMACSHA256.HashData(key, nonce);
    }

    public static bool VerifyProof(byte[] key, byte[] nonce, byte[] proof)
    {
        if (key == null || key.Length != KeySize || nonce == null || proof == null) return false;
        return CryptographicOperations.FixedTimeEquals(ComputeProof(key, nonce), proof);
    }

    public static bool IsValidKey(byte[] key) => key != null && key.Length == KeySize;

    public static bool TryValidatePassword(string password, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            error = $"The password needs at least {MinPasswordLength} characters";
        else if (password.Length > MaxPasswordLength)
            error = $"The password can have at most {MaxPasswordLength} characters";
        return error == null;
    }
}
