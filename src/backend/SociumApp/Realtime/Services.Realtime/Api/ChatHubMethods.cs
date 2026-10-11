namespace Services.Realtime.Api;

/// <summary>
/// Имена клиентских методов SignalR и формат группы чата.
/// </summary>
public static class ChatHubMethods
{
    public const string MessageCreated = "messageCreated";
    public const string MessageUpdated = "messageUpdated";
    public const string MessageDeleted = "messageDeleted";
    public const string ParticipantJoined = "participantJoined";
    public const string ParticipantLeft = "participantLeft";
    public const string ParticipantRoleChanged = "participantRoleChanged";

    public static string ChatGroup(Guid chatId) => $"chat:{chatId:D}";
}
