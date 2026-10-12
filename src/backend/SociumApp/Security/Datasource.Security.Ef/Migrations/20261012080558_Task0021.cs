using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datasource.Security.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Task0021 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoleAssignments_UserId",
                table: "AppRoleAssignments",
                column: "UserId")
                .Annotation("Npgsql:IndexInclude", new[] { "RoleName" });

            migrationBuilder.CreateIndex(
                name: "UX_AppRoleAssignments_UserId_RoleName",
                table: "AppRoleAssignments",
                columns: new[] { "UserId", "RoleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppRoleAssignments");
        }
    }
}
