using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user-oauth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false, comment: "OAuth 提供者"),
                    ProviderUserId = table.Column<string>(type: "text", nullable: false, comment: "OAuth 提供者用户 ID"),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    CreateByName = table.Column<string>(type: "text", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user-oauth", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user-settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    Birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: false),
                    IsProfilePublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "个人资料是否公开"),
                    ShowLearningProgress = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "显示学习进度"),
                    AllowMessages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "允许消息"),
                    ReceiveNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收通知"),
                    ReceiveEmailUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收邮件更新"),
                    ReceivePushNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收推送通知"),
                    AccountActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "账户是否激活"),
                    CreateByName = table.Column<string>(type: "text", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user-settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Avatar = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "头像"),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "用户名"),
                    Email = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "邮箱"),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "个性签名"),
                    PasswordHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "密码哈希值"),
                    PasswordSalt = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false, comment: "注册来源"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    SettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreateByName = table.Column<string>(type: "text", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteByName = table.Column<string>(type: "text", nullable: false),
                    DeleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_user-settings_SettingsId",
                        column: x => x.SettingsId,
                        principalTable: "user-settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user-oauth_Provider_ProviderUserId",
                table: "user-oauth",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user-oauth_Provider_ProviderUserId_UserId",
                table: "user-oauth",
                columns: new[] { "Provider", "ProviderUserId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_user-oauth_UserId",
                table: "user-oauth",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user-settings_UserId",
                table: "user-settings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_SettingsId",
                table: "users",
                column: "SettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user-oauth");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "user-settings");
        }
    }
}
