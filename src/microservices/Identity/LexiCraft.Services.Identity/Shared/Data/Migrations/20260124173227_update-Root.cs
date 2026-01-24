using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_user_settings_settings_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_settings_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "settings_id",
                table: "users");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "user_settings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_o_auths_user_id",
                table: "user_o_auths",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_o_auths_users_user_id",
                table: "user_o_auths",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_permissions_users_user_id",
                table: "user_permissions",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_settings_users_user_id",
                table: "user_settings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_o_auths_users_user_id",
                table: "user_o_auths");

            migrationBuilder.DropForeignKey(
                name: "fk_user_permissions_users_user_id",
                table: "user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_user_settings_users_user_id",
                table: "user_settings");

            migrationBuilder.DropIndex(
                name: "ix_user_settings_user_id",
                table: "user_settings");

            migrationBuilder.DropIndex(
                name: "ix_user_permissions_user_id",
                table: "user_permissions");

            migrationBuilder.DropIndex(
                name: "ix_user_o_auths_user_id",
                table: "user_o_auths");

            migrationBuilder.AddColumn<Guid>(
                name: "settings_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                table: "user_settings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_users_settings_id",
                table: "users",
                column: "settings_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_user_settings_settings_id",
                table: "users",
                column: "settings_id",
                principalTable: "user_settings",
                principalColumn: "id");
        }
    }
}
