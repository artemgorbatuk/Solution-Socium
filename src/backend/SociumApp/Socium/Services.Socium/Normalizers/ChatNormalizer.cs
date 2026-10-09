namespace Services.Socium.Normalizers;

public static class ChatNormalizer
{
    public static string Name(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
