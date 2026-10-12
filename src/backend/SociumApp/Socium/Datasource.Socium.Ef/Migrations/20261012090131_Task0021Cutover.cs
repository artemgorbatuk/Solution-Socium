using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datasource.Socium.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Task0021Cutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-migration до drop IsAdmin (task-0021 п. 4.5 / TT19).
            migrationBuilder.Sql(
                """
                INSERT INTO "ChatRoleAssignments" ("Id", "UserId", "ChatId", "RoleName")
                SELECT gen_random_uuid(), p."UserId", p."ChatId", 'Chat.Participant'
                FROM "Participants" AS p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ChatRoleAssignments" AS a
                    WHERE a."UserId" = p."UserId"
                      AND a."ChatId" = p."ChatId"
                      AND a."RoleName" = 'Chat.Participant');

                INSERT INTO "ChatRoleAssignments" ("Id", "UserId", "ChatId", "RoleName")
                SELECT gen_random_uuid(), p."UserId", p."ChatId", 'Chat.Admin'
                FROM "Participants" AS p
                WHERE p."IsAdmin" = TRUE
                  AND NOT EXISTS (
                    SELECT 1
                    FROM "ChatRoleAssignments" AS a
                    WHERE a."UserId" = p."UserId"
                      AND a."ChatId" = p."ChatId"
                      AND a."RoleName" = 'Chat.Admin');

                -- Repair: в чате с участниками должен быть хотя бы один Chat.Admin.
                INSERT INTO "ChatRoleAssignments" ("Id", "UserId", "ChatId", "RoleName")
                SELECT gen_random_uuid(), first_p."UserId", first_p."ChatId", 'Chat.Admin'
                FROM (
                    SELECT DISTINCT ON (p."ChatId") p."ChatId", p."UserId"
                    FROM "Participants" AS p
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "ChatRoleAssignments" AS a
                        WHERE a."ChatId" = p."ChatId"
                          AND a."RoleName" = 'Chat.Admin')
                    ORDER BY p."ChatId", p."Id"
                ) AS first_p;

                -- RoomAdmin: для комнат без Room.Admin — первый Chat.Admin в комнате.
                INSERT INTO "RoomRoleAssignments" ("Id", "UserId", "RoomId", "RoleName")
                SELECT gen_random_uuid(), first_admin."UserId", first_admin."RoomId", 'Room.Admin'
                FROM (
                    SELECT DISTINCT ON (c."RoomId") c."RoomId", a."UserId"
                    FROM "ChatRoleAssignments" AS a
                    INNER JOIN "Chats" AS c ON c."Id" = a."ChatId"
                    WHERE a."RoleName" = 'Chat.Admin'
                    ORDER BY c."RoomId", a."Id"
                ) AS first_admin
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "RoomRoleAssignments" AS r
                    WHERE r."RoomId" = first_admin."RoomId"
                      AND r."RoleName" = 'Room.Admin');
                """);

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Participants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "Participants" AS p
                SET "IsAdmin" = EXISTS (
                    SELECT 1
                    FROM "ChatRoleAssignments" AS a
                    WHERE a."ChatId" = p."ChatId"
                      AND a."UserId" = p."UserId"
                      AND a."RoleName" = 'Chat.Admin');
                """);
        }
    }
}
