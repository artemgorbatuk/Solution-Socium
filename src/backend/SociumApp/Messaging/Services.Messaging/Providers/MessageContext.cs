using Services.Messaging.Enums;
using Services.Messaging.Models;
using Services.Messaging.Texts;

namespace Services.Messaging.Providers;

public interface IMessageContext
{
    Task<MessageSendResult> SendAsync(Message message, CancellationToken cancellationToken = default);
}

public class MessageContext : IMessageContext
{
    private readonly IReadOnlyDictionary<MessageChannel, IMessageChannel> channels;

    public MessageContext(IEnumerable<IMessageChannel> channels)
    {
        this.channels = channels.ToDictionary(channel => channel.Channel);
    }

    public Task<MessageSendResult> SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!channels.TryGetValue(message.Channel, out var channel))
        {
            return Task.FromResult(MessageSendResult.Failure(MessagingTexts.Messages.Error.ChannelNotRegistered));
        }

        return channel.SendAsync(message, cancellationToken);
    }
}
