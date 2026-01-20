using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eRaven.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monthly_timesheets",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_timesheets", x => new { x.PersonId, x.Year, x.Month });
                });

            migrationBuilder.CreateTable(
                name: "Timesheet_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lane = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    from = table.Column<DateOnly>(type: "date", nullable: false),
                    to = table.Column<DateOnly>(type: "date", nullable: true),
                    reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheet_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "monthly_timesheet_days",
                columns: table => new
                {
                    day = table.Column<int>(type: "integer", nullable: false),
                    Lane = table.Column<int>(type: "integer", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_timesheet_days", x => new { x.PersonId, x.Year, x.Month, x.day, x.Lane });
                    table.ForeignKey(
                        name: "FK_monthly_timesheet_days_monthly_timesheets_PersonId_Year_Mon~",
                        columns: x => new { x.PersonId, x.Year, x.Month },
                        principalTable: "monthly_timesheets",
                        principalColumns: new[] { "PersonId", "Year", "Month" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_timesheet_days_PersonId_Year_Month",
                table: "monthly_timesheet_days",
                columns: new[] { "PersonId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_entry_person_deleted",
                table: "Timesheet_entries",
                columns: new[] { "person_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_entry_person_lane_from",
                table: "Timesheet_entries",
                columns: new[] { "person_id", "lane", "from" });

            migrationBuilder.CreateIndex(
                name: "ix_ts_entry_person_lane_to",
                table: "Timesheet_entries",
                columns: new[] { "person_id", "lane", "to" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monthly_timesheet_days");

            migrationBuilder.DropTable(
                name: "Timesheet_entries");

            migrationBuilder.DropTable(
                name: "monthly_timesheets");
        }
    }
}
