using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Datasource.Socium.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Task0015 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MessageDelivered",
                table: "Recipients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MessageRead",
                table: "Recipients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MessageSent",
                table: "Recipients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql(
                """
                UPDATE "Recipients" AS r
                SET "MessageSent" = m."CreatedAt"
                FROM "Messages" AS m
                WHERE r."MessageId" = m."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Recipients_UserId_MessageRead",
                table: "Recipients",
                columns: new[] { "UserId", "MessageRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipients_UserId_MessageRead",
                table: "Recipients");

            migrationBuilder.DropColumn(
                name: "MessageDelivered",
                table: "Recipients");

            migrationBuilder.DropColumn(
                name: "MessageRead",
                table: "Recipients");

            migrationBuilder.DropColumn(
                name: "MessageSent",
                table: "Recipients");
        }
    }
}
