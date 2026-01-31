using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLoginLogErrorMessageToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "login_logs",
                newName: "message");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "昵称",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "user_account",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                comment: "用户名",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "source",
                table: "users",
                type: "integer",
                nullable: false,
                comment: "注册来源",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "signature",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "个性签名",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                comment: "密码哈希值",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "lockout_end",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "锁定结束时间",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "lockout_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "是否启用锁定",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "邮箱",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "avatar",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                comment: "头像",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "access_failed_count",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "登录失败次数",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "show_learning_progress",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "显示学习进度",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_push_notifications",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "接收推送通知",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_notifications",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "接收通知",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_email_updates",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "接收邮件更新",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_profile_public",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "个人资料是否公开",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "allow_messages",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "允许消息",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "account_active",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "账户是否激活",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "provider_user_id",
                table: "user_o_auths",
                type: "text",
                nullable: false,
                comment: "OAuth 提供者用户 ID",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "user_o_auths",
                type: "text",
                nullable: false,
                comment: "OAuth 提供者",
                oldClrType: typeof(string),
                oldType: "text");

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
                name: "ix_login_logs_user_id",
                table: "login_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_user_o_auths_provider_provider_user_id",
                table: "user_o_auths");

            migrationBuilder.DropIndex(
                name: "ix_user_o_auths_provider_provider_user_id_user_id",
                table: "user_o_auths");

            migrationBuilder.DropIndex(
                name: "ix_login_logs_user_id",
                table: "login_logs");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "login_logs",
                newName: "error_message");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "昵称");

            migrationBuilder.AlterColumn<string>(
                name: "user_account",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "用户名");

            migrationBuilder.AlterColumn<int>(
                name: "source",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "注册来源");

            migrationBuilder.AlterColumn<string>(
                name: "signature",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "个性签名");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "密码哈希值");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "lockout_end",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "锁定结束时间");

            migrationBuilder.AlterColumn<bool>(
                name: "lockout_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "是否启用锁定");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "邮箱");

            migrationBuilder.AlterColumn<string>(
                name: "avatar",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldComment: "头像");

            migrationBuilder.AlterColumn<int>(
                name: "access_failed_count",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "登录失败次数");

            migrationBuilder.AlterColumn<bool>(
                name: "show_learning_progress",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "显示学习进度");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_push_notifications",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "接收推送通知");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_notifications",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "接收通知");

            migrationBuilder.AlterColumn<bool>(
                name: "receive_email_updates",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "接收邮件更新");

            migrationBuilder.AlterColumn<bool>(
                name: "is_profile_public",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "个人资料是否公开");

            migrationBuilder.AlterColumn<bool>(
                name: "allow_messages",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "允许消息");

            migrationBuilder.AlterColumn<bool>(
                name: "account_active",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "账户是否激活");

            migrationBuilder.AlterColumn<string>(
                name: "provider_user_id",
                table: "user_o_auths",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "OAuth 提供者用户 ID");

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "user_o_auths",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "OAuth 提供者");
        }
    }
}
