using System.Text.RegularExpressions;
using Services.Messaging.Models;
using Services.Messaging.Providers;

namespace Tests.Infrastructure;

public static partial class MailTestHelpers
{
    public static string ExtractTokenFromBody(string body)
    {
        var match = TokenInLink().Match(body);
        Assert.True(match.Success, $"В теле письма нет token=: {body}");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    public static string LastToken(CapturingMessageChannel channel)
    {
        var message = Assert.IsType<EmailMessage>(Assert.Single(channel.Sent));
        return ExtractTokenFromBody(message.Body);
    }

    [GeneratedRegex(@"[?&]token=([^\s&]+)")]
    private static partial Regex TokenInLink();
}
