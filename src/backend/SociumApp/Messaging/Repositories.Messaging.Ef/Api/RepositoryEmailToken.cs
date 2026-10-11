using Datasource.Messaging.Ef.Contexts;
using Datasource.Messaging.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Messaging.Ef.Options;

namespace Repositories.Messaging.Ef.Api;

public interface IRepositoryEmailToken
{
    EmailToken GetNew(EmailTokenGetNewOptions options);
    Task<EmailToken?> GetSingleByHashOrDefaultAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailToken>> GetListAsync(EmailTokenQueryOptions? options = null, CancellationToken cancellationToken = default);
    Task CreateAsync(EmailToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(EmailToken token, CancellationToken cancellationToken = default);
    Task InvalidateUnusedAsync(Guid userId, EmailTokenPurpose purpose, DateTime usedAt, CancellationToken cancellationToken = default);
}

public class RepositoryEmailToken : IRepositoryEmailToken
{
    private readonly DbContextMessaging context;

    public RepositoryEmailToken(DbContextMessaging context)
    {
        this.context = context;
    }

    public EmailToken GetNew(EmailTokenGetNewOptions options)
    {
        return new EmailToken
        {
            Id = Guid.Empty,
            UserId = options.UserId,
            Purpose = options.Purpose,
            TokenHash = options.TokenHash,
            CreatedAt = options.CreatedAt,
            ExpiresAt = options.ExpiresAt,
        };
    }

    public Task<EmailToken?> GetSingleByHashOrDefaultAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return context.EmailTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailToken>> GetListAsync(EmailTokenQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        IQueryable<EmailToken> query = context.EmailTokens.AsNoTracking();

        if (options?.UserId is Guid userId)
        {
            query = query.Where(token => token.UserId == userId);
        }

        if (options?.Purpose is EmailTokenPurpose purpose)
        {
            query = query.Where(token => token.Purpose == purpose);
        }

        if (options?.UnusedOnly == true)
        {
            query = query.Where(token => token.UsedAt == null);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(EmailToken token, CancellationToken cancellationToken = default)
    {
        await context.EmailTokens.AddAsync(token, cancellationToken);
    }

    public Task UpdateAsync(EmailToken token, CancellationToken cancellationToken = default)
    {
        context.EmailTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task InvalidateUnusedAsync(Guid userId, EmailTokenPurpose purpose, DateTime usedAt, CancellationToken cancellationToken = default)
    {
        return context.EmailTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose && token.UsedAt == null)
            .ExecuteUpdateAsync(updates => updates.SetProperty(token => token.UsedAt, usedAt), cancellationToken);
    }
}
