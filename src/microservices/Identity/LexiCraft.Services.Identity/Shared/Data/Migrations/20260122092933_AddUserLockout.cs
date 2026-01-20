using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "lockout_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_failed_count",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_enabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_end",
                table: "users");
        }
    }
}
