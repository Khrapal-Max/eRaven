using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePositionUnit2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_position_units_code",
                table: "position_units");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_code",
                table: "position_units",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_position_units_code",
                table: "position_units");

            migrationBuilder.CreateIndex(
                name: "ix_position_units_code",
                table: "position_units",
                column: "code");
        }
    }
}
