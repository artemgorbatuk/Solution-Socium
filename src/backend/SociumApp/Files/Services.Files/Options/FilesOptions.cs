using Services.Files.Providers;

namespace Services.Files.Options;

public class FilesOptions
{
    public const string SectionName = "Files";

    /// <summary>Корень хранения байтов на диске (абсолютный или относительно рабочей директории).</summary>
    public string RootPath { get; set; } = "data/files";

    /// <summary>Максимальный размер одного файла в байтах.</summary>
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Максимум вложений на одно сообщение.</summary>
    public int MaxAttachmentsPerMessage { get; set; } = 5;

    public string[] AllowedContentTypes { get; set; } = ContentTypeResolver.DefaultAllowedContentTypes;
}
