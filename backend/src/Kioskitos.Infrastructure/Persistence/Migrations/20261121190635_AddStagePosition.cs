using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kioskitos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStagePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "stages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "stages");
        }
    }
}
