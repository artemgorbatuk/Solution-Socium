namespace Services.Socium.Normalizers;

public static class RoomNormalizer
{
    public static string Name(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
