using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LexiCraft.Services.Vocabulary.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_word_states",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "用户 ID"),
                    word_id = table.Column<long>(type: "bigint", nullable: false, comment: "单词 ID"),
                    state = table.Column<int>(type: "integer", nullable: false, comment: "掌握状态 (0:未学, 1:模糊, 2:掌握)"),
                    is_in_word_book = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "是否在生词本中"),
                    mastery_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "掌握程度评分"),
                    last_reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_word_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "word_list_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    word_list_id = table.Column<long>(type: "bigint", nullable: false),
                    word_id = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_list_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "word_lists",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "词库名称"),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "分类"),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "描述"),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_word_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "words",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    spelling = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "单词拼写"),
                    phonetic = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "音标"),
                    pronunciation_url = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "发音 URL"),
                    definitions = table.Column<string>(type: "jsonb", nullable: true, comment: "释义 (JSON)"),
                    examples = table.Column<string>(type: "jsonb", nullable: true, comment: "例句 (JSON)"),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    create_by_name = table.Column<string>(type: "text", nullable: true),
                    create_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    update_by_name = table.Column<string>(type: "text", nullable: true),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_words", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_word_states_user_id",
                table: "user_word_states",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_word_states_user_id_word_id",
                table: "user_word_states",
                columns: new[] { "user_id", "word_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_word_list_items_word_list_id_word_id",
                table: "word_list_items",
                columns: new[] { "word_list_id", "word_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_words_spelling",
                table: "words",
                column: "spelling");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_word_states");

            migrationBuilder.DropTable(
                name: "word_list_items");

            migrationBuilder.DropTable(
                name: "word_lists");

            migrationBuilder.DropTable(
                name: "words");
        }
    }
}
