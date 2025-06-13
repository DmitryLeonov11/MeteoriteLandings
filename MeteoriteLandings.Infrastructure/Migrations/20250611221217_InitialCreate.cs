using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteoriteLandings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeteoriteLandings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameType = table.Column<string>(type: "text", nullable: false),
                    RecClass = table.Column<string>(type: "text", nullable: false),
                    Mass = table.Column<long>(type: "bigint", nullable: true),
                    Fall = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Reclat = table.Column<double>(type: "double precision", nullable: true),
                    Reclong = table.Column<double>(type: "double precision", nullable: true),
                    GeoLocation = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeteoriteLandings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeteoriteLandings_ExternalId",
                table: "MeteoriteLandings",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeteoriteLandings");
        }
    }
}
