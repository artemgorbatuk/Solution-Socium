using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Files.Options;

namespace Services.Files.Providers;

public interface IFileStore
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public class LocalFileStore : IFileStore
{
    private readonly string rootPath;
    private readonly ILogger<LocalFileStore> logger;

    public LocalFileStore(IOptions<FilesOptions> options, ILogger<LocalFileStore> logger)
    {
        rootPath = Path.GetFullPath(options.Value.RootPath);
        this.logger = logger;
        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else
        {
            logger.LogWarning("Файл на диске не найден при удалении: {StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(storageKey))
        {
            throw new ArgumentException("Некорректный ключ хранения файла.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(rootPath, storageKey));
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Ключ хранения выходит за пределы RootPath.", nameof(storageKey));
        }

        return fullPath;
    }
}
