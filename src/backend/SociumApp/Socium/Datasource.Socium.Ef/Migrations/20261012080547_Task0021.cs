using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datasource.Socium.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Task0021 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatRoleAssignments_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomRoleAssignments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoleAssignments_ChatId",
                table: "ChatRoleAssignments",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoleAssignments_UserId_ChatId",
                table: "ChatRoleAssignments",
                columns: new[] { "UserId", "ChatId" })
                .Annotation("Npgsql:IndexInclude", new[] { "RoleName" });

            migrationBuilder.CreateIndex(
                name: "UX_ChatRoleAssignments_UserId_ChatId_RoleName",
                table: "ChatRoleAssignments",
                columns: new[] { "UserId", "ChatId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomRoleAssignments_RoomId",
                table: "RoomRoleAssignments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomRoleAssignments_UserId_RoomId",
                table: "RoomRoleAssignments",
                columns: new[] { "UserId", "RoomId" })
                .Annotation("Npgsql:IndexInclude", new[] { "RoleName" });

            migrationBuilder.CreateIndex(
                name: "UX_RoomRoleAssignments_UserId_RoomId_RoleName",
                table: "RoomRoleAssignments",
                columns: new[] { "UserId", "RoomId", "RoleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatRoleAssignments");

            migrationBuilder.DropTable(
                name: "RoomRoleAssignments");
        }
    }
}
