using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiCraft.Services.Identity.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class addVerison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "users");
        }
    }
}
