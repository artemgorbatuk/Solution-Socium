using Services.Files.Stored.Api;
using Services.Shared.Enums;
using Services.Shared.Models;
using Services.Socium.Ports;

namespace WebApi.Ports;

/// <summary>
/// Адаптер порта Socium к сервису файлов области Files.
/// </summary>
public class FilePort : IFilePort
{
    private readonly IServiceStoredFile serviceStoredFile;

    public FilePort(IServiceStoredFile serviceStoredFile)
    {
        this.serviceStoredFile = serviceStoredFile;
    }

    public Task<ResponseInfo<object>> AttachToMessageAsync(
        Guid messageId,
        IReadOnlyList<Guid> fileIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return serviceStoredFile.AttachToMessageAsync(
            new FileAttachToMessageRequest
            {
                MessageId = messageId,
                FileIds = fileIds,
                UserId = userId,
            },
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<FilePortAttachment>>> GetByMessageIdsAsync(
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken = default)
    {
        var map = await serviceStoredFile.GetByMessageIdsAsync(messageIds, cancellationToken);
        return map.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<FilePortAttachment>)pair.Value
                .Select(file => new FilePortAttachment
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.SizeBytes,
                })
                .ToList());
    }

    public Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return serviceStoredFile.DeleteForMessageAsync(messageId, cancellationToken);
    }
}
