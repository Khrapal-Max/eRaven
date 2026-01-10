using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlannedPositionUnitId",
                table: "person_read",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PositionUnitId",
                table: "person_read",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TemporaryPositionUnitId",
                table: "person_read",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedPositionUnitId",
                table: "person_read");

            migrationBuilder.DropColumn(
                name: "PositionUnitId",
                table: "person_read");

            migrationBuilder.DropColumn(
                name: "TemporaryPositionUnitId",
                table: "person_read");
        }
    }
}
