namespace Services.Security.Permissions.Catalogs;

public static class PermissionNames
{
    public const string RoomCreate = "Room.Create";
    public const string RoomRead = "Room.Read";
    public const string RoomUpdate = "Room.Update";
    public const string RoomDelete = "Room.Delete";
    public const string RoomAdminGrant = "RoomAdmin.Grant";
    public const string RoomAdminRevoke = "RoomAdmin.Revoke";

    public const string ChatCreate = "Chat.Create";
    public const string ChatRead = "Chat.Read";
    public const string ChatUpdate = "Chat.Update";
    public const string ChatDelete = "Chat.Delete";

    public const string ParticipantRead = "Participant.Read";
    public const string ChatParticipantGrant = "ChatParticipant.Grant";
    public const string ChatParticipantRevoke = "ChatParticipant.Revoke";
    public const string ChatAdminGrant = "ChatAdmin.Grant";
    public const string ChatAdminRevoke = "ChatAdmin.Revoke";

    public const string MessageCreate = "Message.Create";
    public const string MessageRead = "Message.Read";
    public const string MessageUpdate = "Message.Update";
    public const string MessageDelete = "Message.Delete";

    public const string UserRead = "User.Read";
    public const string UserAccessManage = "UserAccess.Manage";
    public const string AppAdminGrant = "AppAdmin.Grant";
    public const string AppAdminRevoke = "AppAdmin.Revoke";
}
