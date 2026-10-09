using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Options;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryMessage
{
    Task<Message> CreateAsync(Message model, CancellationToken cancellationToken = default);
    Task<Message> UpdateAsync(Message model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Message model, CancellationToken cancellationToken = default);
    Message GetNew();
    Message GetNew(MessageGetNewOptions options);
    Task<Message?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetListAsync(MessageQueryOptions? options = null, CancellationToken cancellationToken = default);
}

public class RepositoryMessage : IRepositoryMessage
{
    private readonly DbContextSocium context;

    public RepositoryMessage(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<Message> CreateAsync(Message model, CancellationToken cancellationToken = default)
    {
        await context.AddAsync(model, cancellationToken);
        return model;
    }

    public Task<Message> UpdateAsync(Message model, CancellationToken cancellationToken = default)
    {
        context.Update(model);
        return Task.FromResult(model);
    }

    public Task DeleteAsync(Message model, CancellationToken cancellationToken = default)
    {
        context.Remove(model);
        return Task.CompletedTask;
    }

    public Message GetNew()
    {
        return new Message
        {
            Id = Guid.Empty,
            ChatId = Guid.Empty,
            Text = string.Empty,
            CreatedAt = default,
        };
    }

    public Message GetNew(MessageGetNewOptions options)
    {
        var model = GetNew();
        model.ChatId = options.ChatId;
        model.CreatedAt = options.CreatedAt;
        model.Sender = new Sender
        {
            Id = Guid.Empty,
            MessageId = Guid.Empty,
            UserId = options.SenderUserId,
        };
        model.Recipients = [.. options.RecipientUserIds.Select(userId => new Recipient
        {
            Id = Guid.Empty,
            MessageId = Guid.Empty,
            UserId = userId,
        })];
        return model;
    }

    public async Task<Message?> GetSingleOrDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Messages
            .Include(message => message.Sender)
            .FirstOrDefaultAsync(message => message.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetListAsync(MessageQueryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = context.Messages
            .AsNoTracking()
            .Include(message => message.Sender)
            .ThenInclude(sender => sender.User)
            .AsQueryable();
        if (options?.ChatId is Guid chatId)
        {
            query = query.Where(message => message.ChatId == chatId);
        }

        return await query
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }
}
