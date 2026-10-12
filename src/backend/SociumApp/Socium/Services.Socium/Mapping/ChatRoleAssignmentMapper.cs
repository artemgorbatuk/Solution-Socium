using Datasource.Socium.Ef.Models;

namespace Services.Socium.Mapping;

public static class ChatRoleAssignmentMapper
{
    public static void Apply(ChatRoleAssignment model, Guid userId, Guid chatId, string roleName)
    {
        model.UserId = userId;
        model.ChatId = chatId;
        model.RoleName = roleName;
    }
}
