using Services.Shared.Models;

namespace Services.Socium.Ports;

/// <summary>
/// Порт Socium к области Files: вложения сообщений.
/// Адаптер в WebApi вызывает <c>IServiceStoredFile</c>.
/// </summary>
public interface IFilePort
{
    Task<ResponseInfo<object>> AttachToMessageAsync(Guid messageId, IReadOnlyList<Guid> fileIds, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<FilePortAttachment>>> GetByMessageIdsAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);
    Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}

public class FilePortAttachment
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}
