using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class UpTimesheetCodeDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "end_date_meaning",
                table: "timesheet_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_planning_cutoff",
                table: "timesheet_codes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "next_code_on_end",
                table: "timesheet_codes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "planning_cutoff_shift_days",
                table: "timesheet_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_date_meaning",
                table: "timesheet_codes");

            migrationBuilder.DropColumn(
                name: "is_planning_cutoff",
                table: "timesheet_codes");

            migrationBuilder.DropColumn(
                name: "next_code_on_end",
                table: "timesheet_codes");

            migrationBuilder.DropColumn(
                name: "planning_cutoff_shift_days",
                table: "timesheet_codes");
        }
    }
}
