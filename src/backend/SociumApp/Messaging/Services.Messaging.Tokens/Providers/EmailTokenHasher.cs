using System.Security.Cryptography;
using System.Text;

namespace Services.Messaging.Tokens.Providers;

public interface IEmailTokenHasher
{
    string CreatePlainToken();
    string Hash(string plainToken);
}

public class EmailTokenHasher : IEmailTokenHasher
{
    public string CreatePlainToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string Hash(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        return Convert.ToBase64String(bytes);
    }
}
