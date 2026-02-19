using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class UpTimesheetTaskSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "closed_by_document_reference",
                table: "timesheet_task_spans",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "opened_by_document_reference",
                table: "timesheet_task_spans",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "closed_by_document_reference",
                table: "timesheet_task_spans");

            migrationBuilder.DropColumn(
                name: "opened_by_document_reference",
                table: "timesheet_task_spans");
        }
    }
}
