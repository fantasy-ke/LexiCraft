using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LexiCraft.Infrastructure.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file-infos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FullPath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDirectory = table.Column<bool>(type: "boolean", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreateByName = table.Column<string>(type: "text", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdateById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file-infos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file-infos_file-infos_ParentId",
                        column: x => x.ParentId,
                        principalTable: "file-infos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "login-log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: true),
                    Token = table.Column<string>(type: "text", nullable: true),
                    LoginTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    LoginType = table.Column<string>(type: "text", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login-log", x => x.Id);
                });

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
                    CreateByName = table.Column<string>(type: "text", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    CreateByName = table.Column<string>(type: "text", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    UserAccount = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "用户名"),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "昵称"),
                    Email = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "邮箱"),
                    Signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "个性签名"),
                    PasswordHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "密码哈希值"),
                    PasswordSalt = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false, comment: "注册来源"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    SettingsId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreateByName = table.Column<string>(type: "text", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateById = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateByName = table.Column<string>(type: "text", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdateById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteByName = table.Column<string>(type: "text", nullable: true),
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
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Avatar", "CreateAt", "CreateById", "CreateByName", "DeleteAt", "DeleteById", "DeleteByName", "Email", "IsDeleted", "LastLoginAt", "PasswordHash", "PasswordSalt", "Phone", "Roles", "SettingsId", "Signature", "Source", "UpdateAt", "UpdateById", "UpdateByName", "UserAccount", "Username" },
                values: new object[] { new Guid("f68f03a9-cf65-4847-acde-ab32bbb6ec91"), "🦜", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, null, null, "one@fatnasyke.fun", false, new DateTime(2025, 5, 2, 3, 22, 58, 74, DateTimeKind.Utc).AddTicks(4840), "634F36CBBAE253523EEE000F2443A0A3", "F1SFic8gFEyhIkpYqNRgSg==", null, new List<string> { "Admin" }, null, null, 1, null, null, null, "admin", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_file-infos_ParentId",
                table: "file-infos",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_login-log_UserId",
                table: "login-log",
                column: "UserId");

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
                name: "file-infos");

            migrationBuilder.DropTable(
                name: "login-log");

            migrationBuilder.DropTable(
                name: "user-oauth");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "user-settings");
        }
    }
}
