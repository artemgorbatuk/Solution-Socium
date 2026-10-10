namespace Services.Security.Tokens.Texts;

public static class TokenTexts
{
    public static class Messages
    {
        public static class Validation
        {
            public const string IssuerRequired = "Не задан издатель токена (Security:Jwt:Issuer).";
            public const string AudienceRequired = "Не задана аудитория токена (Security:Jwt:Audience).";
            public const string SigningKeyRequired = "Не задан ключ подписи токена (Security:Jwt:SigningKey): задайте его через user-secrets или переменную окружения Security__Jwt__SigningKey.";
            public const string SigningKeyTooShort = "Ключ подписи токена (Security:Jwt:SigningKey) короче 32 байт.";
            public const string AccessTokenMinutesInvalid = "Время жизни токена доступа (Security:Jwt:AccessTokenMinutes) должно быть больше нуля.";
            public const string RefreshTokenDaysInvalid = "Время жизни токена обновления (Security:Jwt:RefreshTokenDays) должно быть больше нуля.";
        }
    }
}
