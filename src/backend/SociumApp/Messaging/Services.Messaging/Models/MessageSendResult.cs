namespace Services.Messaging.Models;

public sealed class MessageSendResult
{
    public required bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static MessageSendResult Success() => new() { Succeeded = true };

    public static MessageSendResult Failure(string error) => new()
    {
        Succeeded = false,
        Error = error,
    };
}
