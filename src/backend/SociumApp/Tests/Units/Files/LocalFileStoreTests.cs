using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Services.Files.Options;
using Services.Files.Providers;

namespace Tests.Units.Files;

public sealed class LocalFileStoreTests : IDisposable
{
    private readonly string rootPath = Path.Combine(Path.GetTempPath(), $"socium-store-{Guid.CreateVersion7():N}");
    private readonly LocalFileStore store;

    public LocalFileStoreTests()
    {
        Directory.CreateDirectory(rootPath);
        store = new LocalFileStore(
            Options.Create(new FilesOptions { RootPath = rootPath }),
            NullLogger<LocalFileStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SaveOpenDelete_WithRelativeKey_ShouldRoundTripBytes()
    {
        var key = $"{Guid.CreateVersion7():N}.txt";
        var bytes = "привет"u8.ToArray();
        await using (var input = new MemoryStream(bytes))
        {
            await store.SaveAsync(key, input, TestContext.Current.CancellationToken);
        }

        await using (var read = await store.OpenReadAsync(key, TestContext.Current.CancellationToken))
        using (var memory = new MemoryStream())
        {
            await read.CopyToAsync(memory, TestContext.Current.CancellationToken);
            Assert.Equal(bytes, memory.ToArray());
        }

        await store.DeleteAsync(key, TestContext.Current.CancellationToken);
        Assert.False(File.Exists(Path.Combine(rootPath, key)));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("a/../../escape.txt")]
    public async Task Save_WithPathTraversalKey_ShouldThrow(string key)
    {
        await using var input = new MemoryStream("x"u8.ToArray());
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(key, input, TestContext.Current.CancellationToken));
    }
}
