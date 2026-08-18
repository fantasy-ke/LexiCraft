using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fantasy.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "login_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "text", nullable: true),
                    token = table.Column<string>(type: "text", nullable: true),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    origin = table.Column<string>(type: "text", nullable: true),
                    ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    login_type = table.Column<string>(type: "text", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "登录失败次数"),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "锁定结束时间"),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "是否启用锁定"),
                    avatar = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "头像"),
                    user_account = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "用户名"),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "昵称"),
                    email = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "邮箱"),
                    signature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "个性签名"),
                    password_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "密码哈希值"),
                    source = table.Column<int>(type: "integer", nullable: false, comment: "注册来源"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_by_name = table.Column<string>(type: "text", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_o_auths",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false, comment: "OAuth 提供者"),
                    provider_user_id = table.Column<string>(type: "text", nullable: false, comment: "OAuth 提供者用户 ID"),
                    access_token = table.Column<string>(type: "text", nullable: false),
                    access_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_o_auths", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_o_auths_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_name = table.Column<string>(type: "text", nullable: false),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: false),
                    is_profile_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "个人资料是否公开"),
                    show_learning_progress = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "显示学习进度"),
                    allow_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "允许消息"),
                    receive_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收通知"),
                    receive_email_updates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收邮件更新"),
                    receive_push_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "接收推送通知"),
                    account_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "账户是否激活"),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_login_logs_user_id",
                table: "login_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_o_auths_provider_provider_user_id",
                table: "user_o_auths",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_o_auths_provider_provider_user_id_user_id",
                table: "user_o_auths",
                columns: new[] { "provider", "provider_user_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_o_auths_user_id",
                table: "user_o_auths",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "login_logs");

            migrationBuilder.DropTable(
                name: "user_o_auths");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
