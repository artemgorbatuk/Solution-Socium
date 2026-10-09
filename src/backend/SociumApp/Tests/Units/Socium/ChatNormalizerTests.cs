using Services.Socium.Normalizers;

namespace Tests.Units.Socium;

public sealed class ChatNormalizerTests
{
    [Theory]
    [InlineData("Chat", "Chat")]
    [InlineData("  Chat  ", "Chat")]
    [InlineData("\tChat A\n", "Chat A")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Name_Normalize_WithSpacesOrNull_ShouldReturnTrimmedOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, ChatNormalizer.Name(value));
    }
}
