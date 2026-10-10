using Datasource.Security.Ef.Models;

namespace Services.Security.Mapping;

public static class RefreshTokenMapper
{
    public static void Revoke(RefreshToken model, DateTime revokedAt, Guid? replacedByTokenId = null)
    {
        model.RevokedAt = revokedAt;
        model.ReplacedByTokenId = replacedByTokenId;
    }
}
