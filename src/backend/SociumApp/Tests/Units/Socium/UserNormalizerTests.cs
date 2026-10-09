using Services.Socium.Normalizers;

namespace Tests.Units.Socium;

public sealed class UserNormalizerTests
{
    [Theory]
    [InlineData("ivan", "ivan")]
    [InlineData("  Ivan.Petrov  ", "ivan.petrov")]
    [InlineData("IVAN_1", "ivan_1")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Login_Normalize_WithSpacesCaseOrNull_ShouldReturnTrimmedLowercaseOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, UserNormalizer.Login(value));
    }

    [Theory]
    [InlineData("Иван", "Иван")]
    [InlineData("  Иван Петров  ", "Иван Петров")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Name_Normalize_WithSpacesOrNull_ShouldReturnTrimmedKeepingCase(string? value, string expected)
    {
        Assert.Equal(expected, UserNormalizer.Name(value));
    }
}
