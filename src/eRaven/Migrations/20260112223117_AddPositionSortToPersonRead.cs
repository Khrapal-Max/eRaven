using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionSortToPersonRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position_sort",
                table: "person_read",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_read_kind_possort_last",
                table: "person_read",
                columns: new[] { "enrollment_kind", "position_sort", "last_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_person_read_kind_possort_last",
                table: "person_read");

            migrationBuilder.DropColumn(
                name: "position_sort",
                table: "person_read");
        }
    }
}
