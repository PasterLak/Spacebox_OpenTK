namespace Spacebox.Network.Auth;

public sealed class PlayerAccount
{
    public string Name { get; set; } = "";
    public string Salt { get; set; } = "";
    public string Key { get; set; } = "";
    public int Iterations { get; set; } = PasswordCrypto.DefaultIterations;
    public string Created { get; set; } = "";

    public byte[] GetSalt() => Decode(Salt);
    public byte[] GetKey() => Decode(Key);

    private static byte[] Decode(string value)
    {
        try
        {
            return string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }
}
