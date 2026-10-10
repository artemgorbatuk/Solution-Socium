using Datasource.Security.Ef.Contexts;
using Datasource.Security.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Security.Ef.Options;

namespace Repositories.Security.Ef.Api;

/// <summary>
/// Токены не удаляются, а отзываются — <see cref="RefreshToken.RevokedAt"/>: отозванный токен нужен для распознавания повторного использования.
/// </summary>
public interface IRepositoryRefreshToken
{
    Task<RefreshToken> CreateAsync(RefreshToken model, CancellationToken cancellationToken = default);
    RefreshToken GetNew(RefreshTokenGetNewOptions options);
    Task<RefreshToken?> GetSingleByHashOrDefaultAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetListAsync(RefreshTokenQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryRefreshToken : IRepositoryRefreshToken
{
    private readonly DbContextSecurity context;

    public RepositoryRefreshToken(DbContextSecurity context)
    {
        this.context = context;
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    /// <summary>
    /// Id назначается сразу: на него ссылается <see cref="RefreshToken.ReplacedByTokenId"/> предыдущего токена.
    /// </summary>
    public RefreshToken GetNew(RefreshTokenGetNewOptions options)
    {
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = options.UserId,
            TokenHash = options.TokenHash,
            IsPersistent = options.IsPersistent,
            CreatedAt = options.CreatedAt,
            ExpiresAt = options.ExpiresAt,
        };
    }

    public async Task<RefreshToken?> GetSingleByHashOrDefaultAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    /// <summary>
    /// Возвращает отслеживаемые сущности: список нужен для отзыва токенов.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetListAsync(RefreshTokenQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.RefreshTokens.AsQueryable();

        if (options?.UserId is Guid userId)
        {
            query = query.Where(token => token.UserId == userId);
        }

        if (options?.IsRevoked is bool isRevoked)
        {
            query = isRevoked
                ? query.Where(token => token.RevokedAt != null)
                : query.Where(token => token.RevokedAt == null);
        }

        return await query
            .OrderBy(token => token.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
