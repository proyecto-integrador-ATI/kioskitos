using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kioskitos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientContactPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "client_contacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing contacts have no recorded order; number them per client so each one gets a
            // distinct, stable position (the id is the only deterministic key available).
            migrationBuilder.Sql("""
                UPDATE client_contacts AS cc
                SET "Position" = numbered.position
                FROM (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "ClientId" ORDER BY "Id") - 1 AS position
                    FROM client_contacts
                ) AS numbered
                WHERE cc."Id" = numbered."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "client_contacts");
        }
    }
}
