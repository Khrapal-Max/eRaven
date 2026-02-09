using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_mission_id_from_date_to_date",
                table: "mission_assignments");

            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments");

            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_person_id_from_date_to_date",
                table: "mission_assignments");

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "mission_assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "sourge_document",
                table: "combat_tasks",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_mission_id_status_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "mission_id", "status", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments",
                column: "person_id",
                unique: true,
                filter: "to_date IS NULL AND status IN (0,1)");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id_status_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "person_id", "status", "from_date", "to_date" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_mission_assignments_status",
                table: "mission_assignments",
                sql: "status IN (0,1,2,3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_mission_id_status_from_date_to_date",
                table: "mission_assignments");

            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments");

            migrationBuilder.DropIndex(
                name: "IX_mission_assignments_person_id_status_from_date_to_date",
                table: "mission_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_mission_assignments_status",
                table: "mission_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "mission_assignments");

            migrationBuilder.DropColumn(
                name: "sourge_document",
                table: "combat_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_mission_id_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "mission_id", "from_date", "to_date" });

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id",
                table: "mission_assignments",
                column: "person_id",
                unique: true,
                filter: "to_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_mission_assignments_person_id_from_date_to_date",
                table: "mission_assignments",
                columns: new[] { "person_id", "from_date", "to_date" });
        }
    }
}
