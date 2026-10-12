namespace Services.Shared.Models;

public interface IFileDownload
{
    Stream Stream { get; }
    string FileName { get; }
    string ContentType { get; }
}
