namespace Services.Security.Tokens.Models;

/// <summary>
/// <see cref="Token"/> уходит клиенту в cookie, в БД хранится только <see cref="TokenHash"/>.
/// </summary>
public record IssuedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);
