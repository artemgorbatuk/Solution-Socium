using Services.Messaging.Enums;

namespace Services.Messaging.Models;

public abstract class Message
{
    public abstract MessageChannel Channel { get; }
    public required string To { get; set; }
    public required string Body { get; set; }
}

public class EmailMessage : Message
{
    public override MessageChannel Channel => MessageChannel.Email;
    public string? Subject { get; set; }
    public bool IsHtml { get; set; }
}
