namespace Datasource.Files.Ef.Models;

public class StoredFile
{
    public required Guid Id { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public required Guid UploadedByUserId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public FileOwnerType? OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
}
