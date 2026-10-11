using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Socium.Ef.Models;
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
    Task<IReadOnlyDictionary<Guid, ChatLastMessageRow>> GetLastByChatIdsAsync(IEnumerable<Guid> chatIds, CancellationToken cancellationToken = default);
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
            MessageSent = options.CreatedAt,
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
        var take = options?.Take > 0 ? options.Take : 50;
        var query = context.Messages
            .AsNoTracking()
            .Include(message => message.Sender)
            .AsQueryable();
        if (options?.ChatId is Guid chatId)
        {
            query = query.Where(message => message.ChatId == chatId);
        }

        if (options?.BeforeId is Guid beforeId)
        {
            var cursor = await context.Messages
                .AsNoTracking()
                .Where(message => message.Id == beforeId)
                .Select(message => new { message.CreatedAt, message.Id, message.ChatId })
                .FirstOrDefaultAsync(cancellationToken);
            if (cursor is null)
            {
                return [];
            }

            if (options.ChatId is Guid expectedChatId && cursor.ChatId != expectedChatId)
            {
                return [];
            }

            query = query.Where(message =>
                message.CreatedAt < cursor.CreatedAt
                || (message.CreatedAt == cursor.CreatedAt && message.Id.CompareTo(cursor.Id) < 0));
        }

        var page = await query
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
        page.Reverse();
        return page;
    }

    public async Task<IReadOnlyDictionary<Guid, ChatLastMessageRow>> GetLastByChatIdsAsync(
        IEnumerable<Guid> chatIds,
        CancellationToken cancellationToken = default)
    {
        var ids = chatIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ChatLastMessageRow>();
        }

        var rows = await context.Messages
            .AsNoTracking()
            .Where(message => ids.Contains(message.ChatId))
            .GroupBy(message => message.ChatId)
            .Select(group => group
                .OrderByDescending(message => message.CreatedAt)
                .ThenByDescending(message => message.Id)
                .Select(message => new ChatLastMessageRow
                {
                    ChatId = message.ChatId,
                    MessageId = message.Id,
                    Text = message.Text,
                    CreatedAt = message.CreatedAt,
                    SenderUserId = message.Sender.UserId,
                })
                .First())
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.ChatId);
    }
}
