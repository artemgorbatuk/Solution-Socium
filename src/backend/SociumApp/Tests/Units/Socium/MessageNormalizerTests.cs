using Services.Socium.Normalizers;

namespace Tests.Units.Socium;

public sealed class MessageNormalizerTests
{
    [Theory]
    [InlineData("Text", "Text")]
    [InlineData("  Text  ", "Text")]
    [InlineData("\n\tСтрока 1\nСтрока 2\n", "Строка 1\nСтрока 2")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Text_Normalize_WithSpacesOrNull_ShouldReturnTrimmedOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, MessageNormalizer.Text(value));
    }
}
