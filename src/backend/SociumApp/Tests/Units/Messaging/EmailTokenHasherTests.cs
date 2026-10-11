using Services.Messaging.Tokens.Providers;

namespace Tests.Units.Messaging;

public sealed class EmailTokenHasherTests
{
    private readonly EmailTokenHasher hasher = new();

    [Fact]
    public void CreatePlainToken_Hash_WithSameToken_ShouldBeStableAndDifferentFromPlain()
    {
        var plain = hasher.CreatePlainToken();

        var first = hasher.Hash(plain);
        var second = hasher.Hash(plain);

        Assert.False(string.IsNullOrWhiteSpace(plain));
        Assert.Equal(first, second);
        Assert.NotEqual(plain, first);
    }

    [Fact]
    public void CreatePlainToken_Twice_ShouldReturnDifferentValues()
    {
        Assert.NotEqual(hasher.CreatePlainToken(), hasher.CreatePlainToken());
    }
}
