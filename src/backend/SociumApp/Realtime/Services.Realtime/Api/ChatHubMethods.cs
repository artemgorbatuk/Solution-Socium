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
    public const string ChatListUpdated = "chatListUpdated";

    public static string ChatGroup(Guid chatId) => $"chat:{chatId:D}";
    public static string UserGroup(Guid userId) => $"user:{userId:D}";
}
