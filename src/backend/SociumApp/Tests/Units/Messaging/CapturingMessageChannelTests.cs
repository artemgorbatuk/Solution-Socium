using Services.Messaging.Enums;
using Services.Messaging.Models;
using Services.Messaging.Providers;

namespace Tests.Units.Messaging;

public sealed class CapturingMessageChannelTests
{
    [Fact]
    public async Task Send_WithEmail_ShouldKeepMessageAndSucceed()
    {
        var channel = new CapturingMessageChannel();
        var message = new EmailMessage { To = "a@example.com", Subject = "Тема", Body = "тело" };

        var result = await channel.SendAsync(message, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(MessageChannel.Email, channel.Channel);
        Assert.Same(message, Assert.Single(channel.Sent));
        channel.Clear();
        Assert.Empty(channel.Sent);
    }
}
