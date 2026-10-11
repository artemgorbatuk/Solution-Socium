using Datasource.Files.Ef.Contexts;
using Datasource.Files.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Files.Ef.Options;

namespace Repositories.Files.Ef.Api;

public interface IRepositoryStoredFile
{
    StoredFile GetNew(StoredFileGetNewOptions options);
    Task<StoredFile?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredFile>> GetListAsync(StoredFileQueryOptions? options = null, CancellationToken cancellationToken = default);
    Task CreateAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoredFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default);
}

public class RepositoryStoredFile : IRepositoryStoredFile
{
    private readonly DbContextFiles context;

    public RepositoryStoredFile(DbContextFiles context)
    {
        this.context = context;
    }

    public StoredFile GetNew(StoredFileGetNewOptions options)
    {
        return new StoredFile
        {
            Id = options.Id,
            OriginalFileName = options.OriginalFileName,
            ContentType = options.ContentType,
            SizeBytes = options.SizeBytes,
            StorageKey = options.StorageKey,
            UploadedByUserId = options.UploadedByUserId,
            CreatedAt = options.CreatedAt,
            OwnerType = options.OwnerType,
            OwnerId = options.OwnerId,
        };
    }

    public Task<StoredFile?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.StoredFiles.SingleOrDefaultAsync(file => file.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> GetListAsync(StoredFileQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        IQueryable<StoredFile> query = context.StoredFiles.AsNoTracking();

        if (options?.Ids is { Count: > 0 } ids)
        {
            query = query.Where(file => ids.Contains(file.Id));
        }

        if (options?.UploadedByUserId is Guid uploadedBy)
        {
            query = query.Where(file => file.UploadedByUserId == uploadedBy);
        }

        if (options?.OwnerType is FileOwnerType ownerType)
        {
            query = query.Where(file => file.OwnerType == ownerType);
        }

        if (options?.OwnerId is Guid ownerId)
        {
            query = query.Where(file => file.OwnerId == ownerId);
        }

        if (options?.OwnerIds is { Count: > 0 } ownerIds)
        {
            query = query.Where(file => file.OwnerId != null && ownerIds.Contains(file.OwnerId.Value));
        }

        if (options?.UnboundOnly == true)
        {
            query = query.Where(file => file.OwnerType == null && file.OwnerId == null);
        }

        return await query.OrderBy(file => file.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        await context.StoredFiles.AddAsync(file, cancellationToken);
    }

    public Task UpdateAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        context.StoredFiles.Update(file);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        context.StoredFiles.Remove(file);
        return Task.CompletedTask;
    }
}
