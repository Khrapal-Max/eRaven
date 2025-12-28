using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "position_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    short_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    full_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    special_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_position_units", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_position_units_code",
                table: "position_units",
                column: "code");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_number",
                table: "position_units",
                column: "special_number");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_short_name",
                table: "position_units",
                column: "short_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "position_units");
        }
    }
}
