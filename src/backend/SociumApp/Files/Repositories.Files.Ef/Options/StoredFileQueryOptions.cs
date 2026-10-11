using Datasource.Files.Ef.Models;

namespace Repositories.Files.Ef.Options;

public class StoredFileQueryOptions
{
    public Guid? UploadedByUserId { get; set; }
    public FileOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
    public IReadOnlyCollection<Guid>? OwnerIds { get; set; }
    public IReadOnlyCollection<Guid>? Ids { get; set; }
    public bool? UnboundOnly { get; set; }
}
