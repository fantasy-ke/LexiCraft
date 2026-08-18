using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fantasy.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAuthorizationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_permissions_user_id",
                table: "user_permissions");

            migrationBuilder.AlterColumn<string>(
                name: "permission_name",
                table: "user_permissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(
                """
                DELETE FROM user_permissions current_permission
                USING user_permissions duplicate_permission
                WHERE current_permission.user_id = duplicate_permission.user_id
                  AND current_permission.permission_name = duplicate_permission.permission_name
                  AND current_permission.id > duplicate_permission.id;
                """);

            // Permission checks are exact after this migration. Users with the legacy root permission
            // previously inherited every child permission. Backfill only the normal-user permission set.
            migrationBuilder.Sql(
                """
                INSERT INTO user_permissions (user_id, permission_name, create_by_name, create_at)
                SELECT root_permission.user_id, default_permission.permission_name,
                       'authorization-migration', NOW()
                FROM user_permissions root_permission
                CROSS JOIN (VALUES
                    ('Pages.Identity'),
                    ('Pages.Identity.Users'),
                    ('Pages.Identity.Users.Query'),
                    ('Pages.Identity.Users.UploadAvatar'),
                    ('Pages.Practice'),
                    ('Pages.Practice.Tasks'),
                    ('Pages.Practice.Tasks.Create'),
                    ('Pages.Practice.Tasks.Complete'),
                    ('Pages.Practice.Assessments'),
                    ('Pages.Practice.Assessments.Submit'),
                    ('Pages.Vocabulary'),
                    ('Pages.Vocabulary.Words'),
                    ('Pages.Vocabulary.Words.Query'),
                    ('Pages.Vocabulary.WordLists'),
                    ('Pages.Vocabulary.WordLists.Query'),
                    ('Pages.Vocabulary.UserStates'),
                    ('Pages.Vocabulary.UserStates.Query'),
                    ('Pages.Vocabulary.UserStates.Update'),
                    ('Pages.Files'),
                    ('Pages.Files.Items'),
                    ('Pages.Files.Items.Query'),
                    ('Pages.Files.Items.ReadContent')
                ) AS default_permission(permission_name)
                WHERE root_permission.permission_name = 'Pages'
                  AND NOT EXISTS (
                    SELECT 1
                    FROM user_permissions existing_permission
                    WHERE existing_permission.user_id = root_permission.user_id
                      AND existing_permission.permission_name = default_permission.permission_name
                );
                """);

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id_permission_name",
                table: "user_permissions",
                columns: new[] { "user_id", "permission_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_permissions_user_id_permission_name",
                table: "user_permissions");

            migrationBuilder.AlterColumn<string>(
                name: "permission_name",
                table: "user_permissions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id",
                table: "user_permissions",
                column: "user_id");
        }
    }
}
