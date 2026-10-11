using Datasource.Socium.Ef.Contexts;
using Datasource.Socium.Ef.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories.Socium.Ef.Api;

public interface IRepositoryRecipient
{
    Task<int> MarkDeliveredAsync(Guid userId, Guid chatId, IReadOnlyCollection<Guid> messageIds, DateTime deliveredAt, CancellationToken cancellationToken = default);
    Task<int> MarkReadForChatAsync(Guid userId, Guid chatId, DateTime readAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsByChatIdsAsync(Guid userId, IEnumerable<Guid> chatIds, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default);
}

public class RepositoryRecipient : IRepositoryRecipient
{
    private readonly DbContextSocium context;

    public RepositoryRecipient(DbContextSocium context)
    {
        this.context = context;
    }

    public async Task<int> MarkDeliveredAsync(
        Guid userId,
        Guid chatId,
        IReadOnlyCollection<Guid> messageIds,
        DateTime deliveredAt,
        CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
        {
            return 0;
        }

        return await context.Recipients
            .Where(recipient =>
                recipient.UserId == userId
                && recipient.MessageDelivered == null
                && messageIds.Contains(recipient.MessageId)
                && recipient.Message.ChatId == chatId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(recipient => recipient.MessageDelivered, deliveredAt),
                cancellationToken);
    }

    public async Task<int> MarkReadForChatAsync(Guid userId, Guid chatId, DateTime readAt, CancellationToken cancellationToken = default)
    {
        await context.Recipients
            .Where(recipient =>
                recipient.UserId == userId
                && recipient.MessageDelivered == null
                && recipient.Message.ChatId == chatId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(recipient => recipient.MessageDelivered, readAt),
                cancellationToken);

        return await context.Recipients
            .Where(recipient =>
                recipient.UserId == userId
                && recipient.MessageRead == null
                && recipient.Message.ChatId == chatId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(recipient => recipient.MessageRead, readAt),
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUnreadCountsByChatIdsAsync(
        Guid userId,
        IEnumerable<Guid> chatIds,
        CancellationToken cancellationToken = default)
    {
        var ids = chatIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await context.Recipients
            .AsNoTracking()
            .Where(recipient =>
                recipient.UserId == userId
                && recipient.MessageRead == null
                && ids.Contains(recipient.Message.ChatId))
            .GroupBy(recipient => recipient.Message.ChatId)
            .Select(group => new { ChatId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.ChatId, row => row.Count);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, Guid chatId, CancellationToken cancellationToken = default)
    {
        return context.Recipients
            .AsNoTracking()
            .CountAsync(
                recipient =>
                    recipient.UserId == userId
                    && recipient.MessageRead == null
                    && recipient.Message.ChatId == chatId,
                cancellationToken);
    }
}
