using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
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
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_o_auths",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_user_id = table.Column<string>(type: "text", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    gender = table.Column<string>(type: "text", nullable: false),
                    birthday = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bio = table.Column<string>(type: "text", nullable: false),
                    is_profile_public = table.Column<bool>(type: "boolean", nullable: false),
                    show_learning_progress = table.Column<bool>(type: "boolean", nullable: false),
                    allow_messages = table.Column<bool>(type: "boolean", nullable: false),
                    receive_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    receive_email_updates = table.Column<bool>(type: "boolean", nullable: false),
                    receive_push_notifications = table.Column<bool>(type: "boolean", nullable: false),
                    account_active = table.Column<bool>(type: "boolean", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    avatar = table.Column<string>(type: "text", nullable: false),
                    user_account = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    signature = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    password_salt = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    roles = table.Column<List<string>>(type: "text[]", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    settings_id = table.Column<Guid>(type: "uuid", nullable: true),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    delete_by_name = table.Column<string>(type: "text", nullable: true),
                    delete_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_user_settings_settings_id",
                        column: x => x.settings_id,
                        principalTable: "user_settings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_settings_id",
                table: "users",
                column: "settings_id");
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
                name: "users");

            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
