using System.Collections.Concurrent;
using Services.Messaging.Enums;
using Services.Messaging.Models;

namespace Services.Messaging.Providers;

/// <summary>
/// Канал для тестов: письма не уходят, тело и тема остаются в памяти.
/// </summary>
public class CapturingMessageChannel : IMessageChannel
{
    private readonly ConcurrentQueue<Message> sent = new();

    public MessageChannel Channel => MessageChannel.Email;

    public IReadOnlyCollection<Message> Sent => sent.ToArray();

    public Task<MessageSendResult> SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        sent.Enqueue(message);
        return Task.FromResult(MessageSendResult.Success());
    }

    public void Clear() => sent.Clear();
}
