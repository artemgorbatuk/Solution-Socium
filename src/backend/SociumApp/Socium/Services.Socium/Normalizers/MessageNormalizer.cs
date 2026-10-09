namespace Services.Socium.Normalizers;

public static class MessageNormalizer
{
    /// <summary>Обрезает пробелы и переводы строк по краям; переносы внутри текста сохраняются.</summary>
    public static string Text(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
