using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Enums;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryJoinRequest
{
    Task<JoinRequest> CreateAsync(JoinRequest model, CancellationToken cancellationToken = default);
    Task<JoinRequest> UpdateAsync(JoinRequest model, CancellationToken cancellationToken = default);
    JoinRequest GetNew();
    JoinRequest GetNew(JoinRequestGetNewOptions options);
    Task<JoinRequest?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<JoinRequest>> GetListAsync(JoinRequestQueryOptions? options = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(JoinRequestQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryJoinRequest : IRepositoryJoinRequest
{
    private readonly DbContextSocium context;

    public RepositoryJoinRequest(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<JoinRequest> CreateAsync(JoinRequest model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<JoinRequest> UpdateAsync(JoinRequest model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public JoinRequest GetNew()
    {
        return new JoinRequest
        {
            Id = Guid.Empty,
            ChatId = Guid.Empty,
            UserId = Guid.Empty,
            Status = JoinRequestStatus.Pending,
            CreatedByUserId = Guid.Empty,
        };
    }

    public JoinRequest GetNew(JoinRequestGetNewOptions options)
    {
        var model = GetNew();
        model.ChatId = options.ChatId;
        model.UserId = options.UserId;
        model.CreatedByUserId = options.CreatedByUserId;
        model.Status = JoinRequestStatus.Pending;
        return model;
    }

    public async Task<JoinRequest?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.JoinRequests.FirstOrDefaultAsync(joinRequest => joinRequest.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<JoinRequest>> GetListAsync(
        JoinRequestQueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await BuildQuery(options).OrderBy(joinRequest => joinRequest.Id).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(JoinRequestQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await BuildQuery(options).CountAsync(cancellationToken);
    }

    private IQueryable<JoinRequest> BuildQuery(JoinRequestQueryOptions? options)
    {
        var query = context.JoinRequests.AsNoTracking().AsQueryable();

        if (options?.Id is Guid id)
        {
            query = query.Where(joinRequest => joinRequest.Id == id);
        }

        if (options?.ChatId is Guid chatId)
        {
            query = query.Where(joinRequest => joinRequest.ChatId == chatId);
        }

        if (options?.UserId is Guid userId)
        {
            query = query.Where(joinRequest => joinRequest.UserId == userId);
        }

        if (options?.Status is JoinRequestStatus status)
        {
            query = query.Where(joinRequest => joinRequest.Status == status);
        }

        return query;
    }
}
