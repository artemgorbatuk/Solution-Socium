namespace Services.Security.Normalizers;

public static class UserNormalizer
{
    public static string Login(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public static string Name(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
