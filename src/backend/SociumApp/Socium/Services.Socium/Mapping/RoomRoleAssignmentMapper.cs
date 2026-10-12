using Datasource.Socium.Ef.Models;
using Services.Socium.Models;

namespace Services.Socium.Mapping;

public static class RoomRoleAssignmentMapper
{
    public static void Apply(RoomRoleAssignment model, Guid userId, Guid roomId, string roleName)
    {
        model.UserId = userId;
        model.RoomId = roomId;
        model.RoleName = roleName;
    }

    public static RoomAssignRoomAdminStageRequest ToAssignRoomAdminStageRequest(Guid roomId, Guid userId)
    {
        return new RoomAssignRoomAdminStageRequest
        {
            RoomId = roomId,
            UserId = userId,
        };
    }
}
