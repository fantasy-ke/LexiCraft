using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "roles",
                table: "users",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.Sql(
                """
                UPDATE users
                SET roles = CASE
                    WHEN lower(user_account) = 'admin' THEN ARRAY['admin']::text[]
                    ELSE ARRAY['user']::text[]
                END
                WHERE cardinality(roles) = 0;
                """);

            migrationBuilder.AlterColumn<List<string>>(
                name: "roles",
                table: "users",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]",
                oldDefaultValueSql: "ARRAY[]::text[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "roles",
                table: "users");
        }
    }
}
