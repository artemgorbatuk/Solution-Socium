namespace Services.Files.Providers;

/// <summary>
/// Сопоставление расширения файла с MIME, когда клиент присылает пустой тип или <c>application/octet-stream</c>.
/// </summary>
public static class ContentTypeResolver
{
    private static readonly Dictionary<string, string> ExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".odt"] = "application/vnd.oasis.opendocument.text",
        [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        [".odp"] = "application/vnd.oasis.opendocument.presentation",
        [".rtf"] = "application/rtf",
        [".txt"] = "text/plain",
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "text/javascript",
        [".mjs"] = "text/javascript",
        [".cjs"] = "text/javascript",
        [".ts"] = "application/typescript",
        [".tsx"] = "application/typescript",
        [".jsx"] = "text/javascript",
        [".cs"] = "text/x-csharp",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",
        [".py"] = "text/x-python",
        [".sql"] = "text/x-sql",
        [".yml"] = "text/yaml",
        [".yaml"] = "text/yaml",
        [".sh"] = "text/x-sh",
    };

    public static readonly string[] DefaultAllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.oasis.opendocument.text",
        "application/vnd.oasis.opendocument.spreadsheet",
        "application/vnd.oasis.opendocument.presentation",
        "application/rtf",
        "text/plain",
        "text/html",
        "text/css",
        "text/javascript",
        "application/javascript",
        "application/json",
        "text/xml",
        "application/xml",
        "text/markdown",
        "text/csv",
        "text/x-csharp",
        "text/x-python",
        "text/x-sql",
        "text/yaml",
        "application/x-yaml",
        "text/x-sh",
        "application/typescript",
    ];

    public static string Resolve(string? fileName, string? contentType)
    {
        var normalized = contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(normalized)
            && !string.Equals(normalized, "application/octet-stream", StringComparison.Ordinal))
        {
            return normalized;
        }

        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (extension.Length > 0 && ExtensionToContentType.TryGetValue(extension, out var mapped))
        {
            return mapped;
        }

        return normalized;
    }

    public static bool TryMapExtension(string? fileName, out string contentType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (extension.Length > 0 && ExtensionToContentType.TryGetValue(extension, out var mapped))
        {
            contentType = mapped;
            return true;
        }

        contentType = string.Empty;
        return false;
    }
}
