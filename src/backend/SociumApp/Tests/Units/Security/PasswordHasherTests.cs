using Services.Security.Passwords.Providers;

namespace Tests.Units.Security;

public sealed class PasswordHasherTests
{
    private readonly AspNetIdentityPasswordHasher hasher = new();

    [Fact]
    public void Password_HashAndVerify_WithSamePassword_ShouldVerify()
    {
        var hash = hasher.Hash("Password-123");

        Assert.NotEqual("Password-123", hash);
        Assert.True(hasher.Verify(hash, "Password-123"));
    }

    [Fact]
    public void Password_Verify_WithWrongPassword_ShouldNotVerify()
    {
        var hash = hasher.Hash("Password-123");

        Assert.False(hasher.Verify(hash, "password-123"));
    }

    [Fact]
    public void Password_Hash_WithSamePasswordTwice_ShouldUseDifferentSalt()
    {
        Assert.NotEqual(hasher.Hash("Password-123"), hasher.Hash("Password-123"));
    }
}
