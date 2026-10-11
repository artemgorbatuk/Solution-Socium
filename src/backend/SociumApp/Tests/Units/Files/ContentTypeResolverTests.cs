using Services.Files.Providers;

namespace Tests.Units.Files;

public sealed class ContentTypeResolverTests
{
    [Theory]
    [InlineData("photo.PNG", "application/octet-stream", "image/png")]
    [InlineData("note.cs", "", "text/x-csharp")]
    [InlineData("report.docx", "application/octet-stream", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("readme.md", "text/plain", "text/plain")]
    public void Resolve_WithEmptyOrOctetStream_ShouldMapByExtension(string fileName, string contentType, string expected)
    {
        Assert.Equal(expected, ContentTypeResolver.Resolve(fileName, contentType));
    }

    [Fact]
    public void Resolve_WithKnownContentType_ShouldKeepContentType()
    {
        Assert.Equal("image/jpeg", ContentTypeResolver.Resolve("x.bin", "image/jpeg"));
    }

    [Fact]
    public void TryMapExtension_WithUnknownExtension_ShouldReturnFalse()
    {
        Assert.False(ContentTypeResolver.TryMapExtension("archive.zip", out _));
    }
}
