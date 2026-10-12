using Datasource.Socium.Ef.Models;
using Repositories.Socium.Ef.Options;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class RoleAssignmentMapper
{
    public static RoomRoleAssignmentQueryOptions ToRoomRoleQueryOptions(Guid? userId = null, string? roleName = null)
    {
        return new RoomRoleAssignmentQueryOptions
        {
            UserId = userId,
            RoleName = roleName,
        };
    }

    public static ChatRoleAssignmentQueryOptions ToChatRoleQueryOptions(Guid? userId = null, string? roleName = null)
    {
        return new ChatRoleAssignmentQueryOptions
        {
            UserId = userId,
            RoleName = roleName,
        };
    }

    public static RoleAssignmentChatModel ToChatModel(Chat chat, bool isParticipant, bool isChatAdmin, int chatAdminCount)
    {
        return new RoleAssignmentChatModel
        {
            ChatId = chat.Id,
            ChatName = chat.Name,
            IsParticipant = isParticipant,
            IsChatAdmin = isChatAdmin,
            IsLastChatAdmin = isChatAdmin && chatAdminCount <= 1,
        };
    }

    public static RoleAssignmentRoomModel ToRoomModel(
        Room room,
        bool isRoomAdmin,
        int roomAdminCount,
        ICollection<RoleAssignmentChatModel> chats)
    {
        return new RoleAssignmentRoomModel
        {
            RoomId = room.Id,
            RoomName = room.Name,
            IsRoomAdmin = isRoomAdmin,
            IsLastRoomAdmin = isRoomAdmin && roomAdminCount <= 1,
            Chats = chats,
        };
    }

    public static RoleAssignmentListPageResponse ToListPageResponse(
        Guid userId,
        bool canManage,
        ICollection<RoleAssignmentRoomModel> rooms)
    {
        return new RoleAssignmentListPageResponse
        {
            UserId = userId,
            CanManage = canManage,
            Rooms = rooms,
        };
    }

    public static RoleAssignmentCreateResponse ToCreateResponse(Guid userId, string roleName)
    {
        return new RoleAssignmentCreateResponse
        {
            UserId = userId,
            RoleName = roleName,
        };
    }

    public static RoleAssignmentDeleteResponse ToDeleteResponse()
    {
        return new RoleAssignmentDeleteResponse
        {
            IsDeleted = true,
        };
    }

    public static RoleAssignmentLastAdminListResponse ToLastAdminListResponse(ICollection<string> places)
    {
        return new RoleAssignmentLastAdminListResponse
        {
            Places = places,
        };
    }

    public static RoleAssignmentDeleteByUserResponse ToDeleteByUserResponse()
    {
        return new RoleAssignmentDeleteByUserResponse
        {
            IsDeleted = true,
        };
    }

    public static RoleAssignmentLastAdminListRequest ToLastAdminListRequest(Guid userId)
    {
        return new RoleAssignmentLastAdminListRequest
        {
            UserId = userId,
        };
    }

    public static RoleAssignmentDeleteByUserRequest ToDeleteByUserRequest(Guid userId)
    {
        return new RoleAssignmentDeleteByUserRequest
        {
            UserId = userId,
        };
    }
}
