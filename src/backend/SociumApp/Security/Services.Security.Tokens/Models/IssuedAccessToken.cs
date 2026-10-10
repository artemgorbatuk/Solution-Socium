namespace Services.Security.Tokens.Models;

public record IssuedAccessToken(string Token, DateTime ExpiresAt);
