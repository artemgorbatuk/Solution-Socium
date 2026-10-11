using Datasource.Files.Ef.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Files.Ef.Api;
using Repositories.Files.Ef.Options;
using Services.Files.Stored.Ports;
using Services.Files.Stored.Texts;
using Services.Files.Options;
using Services.Files.Providers;
using Services.Shared.Enums;
using Services.Shared.Models;

namespace Services.Files.Stored.Api;

public class FileUploadRequest
{
    public required Guid UserId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public required Stream Content { get; set; }
}

public class FileUploadResponse
{
    public required Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

public class FileDownloadRequest
{
    public required Guid FileId { get; set; }
    public required Guid UserId { get; set; }
}

public class FileDownloadResponse : IFileDownload
{
    public required Stream Stream { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
}

public class FileAttachToMessageRequest
{
    public required Guid MessageId { get; set; }
    public required Guid UserId { get; set; }
    public required IReadOnlyList<Guid> FileIds { get; set; }
}

public class FileAttachmentInfo
{
    public required Guid Id { get; set; }
    public required Guid OwnerId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
}

public interface IServiceStoredFile
{
    Task<ResponseInfo<FileUploadResponse>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<FileDownloadResponse>> DownloadAsync(FileDownloadRequest request, CancellationToken cancellationToken = default);
    Task<ResponseInfo<object>> AttachToMessageAsync(FileAttachToMessageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<FileAttachmentInfo>>> GetByMessageIdsAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);
    Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}

public class ServiceStoredFile : IServiceStoredFile
{
    private readonly IUnitOfWorkFiles unitOfWork;
    private readonly IFileStore fileStore;
    private readonly IFileOwnerAccess ownerAccess;
    private readonly FilesOptions filesOptions;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ServiceStoredFile> logger;

    public ServiceStoredFile(
        IUnitOfWorkFiles unitOfWork,
        IFileStore fileStore,
        IFileOwnerAccess ownerAccess,
        IOptions<FilesOptions> filesOptions,
        TimeProvider timeProvider,
        ILogger<ServiceStoredFile> logger)
    {
        this.unitOfWork = unitOfWork;
        this.fileStore = fileStore;
        this.ownerAccess = ownerAccess;
        this.filesOptions = filesOptions.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ResponseInfo<FileUploadResponse>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = string.IsNullOrWhiteSpace(request.FileName)
                ? string.Empty
                : Path.GetFileName(request.FileName.Trim());
            var contentType = ContentTypeResolver.Resolve(fileName, request.ContentType);
            var errors = ValidateUpload(request, fileName, contentType).ToList();
            if (errors.Count > 0)
            {
                var text = $"{StoredFileTexts.Messages.Error.UploadFailed}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
                logger.LogError("{Message}: {Errors}", StoredFileTexts.Messages.Error.UploadFailed, string.Join("; ", errors));
                return ResponseInfo.Invalid<FileUploadResponse>(MessageType.INVALID, text);
            }

            var id = Guid.CreateVersion7();
            var extension = Path.GetExtension(fileName);
            if (extension.Length > 32)
            {
                extension = string.Empty;
            }

            var storageKey = id.ToString("N") + extension.ToLowerInvariant();

            await fileStore.SaveAsync(storageKey, request.Content, cancellationToken);

            var model = unitOfWork.StoredFiles.GetNew(new StoredFileGetNewOptions
            {
                Id = id,
                OriginalFileName = fileName,
                ContentType = contentType,
                SizeBytes = request.SizeBytes,
                StorageKey = storageKey,
                UploadedByUserId = request.UserId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            });
            await unitOfWork.StoredFiles.CreateAsync(model, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new FileUploadResponse
            {
                Id = model.Id,
                FileName = model.OriginalFileName,
                ContentType = model.ContentType,
                SizeBytes = model.SizeBytes,
            };
            return ResponseInfo.Success(response, MessageType.SAVED, StoredFileTexts.Messages.Success.UploadCompleted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", StoredFileTexts.Messages.Error.UploadFailed);
            return ResponseInfo.Error<FileUploadResponse>(MessageType.ERROR, StoredFileTexts.Messages.Error.UploadFailed);
        }
    }

    public async Task<ResponseInfo<FileDownloadResponse>> DownloadAsync(FileDownloadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.FileId == Guid.Empty || request.UserId == Guid.Empty)
            {
                return ResponseInfo.BadRequest<FileDownloadResponse>(
                    MessageType.BAD_REQUEST,
                    $"{StoredFileTexts.Messages.Error.DownloadFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.FileIdCannotBeEmpty}");
            }

            var model = await unitOfWork.StoredFiles.GetSingleOrDefaultAsync(request.FileId, cancellationToken);
            if (model == null)
            {
                return ResponseInfo.NotFound<FileDownloadResponse>(
                    MessageType.NOT_FOUND,
                    $"{StoredFileTexts.Messages.Error.DownloadFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.FileNotFound}");
            }

            var allowed = await CanReadAsync(model, request.UserId, cancellationToken);
            if (!allowed)
            {
                return ResponseInfo.Forbidden<FileDownloadResponse>(
                    MessageType.FORBIDDEN,
                    $"{StoredFileTexts.Messages.Error.DownloadFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.AccessDenied}");
            }

            var stream = await fileStore.OpenReadAsync(model.StorageKey, cancellationToken);
            var response = new FileDownloadResponse
            {
                Stream = stream,
                FileName = model.OriginalFileName,
                ContentType = model.ContentType,
            };
            return ResponseInfo.Success(response, MessageType.LOADED, StoredFileTexts.Messages.Success.DownloadCompleted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", StoredFileTexts.Messages.Error.DownloadFailed);
            return ResponseInfo.Error<FileDownloadResponse>(MessageType.ERROR, StoredFileTexts.Messages.Error.DownloadFailed);
        }
    }

    public async Task<ResponseInfo<object>> AttachToMessageAsync(FileAttachToMessageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var errors = ValidateAttach(request).ToList();
            if (errors.Count > 0)
            {
                var text = $"{StoredFileTexts.Messages.Error.AttachFailed}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
                return ResponseInfo.Invalid<object>(MessageType.INVALID, text);
            }

            var distinctIds = request.FileIds.Distinct().ToList();
            var files = await unitOfWork.StoredFiles.GetListAsync(new StoredFileQueryOptions { Ids = distinctIds }, cancellationToken);
            if (files.Count != distinctIds.Count)
            {
                return ResponseInfo.NotFound<object>(
                    MessageType.NOT_FOUND,
                    $"{StoredFileTexts.Messages.Error.AttachFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.FileNotFound}");
            }

            foreach (var file in files)
            {
                if (file.UploadedByUserId != request.UserId)
                {
                    return ResponseInfo.Forbidden<object>(
                        MessageType.FORBIDDEN,
                        $"{StoredFileTexts.Messages.Error.AttachFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.NotUploader}");
                }

                if (file.OwnerType != null || file.OwnerId != null)
                {
                    return ResponseInfo.Invalid<object>(
                        MessageType.INVALID,
                        $"{StoredFileTexts.Messages.Error.AttachFailed}{Environment.NewLine}{StoredFileTexts.Messages.Validation.AlreadyAttached}");
                }
            }

            foreach (var file in files)
            {
                var tracked = await unitOfWork.StoredFiles.GetSingleOrDefaultAsync(file.Id, cancellationToken);
                tracked!.OwnerType = FileOwnerType.Message;
                tracked.OwnerId = request.MessageId;
                await unitOfWork.StoredFiles.UpdateAsync(tracked, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ResponseInfo.Success<object>(MessageType.SAVED, StoredFileTexts.Messages.Success.AttachCompleted);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{Message}", StoredFileTexts.Messages.Error.AttachFailed);
            return ResponseInfo.Error<object>(MessageType.ERROR, StoredFileTexts.Messages.Error.AttachFailed);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<FileAttachmentInfo>>> GetByMessageIdsAsync(
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken = default)
    {
        var ids = messageIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<FileAttachmentInfo>>();
        }

        var files = await unitOfWork.StoredFiles.GetListAsync(
            new StoredFileQueryOptions
            {
                OwnerType = FileOwnerType.Message,
                OwnerIds = ids,
            },
            cancellationToken);

        return files
            .GroupBy(file => file.OwnerId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<FileAttachmentInfo>)group
                    .Select(file => new FileAttachmentInfo
                    {
                        Id = file.Id,
                        OwnerId = file.OwnerId!.Value,
                        FileName = file.OriginalFileName,
                        ContentType = file.ContentType,
                        SizeBytes = file.SizeBytes,
                    })
                    .ToList());
    }

    public async Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            return;
        }

        var files = await unitOfWork.StoredFiles.GetListAsync(
            new StoredFileQueryOptions
            {
                OwnerType = FileOwnerType.Message,
                OwnerId = messageId,
            },
            cancellationToken);

        foreach (var file in files)
        {
            try
            {
                await fileStore.DeleteAsync(file.StorageKey, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Не удалось удалить байты файла {FileId}", file.Id);
            }

            var tracked = await unitOfWork.StoredFiles.GetSingleOrDefaultAsync(file.Id, cancellationToken);
            if (tracked != null)
            {
                await unitOfWork.StoredFiles.DeleteAsync(tracked, cancellationToken);
            }
        }

        if (files.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> CanReadAsync(StoredFile model, Guid userId, CancellationToken cancellationToken)
    {
        if (model.OwnerType is null || model.OwnerId is null)
        {
            return model.UploadedByUserId == userId;
        }

        return await ownerAccess.CanReadAsync(model.OwnerType.Value, model.OwnerId.Value, userId, cancellationToken);
    }

    private IEnumerable<string> ValidateUpload(FileUploadRequest? request, string fileName, string contentType)
    {
        if (request == null)
        {
            yield return StoredFileTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        if (request.UserId == Guid.Empty)
        {
            yield return StoredFileTexts.Messages.Validation.UserIdCannotBeEmpty;
        }

        if (request.Content == null || !request.Content.CanRead)
        {
            yield return StoredFileTexts.Messages.Validation.ContentRequired;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            yield return StoredFileTexts.Messages.Validation.FileNameRequired;
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > filesOptions.MaxBytes)
        {
            yield return StoredFileTexts.Messages.Validation.FileTooLarge;
        }

        if (!filesOptions.AllowedContentTypes.Any(allowed => string.Equals(allowed, contentType, StringComparison.OrdinalIgnoreCase)))
        {
            yield return StoredFileTexts.Messages.Validation.ContentTypeNotAllowed;
        }
    }

    private IEnumerable<string> ValidateAttach(FileAttachToMessageRequest? request)
    {
        if (request == null)
        {
            yield return StoredFileTexts.Messages.Validation.RequestCannotBeNull;
            yield break;
        }

        if (request.MessageId == Guid.Empty)
        {
            yield return StoredFileTexts.Messages.Validation.MessageIdCannotBeEmpty;
        }

        if (request.UserId == Guid.Empty)
        {
            yield return StoredFileTexts.Messages.Validation.UserIdCannotBeEmpty;
        }

        if (request.FileIds == null || request.FileIds.Count == 0)
        {
            yield return StoredFileTexts.Messages.Validation.FileIdsRequired;
        }
        else if (request.FileIds.Count > filesOptions.MaxAttachmentsPerMessage)
        {
            yield return StoredFileTexts.Messages.Validation.TooManyAttachments;
        }
        else if (request.FileIds.Any(id => id == Guid.Empty))
        {
            yield return StoredFileTexts.Messages.Validation.FileIdCannotBeEmpty;
        }
    }
}
