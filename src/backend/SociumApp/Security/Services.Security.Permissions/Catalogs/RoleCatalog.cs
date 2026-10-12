using Services.Security.Permissions.Enums;
using Services.Security.Permissions.Models;

namespace Services.Security.Permissions.Catalogs;

/// <summary>
/// Таблица 2 task-0021: роль → область и выдачи. Includes в первом срезе не используется.
/// </summary>
public static class RoleCatalog
{
    private static readonly IReadOnlyDictionary<string, RoleDefinition> RolesByName = Build();

    public static IReadOnlyList<RoleDefinition> All { get; } = RolesByName.Values.ToList();

    public static bool TryGet(string roleName, out RoleDefinition? definition) =>
        RolesByName.TryGetValue(roleName, out definition);

    private static IReadOnlyDictionary<string, RoleDefinition> Build()
    {
        RoleGrant Grant(string permission, PermissionCondition condition = PermissionCondition.None) =>
            new() { Permission = permission, Condition = condition };

        var roles = new Dictionary<string, RoleDefinition>(StringComparer.Ordinal)
        {
            [RoleNames.AppAdmin] = new RoleDefinition
            {
                Name = RoleNames.AppAdmin,
                Scope = RoleScope.App,
                Grants =
                [
                    Grant(PermissionNames.RoomCreate),
                    Grant(PermissionNames.RoomRead),
                    Grant(PermissionNames.RoomAdminGrant),
                    Grant(PermissionNames.RoomAdminRevoke),
                    Grant(PermissionNames.UserRead),
                    Grant(PermissionNames.UserAccessManage),
                    Grant(PermissionNames.AppAdminGrant),
                    Grant(PermissionNames.AppAdminRevoke)
                ]
            },
            [RoleNames.RoomAdmin] = new RoleDefinition
            {
                Name = RoleNames.RoomAdmin,
                Scope = RoleScope.Room,
                Grants =
                [
                    Grant(PermissionNames.RoomRead),
                    Grant(PermissionNames.RoomUpdate),
                    Grant(PermissionNames.RoomDelete),
                    Grant(PermissionNames.ChatCreate),
                    Grant(PermissionNames.ChatRead),
                    Grant(PermissionNames.ChatAdminGrant),
                    Grant(PermissionNames.ChatAdminRevoke)
                ]
            },
            [RoleNames.ChatAdmin] = new RoleDefinition
            {
                Name = RoleNames.ChatAdmin,
                Scope = RoleScope.Chat,
                Grants =
                [
                    Grant(PermissionNames.RoomRead),
                    Grant(PermissionNames.ChatRead),
                    Grant(PermissionNames.ChatUpdate),
                    Grant(PermissionNames.ChatDelete),
                    Grant(PermissionNames.ChatAdminGrant),
                    Grant(PermissionNames.ChatAdminRevoke),
                    Grant(PermissionNames.ChatParticipantGrant),
                    Grant(PermissionNames.ChatParticipantRevoke)
                ]
            },
            [RoleNames.ChatParticipant] = new RoleDefinition
            {
                Name = RoleNames.ChatParticipant,
                Scope = RoleScope.Chat,
                Grants =
                [
                    Grant(PermissionNames.RoomRead),
                    Grant(PermissionNames.ChatRead),
                    Grant(PermissionNames.ParticipantRead),
                    Grant(PermissionNames.MessageCreate),
                    Grant(PermissionNames.MessageRead),
                    Grant(PermissionNames.MessageUpdate, PermissionCondition.Sender),
                    Grant(PermissionNames.MessageDelete, PermissionCondition.Sender)
                ]
            }
        };

        return roles;
    }
}
