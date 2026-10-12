using Services.Security.Permissions.Catalogs;
using Services.Security.Permissions.Enums;
using Services.Security.Permissions.Models;
using Services.Security.Permissions.Providers;

namespace Tests.Units.Security;

public sealed class PermissionProviderTests
{
    private readonly PermissionProvider provider = new();

    [Fact]
    public void HasPermission_AppAdmin_CanCreateRoom()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.True(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.RoomCreate, context));
    }

    [Fact]
    public void HasPermission_RoomAdmin_CanCreateChat_ButNotCreateRoom()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.True(provider.HasPermission([RoleNames.RoomAdmin], PermissionNames.ChatCreate, context));
        Assert.False(provider.HasPermission([RoleNames.RoomAdmin], PermissionNames.RoomCreate, context));
    }

    [Fact]
    public void HasPermission_ChatAdmin_DoesNotIncludeMessageRead()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.False(provider.HasPermission([RoleNames.ChatAdmin], PermissionNames.MessageRead, context));
        Assert.True(provider.HasPermission([RoleNames.ChatAdmin], PermissionNames.ChatUpdate, context));
    }

    [Fact]
    public void HasPermission_ChatParticipant_MessageUpdate_OnlyWhenSender()
    {
        var subjectId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();

        Assert.True(provider.HasPermission(
            [RoleNames.ChatParticipant],
            PermissionNames.MessageUpdate,
            new PermissionContext { SubjectId = subjectId, SenderId = subjectId }));

        Assert.False(provider.HasPermission(
            [RoleNames.ChatParticipant],
            PermissionNames.MessageUpdate,
            new PermissionContext { SubjectId = subjectId, SenderId = otherId }));

        Assert.False(provider.HasPermission(
            [RoleNames.ChatParticipant],
            PermissionNames.MessageUpdate,
            new PermissionContext { SubjectId = subjectId, SenderId = null }));
    }

    [Fact]
    public void HasPermission_ChatAdminAndParticipant_CanReadMessages()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.True(provider.HasPermission(
            [RoleNames.ChatAdmin, RoleNames.ChatParticipant],
            PermissionNames.MessageRead,
            context));
    }

    [Fact]
    public void GetGrants_UnknownRole_IsIgnored()
    {
        var grants = provider.GetGrants(["Unknown.Role", RoleNames.AppAdmin]);

        Assert.Contains(grants, grant => grant.Permission == PermissionNames.RoomCreate);
        Assert.DoesNotContain(grants, grant => grant.Permission == "Unknown.Role");
    }

    [Fact]
    public void HasPermission_AppAdmin_CanReadAndManageUsersAndAppAdmins()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.True(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.UserRead, context));
        Assert.True(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.UserAccessManage, context));
        Assert.True(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.AppAdminGrant, context));
        Assert.True(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.AppAdminRevoke, context));
    }

    [Fact]
    public void HasPermission_AppAdmin_DoesNotGetChatRoleGrants()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.False(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.ChatParticipantGrant, context));
        Assert.False(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.ChatAdminGrant, context));
        Assert.False(provider.HasPermission([RoleNames.AppAdmin], PermissionNames.MessageRead, context));
    }

    [Fact]
    public void HasPermission_RoomAndChatRoles_CannotReadUsers()
    {
        var context = new PermissionContext { SubjectId = Guid.CreateVersion7() };

        Assert.False(provider.HasPermission(
            [RoleNames.RoomAdmin, RoleNames.ChatAdmin, RoleNames.ChatParticipant],
            PermissionNames.UserRead,
            context));
        Assert.False(provider.HasPermission([RoleNames.RoomAdmin], PermissionNames.UserAccessManage, context));
    }

    [Fact]
    public void RoleCatalog_ChatAdmin_ScopeIsChat_AndHasNoIncludesStyleMessageGrants()
    {
        Assert.True(RoleCatalog.TryGet(RoleNames.ChatAdmin, out var chatAdmin));
        Assert.NotNull(chatAdmin);
        Assert.Equal(RoleScope.Chat, chatAdmin!.Scope);
        Assert.DoesNotContain(chatAdmin.Grants, grant => grant.Permission == PermissionNames.MessageCreate);
    }
}
