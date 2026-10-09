using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryChat
{
    Task<Chat> CreateAsync(Chat model, CancellationToken cancellationToken = default);
    Task<Chat> UpdateAsync(Chat model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Chat model, CancellationToken cancellationToken = default);
    Chat GetNew();
    Chat GetNew(ChatGetNewOptions options);
    Task<Chat?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chat>> GetListAsync(ChatQueryOptions? options = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ChatQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryChat : IRepositoryChat
{
    private readonly DbContextSocium context;

    public RepositoryChat(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<Chat> CreateAsync(Chat model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<Chat> UpdateAsync(Chat model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public Task DeleteAsync(Chat model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public Chat GetNew()
    {
        return new Chat
        {
            Id = Guid.Empty,
            RoomId = Guid.Empty,
            Name = string.Empty,
        };
    }

    public Chat GetNew(ChatGetNewOptions options)
    {
        var model = GetNew();
        model.RoomId = options.RoomId;
        return model;
    }

    public async Task<Chat?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Chats.FirstOrDefaultAsync(chat => chat.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Chat>> GetListAsync(ChatQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Filter(context.Chats.AsNoTracking(), options)
            .OrderBy(chat => chat.Name)
            .ThenBy(chat => chat.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ChatQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Filter(context.Chats, options).CountAsync(cancellationToken);
    }

    private static IQueryable<Chat> Filter(IQueryable<Chat> query, ChatQueryOptions? options)
    {
        if (options?.RoomId is Guid roomId)
        {
            query = query.Where(chat => chat.RoomId == roomId);
        }

        return query;
    }
}
