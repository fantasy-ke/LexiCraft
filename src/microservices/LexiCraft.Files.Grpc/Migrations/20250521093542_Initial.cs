using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Files.Grpc.Migrations
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FullPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsDirectory = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UploadTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastAccessTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreateByName = table.Column<string>(type: "TEXT", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreateById = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdateByName = table.Column<string>(type: "TEXT", nullable: true),
                    UpdateAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdateById = table.Column<Guid>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_file-infos_ParentId",
                table: "file-infos",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file-infos");
        }
    }
}
