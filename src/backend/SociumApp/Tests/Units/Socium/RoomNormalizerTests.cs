using Services.Socium.Normalizers;

namespace Tests.Units.Socium;

public sealed class RoomNormalizerTests
{
    [Theory]
    [InlineData("Room", "Room")]
    [InlineData("  Room  ", "Room")]
    [InlineData("\tRoom A\n", "Room A")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Name_Normalize_WithSpacesOrNull_ShouldReturnTrimmedOrEmpty(string? value, string expected)
    {
        Assert.Equal(expected, RoomNormalizer.Name(value));
    }
}
